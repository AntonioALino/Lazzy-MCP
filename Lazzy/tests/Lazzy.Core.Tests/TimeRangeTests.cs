using Lazzy.Abstractions;

namespace Lazzy.Core.Tests;

public sealed class TimeRangeTests
{
    private static readonly TimeSpan Brt = TimeSpan.FromHours(-3);
    private static readonly DateTimeOffset Start = new(2026, 10, 28, 9, 0, 0, Brt);
    private static readonly DateTimeOffset End = new(2026, 10, 28, 18, 0, 0, Brt);

    [Fact]
    public void KeepsTheGivenBounds()
    {
        var range = new TimeRange(Start, End);

        Assert.Equal(Start, range.From);
        Assert.Equal(End, range.To);
    }

    [Fact]
    public void RejectsAnEndBeforeTheStart()
    {
        var exception = Assert.Throws<ArgumentException>(() => new TimeRange(End, Start));

        Assert.Equal("to", exception.ParamName);
    }

    [Fact]
    public void ContainsItsStart() =>
        Assert.True(new TimeRange(Start, End).Contains(Start));

    [Fact]
    public void DoesNotContainItsEnd() =>
        Assert.False(new TimeRange(Start, End).Contains(End));

    [Fact]
    public void ContainsAnInstantInside() =>
        Assert.True(new TimeRange(Start, End).Contains(Start.AddHours(4)));

    [Fact]
    public void DoesNotContainAnInstantBeforeTheStart() =>
        Assert.False(new TimeRange(Start, End).Contains(Start.AddTicks(-1)));

    [Fact]
    public void AnEmptyRangeContainsNothing()
    {
        var range = new TimeRange(Start, Start);

        Assert.True(range.IsEmpty);
        Assert.False(range.Contains(Start));
    }

    [Fact]
    public void ComparesInstantsRegardlessOfOffset()
    {
        var range = new TimeRange(Start, End);
        var sameInstantInUtc = Start.ToUniversalTime().AddMinutes(1);

        Assert.True(range.Contains(sameInstantInUtc));
    }
}