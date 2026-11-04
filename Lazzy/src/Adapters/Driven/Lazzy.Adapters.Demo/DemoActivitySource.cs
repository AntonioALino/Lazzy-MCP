using Lazzy.Application.Ports.Driven;
using Lazzy.Domain.Services;
using Lazzy.Domain.ValueObjects;

namespace Lazzy.Adapters.Demo;

/// <summary>
/// A built-in provider that needs no token: it tells the scripted story of a small online shop,
/// placed relative to today so that "yesterday" always has something to show.
/// It plays both roles, source control and work tracking.
/// </summary>
public sealed class DemoActivitySource : ISourceControlActivitySource, IWorkTrackingActivitySource
{
    /// <summary>The provider id, the part before <c>:</c> in a demo target.</summary>
    public const string ProviderId = "demo";

    private readonly TimeProvider _clock;
    private readonly TimeZoneInfo _timeZone;
    private readonly BusinessCalendar _calendar;

    /// <param name="clock">Decides what "today" is.</param>
    /// <param name="timeZone">The user's time zone: the story happens during its office hours.</param>
    /// <param name="calendar">Decides which days are business days, so the story skips weekends and holidays.</param>
    public DemoActivitySource(TimeProvider clock, TimeZoneInfo timeZone, BusinessCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendar);

        _clock = clock;
        _timeZone = timeZone;
        _calendar = calendar;
    }

    /// <summary>The targets of the demo project: its code and its board.</summary>
    public static IReadOnlyList<Target> Targets { get; } =
    [
        new(ProviderId, DemoStory.CodeLocation),
        new(ProviderId, DemoStory.BoardLocation),
    ];

    /// <inheritdoc/>
    public string Id => ProviderId;

    /// <inheritdoc/>
    public bool RequiresIdentity => false;

    /// <inheritdoc/>
    /// <exception cref="ActivitySourceException">The location is not one of the demo's.</exception>
    public Task<IReadOnlyList<ActivityEvent>> GetActivityAsync(ActivityQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var location = query.Target.Location;
        if (!Targets.Any(target => string.Equals(target.Location, location, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ActivitySourceException(
                $"The demo has nothing at '{location}'. Use one of its targets: {string.Join(", ", Targets)}.");
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), _timeZone).DateTime);

        IReadOnlyList<ActivityEvent> events =
        [
            .. DemoStory.Steps
                .Where(step => string.Equals(step.Location, location, StringComparison.OrdinalIgnoreCase))
                .Select(step => ToEvent(step, query.Target, today))
                .Where(activity => query.Range.Contains(activity.Timestamp)),
        ];

        return Task.FromResult(events);
    }

    private ActivityEvent ToEvent(DemoStep step, Target target, DateOnly today) => new()
    {
        Kind = step.Kind,
        Timestamp = LocalTime(BusinessDaysBefore(today, step.BusinessDaysAgo), step.At),
        Title = step.Title,
        Url = new Uri(DemoStory.BaseUrl, $"{step.Location}/{step.Path}"),
        Source = ProviderId,
        Target = target,
        Reference = step.Reference,
        FromState = step.FromState,
        ToState = step.ToState,
    };

    private DateOnly BusinessDaysBefore(DateOnly today, int count)
    {
        var day = today;
        for (var step = 0; step < count; step++)
        {
            day = _calendar.PreviousBusinessDay(day);
        }

        return day;
    }

    private DateTimeOffset LocalTime(DateOnly day, TimeOnly time)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, _timeZone.GetUtcOffset(local));
    }
}
