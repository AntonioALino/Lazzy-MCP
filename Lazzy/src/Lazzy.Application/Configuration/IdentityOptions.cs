namespace Lazzy.Application.Configuration;

/// <summary>Who the user is on one provider.</summary>
public sealed record IdentityOptions
{
    /// <summary>The user's login or unique name on the provider.</summary>
    public string? Login { get; init; }

    /// <summary>
    /// Extra emails that identify the user's commits, for commits made with an email
    /// that is not linked to the provider account.
    /// </summary>
    public IReadOnlyList<string> Emails { get; init; } = [];
}
