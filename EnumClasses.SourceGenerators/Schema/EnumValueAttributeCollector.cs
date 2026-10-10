using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumValueAttributeCollector
{
    internal record struct Definition(
        int? EnumIndex,
        string? SerializedName);

    internal static Definition Collect(
        AttributeSyntax enumClassValueAttribute,
        SemanticModel semanticModel,
        CancellationToken token)
    {
        var arguments = enumClassValueAttribute.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count < 1)
            return default;

        var constructor = semanticModel.GetSymbolInfo(enumClassValueAttribute, token).Symbol as IMethodSymbol;
        if (constructor is null)
            return default;

        int? enumIndex = null;
        string? serializedName = null;

        for (var argumentIndex = 0; argumentIndex < arguments.Value.Count; argumentIndex++)
        {
            var argument = arguments.Value[argumentIndex];
            var constant = semanticModel.GetConstantValue(argument.Expression, token);
            if (!constant.HasValue)
                continue;

            if (argument.NameEquals is not null)
            {
                CollectValue(argument.NameEquals.Name.Identifier.ValueText, constant.Value);
                continue;
            }

            var parameterName = argument.NameColon?.Name.Identifier.ValueText
                ?? (argumentIndex < constructor.Parameters.Length
                    ? constructor.Parameters[argumentIndex].Name
                    : null);

            CollectValue(parameterName, constant.Value);
        }

        return new Definition(enumIndex, serializedName);

        void CollectValue(string? memberName, object? value)
        {
            switch (memberName)
            {
                case "enumIndex" or nameof(EnumClassValueAttribute.EnumIndex) when value is int index:
                    enumIndex = index;
                    break;

                case "serializedName" or nameof(EnumClassValueAttribute.SerializedName) when value is string name:
                    serializedName = name;
                    break;
            }
        }
    }
}
