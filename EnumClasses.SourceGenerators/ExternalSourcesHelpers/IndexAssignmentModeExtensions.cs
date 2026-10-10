using System;
using System.Collections.Generic;
using System.Text;

namespace EnumClasses.SourceGenerators.ExternalSourcesHelpers;

internal static class IndexAssignmentModeExtensions
{
    extension(IndexAssignmentMode mode)
    {
        public bool IsRuntime
            => mode is IndexAssignmentMode.RuntimeAtValueInit
                or IndexAssignmentMode.RuntimeAtValueCreation;
    }
}
