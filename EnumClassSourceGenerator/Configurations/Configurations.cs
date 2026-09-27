namespace EnumClasses.SourceGenerators.Configurations;

internal record struct Configuration(
    bool GenerateJsonConverter,
    bool GenerateRawEnum,
    SearchMode SearchMode,
    ConstructionRestrictionMode ConstructionRestrictionMode,
    bool RequireIndexAssignmentInInitializer);


internal record struct ConfigurationOverrides(
    bool? GenerateJsonConverter,
    bool? GenerateRawEnum,
    SearchMode? SearchMode,
    ConstructionRestrictionMode? ConstructionRestrictionMode,
    bool? RequireIndexAssignmentInInitializer);
