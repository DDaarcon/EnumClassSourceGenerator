using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            public SearchMode SearchMode { get; set; }
            public ConstructionRestrictionMode ConstructionRestrictionMode { get; set; }
            public bool RequireIndexAssignmentInInitializer { get; set; }
        }
    }

    public record struct OurAttribute(
        OurAttributeType Type,
        AttributeData Data);

    public enum OurAttributeType
    {
        None,
        EnumClass,
        NumberedEnumClass
    }


    public static Definition CollectDefinition(ClassDeclarationSyntax component, ISymbol componentSymbol, OurAttribute attribute, SemanticModel semanticModel, CancellationToken token)
    {
        var config = CollectConfiguration(attribute.Data, semanticModel, token);

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

        var invalidConstructors = config.ConstructionRestrictionMode is ConstructionRestrictionMode.Off
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
                OurAttributeType: attribute.Type,
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
            OurAttributeType: attribute.Type,
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


    private static Definition.Configuration CollectConfiguration(AttributeData attribute, SemanticModel semanticModel, CancellationToken token)
    {
        bool generateJsonConverter = true;
        bool generateRawEnum = false;
        SearchMode searchMode = EnumClassAttribute.DefaultSearchMode;
        ConstructionRestrictionMode constructionRestrictionMode = EnumClassAttribute.DefaultConstructionRestrictionMode;
        bool requireIndexAssignmentInInitializer = true;


        if (TryGetBooleanProperty(nameof(EnumClassAttribute.GenerateJsonConverter), attribute.NamedArguments, semanticModel, token, out var generateJsonConverterValue))
            generateJsonConverter = generateJsonConverterValue;

        if (TryGetBooleanProperty(nameof(EnumClassAttribute.GenerateRawEnum), attribute.NamedArguments, semanticModel, token, out var generateRawEnumValue))
            generateRawEnum = generateRawEnumValue;

        if (TryGetEnumProperty<SearchMode>(nameof(EnumClassAttribute.SearchMode), attribute.NamedArguments, semanticModel, token, out var searchModeValue))
            searchMode = searchModeValue;

        if (TryGetEnumProperty<ConstructionRestrictionMode>(nameof(EnumClassAttribute.ConstructionRestrictionMode), attribute.NamedArguments, semanticModel, token, out var constructionRestrictionModeValue))
            constructionRestrictionMode = constructionRestrictionModeValue;

        if (TryGetBooleanProperty(nameof(NumberedEnumClassAttribute.RequireIndexAssignmentInInitializer), attribute.NamedArguments, semanticModel, token, out var requireIndexAssignmentInInitializerValue))
            requireIndexAssignmentInInitializer = requireIndexAssignmentInInitializerValue;

        return new Definition.Configuration
        {
            GenerateJsonConverter = generateJsonConverter,
            GenerateRawEnum = generateRawEnum,
            SearchMode = searchMode,
            ConstructionRestrictionMode = constructionRestrictionMode,
            RequireIndexAssignmentInInitializer = requireIndexAssignmentInInitializer
        };


        static bool TryGetBooleanProperty(string propertyName, ImmutableArray<KeyValuePair<string, TypedConstant>> arguments, SemanticModel semanticModel, CancellationToken token, out bool result)
        {
            result = false;

            var arg = arguments.FirstOrDefault(x => x.Key == propertyName).Value;

            if (arg.Kind is TypedConstantKind.Error)
                return false;

            result = ((bool?)arg.Value).GetValueOrDefault();
            return true;
        }


        static bool TryGetEnumProperty<TEnum>(string propertyName, ImmutableArray<KeyValuePair<string, TypedConstant>> arguments, SemanticModel semanticModel, CancellationToken token, out TEnum result)
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
    }
}
