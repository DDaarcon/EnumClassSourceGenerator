namespace EnumClasses.SourceGenerators.Configurations;

internal record struct Configuration(
    bool GenerateJsonConverter,
    bool GenerateRawEnum,
    LookupMode LookupMode,
    ConstructionRestrictionMode ConstructionRestrictionMode,
    IndexAssignmentMode IndexAssignmentMode);


internal record struct ConfigurationOverrides(
    bool? GenerateJsonConverter,
    bool? GenerateRawEnum,
    LookupMode? LookupMode,
    ConstructionRestrictionMode? ConstructionRestrictionMode,
    IndexAssignmentMode? IndexAssignmentMode);
