using EnumClasses.SourceGenerators;
using EnumClasses.SourceGenerators.Schema.Help;
using EnumClasses.SourceGenerators.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumClass
{
    public record struct Definition(
        Definition.StatusCode Status,
        OurAttributeType OurAttributeType,
        Location? Location = null,
        string? DeclarationName = null,
        string? NamespaceName = null,
        string? Modifier = null,
        Definition.Configuration Config = new(),
        bool HasExplicitInstanceConstructor = false,
        EnumValue.CollectResult? EnumValues = null,
        IEnumerable<Diagnostic>? DiagnosticReports = null)
    {
        public readonly string FullyQualifiedName => $"{NamespaceName}.{DeclarationName}";

        public enum StatusCode
        {
            Ok,
            NonApplicable,
            InvalidClassDeclaration,
            InvalidValues
        }

        public record struct Configuration
        {
            public bool GenerateJsonConverter { get; set; }
            public bool GenerateRawEnum { get; set; }
            public bool UseDictionaryForDeserialization { get; set; }
            public bool UnrestrictedConstruction { get; set; }
            public bool RequireIndexAssignmentInInitializer { get; set; }
        }
    }



    public enum OurAttributeType
    {
        None,
        EnumClass,
        NumberedEnumClass
    }

    public static (OurAttributeType Type, AttributeSyntax? Attribute) FindOurAttribute(ClassDeclarationSyntax component)
    {
        if (component.AttributeLists.TryGetByName(SchemaConsts.AttributeNames.EnumClass, out var basicAttr))
            return (OurAttributeType.EnumClass, basicAttr);

        if (component.AttributeLists.TryGetByName(SchemaConsts.AttributeNames.NumberedEnumClass, out var numberedAttr))
            return (OurAttributeType.NumberedEnumClass, numberedAttr);

        return (OurAttributeType.None, null);
    }


    public static Definition CollectDefinition(ClassDeclarationSyntax component, ISymbol componentSymbol, SemanticModel semanticModel, CancellationToken token)
    {
        var (attrSearchResult, foundAttr) = FindOurAttribute(component);

        if (attrSearchResult is OurAttributeType.None)
            return new Definition(
                Status: Definition.StatusCode.NonApplicable,
                OurAttributeType: OurAttributeType.None);


        var config = CollectConfiguration(foundAttr!, semanticModel, token);

        var declarationLocation = component.Identifier.GetLocation();
        var declarationName = component.Identifier.ValueText;

        var namespaceName = GetNamespaceDeclaration(componentSymbol);

        var accessModifiers = component.Modifiers
            .Where(x => x.IsKind(SyntaxKind.PublicKeyword)
                || x.IsKind(SyntaxKind.InternalKeyword)
                || x.IsKind(SyntaxKind.PrivateKeyword)
                || x.IsKind(SyntaxKind.ProtectedKeyword));

        var typeSymbol = (INamedTypeSymbol)componentSymbol;
        var explicitInstanceConstructors = typeSymbol.InstanceConstructors
            .Where(constructor => !constructor.IsImplicitlyDeclared)
            .ToArray();

        var invalidConstructors = config.UnrestrictedConstruction
            ? []
            : explicitInstanceConstructors
                .Where(constructor => constructor.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Protected));


        var reports = new List<Diagnostic>();

        bool isNested = component.Parent is TypeDeclarationSyntax;
        if (isNested)
            reports.Add(Diagnostics.NestedTypeNotSupported(declarationLocation, declarationName));

        bool isGeneric = component.TypeParameterList is { Parameters.Count: > 0 };
        if (isGeneric)
            reports.Add(Diagnostics.GenericTypeNotSupported(declarationLocation, declarationName));

        bool isNamespaceMissing = namespaceName is null;
        if (isNamespaceMissing)
            reports.Add(Diagnostics.NamespaceNotFound(declarationLocation, declarationName));

        bool areAccessorsMissing = !accessModifiers.Any();
        if (areAccessorsMissing)
            reports.Add(Diagnostics.InvalidModifiers(declarationLocation, declarationName));

        bool areInvalidConstructorsDefined = invalidConstructors.Any();
        if (areInvalidConstructorsDefined)
            reports.AddRange(invalidConstructors
                .Select(constructor => Diagnostics.NonPrivateConstructorOnRestrictedEnumClass(
                    constructor.Locations.FirstOrDefault() ?? declarationLocation,
                    declarationName)));

        Span<bool> checks = stackalloc bool[] { isNested, isGeneric, isNamespaceMissing, areAccessorsMissing, areInvalidConstructorsDefined };

        if (IsAnyTrue(checks))
        {
            return new Definition(
                Status: Definition.StatusCode.InvalidClassDeclaration,
                OurAttributeType: attrSearchResult,
                Location: declarationLocation,
                DeclarationName: component.Identifier.ValueText,
                NamespaceName: namespaceName,
                Modifier: accessModifiers.FirstOrDefault().Text,
                Config: config,
                HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
                DiagnosticReports: reports);
        }

        var enumValuesCollectionResult = EnumValue.CollectDefinitions(component, semanticModel, token);

        var enumValuesDiagnosticReports = enumValuesCollectionResult.Definitions.SelectMany(x => x.DiagnosticReports).ToArray();

        return new Definition(
            Status: enumValuesDiagnosticReports.Length > 0
                ? Definition.StatusCode.InvalidValues
                : Definition.StatusCode.Ok,
            OurAttributeType: attrSearchResult,
            Location: component.GetLocation(),
            NamespaceName: namespaceName,
            DeclarationName: component.Identifier.ValueText,
            Modifier: accessModifiers.First().Text,
            Config: config,
            HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
            EnumValues: enumValuesCollectionResult,
            DiagnosticReports: enumValuesDiagnosticReports);

        static bool IsAnyTrue(Span<bool> values)
        {
            foreach (var value in values)
                if (value)
                    return true;
            return false;
        }
    }



    private static string? GetNamespaceDeclaration(ISymbol componentSymbol)
    {
        if (componentSymbol.ContainingNamespace.IsGlobalNamespace)
            return null;

        return componentSymbol.ContainingNamespace.ToDisplayString();
    }


    private static Definition.Configuration CollectConfiguration(AttributeSyntax attribute, SemanticModel semanticModel, CancellationToken token)
    {
        bool generateJsonConverter = true;
        bool generateRawEnum = false;
        bool useDictionaryForDeserialization = false;
        bool unrestrictedConstruction = false;
        bool requireIndexAssignmentInInitializer = true;

        foreach (var arg in attribute.ArgumentList?.Arguments ?? [])
        {
            if (TryGetBooleanProperty(nameof(Definition.Configuration.GenerateJsonConverter), arg, semanticModel, token, out var generateJsonConverterValue))
                generateJsonConverter = generateJsonConverterValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.GenerateRawEnum), arg, semanticModel, token, out var generateRawEnumValue))
                generateRawEnum = generateRawEnumValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.UseDictionaryForDeserialization), arg, semanticModel, token, out var useDictionaryForDeserializationValue))
                useDictionaryForDeserialization = useDictionaryForDeserializationValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.UnrestrictedConstruction), arg, semanticModel, token, out var unrestrictedConstructionValue))
                unrestrictedConstruction = unrestrictedConstructionValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.RequireIndexAssignmentInInitializer), arg, semanticModel, token, out var requireIndexAssignmentInInitializerValue))
                requireIndexAssignmentInInitializer = requireIndexAssignmentInInitializerValue;
        }

        return new Definition.Configuration
        {
            GenerateJsonConverter = generateJsonConverter,
            GenerateRawEnum = generateRawEnum,
            UseDictionaryForDeserialization = useDictionaryForDeserialization,
            UnrestrictedConstruction = unrestrictedConstruction,
            RequireIndexAssignmentInInitializer = requireIndexAssignmentInInitializer
        };


        static bool TryGetBooleanProperty(string propertyName, AttributeArgumentSyntax arg, SemanticModel semanticModel, CancellationToken token, out bool result)
        {
            result = false;

            if (!(arg.NameEquals?.Name.Identifier.Text.Equals(propertyName) ?? false))
                return false;

            var value = semanticModel.GetConstantValue(arg.Expression, token);
            if (!value.HasValue)
                return false;

            result = ((bool?)value.Value).GetValueOrDefault();
            return true;
        }
    }
}
