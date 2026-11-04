using Lazzy.Application.Configuration;

namespace Lazzy.Application.Services;

/// <summary>Finds a configured project by its name or one of its aliases, without regard to case.</summary>
public sealed class ProjectResolver
{
    private readonly Dictionary<string, ProjectOptions> _projects = new(StringComparer.OrdinalIgnoreCase);

    public ProjectResolver(LazzyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        foreach (var project in options.Projects)
        {
            foreach (var nameOrAlias in project.Aliases.Prepend(project.Name))
            {
                // A valid configuration has no clashes; if one slips through, the first project keeps the name.
                if (!string.IsNullOrWhiteSpace(nameOrAlias))
                {
                    _projects.TryAdd(nameOrAlias.Trim(), project);
                }
            }
        }
    }

    /// <summary>The project called <paramref name="nameOrAlias"/>, or <see langword="null"/> when there is none.</summary>
    public ProjectOptions? Resolve(string? nameOrAlias) =>
        string.IsNullOrWhiteSpace(nameOrAlias) ? null : _projects.GetValueOrDefault(nameOrAlias.Trim());
}
