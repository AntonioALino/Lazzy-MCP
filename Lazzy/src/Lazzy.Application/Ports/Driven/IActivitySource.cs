using Lazzy.Domain.ValueObjects;

namespace Lazzy.Application.Ports.Driven;

/// <summary>
/// Somewhere Lazzy reads the user's activity from, such as a git host or a board. Read-only.
/// Implemented by driven adapters, one per provider.
/// </summary>
public interface IActivitySource
{
    /// <summary>
    /// The provider id: a short lowercase token, the part before <c>:</c> in a target
    /// (<c>github</c> in <c>github:my-org/my-repo</c>).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// <see langword="true"/> when the source cannot work out who the user is from the token alone,
    /// so the configuration must name the user.
    /// </summary>
    bool RequiresIdentity { get; }

    /// <summary>The user's activity on one target, inside the query's range.</summary>
    /// <exception cref="ActivitySourceException">An expected failure, with a message the user can act on.</exception>
    Task<IReadOnlyList<ActivityEvent>> GetActivityAsync(ActivityQuery query, CancellationToken cancellationToken);
}
