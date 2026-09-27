namespace EnumClasses
{
#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum LookupMode
#else
    public enum LookupMode
#endif
    {
        /// <summary>
        /// The lookup implementation is selected automatically.
        /// </summary>
        Automatic = 0,
        /// <summary>
        /// Forces matching with if chain.
        /// </summary>
        ForceIfChain = 1,
        /// <summary>
        /// Forces matching with a dictionary.
        /// </summary>
        ForceDictionary = 2
    }
}
