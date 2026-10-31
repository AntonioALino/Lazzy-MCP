using System.Globalization;
using Lazzy.Abstractions;

namespace Lazzy.Core;

/// <summary>
/// Turns the loose period strings people and models use ("yesterday", "7d", "2026-10-01")
/// into a precise <see cref="TimeRange"/> that always ends now.
/// </summary>
public sealed class PeriodParser
{
    /// <summary>
    /// The accepted formats, in words. Reused in the tool parameter description and in error messages,
    /// so a model that sends something unexpected learns how to retry.
    /// </summary>
    public const string Help =
        "Use 'yesterday' (the default: since the start of the previous business day), 'today', " +
        "a number of days such as '7d' (1 to 365), or a start date such as '2026-10-01'.";

    private const int MaxDays = 365;

    private readonly TimeProvider _clock;
    private readonly TimeZoneInfo _timeZone;
    private readonly BusinessCalendar _calendar;

    public PeriodParser(TimeProvider clock, TimeZoneInfo timeZone, BusinessCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timeZone);
        ArgumentNullException.ThrowIfNull(calendar);

        _clock = clock;
        _timeZone = timeZone;
        _calendar = calendar;
    }

    /// <summary>Parses <paramref name="since"/> into a range from its start until now.</summary>
    /// <exception cref="ArgumentException">The input is not understood or starts in the future.</exception>
    public TimeRange Parse(string? since)
    {
        var now = _clock.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, _timeZone).DateTime);
        var input = since?.Trim() ?? string.Empty;

        DateTimeOffset start;
        if (input.Length == 0 || Is(input, "yesterday") || Is(input, "ontem"))
        {
            start = StartOfDay(_calendar.PreviousBusinessDay(today));
        }
        else if (Is(input, "today") || Is(input, "hoje"))
        {
            start = StartOfDay(today);
        }
        else if (TryParseDays(input, out var days))
        {
            start = now.AddDays(-days);
        }
        else if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            start = StartOfDay(date);
        }
        else
        {
            throw new ArgumentException($"Could not understand the period '{since}'. {Help}", nameof(since));
        }

        if (start > now)
        {
            throw new ArgumentException($"The period '{since}' starts in the future. {Help}", nameof(since));
        }

        return new TimeRange(start, now);
    }

    /// <summary>
    /// Local midnight of <paramref name="day"/>, with the offset that was in effect on that date.
    /// Using today's offset would be off by one hour for dates on the other side of a daylight saving change.
    /// </summary>
    private DateTimeOffset StartOfDay(DateOnly day)
    {
        var localMidnight = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        // In zones where daylight saving starts at midnight, 00:00 does not exist on that day.
        if (_timeZone.IsInvalidTime(localMidnight))
        {
            localMidnight = localMidnight.AddHours(1);
        }

        return new DateTimeOffset(localMidnight, _timeZone.GetUtcOffset(localMidnight));
    }

    private static bool TryParseDays(string input, out int days)
    {
        days = 0;
        return (input.EndsWith('d') || input.EndsWith('D')) && int.TryParse(input[..^1], NumberStyles.None, CultureInfo.InvariantCulture, out days) && days is >= 1 and <= MaxDays;
    }

    private static bool Is(string input, string keyword) =>
        string.Equals(input, keyword, StringComparison.OrdinalIgnoreCase);
}