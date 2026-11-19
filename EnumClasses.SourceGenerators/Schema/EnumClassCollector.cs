using EnumClasses.SourceGenerators.Configurations;
using EnumClasses.SourceGenerators.FeatureSupport;
using EnumClasses.SourceGenerators.Schema.Help;
using EnumClasses.SourceGenerators.ExternalSourcesHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EnumClasses.SourceGenerators.Diagnostics;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumClassCollector
{
    public record struct Definition(
        Definition.StatusCode Status,
        Location? Location = null,
        Location? AttributeLocation = null,
        string? DeclarationName = null,
        string? NamespaceName = null,
        string? TypeKeyword = null,
        string? Modifier = null,
        bool HasExplicitInstanceConstructor = false,
        EnumValueCollector.CollectResult EnumValues = default,
        FeatureSupportState FeatureSupport = default,
        Definition.ContainingType[]? ContainersOrderedFromOuter = null,
        bool IsRecord = false,
        bool IsNotSealed = false,

        IEnumerable<Diagnostic>? DiagnosticReports = null)
    {
        public readonly string FullyQualifiedName
        {
            get
            {
                if (ContainersOrderedFromOuter is null || ContainersOrderedFromOuter.Length == 0)
                    return $"{NamespaceName}.{DeclarationName}";

                return $"{NamespaceName}.{string.Join(".", ContainersOrderedFromOuter.Select(x => x.DeclarationName))}.{DeclarationName}";
            }
        }

        public enum StatusCode
        {
            Ok,
            NonApplicable,
            InvalidTypeDeclaration,
            InvalidValues
        }

        public record struct WithConfig(
            Definition Definition,
            ConfigurationOverrides ConfigurationOverrides,
            Configuration Configuration);

        public record struct ContainingType(
            string DeclarationName,
            string TypeKeyword,
            string Modifier);
    }


    public record struct OurAttribute(
        AttributeData Data);


    public static Definition.WithConfig CollectDefinition(TypeDeclarationSyntax component, ISymbol componentSymbol, OurAttribute attribute, SemanticModel semanticModel, CancellationToken token)
    {
        var instanceConfig = CollectInstanceConfiguration(attribute.Data, out var configurationDiagnostic);
        var configDefaults = CollectDefaultConfiguration(componentSymbol.ContainingAssembly);

        var config = ConfigurationComposer.Compose(instanceConfig, configDefaults);

        var declarationLocation = component.Identifier.GetLocation();
        var attributeLocation = attribute.Data.ApplicationSyntaxReference?.GetSyntax(token).GetLocation()
            ?? declarationLocation;
        var declarationName = component.Identifier.ValueText;

        var namespaceName = GetNamespaceDeclaration(componentSymbol);


        var accessModifiers = component.Modifiers.GetAccessModifiers();

        var typeSymbol = (INamedTypeSymbol)componentSymbol;
        var declarations = typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .ToArray();
        var explicitInstanceConstructors = typeSymbol.InstanceConstructors
            .Where(constructor => !constructor.IsImplicitlyDeclared)
            .ToArray();

        var invalidConstructors = config.ConstructionRestrictionMode is ConstructionRestrictionMode.Off
            ? []
            : explicitInstanceConstructors
                .Where(constructor => constructor.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Protected));

        var featureSupport = CollectFeatureSupport(semanticModel);

        var reports = new List<Diagnostic>();
        if (configurationDiagnostic is not null)
            reports.Add(configurationDiagnostic);

        bool isGeneric = component.TypeParameterList is { Parameters.Count: > 0 };
        if (isGeneric)
            reports.Add(EnumClassError.GenericTypeNotSupported(declarationLocation, declarationName));

        bool isNamespaceMissing = namespaceName is null;
        if (isNamespaceMissing)
            reports.Add(EnumClassError.NamespaceNotFound(declarationLocation, declarationName));

        bool areAccessorsMissing = !accessModifiers.Any();
        if (areAccessorsMissing)
            reports.Add(EnumClassError.InvalidModifiers(declarationLocation, declarationName));

        var nonPartialDeclarations = declarations
            .Where(declaration => !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            .ToArray();
        bool arePartialModifiersMissing = nonPartialDeclarations.Length > 0;
        if (arePartialModifiersMissing)
            reports.AddRange(nonPartialDeclarations.Select(declaration =>
                EnumClassError.PartialModifierRequired(declaration.Identifier.GetLocation(), declarationName)));

        bool areInvalidConstructorsDefined = invalidConstructors.Any();
        if (areInvalidConstructorsDefined)
            reports.AddRange(invalidConstructors
                .Select(constructor => EnumClassError.NonPrivateConstructorOnRestrictedEnumClass(
                    constructor.Locations.FirstOrDefault() ?? declarationLocation,
                    declarationName)));


        var collectedContainers = EnumContainerClassesCollector.CollectDefinitions(component);
        bool isContainerInvalid = false;

        foreach (var container in collectedContainers)
        {
            if (container.CollectionStatus is EnumContainerClassesCollector.Definition.Status.Ok)
                continue;

            isContainerInvalid = true;
            reports.Add(container.CollectionStatus switch
            {
                EnumContainerClassesCollector.Definition.Status.MissingPartialKeyword
                    => EnumClassError.ContainerPartialModifierRequired(
                        container.Location,
                        containerName: container.DeclarationName,
                        declarationName),
                EnumContainerClassesCollector.Definition.Status.Misconfigured
                    => EnumClassError.InvalidContainerType(
                        container.Location,
                        containerName: container.DeclarationName,
                        declarationName),
                EnumContainerClassesCollector.Definition.Status.GenericTypeNotSupported
                    => EnumClassError.GenericContainerTypeNotSupported(
                        container.Location,
                        containerName: container.DeclarationName,
                        declarationName),
                _ => throw new InvalidOperationException("Undefined value")
            });
        }

        var containers = collectedContainers.Select(x
            => new Definition.ContainingType(
                x.DeclarationName,
                x.TypeKeyword,
                x.Modifier ?? ""))
            .ToArray();


        bool isRecord = component is RecordDeclarationSyntax;
        bool isNotSealed = !component.Modifiers.Any(SyntaxKind.SealedKeyword);


        Span<bool> checks = stackalloc bool[]
        {
            isGeneric,
            isNamespaceMissing,
            areAccessorsMissing,
            arePartialModifiersMissing,
            areInvalidConstructorsDefined,
            isContainerInvalid,
            configurationDiagnostic is not null
        };

        if (IsAnyTrue(checks))
        {
            return new(
                new Definition(
                    Status: Definition.StatusCode.InvalidTypeDeclaration,
                    Location: declarationLocation,
                    AttributeLocation: attributeLocation,
                    DeclarationName: component.Identifier.ValueText,
                    NamespaceName: namespaceName,
                    TypeKeyword: component.GetTypeKeyword(),
                    Modifier: accessModifiers.FirstOrDefault().Text,
                    HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
                    FeatureSupport: featureSupport,
                    ContainersOrderedFromOuter: containers,
                    IsRecord: isRecord,
                    IsNotSealed: isNotSealed,
                    DiagnosticReports: reports),
                instanceConfig,
                config
            );
        }

        var declarationsWithValues = declarations
            .Select(declaration =>
            {
                var declarationSemanticModel = semanticModel.Compilation.GetSemanticModel(declaration.SyntaxTree);
                return EnumValueCollector.CollectDefinitions(declaration, declarationSemanticModel, token);
            })
            .Where(result => result.Definitions.Length > 0)
            .ToArray();

        if (declarationsWithValues.Length > 1)
        {
            reports.Add(EnumClassError.ValuesDeclaredInMultiplePartialDeclarations(declarationLocation, declarationName));

            return new(
                new Definition(
                    Status: Definition.StatusCode.InvalidTypeDeclaration,
                    Location: declarationLocation,
                    AttributeLocation: attributeLocation,
                    DeclarationName: declarationName,
                    NamespaceName: namespaceName,
                    TypeKeyword: component.GetTypeKeyword(),
                    Modifier: accessModifiers.First().Text,
                    HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
                    FeatureSupport: featureSupport,
                    ContainersOrderedFromOuter: containers,
                    IsRecord: isRecord,
                    IsNotSealed: isNotSealed,
                    DiagnosticReports: reports),
                instanceConfig,
                config
            );
        }

        var enumValuesCollectionResult = declarationsWithValues.Length == 0
            ? new EnumValueCollector.CollectResult([])
            : declarationsWithValues[0];

        var enumValuesDiagnosticReports = enumValuesCollectionResult.Definitions.SelectMany(x => x.DiagnosticReports).ToArray();

        return new(
            new Definition(
                Status: enumValuesDiagnosticReports.Length > 0
                    ? Definition.StatusCode.InvalidValues
                    : Definition.StatusCode.Ok,
                Location: component.GetLocation(),
                AttributeLocation: attributeLocation,
                NamespaceName: namespaceName,
                TypeKeyword: component.GetTypeKeyword(),
                DeclarationName: component.Identifier.ValueText,
                Modifier: accessModifiers.First().Text,
                HasExplicitInstanceConstructor: explicitInstanceConstructors.Length > 0,
                EnumValues: enumValuesCollectionResult,
                FeatureSupport: featureSupport,
                ContainersOrderedFromOuter: containers,
                IsRecord: isRecord,
                IsNotSealed: isNotSealed,
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

    public static Definition.WithConfig ResolveConfiguration(Definition.WithConfig collectedDefinition, Configuration configuration)
    {
        var definition = collectedDefinition.Definition;
        var configurationDiagnostics = new List<Diagnostic>();

        if (configuration is { IndexAssignmentMode.IsRuntime: true, GenerateRawEnum: true })
            configurationDiagnostics.Add(EnumClassWarning.RuntimeIndexesMayDifferFromRawEnumValues(definition.AttributeLocation));

        if (configuration.IndexAssignmentMode.IsRuntime
            && !definition.FeatureSupport.InitKeyword.IsSupported)
        {
            configurationDiagnostics.Add(EnumClassWarning.InitKeywordUnavailable(
                definition.AttributeLocation,
                definition.DeclarationName!));
        }

        if (configuration.GenerateJsonConverter
            && !definition.FeatureSupport.JsonConverter.IsSupported)
        {
            configurationDiagnostics.Add(EnumClassError.JsonConverterUnavailable(
                definition.AttributeLocation,
                definition.DeclarationName!));
        }

        if (definition.Status is Definition.StatusCode.InvalidTypeDeclaration)
        {
            return collectedDefinition with
            {
                Definition = definition with
                {
                    DiagnosticReports = (definition.DiagnosticReports ?? [])
                        .Concat(configurationDiagnostics)
                        .ToArray()
                },
                Configuration = configuration
            };
        }

        var resolvedValues = EnumValueCollector.ResolveIndexes(
            definition.EnumValues,
            configuration,
            definition.DeclarationName!);
        var diagnostics = resolvedValues.Definitions
            .SelectMany(value => value.DiagnosticReports ?? [])
            .Concat(configurationDiagnostics)
            .ToArray();

        return collectedDefinition with
        {
            Definition = definition with
            {
                Status = diagnostics.Length > 0
                    ? Definition.StatusCode.InvalidValues
                    : Definition.StatusCode.Ok,
                EnumValues = resolvedValues,
                DiagnosticReports = diagnostics
            },
            Configuration = configuration
        };
    }



    private static string? GetNamespaceDeclaration(ISymbol componentSymbol)
    {
        if (componentSymbol.ContainingNamespace.IsGlobalNamespace)
            return null;

        return componentSymbol.ContainingNamespace.ToDisplayString();
    }


    private static ConfigurationOverrides CollectInstanceConfiguration(
        AttributeData attribute,
        out Diagnostic? configurationDiagnostic)
    {
        var indexAssignmentMode = attribute.NamedArguments
            .GetEnumProperty<IndexAssignmentMode>(nameof(EnumClassAttribute.IndexAssignmentMode));
        configurationDiagnostic = GetInvalidIndexAssignmentModeDiagnostic(attribute, indexAssignmentMode);

        return new ConfigurationOverrides(
            GenerateJsonConverter: attribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateJsonConverter)),
            GenerateRawEnum: attribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateRawEnum)),
            LookupMode: attribute.NamedArguments.GetEnumProperty<LookupMode>(nameof(EnumClassAttribute.LookupMode)),
            ConstructionRestrictionMode: attribute.NamedArguments.GetEnumProperty<ConstructionRestrictionMode>(nameof(EnumClassAttribute.ConstructionRestrictionMode)),
            IndexAssignmentMode: indexAssignmentMode,
            DocumentationMode: attribute.NamedArguments.GetEnumProperty<DocumentationMode>(nameof(EnumClassAttribute.DocumentationMode)));
    }

    private static Diagnostic? GetInvalidIndexAssignmentModeDiagnostic(
        AttributeData attribute,
        IndexAssignmentMode? indexAssignmentMode)
    {
        var namedArgument = attribute.NamedArguments
            .FirstOrDefault(x => x.Key == nameof(EnumClassAttribute.IndexAssignmentMode));

        if (namedArgument.Key is null
            || indexAssignmentMode.HasValue
            || namedArgument.Value.Value is not int invalidValue)
        {
            return null;
        }

        return EnumClassError.UnsupportedIndexAssignmentMode(
            attribute.GetNamedArgumentValueLocation(nameof(EnumClassAttribute.IndexAssignmentMode)),
            invalidValue);
    }

    private static Configuration CollectDefaultConfiguration(IAssemblySymbol assembly)
        => EnumClassDefaultsCollector.Collect(assembly).Configuration;



    private static FeatureSupportState CollectFeatureSupport(SemanticModel semanticModel)
    {
        var compilation = semanticModel.Compilation;
        var parseOptions = (CSharpParseOptions) semanticModel.SyntaxTree.Options;

        return FeaturesSupportResolver.GetSupportState(compilation, parseOptions);
    }


}
