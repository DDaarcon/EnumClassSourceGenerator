using System;

namespace EnumClasses.SourceGenerators.Schema;

internal static class SchemaConsts
{
    public static class AttributeNames
    {
        static AttributeNames()
        {
            (EnumClassFullyQualified, EnumClass) = GetIdentification(typeof(EnumClassAttribute));
            (NumberedEnumClassFullyQualified, NumberedEnumClass) = GetIdentification(typeof(NumberedEnumClassAttribute));
            (EnumClassIgnoreFullyQualified, EnumClassIgnore) = GetIdentification(typeof(EnumClassIgnoreAttribute));
            (EnumClassValueFullyQualified, EnumClassValue) = GetIdentification(typeof(EnumClassValueAttribute));
            (EnumClassDefaultsFullyQualified, EnumClassDefaults) = GetIdentification(typeof(EnumClassDefaultsAttribute));
        }

        public static string EnumClass { get; }
        public static string EnumClassFullyQualified { get; }
        public static string NumberedEnumClass { get; }
        public static string NumberedEnumClassFullyQualified { get; }

        public static string EnumClassIgnore { get; }
        public static string EnumClassIgnoreFullyQualified { get; }

        public static string EnumClassValue { get; }
        public static string EnumClassValueFullyQualified { get; }

        public static string EnumClassDefaults { get; }
        public static string EnumClassDefaultsFullyQualified { get; }

        private static (string FullyQualifiedName, string Name) GetIdentification(Type type)
            => (
                FullyQualifiedName: $"{type.Namespace}.{type.Name}",
                Name: type.Name.Substring(0, type.Name.Length - "Attribute".Length)
            );
    }
}
