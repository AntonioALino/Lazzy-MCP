using Lazzy.Domain;

namespace Lazzy.Application.Ports.Driven;

/// <summary>What to ask an activity source for: the user's activity on one target, inside one range.</summary>
/// <param name="Target">Where to look. Its <see cref="Target.Location"/> is interpreted by the source.</param>
/// <param name="Range">When to look, half-open. Return only events inside it.</param>
/// <param name="Identity">Who the user is, when configured; <see langword="null"/> means "the user of the token".</param>
public sealed record ActivityQuery(Target Target, TimeRange Range, UserIdentity? Identity);
