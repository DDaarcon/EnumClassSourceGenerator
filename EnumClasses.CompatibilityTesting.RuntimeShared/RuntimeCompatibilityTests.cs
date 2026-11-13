using System.Text.Json;
using EnumClasses;
using Xunit;

[assembly: EnumClassDefaults(
    GenerateJsonConverter = true,
    GenerateRawEnum = false,
    LookupMode = LookupMode.ForceDictionary,
    ConstructionRestrictionMode = ConstructionRestrictionMode.Off,
    IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit,
    DocumentationMode = DocumentationMode.Disabled)]

namespace EnumClasses.CompatibilityTesting.Runtime
{
    [EnumClass]
    internal partial class RuntimeStatus
    {
        public static readonly RuntimeStatus Started = new RuntimeStatus
        {
            EnumIndex = -5
        };

        [EnumClassValue(SerializedName = "completed-successfully")]
        public static readonly RuntimeStatus Completed = new RuntimeStatus
        {
            EnumIndex = 42
        };
    }

    public sealed class RuntimeCompatibilityTests
    {
        [Fact]
        public void GeneratedLookupsExecute()
        {
            Assert.Same(RuntimeStatus.Started, RuntimeStatus.GetByEnumIndex(-5));
            Assert.Same(
                RuntimeStatus.Completed,
                RuntimeStatus.Deserialize("completed-successfully"));

            RuntimeStatus? result;
            Assert.True(RuntimeStatus.TryDeserialize("Started", out result));
            Assert.Same(RuntimeStatus.Started, result);
        }

        [Fact]
        public void GeneratedJsonConverterExecutes()
        {
            var json = JsonSerializer.Serialize(RuntimeStatus.Completed);

            Assert.Equal("\"completed-successfully\"", json);
            Assert.Same(
                RuntimeStatus.Completed,
                JsonSerializer.Deserialize<RuntimeStatus>(json));
        }
    }
}
