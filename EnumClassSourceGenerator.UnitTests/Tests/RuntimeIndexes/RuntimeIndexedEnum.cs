using EnumClasses;
using System.Diagnostics.CodeAnalysis;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
internal partial class RuntimeIndexedEnum
{
    [EnumClassValue]
    public static readonly RuntimeIndexedEnum Ten = new()
    {
        EnumIndex = 10
    };

    public static readonly RuntimeIndexedEnum Twenty = new()
    {
        EnumIndex = 20
    };
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
internal partial class DuplicateRuntimeIndexedEnum
{
    public static readonly DuplicateRuntimeIndexedEnum First = new()
    {
        EnumIndex = 7
    };

    public static readonly DuplicateRuntimeIndexedEnum Second = new()
    {
        EnumIndex = 7
    };
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
internal partial class UnassignedRuntimeIndexedEnum
{
    [SetsRequiredMembers]
    private UnassignedRuntimeIndexedEnum()
    {
    }

    public static readonly UnassignedRuntimeIndexedEnum Value = new();
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
internal partial class ExtremeRuntimeIndexedEnum
{
    public static readonly ExtremeRuntimeIndexedEnum Minimum = new()
    {
        EnumIndex = int.MinValue
    };

    public static readonly ExtremeRuntimeIndexedEnum Negative = new()
    {
        EnumIndex = -1
    };

    public static readonly ExtremeRuntimeIndexedEnum Maximum = new()
    {
        EnumIndex = int.MaxValue
    };
}

[EnumClass(
    IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit,
    LookupMode = LookupMode.ForceDictionary)]
internal partial class DictionaryRuntimeIndexedEnum
{
    public static readonly DictionaryRuntimeIndexedEnum Negative = new()
    {
        EnumIndex = -100
    };

    public static readonly DictionaryRuntimeIndexedEnum Positive = new()
    {
        EnumIndex = 100
    };
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueInit)]
internal partial class RuntimeAtValueInitAssignmentEnum
{
    private RuntimeAtValueInitAssignmentEnum()
    {
    }

    [SetsRequiredMembers]
    private RuntimeAtValueInitAssignmentEnum(int enumIndex)
    {
        EnumIndex = enumIndex;
    }

    public static readonly RuntimeAtValueInitAssignmentEnum FromConstructor = new(30);

    public static readonly RuntimeAtValueInitAssignmentEnum FromInitializer = new()
    {
        EnumIndex = 40
    };
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueCreation)]
internal partial class RuntimeAtValueCreationAssignmentEnum
{
    private RuntimeAtValueCreationAssignmentEnum()
    {
    }

    private RuntimeAtValueCreationAssignmentEnum(int enumIndex)
    {
        EnumIndex = enumIndex;
    }

    public static readonly RuntimeAtValueCreationAssignmentEnum FromConstructor = new(50);

    public static readonly RuntimeAtValueCreationAssignmentEnum FromInitializer = new()
    {
        EnumIndex = 60
    };
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.RuntimeAtValueCreation)]
internal partial class UnassignedRuntimeAtValueCreationEnum
{
    public static readonly UnassignedRuntimeAtValueCreationEnum Value = new();
}
