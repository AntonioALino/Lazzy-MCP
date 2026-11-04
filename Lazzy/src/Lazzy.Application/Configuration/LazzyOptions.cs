using System.Collections.ObjectModel;

namespace Lazzy.Application.Configuration;

/// <summary>
/// The configuration of one Lazzy server, as written in <c>lazzy.yaml</c>. Values are kept as the user wrote them;
/// <see cref="Validation.LazzyOptionsValidator"/> decides whether they make sense.
/// </summary>
public sealed record LazzyOptions
{
    /// <summary>IANA time zone id that decides what "yesterday" means; <see langword="null"/> uses this machine's.</summary>
    public string? TimeZone { get; init; }

    /// <summary>Non-working days, each in <c>yyyy-MM-dd</c>.</summary>
    public IReadOnlyList<string> Holidays { get; init; } = [];

    /// <summary>
    /// Who "me" is on each provider, keyed by provider id (the part before <c>:</c> in a target).
    /// A provider without an entry discovers the user from its token, when it can.
    /// </summary>
    public IReadOnlyDictionary<string, IdentityOptions> Identity { get; init; } =
        ReadOnlyDictionary<string, IdentityOptions>.Empty;

    /// <summary>The projects this server knows about. Nothing outside them is ever queried.</summary>
    public IReadOnlyList<ProjectOptions> Projects { get; init; } = [];
}
