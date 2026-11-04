using Lazzy.Application.Configuration;
using Lazzy.Application.Models;
using Lazzy.Application.Ports.Driven;
using Lazzy.Application.Services;
using Lazzy.Application.Tests.Fakes;
using Lazzy.Domain.Enums;
using Lazzy.Domain.ValueObjects;

namespace Lazzy.Application.Tests.Services;

public sealed class ActivityServiceTests
{
    private static readonly TimeSpan Brt = TimeSpan.FromHours(-3);

    // Monday, November 9, 2026, 09:00 in São Paulo; the range covers the previous 24 hours.
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 9, 0, 0, Brt);
    private static readonly TimeRange Range = new(Now.AddDays(-1), Now);

    private static readonly Target Api = new("github", "my-org/checkout-api");
    private static readonly Target Web = new("github", "my-org/checkout-web");
    private static readonly Target Board = new("ado", "my-org/Checkout");

    private static readonly ProjectOptions Checkout = new()
    {
        Name = "Checkout",
        Targets = ["github:my-org/checkout-api", "github:my-org/checkout-web", "ado:my-org/Checkout"],
    };

    private static readonly ActivityEvent Commit = new()
    {
        Kind = ActivityKind.CommitAuthored,
        Timestamp = Now.AddHours(-2),
        Title = "Fix rounding of discounts",
        Url = new Uri("https://example.com/my-org/checkout-api/commit/1a2b3c4"),
        Source = "github",
        Target = Api,
        Reference = "1a2b3c4",
    };

    private static readonly ActivityEvent StateChange = new()
    {
        Kind = ActivityKind.WorkItemStateChanged,
        Timestamp = Now.AddHours(-1),
        Title = "Discount rounding",
        Url = new Uri("https://example.com/my-org/Checkout/_workitems/edit/42"),
        Source = "ado",
        Target = Board,
        Reference = "42",
        FromState = "Doing",
        ToState = "Ready to Test",
    };

    private static LazzyOptions Options(IReadOnlyDictionary<string, IdentityOptions>? identity = null) => new()
    {
        Identity = identity ?? new Dictionary<string, IdentityOptions>
        {
            ["github"] = new() { Login = "octo-dev", Emails = ["dev@example.com"] },
        },
        Projects = [Checkout],
    };

    private static Task<ActivityReport> Run(
        IEnumerable<IActivitySource> sources,
        LazzyOptions? options = null,
        IReadOnlyList<ProjectOptions>? projects = null,
        CancellationToken cancellationToken = default) =>
        new ActivityService(sources, options ?? Options()).GetActivityAsync(projects ?? [Checkout], Range, cancellationToken);

    [Fact]
    public async Task MergesTheEventsOfEverySourceIntoOneReport()
    {
        var report = await Run([FakeActivitySource.Returning("github", Commit), FakeActivitySource.Returning("ado", StateChange)]);

        Assert.Equal(Range, report.Range);
        Assert.Equal([Commit, StateChange], report.Events);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task AFailingSourceBecomesAWarningWhileTheOthersStillAnswer()
    {
        var failing = FakeActivitySource.Throwing("github", new ActivitySourceException("The token was rejected. Create a new one and set GITHUB_TOKEN."));

        var report = await Run([failing, FakeActivitySource.Returning("ado", StateChange)]);

        Assert.Equal([StateChange], report.Events);
        Assert.Equal(
            [
                "github:my-org/checkout-api: The token was rejected. Create a new one and set GITHUB_TOKEN.",
                "github:my-org/checkout-web: The token was rejected. Create a new one and set GITHUB_TOKEN.",
            ],
            report.Warnings);
    }

    [Fact]
    public async Task AnUnexpectedExceptionBecomesAWarning()
    {
        var failing = FakeActivitySource.Throwing("ado", new InvalidOperationException("Unexpected JSON."));

        var report = await Run([FakeActivitySource.Returning("github", Commit), failing]);

        Assert.Equal([Commit], report.Events);
        var warning = Assert.Single(report.Warnings);
        Assert.StartsWith("ado:my-org/Checkout: ", warning, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException: Unexpected JSON.", warning, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ASourceThatThrowsBeforeReturningATaskBecomesAWarning()
    {
        var failing = new FakeActivitySource("ado", (_, _) => throw new HttpRequestException("Connection refused."));

        var report = await Run([FakeActivitySource.Returning("github", Commit), failing]);

        Assert.Equal([Commit], report.Events);
        Assert.Contains("HttpRequestException", Assert.Single(report.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public async Task EventsOutsideTheRangeAreDropped()
    {
        var beforeStart = Commit with { Reference = "a", Timestamp = Range.From.AddTicks(-1) };
        var atStart = Commit with { Reference = "b", Timestamp = Range.From };
        var lastInstant = Commit with { Reference = "c", Timestamp = Range.To.AddTicks(-1) };
        var atEnd = Commit with { Reference = "d", Timestamp = Range.To };

        var report = await Run(
            [FakeActivitySource.Returning("github", beforeStart, atStart, lastInstant, atEnd)],
            projects: [Checkout with { Targets = ["github:my-org/checkout-api"] }]);

        Assert.Equal(["b", "c"], report.Events.Select(activity => activity.Reference));
    }

    [Fact]
    public async Task TheSameEventReportedTwiceIsReturnedOnce()
    {
        // The fake returns the same commit for both GitHub targets.
        var report = await Run([FakeActivitySource.Returning("github", Commit)]);

        Assert.Equal([Commit], report.Events);
    }

    [Fact]
    public async Task EventsOnTheSameItemAtDifferentTimesOrWithDifferentKindsAreKept()
    {
        var review = Commit with { Kind = ActivityKind.PullRequestReviewed, Url = new Uri("https://example.com/my-org/checkout-api/pull/7") };
        var secondReview = review with { Timestamp = review.Timestamp.AddMinutes(30) };
        var reviewOnAnotherAnchor = review with { Url = new Uri("https://example.com/my-org/checkout-api/pull/7#review-2") };
        var opened = review with { Kind = ActivityKind.PullRequestOpened };

        var report = await Run(
            [FakeActivitySource.Returning("github", review, secondReview, reviewOnAnotherAnchor, opened)],
            projects: [Checkout with { Targets = ["github:my-org/checkout-api"] }]);

        Assert.Equal(4, report.Events.Count);
    }

    [Fact]
    public async Task EventsAreSortedOldestFirst()
    {
        var first = Commit with { Reference = "first", Timestamp = Now.AddHours(-5) };
        var second = Commit with { Reference = "second", Timestamp = Now.AddHours(-3) };
        var third = StateChange with { Timestamp = Now.AddHours(-1) };

        var report = await Run([FakeActivitySource.Returning("ado", third), FakeActivitySource.Returning("github", second, first)]);

        Assert.Equal(["first", "second", "42"], report.Events.Select(activity => activity.Reference));
    }

    [Fact]
    public async Task EventsAtTheSameInstantAreOrderedByKindThenUrl()
    {
        var merged = Commit with { Kind = ActivityKind.PullRequestMerged, Url = new Uri("https://example.com/b") };
        var openedB = Commit with { Kind = ActivityKind.PullRequestOpened, Url = new Uri("https://example.com/b") };
        var openedA = Commit with { Kind = ActivityKind.PullRequestOpened, Url = new Uri("https://example.com/a") };

        var report = await Run(
            [FakeActivitySource.Returning("github", merged, openedB, openedA)],
            projects: [Checkout with { Targets = ["github:my-org/checkout-api"] }]);

        Assert.Equal([openedA, openedB, merged], report.Events);
    }

    [Fact]
    public async Task EachTargetIsQueriedOnceEvenWhenProjectsShareIt()
    {
        var github = FakeActivitySource.Returning("github");
        var shared = new ProjectOptions { Name = "Shared", Targets = ["github:my-org/checkout-api", " github : my-org/checkout-api "] };

        await Run([github, FakeActivitySource.Returning("ado")], projects: [Checkout, shared]);

        Assert.Equal(2, github.Queries.Count);
        Assert.Equal(new HashSet<Target> { Api, Web }, github.Queries.Select(query => query.Target).ToHashSet());
    }

    [Fact]
    public async Task QueriesCarryTheTargetTheRangeAndTheConfiguredIdentity()
    {
        var github = FakeActivitySource.Returning("github");
        var options = Options(new Dictionary<string, IdentityOptions> { ["github"] = new() { Login = "  octo-dev  ", Emails = ["dev@example.com"] } });

        await Run([github], options, projects: [Checkout with { Targets = ["github:my-org/checkout-api"] }]);

        var query = Assert.Single(github.Queries);
        Assert.Equal(Api, query.Target);
        Assert.Equal(Range, query.Range);
        Assert.Equal("octo-dev", query.Identity?.Login);
        Assert.Equal(["dev@example.com"], query.Identity?.Emails ?? []);
    }

    [Fact]
    public async Task ASourceThatFindsTheUserFromItsTokenGetsNoIdentityWhenNoneIsConfigured()
    {
        var ado = FakeActivitySource.Returning("ado");

        await Run([FakeActivitySource.Returning("github"), ado]);

        Assert.Null(Assert.Single(ado.Queries).Identity);
    }

    [Fact]
    public async Task AnIdentityWithoutLoginIsPassedWithItsEmails()
    {
        var ado = FakeActivitySource.Returning("ado");
        var options = Options(new Dictionary<string, IdentityOptions> { ["ado"] = new() { Login = " ", Emails = ["dev@example.com"] } });

        await Run([ado], options, projects: [Checkout with { Targets = ["ado:my-org/Checkout"] }]);

        var identity = Assert.Single(ado.Queries).Identity;
        Assert.NotNull(identity);
        Assert.Null(identity.Login);
        Assert.Equal(["dev@example.com"], identity.Emails);
    }

    [Fact]
    public async Task UnknownProviderIsReportedAsAWarning()
    {
        var project = Checkout with { Targets = ["gitlab:my-team/app", "gitlab:my-team/web", "github:my-org/checkout-api"] };

        var report = await Run([FakeActivitySource.Returning("github", Commit), FakeActivitySource.Returning("ado")], projects: [project]);

        Assert.Equal([Commit], report.Events);
        Assert.Equal(
            "Skipped gitlab:my-team/app, gitlab:my-team/web: no provider called 'gitlab' is available. "
            + "Check the part before ':' in the target; the available providers are: ado, github.",
            Assert.Single(report.Warnings));
    }

    [Fact]
    public async Task UnknownProviderWithNoSourcesAtAllSaysNoneIsAvailable()
    {
        var report = await Run([], projects: [Checkout with { Targets = ["github:my-org/checkout-api"] }]);

        Assert.EndsWith("the available providers are: none.", Assert.Single(report.Warnings), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingIdentityIsReportedWithWhatToAdd(string? login)
    {
        var github = new FakeActivitySource("github", (_, _) => Task.FromResult<IReadOnlyList<ActivityEvent>>([Commit]), requiresIdentity: true);
        var identity = login is null
            ? new Dictionary<string, IdentityOptions>()
            : new Dictionary<string, IdentityOptions> { ["github"] = new() { Login = login, Emails = ["dev@example.com"] } };

        var report = await Run([github, FakeActivitySource.Returning("ado", StateChange)], Options(identity));

        Assert.Empty(github.Queries);
        Assert.Equal([StateChange], report.Events);
        Assert.Equal(
            "Skipped github:my-org/checkout-api, github:my-org/checkout-web: Lazzy does not know who you are on 'github'. "
            + "Add your login under identity.github.login in lazzy.yaml.",
            Assert.Single(report.Warnings));
    }

    [Fact]
    public async Task ProviderIdsAndIdentityKeysIgnoreCase()
    {
        var github = new FakeActivitySource("github", (_, _) => Task.FromResult<IReadOnlyList<ActivityEvent>>([Commit]), requiresIdentity: true);
        var options = Options(new Dictionary<string, IdentityOptions> { [" GITHUB "] = new() { Login = "octo-dev" } });

        var report = await Run([github], options, projects: [Checkout with { Targets = ["GitHub:my-org/checkout-api"] }]);

        Assert.Equal([Commit], report.Events);
        Assert.Equal("octo-dev", Assert.Single(github.Queries).Identity?.Login);
    }

    [Fact]
    public async Task AnInvalidTargetIsReportedAsAWarning()
    {
        var report = await Run([FakeActivitySource.Returning("github", Commit)], projects: [Checkout with { Targets = ["my-org/checkout-api", "github:my-org/checkout-api"] }]);

        Assert.Equal([Commit], report.Events);
        Assert.StartsWith("Skipped 'my-org/checkout-api': it is not a target.", Assert.Single(report.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WarningsAreCollectedSafelyUnderConcurrency()
    {
        const int TargetCount = 200;
        var project = new ProjectOptions { Name = "Big", Targets = [.. Enumerable.Range(0, TargetCount).Select(index => $"github:my-org/repo-{index}")] };
        var failing = new FakeActivitySource("github", async (query, _) =>
        {
            await Task.Yield();
            throw new ActivitySourceException($"Rate limit reached for {query.Target.Location}.");
        });

        var report = await Run([failing], projects: [project]);

        Assert.Equal(TargetCount, report.Warnings.Count);
        Assert.Equal(TargetCount, report.Warnings.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("github:my-org/repo-0: Rate limit reached for my-org/repo-0.", report.Warnings[0]);
    }

    [Fact]
    public async Task SourcesAreCalledInParallel()
    {
        // Each call waits until all three have started: called one after the other, the first would time out.
        var started = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new FakeActivitySource("github", async (_, cancellationToken) =>
        {
            if (Interlocked.Increment(ref started) == 3)
            {
                allStarted.SetResult();
            }

            await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return [];
        });
        var project = new ProjectOptions { Name = "Three", Targets = ["github:my-org/a", "github:my-org/b", "github:my-org/c"] };

        var report = await Run([source], projects: [project]);

        Assert.Empty(report.Warnings);
        Assert.Equal(3, source.Queries.Count);
    }

    [Fact]
    public async Task CancellationPropagates()
    {
        using var cancellation = new CancellationTokenSource();
        var source = new FakeActivitySource("github", (_, cancellationToken) =>
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<ActivityEvent>>([]);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run([source], cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task ATimeoutInsideASourceIsAWarningNotACancellation()
    {
        var timingOut = FakeActivitySource.Throwing("ado", new TaskCanceledException("The request timed out."));

        var report = await Run([FakeActivitySource.Returning("github", Commit), timingOut]);

        Assert.Equal([Commit], report.Events);
        Assert.Contains("TaskCanceledException", Assert.Single(report.Warnings), StringComparison.Ordinal);
    }

    [Fact]
    public void TwoSourcesWithTheSameIdAreRejected()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new ActivityService([FakeActivitySource.Returning("github"), FakeActivitySource.Returning("GitHub")], Options()));

        Assert.Contains("'GitHub'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConstructorRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ActivityService(null!, Options()));
        Assert.Throws<ArgumentNullException>(() => new ActivityService([], null!));
    }

    [Fact]
    public async Task GetActivityRejectsNullProjects()
    {
        var service = new ActivityService([], Options());

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.GetActivityAsync(null!, Range, CancellationToken.None));
    }
}
