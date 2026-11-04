using Lazzy.Domain.Enums;
using Lazzy.Domain.ValueObjects;

namespace Lazzy.Domain.Tests.ValueObjects;

public sealed class ActivityEventTests
{
    private static readonly ActivityEvent Commit = new()
    {
        Kind = ActivityKind.CommitAuthored,
        Timestamp = new DateTimeOffset(2026, 11, 9, 7, 0, 0, TimeSpan.FromHours(-3)),
        Title = "Fix rounding of discounts",
        Url = new Uri("https://example.com/my-org/checkout-api/commit/1a2b3c4"),
        Source = "github",
        Target = new Target("github", "my-org/checkout-api"),
        Reference = "1a2b3c4",
    };

    [Fact]
    public void KeepsWhatItWasGiven()
    {
        var change = Commit with { Kind = ActivityKind.WorkItemStateChanged, FromState = "Doing", ToState = "Ready to Test" };

        Assert.Equal(ActivityKind.WorkItemStateChanged, change.Kind);
        Assert.Equal("Fix rounding of discounts", change.Title);
        Assert.Equal("github", change.Source);
        Assert.Equal("1a2b3c4", change.Reference);
        Assert.Equal("Doing", change.FromState);
        Assert.Equal("Ready to Test", change.ToState);
    }

    [Fact]
    public void TitleMayBeEmpty()
    {
        Assert.Equal(string.Empty, (Commit with { Title = string.Empty }).Title);
    }

    [Fact]
    public void RejectsARelativeUrl()
    {
        var exception = Assert.Throws<ArgumentException>(() => Commit with { Url = new Uri("/commit/1a2b3c4", UriKind.Relative) });

        Assert.Contains("absolute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsAnUndefinedKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Commit with { Kind = (ActivityKind)99 });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsABlankSourceOrReference(string blank)
    {
        Assert.ThrowsAny<ArgumentException>(() => Commit with { Source = blank });
        Assert.ThrowsAny<ArgumentException>(() => Commit with { Reference = blank });
    }

    [Fact]
    public void RejectsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => Commit with { Title = null! });
        Assert.Throws<ArgumentNullException>(() => Commit with { Url = null! });
        Assert.Throws<ArgumentNullException>(() => Commit with { Target = null! });
    }
}
