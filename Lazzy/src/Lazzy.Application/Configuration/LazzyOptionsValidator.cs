using System.Globalization;
using Lazzy.Domain;

namespace Lazzy.Application.Configuration;

/// <summary>
/// Checks a <see cref="LazzyOptions"/> and reports every problem at once,
/// so the user can fix the whole file in one pass instead of one error per restart.
/// </summary>
public static class LazzyOptionsValidator
{
    /// <summary>The format of every date in configuration.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>All the problems in <paramref name="options"/>; empty when the configuration is valid.</summary>
    public static IReadOnlyList<ConfigurationProblem> Validate(LazzyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var problems = new List<ConfigurationProblem>();

        ValidateTimeZone(options.TimeZone, problems);
        ValidateHolidays(options.Holidays, problems);
        ValidateProjects(options.Projects, problems);

        return problems;
    }

    private static void ValidateTimeZone(string? timeZone, List<ConfigurationProblem> problems)
    {
        if (timeZone is not null && !TimeZoneInfo.TryFindSystemTimeZoneById(timeZone.Trim(), out _))
        {
            problems.Add(new(
                "timeZone",
                $"'{timeZone}' is not a known time zone. Use an IANA id such as 'America/Sao_Paulo' or 'Europe/Lisbon', or remove the line to use this machine's time zone."));
        }
    }

    private static void ValidateHolidays(IReadOnlyList<string> holidays, List<ConfigurationProblem> problems)
    {
        for (var index = 0; index < holidays.Count; index++)
        {
            var holiday = holidays[index];
            if (!DateOnly.TryParseExact(holiday?.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                problems.Add(new(
                    $"holidays[{index}]",
                    $"'{holiday}' is not a date in {DateFormat} format. Write it like '2026-11-02'."));
            }
        }
    }

    private static void ValidateProjects(IReadOnlyList<ProjectOptions> projects, List<ConfigurationProblem> problems)
    {
        // Every name and alias seen so far, and the project that claimed it first.
        var owners = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < projects.Count; index++)
        {
            var project = projects[index];
            var path = $"projects[{index}]";

            if (string.IsNullOrWhiteSpace(project.Name))
            {
                problems.Add(new($"{path}.name", "The project has no name. Give it the name people know it by, such as 'Checkout'."));
            }
            else
            {
                Claim(project.Name, $"{path}.name", index, projects, owners, problems);
            }

            for (var aliasIndex = 0; aliasIndex < project.Aliases.Count; aliasIndex++)
            {
                Claim(project.Aliases[aliasIndex], $"{path}.aliases[{aliasIndex}]", index, projects, owners, problems);
            }

            for (var targetIndex = 0; targetIndex < project.Targets.Count; targetIndex++)
            {
                var target = project.Targets[targetIndex];
                if (!Target.TryParse(target, out _))
                {
                    problems.Add(new(
                        $"{path}.targets[{targetIndex}]",
                        $"'{target}' is not a target. Write it as {Target.Format}."));
                }
            }
        }
    }

    /// <summary>
    /// Records that project <paramref name="index"/> answers to <paramref name="nameOrAlias"/>,
    /// reporting a problem when another project already does.
    /// </summary>
    private static void Claim(
        string nameOrAlias,
        string path,
        int index,
        IReadOnlyList<ProjectOptions> projects,
        Dictionary<string, int> owners,
        List<ConfigurationProblem> problems)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
        {
            return;
        }

        var key = nameOrAlias.Trim();
        if (owners.TryAdd(key, index) || owners[key] == index)
        {
            return;
        }

        problems.Add(new(
            path,
            $"'{key}' is already used by the project '{projects[owners[key]].Name}'. "
            + "Each name and alias must point to a single project: rename or remove one of them."));
    }
}
