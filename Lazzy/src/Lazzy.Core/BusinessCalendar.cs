using System.Globalization;

namespace Lazzy.Core;


/// <summary>Decides which days are business days: weekends and configured holidays are not.</summary>
public sealed class BusinessCalendar
{
    public const int MaxLookBackDays = 31;

    private readonly HashSet<DateOnly> _holidays;

    public BusinessCalendar(IEnumerable<DateOnly> holidays)
    {
        ArgumentNullException.ThrowIfNull(holidays);
        _holidays = [.. holidays];
    }

    /// <summary>A calendar with no holidays: only Saturdays and Sundays are skipped.</summary>
    public static BusinessCalendar WeekendsOnly { get; } = new([]);

    public bool IsBusinessDay(DateOnly day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_holidays.Contains(day);

    /// <summary>The closest business day before <paramref name="day"/>.</summary>
    /// <exception cref="InvalidOperationException">
    /// No business day exists in the <see cref="MaxLookBackDays"/> days before <paramref name="day"/>,
    /// which means the holiday list is misconfigured.
    /// </exception>
    public DateOnly PreviousBusinessDay(DateOnly day)
    {
        for (var daysBack = 1; daysBack <= MaxLookBackDays; daysBack++)
        {
            var candidate = day.AddDays(-daysBack);
            if (IsBusinessDay(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"No business day found in the {MaxLookBackDays} days before " +
            $"{day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}. Check the configured holidays.");
    }
}