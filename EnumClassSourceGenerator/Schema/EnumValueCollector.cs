using EnumClasses.SourceGenerators.Schema.Help;
using EnumClasses.SourceGenerators.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumValueCollector
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
        Location Location,
        string? FullyQualifiedCustomType = null,
        IEnumerable<Diagnostic>? DiagnosticReports = null)
    {
        public readonly bool IsValid => DiagnosticReports is null || !DiagnosticReports.Any();
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
        var declarationName = component.Identifier.ValueText;

        var definitions = component.ChildNodes()
            .Where(x => x.IsKind(SyntaxKind.PropertyDeclaration))
            .OfType<PropertyDeclarationSyntax>()
            .Where(propertySyntax =>
            {
                if (propertySyntax.AttributeLists.TryGetByName(SchemaConsts.AttributeNames.EnumClassIgnoreFullyQualified, semanticModel, token, out _))
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

                var reports = new List<Diagnostic>();

                var isNameNotReserved = !EnumClassDeclarationTemplate.CheckIfNameIsReserved(name);
                if (!isNameNotReserved)
                    reports.Add(Diagnostics.ReservedKeywordUsedForValue(location, name, declarationName));

                var areAccessorsCorrect = IsInitializedGetOnlyAutoProperty(propertySyntax);
                if (!areAccessorsCorrect)
                    reports.Add(Diagnostics.InvalidEnumValueAccessors(location, name, declarationName));

                var isOfMainType = SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol);

                var isOfMainOrDerivedType = isOfMainType || IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel);
                if (!isOfMainOrDerivedType)
                    reports.Add(Diagnostics.InvalidEnumValueType(location, name, declarationName, valueTypeName: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));


                return new Definition(
                    Name: name,
                    NormalizedName: normalizedName,
                    Location: location,
                    FullyQualifiedCustomType: !isOfMainType ? fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
                    DiagnosticReports: reports);
            })
            .ToArray();

        return new CollectResult(
            Definitions: definitions);

        static bool IsInitializedGetOnlyAutoProperty(
            PropertyDeclarationSyntax property)
        {
            if (property.Initializer is null ||
                property.ExpressionBody is not null ||
                property.AccessorList is null)
            {
                return false;
            }

            var accessors = property.AccessorList.Accessors;

            if (accessors.Count != 1)
                return false;

            var getter = accessors[0];

            return getter.IsKind(SyntaxKind.GetAccessorDeclaration) &&
                   getter.Body is null &&
                   getter.ExpressionBody is null &&
                   getter.SemicolonToken.IsKind(SyntaxKind.SemicolonToken);
        }
    }


    private static readonly SyntaxKind[] _requiredModifiersForEnumFields = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.StaticKeyword
    ];
    private static CollectResult CollectFieldDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;
        var declarationName = component.Identifier.ValueText;

        var definitions = component.ChildNodes()
            .Where(x => x.IsKind(SyntaxKind.FieldDeclaration))
            .OfType<FieldDeclarationSyntax>()
            .Where(fieldSyntax =>
            {
                if (fieldSyntax.AttributeLists.TryGetByName(SchemaConsts.AttributeNames.EnumClassIgnoreFullyQualified, semanticModel, token, out _))
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


                    var reports = new List<Diagnostic>();

                    var isNameNotReserved = !EnumClassDeclarationTemplate.CheckIfNameIsReserved(name);
                    if (!isNameNotReserved)
                        reports.Add(Diagnostics.ReservedKeywordUsedForValue(location, name, declarationName));

                    var isOfMainType = SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol);

                    var isOfMainOrDerivedType = isOfMainType || IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel);
                    if (!isOfMainOrDerivedType)
                        reports.Add(Diagnostics.InvalidEnumValueType(location, name, declarationName, valueTypeName: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

                    return new Definition(
                        Name: name,
                        NormalizedName: normalizedName,
                        Location: location,
                        FullyQualifiedCustomType: !isOfMainType ? fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
                        DiagnosticReports: reports);
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
