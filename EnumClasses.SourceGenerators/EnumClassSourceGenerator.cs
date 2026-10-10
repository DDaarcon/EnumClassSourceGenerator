using EnumClasses.SourceGenerators.Configurations;
using EnumClasses.SourceGenerators.Schema;
using EnumClasses.SourceGenerators.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace EnumClasses.SourceGenerators
{
    /*
     * TODO: 
     * For Numbered Enum Class verify whether assigned EnumIndex values are free - upgrade to compile-time validation
     * 
     */
    /// <summary>
    /// Generates enum-class members for classes marked with <c>EnumClass</c> attributes.
    /// </summary>
    [Generator]
    public class EnumClassSourceGenerator : IIncrementalGenerator
    {
        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var defaultsResults = context.CompilationProvider
                .Select(static (compilation, token) => EnumClassDefaultsCollector.Collect(compilation.Assembly));
            context.RegisterSourceOutput(defaultsResults, ReportDefaultsDiagnostics);
            var defaultsIncrementalProps = defaultsResults
                .Select(static (result, token) => result.Configuration);

            var basicAttributeIncrementalProps = context.SyntaxProvider.ForAttributeWithMetadataName(SchemaConsts.AttributeNames.EnumClassFullyQualified,
                predicate: CheckIfApplicable,
                transform: static (context, token) => ConstructClassDefinitions(context, token));
            context.RegisterSourceOutput(CombineDeclarationWithDefaults(basicAttributeIncrementalProps, defaultsIncrementalProps), GenerateEnumClass);
        }

        private static void ReportDefaultsDiagnostics(
            SourceProductionContext context,
            EnumClassDefaultsCollector.CollectResult result)
        {
            foreach (var diagnostic in result.DiagnosticReports)
                context.ReportDiagnostic(diagnostic);
        }


        private static bool CheckIfApplicable(SyntaxNode node, CancellationToken token)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration))
                return false;

            if (node is not ClassDeclarationSyntax)
                return false;

            return true;
        }


        private static EnumClassCollector.Definition.WithConfig ConstructClassDefinitions(GeneratorAttributeSyntaxContext context, CancellationToken token)
        {
            var classNode = (context.TargetNode as ClassDeclarationSyntax)!;

            return EnumClassCollector.CollectDefinition(
                component: classNode,
                componentSymbol: context.TargetSymbol,
                attribute: new EnumClassCollector.OurAttribute(
                    Data: context.Attributes.First()),
                context.SemanticModel,
                token);
        }

        private static IncrementalValuesProvider<EnumClassCollector.Definition.WithConfig> CombineDeclarationWithDefaults(
            IncrementalValuesProvider<EnumClassCollector.Definition.WithConfig> declarations,
            IncrementalValueProvider<Configuration> defaults)
            => declarations.Combine(defaults)
                .Select(static (item, token) => EnumClassCollector.ResolveConfiguration(
                    item.Left,
                    ConfigurationComposer.Compose(item.Left.ConfigurationOverrides, item.Right)));




        private static void GenerateEnumClass(SourceProductionContext context, EnumClassCollector.Definition.WithConfig props)
        {
            try
            {
                ReportErrors(context, props.Definition);

                if (props.Definition.Status is not
                    (EnumClassCollector.Definition.StatusCode.Ok
                        or EnumClassCollector.Definition.StatusCode.InvalidValues)) // continue rendering skipping invalid values 
                {
                    return;
                }

                if (props.Configuration.GenerateJsonConverter)
                {
                    context.AddSource($"{props.Definition.FullyQualifiedName}JsonConverter.g.cs",
                        SourceText.From(EnumClassSerializationConverterTemplate.Build(props), Encoding.UTF8));
                }
                context.AddSource($"{props.Definition.FullyQualifiedName}.g.cs",
                    SourceText.From(EnumClassDeclarationTemplate.Build(props), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostics.UnexpectedException(props.Definition.Location, ex.Message, props.Definition.DeclarationName ?? "N/A"));
            }
        }


        private static void ReportErrors(SourceProductionContext context, EnumClassCollector.Definition props)
        {
            foreach (var report in props.DiagnosticReports ?? [])
            {
                context.ReportDiagnostic(report);
            }
        }
    }
}
