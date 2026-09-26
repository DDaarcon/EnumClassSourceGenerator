using EnumClassSourceGenerator.Schema;
using EnumClassSourceGenerator.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace EnumClassSourceGenerator
{
    /*
     * TODO: 
     * Auto generated switch method
     * Nonenumerable value attribute - to ignore static props and fields that would normally be considered an enum value
     * For Numbered Enum Class verify whether assigned EnumIndex values are free - upgrade to compile-time validation
     * 
     */
    [Generator]
    public class EnumClassSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG && false
            System.Diagnostics.Debugger.Launch();
#endif
            context.RegisterSourceOutput(context.CompilationProvider, static (context, compilation) =>
            {
                if (compilation.GetTypeByMetadataName("GenEnumClass.BaseEnumClassAttribute") is null)
                {
                    context.AddSource("EnumClassAttribute.g.cs", SourceText.From(EnumClassAttributesTemplate.Template, Encoding.UTF8));
                }
            });

            var incrementalEnumDeclarationProps = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: CheckIfApplicable,
                transform: ConstructModels);

            context.RegisterSourceOutput(incrementalEnumDeclarationProps, static (context, props) =>
            {
                try
                {
                    ReportErrors(context, props);

                    if (props.Status is not (EnumClass.Definition.StatusCode.Ok
                        or EnumClass.Definition.StatusCode.InvalidValues)) // continue rendering skipping invalid values 
                    {
                        return;
                    }

                    if (props.Config.GenerateJsonConverter)
                    {
                        context.AddSource($"{props.DeclarationName}JsonConverter.g.cs",
                            SourceText.From(EnumClassSerializationConverterTemplate.Build(props), Encoding.UTF8));
                    }
                    context.AddSource($"{props.DeclarationName}.g.cs",
                        SourceText.From(EnumClassDeclarationTemplate.Build(props), Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostics.UnexpectedException(props.Location, ex.Message, props.DeclarationName ?? "N/A"));
                }
            });
        }


        private static bool CheckIfApplicable(SyntaxNode node, CancellationToken token)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration))
                return false;

            if (node is not ClassDeclarationSyntax classNode)
                return false;

            var attr = EnumClass.FindOurAttribute(classNode);

            return attr.Type is not EnumClass.OurAttributeType.None;
        }


        private static EnumClass.Definition ConstructModels(GeneratorSyntaxContext context, CancellationToken token)
        {
            var classNode = (context.Node as ClassDeclarationSyntax)!;

            return EnumClass.CollectDefinition(classNode, context.SemanticModel, token);
        }


        private static void ReportErrors(SourceProductionContext context, EnumClass.Definition props)
        {
            foreach (var report in props.DiagnosticReports ?? [])
            {
                context.ReportDiagnostic(report);
            }
        }
    }
}
