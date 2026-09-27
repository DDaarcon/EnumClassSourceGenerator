using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace EnumClasses.SourceGenerators.Schema.Help;

internal static class SyntaxExtensions
{
    extension (SyntaxList<AttributeListSyntax> attributesList)
    {
        public bool TryGetByName(string fullyQualifiedName, SemanticModel semanticModel, CancellationToken cancellationToken, out AttributeSyntax attribute)
        {
            attribute = attributesList.GetByName(fullyQualifiedName, semanticModel, cancellationToken)!;
            return attribute is not null;
        }

        public AttributeSyntax? GetByName(string fullyQualifiedName, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var attributes = attributesList.SelectMany(x => x.Attributes).ToArray();

            if (attributes.Length == 0)
                return null;

            foreach (var attribute in attributes)
            {
                var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol;
                if (symbol is null)
                    continue;

                var symbolName = symbol
                    .ContainingSymbol // from constructor to actual attribute
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (FullyQualifiedNamesComparer.AreEqual(symbolName, fullyQualifiedName))
                    return attribute;
            }

            return null;
        }
    }


    extension (ImmutableArray<KeyValuePair<string, TypedConstant>> arguments)
    {
        public bool TryGetBooleanProperty(string propertyName, out bool result)
        {
            result = false;

            var arg = arguments.FirstOrDefault(x => x.Key == propertyName).Value;

            if (arg.Kind is TypedConstantKind.Error)
                return false;

            result = ((bool?)arg.Value).GetValueOrDefault();
            return true;
        }

        public bool? GetBooleanProperty(string propertyName)
        {
            var arg = arguments.FirstOrDefault(x => x.Key == propertyName).Value;

            if (arg.Kind is TypedConstantKind.Error)
                return null;

            return (bool?)arg.Value;
        }


        public bool TryGetEnumProperty<TEnum>(string propertyName, out TEnum result)
            where TEnum : struct, Enum
        {
            result = default;

            var arg = arguments.FirstOrDefault(x => x.Key == propertyName).Value;

            if (arg.Kind is TypedConstantKind.Error)
                return false;

            var converted = (int?)arg.Value;

            if (!converted.HasValue)
                return false;

            if (!Enum.IsDefined(typeof(TEnum), converted.Value))
                return false;

            result = (TEnum)(object)converted.Value;
            return true;
        }

        public TEnum? GetEnumProperty<TEnum>(string propertyName)
            where TEnum : struct, Enum
        {
            var arg = arguments.FirstOrDefault(x => x.Key == propertyName).Value;

            if (arg.Kind is TypedConstantKind.Error)
                return null;

            var converted = (int?)arg.Value;

            if (!converted.HasValue)
                return null;

            if (!Enum.IsDefined(typeof(TEnum), converted.Value))
                return null;

            return (TEnum)(object)converted.Value;
        }
    }
}
