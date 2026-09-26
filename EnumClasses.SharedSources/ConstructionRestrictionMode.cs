namespace EnumClasses
{
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum ConstructionRestrictionMode
#else
    public enum ConstructionRestrictionMode
#endif
    {
        /// <summary>
        /// All user-defined instance constructors must be either private or protected;
        /// a private parameterless constructor is generated when no instance constructor is declared.
        /// </summary>
        WithPrivateDefaultConstructor = 0,

        /// <summary>
        /// All user-defined instance constructors must be either private or protected;
        /// a protected parameterless constructor is generated when no instance constructor is declared.
        /// </summary>
        WithProtectedDefaultConstructor = 1,

        /// <summary>
        /// No restrictions on constructors.
        /// </summary>
        Off = 2
    }
}
