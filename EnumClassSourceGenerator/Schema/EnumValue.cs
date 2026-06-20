using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace EnumClassSourceGenerator.Schema;

internal static class EnumValue
{
    internal record struct CollectionResult(
        Definition[] Definitions,
        bool HasAtLeastOneInvalidValueType = false,
        bool HasAtLeastOneInvalidValueAccessor = false);

    internal record struct Definition(
        string Name,
        string? FullyQualifiedCustomType,
        Location Location,
        bool HasInvalidType = false,
        bool HasInvalidAccessors = false)
    {
        public readonly bool IsValid => !HasInvalidType && !HasInvalidAccessors;
    }


    public static CollectionResult CollectDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var fieldDefinitionsCollection = CollectFieldDefinitions(component, semanticModel, token);
        var propertyDefinitionsCollection = CollectPropertyDefinitions(component, semanticModel, token);

        return new CollectionResult(
            Definitions: [.. fieldDefinitionsCollection.Definitions, .. propertyDefinitionsCollection.Definitions],
            HasAtLeastOneInvalidValueType: fieldDefinitionsCollection.HasAtLeastOneInvalidValueType || propertyDefinitionsCollection.HasAtLeastOneInvalidValueType,
            HasAtLeastOneInvalidValueAccessor: fieldDefinitionsCollection.HasAtLeastOneInvalidValueAccessor || propertyDefinitionsCollection.HasAtLeastOneInvalidValueAccessor);
    }


    private static readonly SyntaxKind[] _requiredModifiersForEnumProperties = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.StaticKeyword
    ];
    private static CollectionResult CollectPropertyDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;

        bool hasAnyInvalidType = false;
        bool hasAnyInvalidAccessor = false;

        var definitions = component.ChildNodes()
            .Where(x => x.IsKind(SyntaxKind.PropertyDeclaration))
            .OfType<PropertyDeclarationSyntax>()
            .Where(x =>
            {
                var fieldTokenKinds = x.ChildTokens().Select(x => x.Kind());
                return _requiredModifiersForEnumProperties.All(xx => fieldTokenKinds.Contains(xx));
            })
            .Select(propertySyntax =>
            {

                var fieldType = propertySyntax.Type;
                ITypeSymbol fieldTypeSymbol = semanticModel.GetTypeInfo(fieldType, token).Type!;

                var name = propertySyntax.Identifier.ValueText;
                var location = propertySyntax.GetLocation();

                if (propertySyntax.AccessorList!.Accessors.Any(x => x.IsKind(SyntaxKind.SetAccessorDeclaration) || x.IsKind(SyntaxKind.InitAccessorDeclaration))
                    || propertySyntax.AccessorList.Accessors.All(x => !x.IsKind(SyntaxKind.GetAccessorDeclaration)))
                {
                    hasAnyInvalidAccessor = true;

                    return new Definition(
                        Name: name,
                        FullyQualifiedCustomType: null,
                        Location: location,
                        HasInvalidAccessors: true);
                }

                if (SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol))
                    return new Definition(
                        Name: name,
                        FullyQualifiedCustomType: null,
                        Location: location);

                if (IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel))
                    return new Definition(
                        Name: name,
                        FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        Location: location);

                hasAnyInvalidType = true;

                return new Definition(
                    Name: name,
                    FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Location: location,
                    HasInvalidType: true);
            })
            .ToArray();

        return new CollectionResult(
            Definitions: definitions,
            HasAtLeastOneInvalidValueType: hasAnyInvalidType,
            HasAtLeastOneInvalidValueAccessor: hasAnyInvalidAccessor);
    }


    private static readonly SyntaxKind[] _requiredModifiersForEnumFields = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.StaticKeyword
    ];
    private static CollectionResult CollectFieldDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;

        bool hasAnyInvalidType = false;

        var definitions = component.ChildNodes()
            .Where(x => x.IsKind(SyntaxKind.FieldDeclaration))
            .OfType<FieldDeclarationSyntax>()
            .Where(x =>
            {
                var fieldTokenKinds = x.ChildTokens().Select(x => x.Kind());
                return _requiredModifiersForEnumFields.All(xx => fieldTokenKinds.Contains(xx));
            })
            .Select(x =>
            {
                var fieldType = x.Declaration.Type;
                ITypeSymbol fieldTypeSymbol = semanticModel.GetTypeInfo(fieldType, token).Type!;

                var name = x.Declaration.Variables.First().Identifier.Text;
                var location = x.GetLocation();

                if (SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol))
                    return new Definition(
                        Name: name,
                        FullyQualifiedCustomType: null,
                        Location: location);

                if (IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel))
                    return new Definition(
                        Name: name,
                        FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Location: location);

                hasAnyInvalidType = true;
                return new Definition(
                    Name: name,
                    FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Location: location,
                    HasInvalidType: true);
            })
            .ToArray();

        return new CollectionResult(
            Definitions: definitions,
            HasAtLeastOneInvalidValueType: hasAnyInvalidType);
    }


    private static bool IsAssignable(ITypeSymbol fieldType, ITypeSymbol baseType, SemanticModel semanticModel)
    {
        var compilation = semanticModel.Compilation;
        var conversion = compilation.ClassifyConversion(fieldType, baseType);

        return conversion.Exists && conversion.IsImplicit;
    }
}
