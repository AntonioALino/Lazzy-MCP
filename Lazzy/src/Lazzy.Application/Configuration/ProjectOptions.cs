namespace Lazzy.Application.Configuration;

/// <summary>A project as configured: how people call it and where it lives.</summary>
public sealed record ProjectOptions
{
    /// <summary>The project's name, as shown in answers.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Other names people type for the project. Matched without regard to case.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>Where the project lives, each written as <c>provider:location</c>.</summary>
    public IReadOnlyList<string> Targets { get; init; } = [];
}
