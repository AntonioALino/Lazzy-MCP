using System.Reflection;
using System.Reflection.Emit;

namespace Lazzy.Architecture.Tests;

/// <summary>
/// Finds the types one type uses: in its signatures and in the IL of its methods. Unlike assembly references,
/// this tells which type inside an assembly is responsible for a dependency.
/// </summary>
internal static class TypeUsage
{
    private const BindingFlags Declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => field.GetValue(null))
        .OfType<OpCode>()
        .ToDictionary(opCode => opCode.Value);

    public static IReadOnlySet<Type> Of(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var used = new HashSet<Type>();

        Add(used, type.BaseType);
        foreach (var contract in type.GetInterfaces())
        {
            Add(used, contract);
        }

        foreach (var field in type.GetFields(Declared))
        {
            Add(used, field.FieldType);
        }

        foreach (var property in type.GetProperties(Declared))
        {
            Add(used, property.PropertyType);
        }

        foreach (var method in type.GetMethods(Declared).Cast<MethodBase>().Concat(type.GetConstructors(Declared)))
        {
            if (method is MethodInfo info)
            {
                Add(used, info.ReturnType);
            }

            foreach (var parameter in method.GetParameters())
            {
                Add(used, parameter.ParameterType);
            }

            foreach (var member in MembersUsedInBody(method))
            {
                Add(used, member as Type ?? member.DeclaringType);
            }
        }

        return used;
    }

    /// <summary>The outermost declaring type, so a lambda or async state machine belongs to the type that wrote it.</summary>
    public static Type Outermost(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        while (type.DeclaringType is { } declaring)
        {
            type = declaring;
        }

        return type;
    }

    private static void Add(HashSet<Type> used, Type? type)
    {
        if (type is null || type.IsGenericParameter)
        {
            return;
        }

        if (type.HasElementType)
        {
            Add(used, type.GetElementType());
            return;
        }

        if (type.IsConstructedGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                Add(used, argument);
            }

            type = type.GetGenericTypeDefinition();
        }

        used.Add(type);
    }

    private static IEnumerable<MemberInfo> MembersUsedInBody(MethodBase method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            yield break;
        }

        var typeArguments = method.DeclaringType is { IsGenericType: true } owner ? owner.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;

        var position = 0;
        while (position < il.Length)
        {
            OpCode opCode;
            if (il[position] == 0xFE)
            {
                opCode = OpCodesByValue[unchecked((short)(0xFE00 | il[position + 1]))];
                position += 2;
            }
            else
            {
                opCode = OpCodesByValue[il[position]];
                position += 1;
            }

            if (opCode.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok)
            {
                var token = BitConverter.ToInt32(il, position);
                if (method.Module.ResolveMember(token, typeArguments, methodArguments) is { } member)
                {
                    yield return member;
                }
            }

            position += OperandSize(opCode.OperandType, il, position);
        }
    }

    private static int OperandSize(OperandType operandType, byte[] il, int position) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, position)),
        _ => 4,
    };
}
