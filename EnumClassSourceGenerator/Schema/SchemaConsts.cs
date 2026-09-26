using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses.SourceGenerators.Schema;

internal static class SchemaConsts
{
    public static class AttributeNames
    {
        public const string EnumClass = "EnumClass";
        public const string EnumClassFullyQualified = "EnumClasses.EnumClassAttribute";
        public const string NumberedEnumClass = "NumberedEnumClass";
        public const string NumberedEnumClassFullyQualified = "EnumClasses.NumberedEnumClassAttribute";

        public const string EnumClassIgnore = "EnumClassIgnore";
    }
}
