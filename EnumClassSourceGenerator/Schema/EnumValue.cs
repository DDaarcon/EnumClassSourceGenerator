using EnumClasses.SourceGenerators.Schema;
using EnumClasses.SourceGenerators.Schema.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace EnumClassSourceGenerator.Schema;

internal static class EnumValue
{
    internal record struct CollectResult(
        Definition[] Definitions);

    /// <param name="Name">Name as declared in code</param>
    /// <param name="NormalizedName">Displayable name, e.g. without '@' sign</param>
    /// <param name="FullyQualifiedCustomType"></param>
    /// <param name="Location"></param>
    /// <param name="HasInvalidType"></param>
    /// <param name="HasInvalidAccessors"></param>
    internal record struct Definition(
        string Name,
        string NormalizedName,
        string? FullyQualifiedCustomType,
        Location Location,
        bool HasInvalidType = false,
        bool HasInvalidAccessors = false)
    {
        public readonly bool IsValid => !HasInvalidType && !HasInvalidAccessors;
    }


    public static CollectResult CollectDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var fieldDefinitionsCollection = CollectFieldDefinitions(component, semanticModel, token);
        var propertyDefinitionsCollection = CollectPropertyDefinitions(component, semanticModel, token);

        var definitionsInDeclarationOrder = fieldDefinitionsCollection.Definitions
            .Concat(propertyDefinitionsCollection.Definitions)
            .OrderBy(definition => definition.Location.SourceSpan.Start)
            .ToArray();

        return new CollectResult(
            Definitions: definitionsInDeclarationOrder);
    }


    private static readonly SyntaxKind[] _requiredModifiersForEnumProperties = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.StaticKeyword
    ];
    private static CollectResult CollectPropertyDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;

        var definitions = component.ChildNodes()
            .Where(x => x.IsKind(SyntaxKind.PropertyDeclaration))
            .OfType<PropertyDeclarationSyntax>()
            .Where(propertySyntax =>
            {
                if (propertySyntax.AttributeLists.TryGetByName(SchemaConsts.AttributeNames.EnumClassIgnore, out _))
                    return false;

                var fieldTokenKinds = propertySyntax.ChildTokens().Select(x => x.Kind());
                return _requiredModifiersForEnumProperties.All(xx => fieldTokenKinds.Contains(xx));
            })
            .Select(propertySyntax =>
            {

                var fieldType = propertySyntax.Type;
                ITypeSymbol fieldTypeSymbol = semanticModel.GetTypeInfo(fieldType, token).Type!;

                var name = propertySyntax.Identifier.Text;
                var normalizedName = propertySyntax.Identifier.ValueText;
                var location = propertySyntax.GetLocation();

                if (propertySyntax.AccessorList!.Accessors.Any(x => x.IsKind(SyntaxKind.SetAccessorDeclaration) || x.IsKind(SyntaxKind.InitAccessorDeclaration))
                    || propertySyntax.AccessorList.Accessors.All(x => !x.IsKind(SyntaxKind.GetAccessorDeclaration)))
                {
                    return new Definition(
                        Name: name,
                        NormalizedName: normalizedName,
                        FullyQualifiedCustomType: null,
                        Location: location,
                        HasInvalidAccessors: true);
                }

                if (SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol))
                    return new Definition(
                        Name: name,
                        NormalizedName: normalizedName,
                        FullyQualifiedCustomType: null,
                        Location: location);

                if (IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel))
                    return new Definition(
                        Name: name,
                        NormalizedName: normalizedName,
                        FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        Location: location);

                return new Definition(
                    Name: name,
                    NormalizedName: normalizedName,
                    FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Location: location,
                    HasInvalidType: true);
            })
            .ToArray();

        return new CollectResult(
            Definitions: definitions);
    }


    private static readonly SyntaxKind[] _requiredModifiersForEnumFields = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.StaticKeyword
    ];
    private static CollectResult CollectFieldDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;

        var definitions = component.ChildNodes()
            .Where(x => x.IsKind(SyntaxKind.FieldDeclaration))
            .OfType<FieldDeclarationSyntax>()
            .Where(fieldSyntax =>
            {
                if (fieldSyntax.AttributeLists.TryGetByName(SchemaConsts.AttributeNames.EnumClassIgnore, out _))
                    return false;

                var fieldTokenKinds = fieldSyntax.ChildTokens().Select(x => x.Kind());
                return _requiredModifiersForEnumFields.All(xx => fieldTokenKinds.Contains(xx));
            })
            .SelectMany(fieldSyntax =>
            {
                var fieldType = fieldSyntax.Declaration.Type;
                ITypeSymbol fieldTypeSymbol = semanticModel.GetTypeInfo(fieldType, token).Type!;

                return fieldSyntax.Declaration.Variables.Select(variable =>
                {
                    var name = variable.Identifier.Text;
                    var normalizedName = variable.Identifier.ValueText;
                    var location = variable.GetLocation();

                    if (SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol))
                        return new Definition(
                            Name: name,
                            NormalizedName: normalizedName,
                            FullyQualifiedCustomType: null,
                            Location: location);

                    if (IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel))
                        return new Definition(
                            Name: name,
                            NormalizedName: normalizedName,
                            FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            Location: location);

                    return new Definition(
                        Name: name,
                        NormalizedName: normalizedName,
                        FullyQualifiedCustomType: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Location: location,
                        HasInvalidType: true);
                });
            })
            .ToArray();

        return new CollectResult(
            Definitions: definitions);
    }


    private static bool IsAssignable(ITypeSymbol fieldType, ITypeSymbol baseType, SemanticModel semanticModel)
    {
        var compilation = semanticModel.Compilation;
        var conversion = compilation.ClassifyConversion(fieldType, baseType);

        return conversion.Exists && conversion.IsImplicit;
    }
}
