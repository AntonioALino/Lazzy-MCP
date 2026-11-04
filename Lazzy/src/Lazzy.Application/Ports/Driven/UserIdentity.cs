namespace Lazzy.Application.Ports.Driven;

/// <summary>Who the user is on one provider.</summary>
/// <param name="Login">The user's login or unique name, when configured.</param>
/// <param name="Emails">Extra emails that identify the user's commits.</param>
public sealed record UserIdentity(string? Login, IReadOnlyList<string> Emails);
