using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading;

namespace EnumClassSourceGenerator.Schema;

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
        EnumValue.CollectionResult? EnumValues = null)
    {
        public enum StatusCode
        {
            Ok,
            NonApplicable,
            NamespaceNotFound,
            InvalidModifiers,
            InvalidValues
        }

        public record struct Configuration
        {
            public bool GenerateJsonConverter { get; set; }
            public bool UseDictionaryForDeserialization { get; set; }
            public bool RequireIndexAssignmentInInitializer { get; set; }
            public bool UseDictionaryForIndexMatching { get; set; }
        }
    }



    public enum OurAttributeType
    {
        None,
        EnumClass,
        NumberedEnumClass
    }

    private const string _enumClassAttributeName = "EnumClass";
    private const string _numberedEnumClassAttributeName = "NumberedEnumClass";

    public static (OurAttributeType Type, AttributeSyntax? Attribute) FindOurAttribute(ClassDeclarationSyntax component)
    {
        var attributes = component.AttributeLists
            .SelectMany(x => x.Attributes)
            .ToArray();

        if (attributes.Length == 0)
            return (OurAttributeType.None, null);

        var enumClassAttr = attributes.FirstOrDefault(attr => attr.Name.ToString() == _enumClassAttributeName);
        if (enumClassAttr is not null)
            return (OurAttributeType.EnumClass, enumClassAttr);
    
        var numberedEnumClassAttr = attributes.FirstOrDefault(attr => attr.Name.ToString() == _numberedEnumClassAttributeName);
        if (numberedEnumClassAttr is not null)
            return (OurAttributeType.NumberedEnumClass, numberedEnumClassAttr);

        return (OurAttributeType.None, null);
    }


    public static Definition CollectDefinition(ClassDeclarationSyntax component, SemanticModel semanticModel, CancellationToken token)
    {
        var (attrSearchResult, foundAttr) = FindOurAttribute(component);

        if (attrSearchResult is OurAttributeType.None)
            return new Definition(
                Status: Definition.StatusCode.NonApplicable,
                OurAttributeType: OurAttributeType.None);

        var namespaceName = GetNamespaceDeclaration(component);
        if (namespaceName is null)
            return new Definition(
                Status: Definition.StatusCode.NamespaceNotFound,
                OurAttributeType: attrSearchResult);

        var modifiers = component.Modifiers
            .Where(x => x.IsKind(SyntaxKind.PublicKeyword)
                || x.IsKind(SyntaxKind.InternalKeyword)
                || x.IsKind(SyntaxKind.PrivateKeyword)
                || x.IsKind(SyntaxKind.ProtectedKeyword));
        if (!modifiers.Any())
            return new Definition(
                Status: Definition.StatusCode.InvalidModifiers,
                OurAttributeType: attrSearchResult);

        var config = CollectConfiguration(foundAttr!, semanticModel, token);

        var enumValuesCollectionResult = EnumValue.CollectDefinitions(component, semanticModel, token);

        return new Definition(
            Status: enumValuesCollectionResult.HasAtLeastOneInvalidValueAccessor || enumValuesCollectionResult.HasAtLeastOneInvalidValueType
                ? Definition.StatusCode.InvalidValues
                : Definition.StatusCode.Ok,
            OurAttributeType: attrSearchResult,
            Location: component.GetLocation(),
            NamespaceName: namespaceName,
            DeclarationName: component.Identifier.ValueText,
            Modifier: modifiers.First().Text,
            Config: config,
            EnumValues: enumValuesCollectionResult);
    }

    private static string? GetNamespaceDeclaration(SyntaxNode? component)
    {
        if (component is null)
            return null;
        if (component is FileScopedNamespaceDeclarationSyntax
            or NamespaceDeclarationSyntax)
        {
            return (component as BaseNamespaceDeclarationSyntax)!.Name.ToString();
        }

        return GetNamespaceDeclaration(component.Parent);
    }


    private static Definition.Configuration CollectConfiguration(AttributeSyntax attribute, SemanticModel semanticModel, CancellationToken token)
    {
        bool generateJsonConverter = true;
        bool useDictionaryForDeserialization = false;
        bool requireIndexAssignmentInInitializer = true;
        bool useDictionaryForIndexMatching = false;

        foreach (var arg in attribute.ArgumentList?.Arguments ?? [])
        {
            if (TryGetBooleanProperty(nameof(Definition.Configuration.GenerateJsonConverter), arg, semanticModel, token, out var generateJsonConverterValue))
                generateJsonConverter = generateJsonConverterValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.UseDictionaryForDeserialization), arg, semanticModel, token, out var useDictionaryForDeserializationValue))
                useDictionaryForDeserialization = useDictionaryForDeserializationValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.RequireIndexAssignmentInInitializer), arg, semanticModel, token, out var requireIndexAssignmentInInitializerValue))
                requireIndexAssignmentInInitializer = requireIndexAssignmentInInitializerValue;

            if (TryGetBooleanProperty(nameof(Definition.Configuration.UseDictionaryForIndexMatching), arg, semanticModel, token, out var useDictionaryForIndexMatchingValue))
                useDictionaryForIndexMatching = useDictionaryForIndexMatchingValue;
        }

        return new Definition.Configuration
        {
            GenerateJsonConverter = generateJsonConverter,
            UseDictionaryForDeserialization = useDictionaryForDeserialization,
            RequireIndexAssignmentInInitializer = requireIndexAssignmentInInitializer,
            UseDictionaryForIndexMatching = useDictionaryForIndexMatching
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
