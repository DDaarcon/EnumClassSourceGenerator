using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses
{

#if ENUMCLASSES_SOURCE_GENERATOR
    internal enum DeserializationMode
#else
    public enum DeserializationMode
#endif
    {
        /// <summary>
        /// The matching method is decided based on the amount of values.
        /// </summary>
        Optimized,
        /// <summary>
        /// Forces matching with if chain.
        /// </summary>
        ForceIfChain,
        /// <summary>
        /// Forces matching with a dictionary.
        /// </summary>
        ForceDictionary
    }
}
