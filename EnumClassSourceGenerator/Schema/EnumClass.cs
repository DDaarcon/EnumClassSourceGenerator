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
        EnumValue.CollectResult? EnumValues = null,
        IEnumerable<Diagnostic>? DiagnosticReports = null)
    {
        public readonly string FullyQualifiedName => $"{NamespaceName}.{DeclarationName}";

        public enum StatusCode
        {
            Ok,
            NonApplicable,
            NamespaceNotFound,
            InvalidModifiers,
            InvalidValues,
            NestedTypeNotSupported,
            GenericTypeNotSupported
        }

        public record struct Configuration
        {
            public bool GenerateJsonConverter { get; set; }
            public bool GenerateRawEnum { get; set; }
            public bool UseDictionaryForDeserialization { get; set; }
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

        var declarationLocation = component.Identifier.GetLocation();
        var declarationName = component.Identifier.ValueText;

        var namespaceName = GetNamespaceDeclaration(componentSymbol);


        var userDeclarationValidationResult = ValidateUserDeclaration();
        if (userDeclarationValidationResult.Status is not Definition.StatusCode.Ok)
            return userDeclarationValidationResult;
        Definition ValidateUserDeclaration()
        {
            bool isNested = component.Parent is TypeDeclarationSyntax;
            bool isGeneric = component.TypeParameterList is { Parameters.Count: > 0 };
            bool isNamespaceMissing = namespaceName is null;

            var reports = new List<Diagnostic>();
            if (isNested)
                reports.Add(Diagnostics.NestedTypeNotSupported(declarationLocation, declarationName));
            if (isGeneric)
                reports.Add(Diagnostics.GenericTypeNotSupported(declarationLocation, declarationName));
            if (isNamespaceMissing)
                reports.Add(Diagnostics.NamespaceNotFound(declarationLocation, declarationName));

            return new Definition(
                Status: isNested
                    ? Definition.StatusCode.NestedTypeNotSupported
                    : isGeneric
                        ? Definition.StatusCode.GenericTypeNotSupported
                        : Definition.StatusCode.Ok,
                OurAttributeType: attrSearchResult,
                Location: declarationLocation,
                DeclarationName: component.Identifier.ValueText,
                NamespaceName: namespaceName,
                DiagnosticReports: reports);
        }

        if (namespaceName is null)
            return new Definition(
                Status: Definition.StatusCode.NamespaceNotFound,
                OurAttributeType: attrSearchResult,
                DiagnosticReports: [Diagnostics.NamespaceNotFound(declarationLocation, declarationName)]);

        var modifiers = component.Modifiers
            .Where(x => x.IsKind(SyntaxKind.PublicKeyword)
                || x.IsKind(SyntaxKind.InternalKeyword)
                || x.IsKind(SyntaxKind.PrivateKeyword)
                || x.IsKind(SyntaxKind.ProtectedKeyword));
        if (!modifiers.Any())
            return new Definition(
                Status: Definition.StatusCode.InvalidModifiers,
                OurAttributeType: attrSearchResult,
                DiagnosticReports: [Diagnostics.InvalidModifiers(declarationLocation, declarationName)]);

        var config = CollectConfiguration(foundAttr!, semanticModel, token);

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
            Modifier: modifiers.First().Text,
            Config: config,
            EnumValues: enumValuesCollectionResult,
            DiagnosticReports: enumValuesDiagnosticReports);
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
        bool requireIndexAssignmentInInitializer = true;

        foreach (var arg in attribute.ArgumentList?.Arguments ?? [])
        {
            if (TryGetBooleanProperty(nameof(Definition.Configuration.GenerateJsonConverter), arg, semanticModel, token, out var generateJsonConverterValue))
                generateJsonConverter = generateJsonConverterValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.GenerateRawEnum), arg, semanticModel, token, out var generateRawEnumValue))
                generateRawEnum = generateRawEnumValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.UseDictionaryForDeserialization), arg, semanticModel, token, out var useDictionaryForDeserializationValue))
                useDictionaryForDeserialization = useDictionaryForDeserializationValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.RequireIndexAssignmentInInitializer), arg, semanticModel, token, out var requireIndexAssignmentInInitializerValue))
                requireIndexAssignmentInInitializer = requireIndexAssignmentInInitializerValue;
        }

        return new Definition.Configuration
        {
            GenerateJsonConverter = generateJsonConverter,
            GenerateRawEnum = generateRawEnum,
            UseDictionaryForDeserialization = useDictionaryForDeserialization,
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
