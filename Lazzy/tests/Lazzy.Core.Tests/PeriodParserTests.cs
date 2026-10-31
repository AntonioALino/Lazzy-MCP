using Microsoft.Extensions.Time.Testing;

namespace Lazzy.Core.Tests;

public sealed class PeriodParserTests
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly TimeZoneInfo Lisbon = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
    private static readonly TimeSpan Brt = TimeSpan.FromHours(-3);

    // Wednesday, October 28, 2026, 09:00 in São Paulo.
    private static readonly DateTimeOffset Wednesday = new(2026, 10, 28, 9, 0, 0, Brt);

    private static PeriodParser ParserAt(DateTimeOffset now, TimeZoneInfo? timeZone = null, params DateOnly[] holidays) =>
        new(new FakeTimeProvider(now), timeZone ?? SaoPaulo, new BusinessCalendar(holidays));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("yesterday")]
    [InlineData("ontem")]
    [InlineData("  Yesterday  ")]
    public void YesterdayOnAWednesdayStartsOnTuesdayAtLocalMidnight(string? input)
    {
        var range = ParserAt(Wednesday).Parse(input);

        Assert.Equal(new DateTimeOffset(2026, 10, 27, 0, 0, 0, Brt), range.From);
    }

    [Fact]
    public void YesterdayOnAMondayReachesBackToFriday()
    {
        var monday = new DateTimeOffset(2026, 11, 9, 9, 0, 0, Brt);

        var range = ParserAt(monday).Parse("yesterday");

        Assert.Equal(new DateTimeOffset(2026, 11, 6, 0, 0, 0, Brt), range.From);
    }

    [Fact]
    public void YesterdaySkipsAHolidayOnMonday()
    {
        var tuesday = new DateTimeOffset(2026, 11, 3, 9, 0, 0, Brt);

        var range = ParserAt(tuesday, holidays: new DateOnly(2026, 11, 2)).Parse("yesterday");

        Assert.Equal(new DateTimeOffset(2026, 10, 30, 0, 0, 0, Brt), range.From);
    }

    [Theory]
    [InlineData("today")]
    [InlineData("hoje")]
    public void TodayStartsAtLocalMidnight(string input)
    {
        var range = ParserAt(Wednesday).Parse(input);

        Assert.Equal(new DateTimeOffset(2026, 10, 28, 0, 0, 0, Brt), range.From);
    }

    [Theory]
    [InlineData("7d", 7)]
    [InlineData("1D", 1)]
    [InlineData("365d", 365)]
    public void DayCountsGoBackExactlyFromNow(string input, int days)
    {
        var range = ParserAt(Wednesday).Parse(input);

        Assert.Equal(Wednesday.AddDays(-days), range.From);
    }

    [Fact]
    public void AnExplicitDateStartsAtLocalMidnight()
    {
        var range = ParserAt(Wednesday).Parse("2026-10-01");

        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, Brt), range.From);
    }

    [Fact]
    public void EveryRangeEndsNow()
    {
        var range = ParserAt(Wednesday).Parse("7d");

        Assert.Equal(Wednesday, range.To);
    }

    [Fact]
    public void LocalMidnightUsesTheOffsetOfThatDate()
    {
        // On October 30, Lisbon is already on winter time (+00:00), but on July 1 it was on summer time (+01:00).
        var afterTheChange = new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);

        var range = ParserAt(afterTheChange, Lisbon).Parse("2026-07-01");

        Assert.Equal(TimeSpan.FromHours(1), range.From.Offset);
        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.FromHours(1)), range.From);
    }

    [Theory]
    [InlineData("banana")]
    [InlineData("0d")]
    [InlineData("366d")]
    [InlineData("-7d")]
    [InlineData("01/10/2026")]
    public void RejectsUnclearInputListingTheAcceptedFormats(string input)
    {
        var exception = Assert.Throws<ArgumentException>(() => ParserAt(Wednesday).Parse(input));

        Assert.Equal("since", exception.ParamName);
        Assert.Contains(PeriodParser.Help, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsADateInTheFuture()
    {
        var exception = Assert.Throws<ArgumentException>(() => ParserAt(Wednesday).Parse("2026-12-25"));

        Assert.Contains("future", exception.Message, StringComparison.Ordinal);
    }
}