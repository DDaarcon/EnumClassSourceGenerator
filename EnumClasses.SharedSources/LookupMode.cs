namespace EnumClasses
{
    /// <summary>
    /// Specifies how lookup code is generated for operations such as deserialization.
    /// </summary>
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum LookupMode
#else
    public enum LookupMode
#endif
    {
        /// <summary>
        /// Lets the generator select an implementation based on the target framework and number of values.
        /// </summary>
        Automatic = 0,

        /// <summary>
        /// Generates a sequence of equality checks.
        /// </summary>
        ForceIfChain = 1,

        /// <summary>
        /// Generates a dictionary-based lookup.
        /// </summary>
        ForceDictionary = 2
    }
}
