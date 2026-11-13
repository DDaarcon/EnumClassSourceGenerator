# EnumClasses

EnumClasses is a Roslyn source generator for defining type-safe, class-based enum values while retaining generated lookup, serialization, matching, and optional raw-enum APIs.

The consumer-facing guide—including installation, examples, generated API, configuration, indexing rules, JSON behavior, diagnostics, requirements, and limitations—is maintained in the [NuGet package README](EnumClasses.Package/README.md).

## Repository layout

- `EnumClasses.Package` builds the `DDaarcon.EnumClasses` NuGet package and contains its README.
- `EnumClasses.SourceGenerators` contains the incremental source generator.
- `EnumClasses.SharedSources` contains the public attributes and option types shared by the package and generator.
- `EnumClassSourceGenerator.UnitTests` and `EnumClassSourceGenerator.UnitTestsLite` exercise runtime and generator behavior.
- `EnumClasses.CompatibilityTesting.NetStandard2` verifies C# 7 source generation against the `netstandard2.0` API surface.
- `EnumClasses.CompatibilityTesting.Net462`, `EnumClasses.CompatibilityTesting.Net6`, and `EnumClasses.CompatibilityTesting.Net8` exercise generated code on representative consumer runtimes.
- `EnumClasses.CompatibilityTesting.NugetPackage` packs the project and verifies the resulting package through a real `PackageReference`.
- `EnumClassSourceGenerator.Benchmarks` contains lookup benchmarks.

## Build and test

The repository requires a .NET SDK capable of building the solution's .NET 10 test projects.

```console
dotnet build EnumClassSourceGenerator.slnx
dotnet test EnumClassSourceGenerator.slnx
dotnet pack EnumClasses.Package/EnumClasses.Package.csproj
```

The packed-package compatibility test is not part of the solution. It creates a local package during restore and consumes it through `PackageReference`:

```console
dotnet test EnumClasses.CompatibilityTesting.NugetPackage/EnumClasses.CompatibilityTesting.NugetPackage.csproj --configuration Release
```

The package provides `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0` assets. Generated code adapts to the consumer's language and framework capabilities; see the [package requirements](EnumClasses.Package/README.md#requirements) for the supported feature combinations.
