using System.Reflection;
using ModelContextProtocol.Server;
using static Lazzy.Architecture.Tests.LazzyAssemblies;

namespace Lazzy.Architecture.Tests;

/// <summary>
/// The dependency rules of the hexagonal architecture (ADR 0021, AGENTS.md).
/// If one of these fails, fix the dependency. Do not weaken the rule.
/// </summary>
public sealed class DependencyRulesTests
{
    private static readonly string[] VendorPrefixes = ["ModelContextProtocol", "Microsoft.Extensions.Hosting", "YamlDotNet"];

    [Fact]
    public void DomainDependsOnNothingInLazzy()
    {
        Assert.Empty(LazzyReferencesOf(DomainAssembly));
    }

    [Fact]
    public void ApplicationDependsOnlyOnDomain()
    {
        Assert.All(LazzyReferencesOf(ApplicationAssembly), name => Assert.Equal(DomainName, name));
    }

    [Theory]
    [InlineData(DomainName)]
    [InlineData(ApplicationName)]
    public void HexagonKnowsNoProtocolOrHostingLibrary(string assemblyName)
    {
        var vendorReferences = Assembly.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => VendorPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));

        Assert.Empty(vendorReferences);
    }

    [Fact]
    public void EveryDrivenAdapterIsInspected()
    {
        Assert.NotEmpty(DrivenAdapters);
    }

    [Fact]
    public void DrivenAdaptersDependOnlyOnDomainAndApplication()
    {
        var violations =
            from adapter in DrivenAdapters
            from name in LazzyReferencesOf(adapter)
            where name is not (DomainName or ApplicationName)
            select $"{adapter.GetName().Name} references {name}";

        Assert.Empty(violations);
    }

    [Fact]
    public void DrivenAdaptersUseOnlyTheDrivenPortsOfApplication()
    {
        var violations =
            from adapter in DrivenAdapters
            from type in ReferencedTypes.Of(adapter)
            where string.Equals(type.Assembly, ApplicationName, StringComparison.Ordinal)
                && !IsInNamespace(type.Namespace, DrivenPortsNamespace)
            select $"{adapter.GetName().Name} uses {type.FullName}";

        Assert.Empty(violations);
    }

    [Fact]
    public void OnlyTheCompositionRootUsesUseCasesOrDrivenAdapters()
    {
        var drivenAdapters = DrivenAdapters.ToHashSet();

        var violations =
            from type in McpAssembly.GetTypes()
            where !IsCompositionRoot(type)
            from used in TypeUsage.Of(type)
            where IsInNamespace(used.Namespace, UseCasesNamespace) || drivenAdapters.Contains(used.Assembly)
            select $"{type.FullName} uses {used.FullName}";

        Assert.Empty(violations);
    }

    [Fact]
    public void McpToolsAndPromptsDependOnDrivingPortsNotDrivenPorts()
    {
        var violations =
            from type in ToolsAndPrompts()
            from used in TypeUsage.Of(type)
            where IsInNamespace(used.Namespace, DrivenPortsNamespace)
            select $"{type.FullName} uses {used.FullName}";

        Assert.Empty(violations);
    }

    [Fact]
    public void McpToolsAreFound()
    {
        Assert.NotEmpty(ToolsAndPrompts());
    }

    /// <summary>Program.cs, compiled from top-level statements, together with its lambdas and state machines.</summary>
    private static bool IsCompositionRoot(Type type)
    {
        var outermost = TypeUsage.Outermost(type);
        return outermost.Namespace is null && string.Equals(outermost.Name, "Program", StringComparison.Ordinal);
    }

    private static Type[] ToolsAndPrompts() =>
    [
        .. McpAssembly.GetTypes().Where(type =>
        {
            var outermost = TypeUsage.Outermost(type);
            return outermost.IsDefined(typeof(McpServerToolTypeAttribute))
                || outermost.IsDefined(typeof(McpServerPromptTypeAttribute));
        }),
    ];
}
