using Lazzy.Adapters.Demo;
using Lazzy.Application.Ports.Driven;
using Lazzy.Application.Services;
using Lazzy.Domain.Enums;
using Lazzy.Domain.Services;
using Lazzy.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Lazzy.Adapters.Contract.Demo;

public sealed class DemoActivitySourceTests
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly TimeSpan Brt = TimeSpan.FromHours(-3);

    // Monday, November 9, 2026, 09:00 in São Paulo.
    private static readonly DateTimeOffset Monday = new(2026, 11, 9, 9, 0, 0, Brt);
    private static readonly DateOnly Friday = new(2026, 11, 6);

    private static readonly Target Code = DemoActivitySource.Targets[0];
    private static readonly Target Board = DemoActivitySource.Targets[1];

    private static DemoActivitySource Source(DateTimeOffset now, BusinessCalendar? calendar = null) =>
        new(new FakeTimeProvider(now), SaoPaulo, calendar ?? BusinessCalendar.WeekendsOnly);

    private static async Task<List<ActivityEvent>> ReadAllAsync(DemoActivitySource source, TimeRange range)
    {
        var events = new List<ActivityEvent>();
        foreach (var target in DemoActivitySource.Targets)
        {
            events.AddRange(await source.GetActivityAsync(new ActivityQuery(target, range, null), CancellationToken.None));
        }

        return events;
    }

    private static DateOnly LocalDate(ActivityEvent activity) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(activity.Timestamp, SaoPaulo).DateTime);

    [Fact]
    public void IsTheDemoProviderAndNeedsNoIdentity()
    {
        var source = Source(Monday);

        Assert.Equal("demo", source.Id);
        Assert.False(source.RequiresIdentity);
        Assert.Equal(["demo:example-shop/storefront", "demo:example-shop/board"], DemoActivitySource.Targets.Select(target => target.ToString()));
    }

    [Fact]
    public async Task AMondayRunShowsFridaysWork()
    {
        var yesterday = new PeriodParser(new FakeTimeProvider(Monday), SaoPaulo, BusinessCalendar.WeekendsOnly).Parse("yesterday");

        var events = await ReadAllAsync(Source(Monday), yesterday);

        Assert.All(events, activity => Assert.Equal(Friday, LocalDate(activity)));
        Assert.Equal(
            ["#40", "c4e19d8", "#42", "#101", "#102", "e05f3b9"],
            events.OrderBy(activity => activity.Timestamp).Select(activity => activity.Reference));
        Assert.Contains(events, activity => activity is { Kind: ActivityKind.PullRequestMerged, Reference: "#42" });
        Assert.Contains(events, activity => activity is { Kind: ActivityKind.WorkItemStateChanged, Reference: "#101", ToState: "Ready to Test" });
    }

    [Fact]
    public async Task AHolidayOnFridayMovesTheStoryBackToThursday()
    {
        var calendar = new BusinessCalendar([Friday]);
        var yesterday = new PeriodParser(new FakeTimeProvider(Monday), SaoPaulo, calendar).Parse("yesterday");

        var events = await ReadAllAsync(Source(Monday, calendar), yesterday);

        Assert.NotEmpty(events);
        Assert.All(events, activity => Assert.Equal(new DateOnly(2026, 11, 5), LocalDate(activity)));
    }

    [Fact]
    public async Task TheWholeStorySpansTheLastFiveBusinessDays()
    {
        var events = await ReadAllAsync(Source(Monday), new TimeRange(Monday.AddDays(-14), Monday));

        Assert.Equal(12, events.Count);
        Assert.Equal(9, events.Count(activity => activity.Target == Code));
        Assert.Equal(3, events.Count(activity => activity.Target == Board));

        // Five business days before Monday, November 9, skipping the weekend, is Monday, November 2.
        var first = events.MinBy(activity => activity.Timestamp)!;
        Assert.Equal("Upgrade the payment SDK", first.Title);
        Assert.Equal(new DateTimeOffset(2026, 11, 2, 10, 0, 0, Brt), first.Timestamp);
    }

    [Fact]
    public async Task OnASaturdayYesterdayIsStillFriday()
    {
        var saturday = new DateTimeOffset(2026, 11, 7, 10, 0, 0, Brt);
        var range = new TimeRange(new DateTimeOffset(2026, 11, 6, 0, 0, 0, Brt), saturday);

        var events = await ReadAllAsync(Source(saturday), range);

        Assert.Equal(6, events.Count);
    }

    [Fact]
    public async Task TimestampsAreInTheConfiguredTimeZone()
    {
        var events = await ReadAllAsync(Source(Monday), new TimeRange(Monday.AddDays(-14), Monday));

        Assert.All(events, activity => Assert.Equal(Brt, activity.Timestamp.Offset));
    }

    [Fact]
    public async Task EventsAreTiedToTheQueriedTargetWithAnAbsoluteUrl()
    {
        var events = await Source(Monday).GetActivityAsync(new ActivityQuery(Board, new TimeRange(Monday.AddDays(-14), Monday), null), CancellationToken.None);

        var started = Assert.Single(events, activity => activity.Reference == "#102");
        Assert.Equal(Board, started.Target);
        Assert.Equal("demo", started.Source);
        Assert.Equal(("To Do", "Doing"), (started.FromState, started.ToState));
        Assert.Equal(new Uri("https://demo.example.com/example-shop/board/items/102"), started.Url);
    }

    [Fact]
    public async Task LocationsIgnoreCase()
    {
        var query = new ActivityQuery(new Target("demo", "Example-Shop/Board"), new TimeRange(Monday.AddDays(-14), Monday), null);

        Assert.Equal(3, (await Source(Monday).GetActivityAsync(query, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task AnUnknownLocationIsAnActivitySourceExceptionListingTheDemoTargets()
    {
        var query = new ActivityQuery(new Target("demo", "my-org/elsewhere"), new TimeRange(Monday.AddDays(-14), Monday), null);

        var exception = await Assert.ThrowsAsync<ActivitySourceException>(() => Source(Monday).GetActivityAsync(query, CancellationToken.None));

        Assert.Contains("'my-org/elsewhere'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("demo:example-shop/storefront, demo:example-shop/board", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACanceledQueryIsNotAnswered()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var query = new ActivityQuery(Code, new TimeRange(Monday.AddDays(-14), Monday), null);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Source(Monday).GetActivityAsync(query, cancellation.Token));
    }

    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DemoActivitySource(null!, SaoPaulo, BusinessCalendar.WeekendsOnly));
        Assert.Throws<ArgumentNullException>(() => new DemoActivitySource(new FakeTimeProvider(Monday), null!, BusinessCalendar.WeekendsOnly));
        Assert.Throws<ArgumentNullException>(() => new DemoActivitySource(new FakeTimeProvider(Monday), SaoPaulo, null!));
    }

    [Fact]
    public async Task GetActivityRejectsNullQuery()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Source(Monday).GetActivityAsync(null!, CancellationToken.None));
    }
}
