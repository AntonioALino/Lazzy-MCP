using System.Diagnostics.CodeAnalysis;

namespace Lazzy.Domain.ValueObjects;

/// <summary>
/// A place where a project lives on one provider, written as <c>provider:location</c>,
/// for example <c>github:my-org/my-repo</c>. The domain does not interpret either part:
/// each provider decides what its locations mean.
/// </summary>
public sealed record Target
{
    /// <summary>The character between the provider and the location.</summary>
    public const char Separator = ':';

    /// <summary>The accepted format, in words, for error messages.</summary>
    public const string Format = "provider:location, for example 'github:my-org/my-repo'";

    /// <summary>Creates a target, trimming the spaces around each part.</summary>
    /// <exception cref="ArgumentException">
    /// A part is empty, or the provider contains a space or <see cref="Separator"/>.
    /// </exception>
    public Target(string provider, string location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(location);

        provider = provider.Trim();
        if (provider.Any(character => character == Separator || char.IsWhiteSpace(character)))
        {
            throw new ArgumentException(
                $"The provider '{provider}' must be a single word without spaces or '{Separator}', such as 'github'.",
                nameof(provider));
        }

        Provider = provider;
        Location = location.Trim();
    }

    /// <summary>The provider id, such as <c>github</c>.</summary>
    public string Provider { get; }

    /// <summary>Where the project lives on that provider, such as <c>my-org/my-repo</c>.</summary>
    public string Location { get; }

    /// <summary>
    /// Parses <c>provider:location</c>. Only the first <see cref="Separator"/> splits,
    /// so the location may contain more of them.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? value, [NotNullWhen(true)] out Target? target)
    {
        target = null;

        var separatorIndex = value?.IndexOf(Separator, StringComparison.Ordinal) ?? -1;
        if (value is null || separatorIndex < 0)
        {
            return false;
        }

        var provider = value[..separatorIndex].Trim();
        var location = value[(separatorIndex + 1)..].Trim();
        if (provider.Length == 0 || location.Length == 0 || provider.Any(char.IsWhiteSpace))
        {
            return false;
        }

        target = new Target(provider, location);
        return true;
    }

    /// <summary>The target as written in configuration: <c>provider:location</c>.</summary>
    public override string ToString() => $"{Provider}{Separator}{Location}";
}
