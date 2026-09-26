using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
}
