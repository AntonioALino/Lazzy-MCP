using System.Reflection;

namespace Lazzy.Architecture.Tests;

/// <summary>The production assemblies the dependency rules inspect, loaded from the test output folder.</summary>
internal static class LazzyAssemblies
{
    public const string DomainName = "Lazzy.Domain";
    public const string ApplicationName = "Lazzy.Application";
    public const string McpName = "Lazzy.Adapters.Mcp";

    public const string UseCasesNamespace = "Lazzy.Application.UseCases";
    public const string DrivenPortsNamespace = "Lazzy.Application.Ports.Driven";

    private const string AdapterPrefix = "Lazzy.Adapters.";

    public static Assembly DomainAssembly { get; } = Assembly.Load(DomainName);

    public static Assembly ApplicationAssembly { get; } = Assembly.Load(ApplicationName);

    public static Assembly McpAssembly { get; } = Assembly.Load(McpName);

    /// <summary>
    /// Every driven adapter copied next to the tests: any <c>Lazzy.Adapters.*</c> assembly except the MCP adapter.
    /// </summary>
    public static IReadOnlyList<Assembly> DrivenAdapters { get; } =
    [
        .. Directory.EnumerateFiles(AppContext.BaseDirectory, AdapterPrefix + "*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(name => !string.Equals(name, McpName, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(Assembly.Load),
    ];

    /// <summary>The names of the Lazzy assemblies that <paramref name="assembly"/> actually uses.</summary>
    public static IReadOnlyList<string> LazzyReferencesOf(Assembly assembly) =>
    [
        .. assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .Where(IsLazzy)
            .Order(StringComparer.Ordinal),
    ];

    public static bool IsInNamespace(string? typeNamespace, string expected) =>
        typeNamespace is not null
        && (string.Equals(typeNamespace, expected, StringComparison.Ordinal)
            || typeNamespace.StartsWith(expected + ".", StringComparison.Ordinal));

    private static bool IsLazzy(string name) =>
        string.Equals(name, "Lazzy", StringComparison.Ordinal) || name.StartsWith("Lazzy.", StringComparison.Ordinal);
}
