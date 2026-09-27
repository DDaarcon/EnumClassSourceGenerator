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



    /// <summary>Describes a candidate enum-class value found in the target declaration.</summary>
    /// <param name="InternalIndex">The zero-based position in source declaration order.</param>
    /// <param name="ResolvedIndex">Index value resolved either from increment by 1 or from value provided by the user</param>
    /// <param name="ExplicitIndex">Index value provided by the user using from a compile-time constants</param>
    /// <param name="Name">The identifier as written in source.</param>
    /// <param name="NormalizedName">The identifier without contextual escaping, such as a leading <c>@</c>.</param>
    /// <param name="Location">The source location of the declaration.</param>
    /// <param name="FullyQualifiedCustomType">The fully qualified derived type, or <see langword="null"/> for the enum-class type itself.</param>
    /// <param name="IsEnumClassValueAttributeShared">Whether the attribute is applied to a field declaration containing multiple comma-separated values.</param>
    /// <param name="DiagnosticReports">Diagnostics associated with the candidate.</param>
    internal record struct Definition(
        int InternalIndex,
        int? ResolvedIndex,
        int? ExplicitIndex,
        string Name,
        string NormalizedName,
        Location Location,
        string? FullyQualifiedCustomType,
        bool IsEnumClassValueAttributeShared,
        IEnumerable<Diagnostic>? DiagnosticReports)
    {
        public readonly bool IsValid => DiagnosticReports is null || !DiagnosticReports.Any();
    }


    public static CollectResult CollectDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var fieldDefinitionsCollection = CollectFieldDefinitions(component, semanticModel, token);
        var propertyDefinitionsCollection = CollectPropertyDefinitions(component, semanticModel, token);

        var definitionsInDeclarationOrder = fieldDefinitionsCollection
            .Concat(propertyDefinitionsCollection)
            .OrderBy(definition => definition.Location.SourceSpan.Start)
            .ToArray();

        return new CollectResult(
            Definitions: ConstructDefinitionsFromOrderedData(definitionsInDeclarationOrder, declarationName: component.Identifier.ValueText));
    }


    private static readonly SyntaxKind[] _requiredModifiersForEnumProperties = [
        SyntaxKind.PublicKeyword,
        SyntaxKind.StaticKeyword
    ];
    private static CollectedValueData[] CollectPropertyDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;
        var declarationName = component.Identifier.ValueText;

        var valuesData = component.ChildNodes()
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
                var enumClassValueAttribute = propertySyntax.AttributeLists.GetByName(
                    SchemaConsts.AttributeNames.EnumClassValueFullyQualified,
                    semanticModel,
                    token);

                var explicitIndex = GetEnumIndex(enumClassValueAttribute, semanticModel, token);

                var fieldType = propertySyntax.Type;
                ITypeSymbol fieldTypeSymbol = semanticModel.GetTypeInfo(fieldType, token).Type!;

                var name = propertySyntax.Identifier.Text;
                var normalizedName = propertySyntax.Identifier.ValueText;
                var location = propertySyntax.GetLocation();

                var reports = new List<Diagnostic>();

                var isNameNotReserved = !EnumClassDeclarationTemplate.CheckIfNameIsReserved(name)
                    && !EnumClassDeclarationTemplate.CheckIfNameIsReserved(normalizedName);
                if (!isNameNotReserved)
                    reports.Add(Diagnostics.ReservedKeywordUsedForValue(location, name, declarationName));

                var areAccessorsCorrect = IsInitializedGetOnlyAutoProperty(propertySyntax);
                if (!areAccessorsCorrect)
                    reports.Add(Diagnostics.InvalidEnumValueAccessors(location, name, declarationName));

                var isOfMainType = SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol);

                var isOfMainOrDerivedType = isOfMainType || IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel);
                if (!isOfMainOrDerivedType)
                    reports.Add(Diagnostics.InvalidEnumValueType(location, name, declarationName, valueTypeName: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));


                return new CollectedValueData(
                    Name: name,
                    NormalizedName: normalizedName,
                    ExplicitIndex: explicitIndex,
                    Location: location,
                    FullyQualifiedCustomType: !isOfMainType ? fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
                    IsEnumClassValueAttributeShared: false,
                    DiagnosticReports: reports);
            })
            .ToArray();

        return valuesData;

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
    private static CollectedValueData[] CollectFieldDefinitions(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var componentTypeSymbol = (ITypeSymbol)semanticModel.GetDeclaredSymbol(component)!;
        var declarationName = component.Identifier.ValueText;

        var valuesData = component.ChildNodes()
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
                var enumClassValueAttribute = fieldSyntax.AttributeLists.GetByName(
                    SchemaConsts.AttributeNames.EnumClassValueFullyQualified,
                    semanticModel,
                    token);
                var isEnumClassValueAttributeShared = enumClassValueAttribute is not null
                    && fieldSyntax.Declaration.Variables.Count > 1;

                var explicitIndex = GetEnumIndex(enumClassValueAttribute, semanticModel, token);

                var fieldType = fieldSyntax.Declaration.Type;
                ITypeSymbol fieldTypeSymbol = semanticModel.GetTypeInfo(fieldType, token).Type!;

                return fieldSyntax.Declaration.Variables.Select(variable =>
                {
                    var name = variable.Identifier.Text;
                    var normalizedName = variable.Identifier.ValueText;
                    var location = variable.GetLocation();


                    var reports = new List<Diagnostic>();

                    var isNameNotReserved = !EnumClassDeclarationTemplate.CheckIfNameIsReserved(name)
                        && !EnumClassDeclarationTemplate.CheckIfNameIsReserved(normalizedName);
                    if (!isNameNotReserved)
                        reports.Add(Diagnostics.ReservedKeywordUsedForValue(location, name, declarationName));

                    if (variable.Initializer is null)
                        reports.Add(Diagnostics.UninitializedEnumValueField(location, name, declarationName));

                    var isOfMainType = SymbolEqualityComparer.Default.Equals(fieldTypeSymbol, componentTypeSymbol);

                    var isOfMainOrDerivedType = isOfMainType || IsAssignable(fieldTypeSymbol, componentTypeSymbol, semanticModel);
                    if (!isOfMainOrDerivedType)
                        reports.Add(Diagnostics.InvalidEnumValueType(location, name, declarationName, valueTypeName: fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

                    return new CollectedValueData(
                        Name: name,
                        NormalizedName: normalizedName,
                        ExplicitIndex: explicitIndex,
                        Location: location,
                        FullyQualifiedCustomType: !isOfMainType ? fieldTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
                        IsEnumClassValueAttributeShared: isEnumClassValueAttributeShared,
                        DiagnosticReports: reports);
                });
            })
            .ToArray();

        return valuesData;
    }


    private static bool IsAssignable(ITypeSymbol fieldType, ITypeSymbol baseType, SemanticModel semanticModel)
    {
        var compilation = semanticModel.Compilation;
        var conversion = compilation.ClassifyConversion(fieldType, baseType);

        return conversion.Exists && conversion.IsImplicit;
    }

    private static int? GetEnumIndex(
        AttributeSyntax? enumClassValueAttribute,
        SemanticModel semanticModel,
        CancellationToken token)
    {
        if (enumClassValueAttribute is null)
            return null;

        var arguments = enumClassValueAttribute.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count != 1)
            return null;

        var constantValue = semanticModel.GetConstantValue(arguments.Value[0].Expression, token);
        return constantValue.HasValue && constantValue.Value is int enumIndex
            ? enumIndex
            : null;
    }




    private static Definition[] ConstructDefinitionsFromOrderedData(CollectedValueData[] data, string declarationName)
    {
        var definitions = new Definition[data.Length];

        var internalIndexCounter = 0; // equivalent to array indexer

        var internalIndexesPerResolvedIndex = new Dictionary<int, List<int>>(data.Length);
        var resolvedIndexesInInternalIndexOrder = new int[data.Length];
        int resolvedIndexCounter = 0;

        while (internalIndexCounter < data.Length)
        {
            var valueData = data[internalIndexCounter];

            if (valueData.ExplicitIndex.HasValue)
            {
                resolvedIndexCounter = valueData.ExplicitIndex.Value;
            }

            if (internalIndexesPerResolvedIndex.ContainsKey(resolvedIndexCounter))
            {
                internalIndexesPerResolvedIndex[resolvedIndexCounter].Add(internalIndexCounter);
            }
            else
            {
                internalIndexesPerResolvedIndex[resolvedIndexCounter] = [internalIndexCounter];
            }

            resolvedIndexesInInternalIndexOrder[internalIndexCounter] = resolvedIndexCounter;


            internalIndexCounter++;

            if (resolvedIndexCounter < int.MaxValue)
                resolvedIndexCounter++;
        }

        internalIndexCounter = 0;

        while (internalIndexCounter < data.Length)
        {
            var valueData = data[internalIndexCounter];

            var resolvedIndex = resolvedIndexesInInternalIndexOrder[internalIndexCounter];
            var collidingInternalIndexes = internalIndexesPerResolvedIndex[resolvedIndex];

            if (collidingInternalIndexes.Count > 1)
            {
                var collidingDefinitionsNames = collidingInternalIndexes.Where(x => x != internalIndexCounter)
                    .Select(collidingInternalIndex => data[collidingInternalIndex].NormalizedName);

                valueData.DiagnosticReports ??= [];
                valueData.DiagnosticReports.Add(
                    Diagnostics.CollidingEnumIndexes(valueData.Location, declarationName, valueData.NormalizedName, string.Join(", ", collidingDefinitionsNames)));
            }

            definitions[internalIndexCounter]
                = new Definition(
                    InternalIndex: internalIndexCounter,
                    ResolvedIndex: resolvedIndex,
                    ExplicitIndex: valueData.ExplicitIndex,
                    Name: valueData.Name,
                    NormalizedName: valueData.NormalizedName,
                    Location: valueData.Location,
                    FullyQualifiedCustomType: valueData.FullyQualifiedCustomType,
                    IsEnumClassValueAttributeShared: valueData.IsEnumClassValueAttributeShared,
                    DiagnosticReports: valueData.DiagnosticReports);

            internalIndexCounter++;
        }

        return definitions;
    }




    private record struct CollectedValueData(
        string Name,
        string NormalizedName,
        int? ExplicitIndex,
        Location Location,
        string? FullyQualifiedCustomType,
        bool IsEnumClassValueAttributeShared,
        List<Diagnostic>? DiagnosticReports);
}
