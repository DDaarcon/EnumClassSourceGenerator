using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses.SourceGenerators.GenerationStrategies;

internal enum LookupImplementation
{
    Undefined = -1,
    IfChain,
    Dictionary
}
