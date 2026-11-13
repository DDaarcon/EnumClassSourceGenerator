using EnumClasses;

[assembly: EnumClassDefaults(
    GenerateJsonConverter = false,
    GenerateRawEnum = true,
    LookupMode = LookupMode.ForceDictionary,
    ConstructionRestrictionMode = ConstructionRestrictionMode.WithPrivateDefaultConstructor,
    IndexAssignmentMode = IndexAssignmentMode.CompileTime,
    DocumentationMode = DocumentationMode.Disabled)]

namespace EnumClasses.CompatibilityTesting.NetStandard2
{
    [EnumClass]
    internal partial class CompileTimeStatus
    {
        [EnumClassValue(10, "ready")]
        public static readonly CompileTimeStatus Ready = new CompileTimeStatus();

        public static readonly CompileTimeStatus Completed = new CompileTimeStatus();
    }

    [EnumClass(
        GenerateJsonConverter = false,
        GenerateRawEnum = false,
        LookupMode = LookupMode.ForceDictionary,
        ConstructionRestrictionMode = ConstructionRestrictionMode.Off,
        IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit,
        DocumentationMode = DocumentationMode.Disabled)]
    internal partial class RuntimeStatus
    {
        public static readonly RuntimeStatus Started = new RuntimeStatus
        {
            EnumIndex = -5
        };

        public static readonly RuntimeStatus Completed = new RuntimeStatus
        {
            EnumIndex = 42
        };
    }

    internal static class CompatibilityProbe
    {
        public static bool CanResolveCompileTimeStatus()
        {
            return CompileTimeStatus.Deserialize("ready") == CompileTimeStatus.Ready
                && CompileTimeStatus.GetByEnumIndex(11) == CompileTimeStatus.Completed;
        }

        public static bool CanResolveRuntimeStatus()
        {
            return RuntimeStatus.GetByEnumIndex(42) == RuntimeStatus.Completed
                && RuntimeStatus.Deserialize("Started") == RuntimeStatus.Started;
        }
    }
}
