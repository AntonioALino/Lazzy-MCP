namespace Lazzy.Core.Tests;

public sealed class BusinessCalendarTests
{
    private static readonly DateOnly MondayHoliday = new(2026, 11, 2);

    [Theory]
    [InlineData(2026, 10, 26)] // Monday
    [InlineData(2026, 10, 27)] // Tuesday
    [InlineData(2026, 10, 28)] // Wednesday
    [InlineData(2026, 10, 29)] // Thursday
    [InlineData(2026, 10, 30)] // Friday
    public void WeekdaysAreBusinessDays(int year, int month, int day) =>
        Assert.True(BusinessCalendar.WeekendsOnly.IsBusinessDay(new DateOnly(year, month, day)));

    [Theory]
    [InlineData(2026, 10, 31)] // Saturday
    [InlineData(2026, 11, 1)]  // Sunday
    public void WeekendsAreNotBusinessDays(int year, int month, int day) =>
        Assert.False(BusinessCalendar.WeekendsOnly.IsBusinessDay(new DateOnly(year, month, day)));

    [Fact]
    public void HolidaysAreNotBusinessDays()
    {
        var calendar = new BusinessCalendar([MondayHoliday]);

        Assert.False(calendar.IsBusinessDay(MondayHoliday));
    }

    [Fact]
    public void TheDayBeforeTuesdayIsMonday() =>
        Assert.Equal(
            new DateOnly(2026, 10, 26),
            BusinessCalendar.WeekendsOnly.PreviousBusinessDay(new DateOnly(2026, 10, 27)));

    [Fact]
    public void TheDayBeforeMondayIsFriday() =>
        Assert.Equal(
            new DateOnly(2026, 10, 30),
            BusinessCalendar.WeekendsOnly.PreviousBusinessDay(new DateOnly(2026, 11, 2)));

    [Fact]
    public void SkipsAHolidayOnMonday()
    {
        var calendar = new BusinessCalendar([MondayHoliday]);

        var previous = calendar.PreviousBusinessDay(new DateOnly(2026, 11, 3));

        Assert.Equal(new DateOnly(2026, 10, 30), previous);
    }

    [Fact]
    public void FailsClearlyWhenNoBusinessDayIsFound()
    {
        var everyDay = Enumerable.Range(0, 90).Select(offset => new DateOnly(2026, 9, 1).AddDays(offset));
        var calendar = new BusinessCalendar(everyDay);

        var exception = Assert.Throws<InvalidOperationException>(
            () => calendar.PreviousBusinessDay(new DateOnly(2026, 11, 29)));

        Assert.Contains("2026-11-29", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsANullHolidayList() =>
        Assert.Throws<ArgumentNullException>(() => new BusinessCalendar(null!));
}