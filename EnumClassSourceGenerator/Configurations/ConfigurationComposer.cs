using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses.SourceGenerators.Configurations;

internal static class ConfigurationComposer
{
    public static Configuration Compose(ConfigurationOverrides instance, Configuration defaults)
        => new(
            instance.GenerateJsonConverter ?? defaults.GenerateJsonConverter,
            instance.GenerateRawEnum ?? defaults.GenerateRawEnum,
            instance.SearchMode ?? defaults.SearchMode,
            instance.ConstructionRestrictionMode ?? defaults.ConstructionRestrictionMode,
            instance.RequireIndexAssignmentInInitializer ?? defaults.RequireIndexAssignmentInInitializer);
}
