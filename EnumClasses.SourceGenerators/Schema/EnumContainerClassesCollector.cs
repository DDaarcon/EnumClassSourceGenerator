using EnumClasses.SourceGenerators.Schema.Help;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace EnumClasses.SourceGenerators.Schema;

internal class EnumContainerClassesCollector
{
    public record struct Definition(
        string DeclarationName,
        string TypeKeyword,
        string? Modifier,
        Location Location,
        Definition.Status CollectionStatus)
    {
        public enum Status
        {
            Ok,
            Misconfigured,
            MissingPartialKeyword,
            GenericTypeNotSupported
        }
    }

    public static Definition[] CollectDefinitions(TypeDeclarationSyntax component)
    {
        return component.Ancestors().OfType<TypeDeclarationSyntax>()
            .Select(CollectContainer)
            .Reverse()
            .ToArray();
    }

    private static Definition CollectContainer(TypeDeclarationSyntax containerComponent)
    {
        var name = containerComponent.Identifier.ValueText;
        var location = containerComponent.Identifier.GetLocation();

        if (containerComponent.TypeParameterList is { Parameters.Count: > 0 })
        {
            return new Definition(
                DeclarationName: name,
                TypeKeyword: containerComponent.GetTypeKeyword(),
                Modifier: null,
                Location: location,
                CollectionStatus: Definition.Status.GenericTypeNotSupported);
        }

        var accessModifiers = containerComponent.Modifiers.GetAccessModifiers();
        if (!accessModifiers.Any())
        {
            return new Definition(
                DeclarationName: name,
                TypeKeyword: containerComponent.GetTypeKeyword(),
                Modifier: null,
                Location: location,
                CollectionStatus: Definition.Status.Misconfigured);
        }

        if (!containerComponent.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return new Definition(
                DeclarationName: name,
                TypeKeyword: containerComponent.GetTypeKeyword(),
                Modifier: accessModifiers.First().Text,
                Location: location,
                CollectionStatus: Definition.Status.MissingPartialKeyword);
        }

        return new Definition(
            DeclarationName: name,
            TypeKeyword: containerComponent.GetTypeKeyword(),
            Modifier: accessModifiers.First().Text,
            Location: location,
            CollectionStatus: Definition.Status.Ok);
    }
}
