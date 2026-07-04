using GenEnumClass;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClassSourceGenerator.UnitTests.Tests;

[EnumClass]
internal partial class BasicEnum
{
    public static readonly BasicEnum One = new()
    {
        Value = "SomeValueOne"
    };
    public static readonly BasicEnum Two = new()
    {
        Value = "SomeValueTwo"
    };
    public static readonly BasicEnum Three = new()
    {
        Value = "SomeValueThree"
    };
    public static readonly BasicEnum Four = new()
    {
        Value = "SomeValueFour"
    };


    public required string Value { get; init; }
}
