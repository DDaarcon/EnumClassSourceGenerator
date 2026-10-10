using EnumClasses;
using System.Diagnostics.CodeAnalysis;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.Runtime)]
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

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.Runtime)]
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

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.Runtime)]
internal partial class UnassignedRuntimeIndexedEnum
{
    [SetsRequiredMembers]
    private UnassignedRuntimeIndexedEnum()
    {
    }

    public static readonly UnassignedRuntimeIndexedEnum Value = new();
}

[EnumClass(IndexAssignmentMode = IndexAssignmentMode.Runtime)]
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
    IndexAssignmentMode = IndexAssignmentMode.Runtime,
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
