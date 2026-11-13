using Microsoft.CodeAnalysis;

namespace EnumClasses.SourceGenerators.Diagnostics;

internal class EnumClassWarning
{
    public static Diagnostic InitKeywordUnavailable(Location? attributeLocation, string declarationName)
        => Diagnostic.Create(InitKeywordUnavailableDescriptor, attributeLocation, declarationName);
    public static readonly DiagnosticDescriptor InitKeywordUnavailableDescriptor =
        new(
            id: "ENUMCLGEN018",
            title: "Runtime EnumIndex uses a mutable setter",
            messageFormat: "Enum Class '{0}' uses runtime EnumIndex assignment, but an init accessor is unavailable because init requires C# 9 or later and System.Runtime.CompilerServices.IsExternalInit. The generator is using a public set accessor instead. EnumIndex can therefore be changed after initialization, which may make index lookups inconsistent, especially after a lookup dictionary has been created.",
            category: DiagnosticsShared.Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

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
