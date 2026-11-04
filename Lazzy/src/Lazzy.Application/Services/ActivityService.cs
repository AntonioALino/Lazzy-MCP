using Lazzy.Application.Configuration;
using Lazzy.Application.Models;
using Lazzy.Application.Ports.Driven;
using Lazzy.Domain.ValueObjects;

namespace Lazzy.Application.Services;

/// <summary>
/// Asks every activity source about the targets of the selected projects and merges the answers into one report.
/// The report never fails as a whole: whatever cannot be read becomes a warning, and the rest is still returned.
/// </summary>
public sealed class ActivityService
{
    private readonly Dictionary<string, IActivitySource> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IdentityOptions> _identities = new(StringComparer.OrdinalIgnoreCase);

    /// <exception cref="ArgumentException">Two sources share an id.</exception>
    public ActivityService(IEnumerable<IActivitySource> sources, LazzyOptions options)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(options);

        foreach (var source in sources)
        {
            if (!_sources.TryAdd(source.Id, source))
            {
                throw new ArgumentException(
                    $"Two activity sources use the id '{source.Id}'. Give each provider its own id.",
                    nameof(sources));
            }
        }

        foreach (var (provider, identity) in options.Identity)
        {
            _identities.TryAdd(provider.Trim(), identity);
        }
    }

    /// <summary>The user's activity on every target of <paramref name="projects"/> inside <paramref name="range"/>.</summary>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
    public async Task<ActivityReport> GetActivityAsync(
        IReadOnlyList<ProjectOptions> projects,
        TimeRange range,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var warnings = new List<string>();
        var questions = new List<Question>();

        foreach (var targets in DistinctTargets(projects, warnings).GroupBy(target => target.Provider, StringComparer.OrdinalIgnoreCase))
        {
            if (!_sources.TryGetValue(targets.Key, out var source))
            {
                warnings.Add(
                    $"Skipped {List(targets)}: no provider called '{targets.Key}' is available. "
                    + $"Check the part before ':' in the target; the available providers are: {AvailableProviders()}.");
                continue;
            }

            var identity = IdentityFor(targets.Key);
            if (source.RequiresIdentity && identity?.Login is null)
            {
                warnings.Add(
                    $"Skipped {List(targets)}: Lazzy does not know who you are on '{source.Id}'. "
                    + $"Add your login under identity.{source.Id}.login in lazzy.yaml.");
                continue;
            }

            questions.AddRange(targets.Select(target => new Question(source, new ActivityQuery(target, range, identity))));
        }

        // One call per target, all at once. Each call returns its own answer, so no list is shared between threads.
        var answers = await Task.WhenAll(questions.Select(question => AskAsync(question, cancellationToken))).ConfigureAwait(false);

        var events = answers
            .SelectMany(answer => answer.Events)
            .Where(activity => range.Contains(activity.Timestamp))
            .DistinctBy(activity => (activity.Kind, activity.Url.AbsoluteUri, activity.Timestamp))
            .OrderBy(activity => activity.Timestamp)
            .ThenBy(activity => activity.Kind)
            .ThenBy(activity => activity.Url.AbsoluteUri, StringComparer.Ordinal)
            .ToList();

        warnings.AddRange(answers.Select(answer => answer.Warning).OfType<string>());

        return new ActivityReport(range, events, warnings);
    }

    private static async Task<Answer> AskAsync(Question question, CancellationToken cancellationToken)
    {
        var (source, query) = question;
        try
        {
            var events = await source.GetActivityAsync(query, cancellationToken).ConfigureAwait(false);
            return new Answer(events, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ActivitySourceException exception)
        {
            return new Answer([], $"{query.Target}: {exception.Message}");
        }
        catch (Exception exception)
        {
            // Anything else is a bug or an outage in one provider; it must not hide what the others found.
            return new Answer(
                [],
                $"{query.Target}: the provider '{source.Id}' failed unexpectedly ({exception.GetType().Name}: {exception.Message}). "
                + "The other targets were still read; if this keeps happening, please report it.");
        }
    }

    /// <summary>The valid targets of <paramref name="projects"/>, each once, in the order they were configured.</summary>
    private static List<Target> DistinctTargets(IReadOnlyList<ProjectOptions> projects, List<string> warnings)
    {
        var targets = new List<Target>();
        var seen = new HashSet<Target>();

        foreach (var value in projects.SelectMany(project => project.Targets))
        {
            if (!Target.TryParse(value, out var target))
            {
                warnings.Add($"Skipped '{value}': it is not a target. Write it as {Target.Format}.");
            }
            else if (seen.Add(target))
            {
                targets.Add(target);
            }
        }

        return targets;
    }

    private UserIdentity? IdentityFor(string provider)
    {
        if (!_identities.TryGetValue(provider, out var identity))
        {
            return null;
        }

        var login = string.IsNullOrWhiteSpace(identity.Login) ? null : identity.Login.Trim();
        return new UserIdentity(login, identity.Emails);
    }

    private string AvailableProviders() =>
        _sources.Count == 0 ? "none" : string.Join(", ", _sources.Keys.Order(StringComparer.Ordinal));

    private static string List(IEnumerable<Target> targets) => string.Join(", ", targets);

    private sealed record Question(IActivitySource Source, ActivityQuery Query);

    private sealed record Answer(IReadOnlyList<ActivityEvent> Events, string? Warning);
}
