namespace EnumClasses.SourceGenerators.Configurations;

internal static class ConfigurationComposer
{
    public static Configuration Compose(ConfigurationOverrides instance, Configuration defaults)
        => new(
            instance.GenerateJsonConverter ?? defaults.GenerateJsonConverter,
            instance.GenerateRawEnum ?? defaults.GenerateRawEnum,
            instance.LookupMode ?? defaults.LookupMode,
            instance.ConstructionRestrictionMode ?? defaults.ConstructionRestrictionMode,
            instance.IndexAssignmentMode ?? defaults.IndexAssignmentMode,
            instance.DocumentationMode ?? defaults.DocumentationMode);
}
