using Microsoft.CodeAnalysis;

namespace EnumClasses.SourceGenerators.Diagnostics;

internal class EnumClassWarning
{
    public static Diagnostic RuntimeIndexesMayDifferFromRawEnumValues(Location? attributeLocation)
        => Diagnostic.Create(RuntimeIndexesMayDifferFromRawEnumValuesDescriptor, attributeLocation);
    public static readonly DiagnosticDescriptor RuntimeIndexesMayDifferFromRawEnumValuesDescriptor =
        new(
            id: "ENUMCLGEN015",
            title: "Runtime indexes may differ from generated Raw enum values",
            messageFormat: "Runtime EnumIndex values may not match the numeric values of the generated Raw enum, whose values follow declaration order",
            category: DiagnosticsShared.Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public static Diagnostic IndexProvidedToTheAttributeWillBeIgnoredForRuntimeIndexes(Location? attributeLocation)
        => Diagnostic.Create(IndexProvidedToTheAttributeWillBeIgnoredForRuntimeIndexesDescriptor, attributeLocation);
    public static readonly DiagnosticDescriptor IndexProvidedToTheAttributeWillBeIgnoredForRuntimeIndexesDescriptor =
        new(
            id: "ENUMCLGEN013",
            title: "Redundant index specification",
            messageFormat: "Index specified in EnumClassValueAttribute is ignored when IndexAssignmentMode uses runtime assignment",
            category: DiagnosticsShared.Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
}
