using Lazzy.Application.Ports.Driven;
using Lazzy.Domain.Enums;
using Lazzy.Domain.ValueObjects;

namespace Lazzy.Adapters.Contract;

/// <summary>
/// The rules every <see cref="IActivitySource"/> must follow, as tests.
/// Subclass it once per adapter, implement <see cref="CreateSource"/> and <see cref="QueriesWithActivity"/>,
/// and every rule below runs against that adapter. See README.md in this project for an example.
/// </summary>
public abstract class ActivitySourceContract
{
    /// <summary>The longest id allowed: ids are written in every target, so they stay short.</summary>
    public const int MaxIdLength = 16;

    /// <summary>
    /// A new source, ready to answer. It must not reach the network: give it recorded fixtures
    /// (<c>tests/Fixtures/&lt;adapter&gt;/&lt;scenario&gt;/</c>) and a fake clock.
    /// </summary>
    protected abstract IActivitySource CreateSource();

    /// <summary>
    /// Queries the source has activity for, with ranges wide enough to contain it.
    /// Give one per kind of target the source reads (a repository and a board, for example),
    /// and include work item state changes when the source reports them, so every rule is exercised.
    /// </summary>
    protected abstract IEnumerable<ActivityQuery> QueriesWithActivity { get; }

    [Fact]
    public void IdIsAShortLowercaseToken()
    {
        var id = CreateSource().Id;

        Assert.Matches("^[a-z][a-z0-9]*$", id);
        Assert.InRange(id.Length, 1, MaxIdLength);
    }

    [Fact]
    public async Task ReturnsActivityForARangeThatHasSome()
    {
        Assert.NotEmpty(QueriesWithActivity);

        foreach (var query in QueriesWithActivity)
        {
            Assert.NotEmpty(await ReadAsync(query));
        }
    }

    [Fact]
    public async Task NeverReturnsEventsOutsideTheRequestedRange()
    {
        foreach (var query in QueriesWithActivity)
        {
            var full = query.Range;
            var middle = full.From + ((full.To - full.From) / 2);

            foreach (var range in new[] { full, new TimeRange(full.From, middle), new TimeRange(middle, full.To) })
            {
                var events = await ReadAsync(query with { Range = range });

                Assert.All(events, activity => Assert.True(
                    range.Contains(activity.Timestamp),
                    $"{activity.Reference} at {activity.Timestamp:O} is outside [{range.From:O}, {range.To:O}) on {query.Target}."));
            }
        }
    }

    [Fact]
    public async Task EveryEventIsTraceable()
    {
        var source = CreateSource();

        foreach (var query in QueriesWithActivity)
        {
            var events = await source.GetActivityAsync(query, CancellationToken.None);

            Assert.All(events, activity =>
            {
                Assert.Equal(source.Id, activity.Source);
                Assert.Equal(source.Id, activity.Target.Provider);
                Assert.False(string.IsNullOrWhiteSpace(activity.Reference), "Every event needs a reference people can use.");
                Assert.True(activity.Url.IsAbsoluteUri, $"{activity.Url} must be an absolute URL.");
            });
        }
    }

    [Fact]
    public async Task StateChangesAlwaysCarryTheNewState()
    {
        foreach (var query in QueriesWithActivity)
        {
            var changes = (await ReadAsync(query)).Where(activity => activity.Kind == ActivityKind.WorkItemStateChanged);

            Assert.All(changes, change => Assert.False(
                string.IsNullOrWhiteSpace(change.ToState),
                $"The state change {change.Reference} at {change.Timestamp:O} has no new state."));
        }
    }

    [Fact]
    public async Task AnEmptyRangeReturnsNothing()
    {
        foreach (var query in QueriesWithActivity)
        {
            // Empty ranges exactly where activity exists are the ones an off-by-one would get wrong.
            var instants = (await ReadAsync(query)).Select(activity => activity.Timestamp).Prepend(query.Range.From).Distinct();

            foreach (var instant in instants)
            {
                Assert.Empty(await ReadAsync(query with { Range = new TimeRange(instant, instant) }));
            }
        }
    }

    private Task<IReadOnlyList<ActivityEvent>> ReadAsync(ActivityQuery query) =>
        CreateSource().GetActivityAsync(query, CancellationToken.None);
}
