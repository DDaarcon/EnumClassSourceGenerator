using System;

namespace EnumClasses
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
#if ENUMCLASSES_SOURCE_GENERATOR
    internal class EnumClassDefaultsAttribute : Attribute
#else
    public class EnumClassDefaultsAttribute : Attribute
#endif
    {
        /// <summary>
        /// Flag enabling generation of a custom <see cref="Text.Json.Serialization.JsonConverter{T}"/> for the enum class. Defaults to <see cref="DefaultGenerateJsonConverter">.
        /// </summary>
        public bool GenerateJsonConverter { get; set; } = DefaultGenerateJsonConverter;
        public const bool DefaultGenerateJsonConverter = true;

        /// <summary>
        /// Flag enabling generation of a nested <c>Raw</c> enum and conversions between it and the enum class. Defaults to <see cref="DefaultGenerateRawEnum"/>.
        /// </summary>
        public bool GenerateRawEnum { get; set; } = DefaultGenerateRawEnum;
        public const bool DefaultGenerateRawEnum = false;

        /// <summary>
        /// Specifies the method for searching/matching values. Used, among others, in deserialization. Defaults to <see cref="DefaultSearchMode"/>.
        /// </summary>
        public SearchMode SearchMode { get; set; } = DefaultSearchMode;
        public const SearchMode DefaultSearchMode = SearchMode.Optimized;

        /// <summary>
        /// Controls the constructors restrictions. Defaults to <see cref="DefaultConstructionRestrictionMode" />.
        /// </summary>
        public ConstructionRestrictionMode ConstructionRestrictionMode { get; set; } = DefaultConstructionRestrictionMode;
        public const ConstructionRestrictionMode DefaultConstructionRestrictionMode = ConstructionRestrictionMode.WithProtectedDefaultConstructor;


        // NumberedEnumClass

        /// <summary>
        /// Flag controlling presence of `require` keyword for `EnumIndex` property. Defaults to <see cref="DefaultRequireIndexAssignmentInInitializer"/>.
        /// </summary>
        public bool RequireIndexAssignmentInInitializer { get; set; } = DefaultRequireIndexAssignmentInInitializer;
        public const bool DefaultRequireIndexAssignmentInInitializer = true;

    }
}
