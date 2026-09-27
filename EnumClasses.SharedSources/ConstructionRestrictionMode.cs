namespace EnumClasses
{
    /// <summary>
    /// Specifies how the generator restricts and supplies enum-class constructors.
    /// </summary>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum ConstructionRestrictionMode
#else
    public enum ConstructionRestrictionMode
#endif
    {
        /// <summary>
        /// Requires user-declared instance constructors to be private or protected and generates a private
        /// parameterless constructor when none is declared.
        /// </summary>
        WithPrivateDefaultConstructor = 0,

        /// <summary>
        /// Requires user-declared instance constructors to be private or protected and generates a protected
        /// parameterless constructor when none is declared.
        /// </summary>
        WithProtectedDefaultConstructor = 1,

        /// <summary>
        /// Does not validate or generate instance constructors.
        /// </summary>
        Off = 2
    }
}
