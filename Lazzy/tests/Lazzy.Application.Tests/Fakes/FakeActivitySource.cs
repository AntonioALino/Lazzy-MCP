using System.Collections.Concurrent;
using Lazzy.Application.Ports.Driven;
using Lazzy.Domain.ValueObjects;

namespace Lazzy.Application.Tests.Fakes;

/// <summary>An activity source that answers from code and records every query. No network.</summary>
internal sealed class FakeActivitySource(
    string id,
    Func<ActivityQuery, CancellationToken, Task<IReadOnlyList<ActivityEvent>>> answer,
    bool requiresIdentity = false) : IActivitySource
{
    private readonly ConcurrentQueue<ActivityQuery> _queries = new();

    public string Id => id;

    public bool RequiresIdentity => requiresIdentity;

    public IReadOnlyCollection<ActivityQuery> Queries => _queries;

    /// <summary>A source that returns <paramref name="events"/> for every target.</summary>
    public static FakeActivitySource Returning(string id, params ActivityEvent[] events) =>
        new(id, (_, _) => Task.FromResult<IReadOnlyList<ActivityEvent>>(events));

    /// <summary>A source whose every call fails with <paramref name="exception"/>.</summary>
    public static FakeActivitySource Throwing(string id, Exception exception) =>
        new(id, (_, _) => Task.FromException<IReadOnlyList<ActivityEvent>>(exception));

    public Task<IReadOnlyList<ActivityEvent>> GetActivityAsync(ActivityQuery query, CancellationToken cancellationToken)
    {
        _queries.Enqueue(query);
        return answer(query, cancellationToken);
    }
}
