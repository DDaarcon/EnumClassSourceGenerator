using EnumClasses.SourceGenerators.Configurations;
using EnumClasses.SourceGenerators.Schema.Help;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace EnumClasses.SourceGenerators.Schema;

internal static class EnumClassDefaultsCollector
{
    public static Configuration Collect(IAssemblySymbol assemblySymbol)
    {
        var defaultValues = new Configuration(
            EnumClassDefaultsAttribute.DefaultGenerateJsonConverter,
            EnumClassDefaultsAttribute.DefaultGenerateRawEnum,
            EnumClassDefaultsAttribute.DefaultLookupMode,
            EnumClassDefaultsAttribute.DefaultConstructionRestrictionMode,
            EnumClassDefaultsAttribute.DefaultRequireIndexAssignmentInInitializer);

        var attributes = assemblySymbol.GetAttributes();
        if (attributes.Length == 0)
            return defaultValues;

        var defaultsAttribute = attributes
            .Where(x => x.AttributeClass is not null)
            .FirstOrDefault(x
                => FullyQualifiedNamesComparer.AreEqual(
                    x.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    SchemaConsts.AttributeNames.EnumClassDefaultsFullyQualified));
        if (defaultsAttribute is null)
            return defaultValues;


        var newValues = new ConfigurationOverrides
        {
            GenerateJsonConverter = defaultsAttribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateJsonConverter)),
            GenerateRawEnum = defaultsAttribute.NamedArguments.GetBooleanProperty(nameof(EnumClassAttribute.GenerateRawEnum)),
            LookupMode = defaultsAttribute.NamedArguments.GetEnumProperty<LookupMode>(nameof(EnumClassAttribute.LookupMode)),
            ConstructionRestrictionMode = defaultsAttribute.NamedArguments.GetEnumProperty<ConstructionRestrictionMode>(nameof(EnumClassAttribute.ConstructionRestrictionMode)),
            RequireIndexAssignmentInInitializer = defaultsAttribute.NamedArguments.GetBooleanProperty(nameof(NumberedEnumClassAttribute.RequireIndexAssignmentInInitializer)),
        };

        return ConfigurationComposer.Compose(newValues, defaultValues);
    }
}
