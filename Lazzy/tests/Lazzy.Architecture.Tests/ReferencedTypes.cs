using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Lazzy.Architecture.Tests;

/// <summary>A type that one assembly uses from another, read from the metadata of the using assembly.</summary>
internal sealed record ReferencedType(string Assembly, string? Namespace, string FullName);

/// <summary>
/// Reads every type an assembly references from other assemblies. The compiler records a reference for each type
/// used anywhere (signatures, method bodies, attributes), so nothing escapes this list.
/// </summary>
internal static class ReferencedTypes
{
    public static IReadOnlyList<ReferencedType> Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        using var stream = File.OpenRead(assembly.Location);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        return [.. reader.TypeReferences.Select(handle => Describe(reader, handle)).OfType<ReferencedType>()];
    }

    private static ReferencedType? Describe(MetadataReader reader, TypeReferenceHandle handle)
    {
        var type = reader.GetTypeReference(handle);
        var name = reader.GetString(type.Name);

        // A nested type is scoped by its declaring type: it lives wherever the outermost type lives.
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            var outer = Describe(reader, (TypeReferenceHandle)type.ResolutionScope);
            return outer is null ? null : outer with { FullName = outer.FullName + "+" + name };
        }

        if (type.ResolutionScope.Kind != HandleKind.AssemblyReference)
        {
            return null;
        }

        var assemblyName = reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope).Name);
        var typeNamespace = reader.GetString(type.Namespace);
        var fullName = typeNamespace.Length == 0 ? name : typeNamespace + "." + name;

        return new ReferencedType(assemblyName, typeNamespace, fullName);
    }
}
