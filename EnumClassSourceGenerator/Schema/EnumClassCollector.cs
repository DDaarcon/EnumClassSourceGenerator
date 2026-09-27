using EnumClasses.SourceGenerators.Configurations;
using EnumClasses.SourceGenerators.Schema.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumClassCollector
{
    public record struct Definition(
        Definition.StatusCode Status,
        OurAttributeType OurAttributeType,
        Location? Location = null,
        string? DeclarationName = null,
        string? NamespaceName = null,
        string? Modifier = null,
        bool HasExplicitInstanceConstructor = false,
        EnumValueCollector.CollectResult? EnumValues = null,
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

        public record struct WithConfig(
            Definition Definition,
            ConfigurationOverrides ConfigurationOverrides,
            Configuration Configuration);
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


    public static Definition.WithConfig CollectDefinition(ClassDeclarationSyntax component, ISymbol componentSymbol, OurAttribute attribute, SemanticModel semanticModel, CancellationToken token)
    {
        var instanceConfig = CollectInstanceConfiguration(attribute.Data);
        var configDefaults = CollectDefaultConfiguration(componentSymbol.ContainingAssembly);

        var config = ConfigurationComposer.Compose(instanceConfig, configDefaults);

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
            return new(
                new Definition(
                    Status: Definition.StatusCode.InvalidClassDeclaration,
                    OurAttributeType: attribute.Type,
                    Location: declarationLocation,
                    DeclarationName: component.Identifier.ValueText,
                    NamespaceName: namespaceName,
                    Modifier: accessModifiers.FirstOrDefault().Text,
                    HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
                    DiagnosticReports: reports),
                instanceConfig,
                config
            );
        }

        var enumValuesCollectionResult = EnumValueCollector.CollectDefinitions(component, semanticModel, token);

        var enumValuesDiagnosticReports = enumValuesCollectionResult.Definitions.SelectMany(x => x.DiagnosticReports).ToArray();

        return new(
            new Definition(
                Status: enumValuesDiagnosticReports.Length > 0
                    ? Definition.StatusCode.InvalidValues
                    : Definition.StatusCode.Ok,
                OurAttributeType: attribute.Type,
                Location: component.GetLocation(),
                NamespaceName: namespaceName,
                DeclarationName: component.Identifier.ValueText,
                Modifier: accessModifiers.First().Text,
                HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
                EnumValues: enumValuesCollectionResult,
                DiagnosticReports: enumValuesDiagnosticReports),
            instanceConfig,
            config
        );

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


    private static ConfigurationOverrides CollectInstanceConfiguration(AttributeData attribute)
    {
        return new ConfigurationOverrides
        {
            GenerateJsonConverter = attribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateJsonConverter)),
            GenerateRawEnum = attribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateRawEnum)),
            SearchMode = attribute.NamedArguments.GetEnumProperty<SearchMode>(nameof(EnumClassAttribute.SearchMode)),
            ConstructionRestrictionMode = attribute.NamedArguments.GetEnumProperty<ConstructionRestrictionMode>(nameof(EnumClassAttribute.ConstructionRestrictionMode)),
            RequireIndexAssignmentInInitializer = attribute.NamedArguments.GetBooleanProperty(nameof(NumberedEnumClassAttribute.RequireIndexAssignmentInInitializer)),
        };
    }

    private static Configuration CollectDefaultConfiguration(IAssemblySymbol assembly)
        => EnumClassDefaultsCollector.Collect(assembly);
}
