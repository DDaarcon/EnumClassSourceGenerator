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
     * Nonenumerable value attribute - to ignore static props and fields that would normally be considered an enum value
     * For Numbered Enum Class verify whether assigned EnumIndex values are free - upgrade to compile-time validation
     * 
     */
    [Generator]
    public class EnumClassSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var basicAttributeIncrementalProps = context.SyntaxProvider.ForAttributeWithMetadataName(SchemaConsts.AttributeNames.EnumClassFullyQualified,
                predicate: CheckIfApplicable,
                transform: static (context, token) => ConstructModels(context, EnumClass.OurAttributeType.EnumClass, token));
            context.RegisterSourceOutput(basicAttributeIncrementalProps, GenerateEnumClass);

            var numberedAttributeIncrementalProps = context.SyntaxProvider.ForAttributeWithMetadataName(SchemaConsts.AttributeNames.NumberedEnumClassFullyQualified,
                predicate: CheckIfApplicable,
                transform: static (context, token) => ConstructModels(context, EnumClass.OurAttributeType.NumberedEnumClass, token));
            context.RegisterSourceOutput(numberedAttributeIncrementalProps, GenerateEnumClass);
        }


        private static bool CheckIfApplicable(SyntaxNode node, CancellationToken token)
        {
            if (!node.IsKind(SyntaxKind.ClassDeclaration))
                return false;

            if (node is not ClassDeclarationSyntax classNode)
                return false;

            return true;
        }


        private static EnumClass.Definition ConstructModels(GeneratorAttributeSyntaxContext context, EnumClass.OurAttributeType attributeType, CancellationToken token)
        {
            //Debug.Debugging.Breakpoint();
            var classNode = (context.TargetNode as ClassDeclarationSyntax)!;

            return EnumClass.CollectDefinition(
                component: classNode,
                componentSymbol: context.TargetSymbol,
                attribute: new EnumClass.OurAttribute(
                    Type: attributeType,
                    Data: context.Attributes.First()),
                context.SemanticModel,
                token);
        }

        private static void GenerateEnumClass(SourceProductionContext context, EnumClass.Definition props)
        {
            try
            {
                ReportErrors(context, props);

                if (props.Status is not
                    (EnumClass.Definition.StatusCode.Ok
                        or EnumClass.Definition.StatusCode.InvalidValues)) // continue rendering skipping invalid values 
                {
                    return;
                }

                if (props.Config.GenerateJsonConverter)
                {
                    context.AddSource($"{props.FullyQualifiedName}JsonConverter.g.cs",
                        SourceText.From(EnumClassSerializationConverterTemplate.Build(props), Encoding.UTF8));
                }
                context.AddSource($"{props.FullyQualifiedName}.g.cs",
                    SourceText.From(EnumClassDeclarationTemplate.Build(props), Encoding.UTF8));
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostics.UnexpectedException(props.Location, ex.Message, props.DeclarationName ?? "N/A"));
            }
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
