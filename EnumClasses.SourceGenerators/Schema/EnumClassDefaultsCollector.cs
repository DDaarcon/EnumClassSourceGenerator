using EnumClasses.SourceGenerators.Configurations;
using EnumClasses.SourceGenerators.Schema.Help;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumClassDefaultsCollector
{
    internal record struct CollectResult(
        Configuration Configuration,
        Diagnostic[] DiagnosticReports);

    public static CollectResult Collect(IAssemblySymbol assemblySymbol)
    {
        var defaultValues = new Configuration(
            EnumClassDefaultsAttribute.DefaultGenerateJsonConverter,
            EnumClassDefaultsAttribute.DefaultGenerateRawEnum,
            EnumClassDefaultsAttribute.DefaultLookupMode,
            EnumClassDefaultsAttribute.DefaultConstructionRestrictionMode,
            EnumClassDefaultsAttribute.DefaultIndexAssignmentMode);

        var attributes = assemblySymbol.GetAttributes();
        if (attributes.Length == 0)
            return new(defaultValues, []);

        var defaultsAttribute = attributes
            .Where(x => x.AttributeClass is not null)
            .FirstOrDefault(x
                => FullyQualifiedNamesComparer.AreEqual(
                    x.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    SchemaConsts.AttributeNames.EnumClassDefaultsFullyQualified));
        if (defaultsAttribute is null)
            return new(defaultValues, []);

        var indexAssignmentMode = defaultsAttribute.NamedArguments
            .GetEnumProperty<IndexAssignmentMode>(nameof(EnumClassAttribute.IndexAssignmentMode));
        var diagnostics = GetInvalidIndexAssignmentModeDiagnostic(defaultsAttribute, indexAssignmentMode);


        var newValues = new ConfigurationOverrides
        {
            GenerateJsonConverter = defaultsAttribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateJsonConverter)),
            GenerateRawEnum = defaultsAttribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateRawEnum)),
            LookupMode = defaultsAttribute.NamedArguments.GetEnumProperty<LookupMode>(nameof(EnumClassAttribute.LookupMode)),
            ConstructionRestrictionMode = defaultsAttribute.NamedArguments.GetEnumProperty<ConstructionRestrictionMode>(nameof(EnumClassAttribute.ConstructionRestrictionMode)),
            IndexAssignmentMode = indexAssignmentMode
        };

        return new(ConfigurationComposer.Compose(newValues, defaultValues), diagnostics);
    }

    private static Diagnostic[] GetInvalidIndexAssignmentModeDiagnostic(
        AttributeData attribute,
        IndexAssignmentMode? indexAssignmentMode)
    {
        var namedArgument = attribute.NamedArguments
            .FirstOrDefault(x => x.Key == nameof(EnumClassAttribute.IndexAssignmentMode));

        if (namedArgument.Key is null
            || indexAssignmentMode.HasValue
            || namedArgument.Value.Value is not int invalidValue)
        {
            return [];
        }

        return
        [
            Diagnostics.UnsupportedIndexAssignmentMode(
                attribute.GetNamedArgumentValueLocation(nameof(EnumClassAttribute.IndexAssignmentMode)),
                invalidValue)
        ];
    }
}
