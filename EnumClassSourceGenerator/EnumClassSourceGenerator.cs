using EnumClassSourceGenerator.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;
using System.Threading;

namespace EnumClassSourceGenerator
{
    /*
     * TODO: 
     * Optional auto generation of enum when flag in attribute is set
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
            context.RegisterPostInitializationOutput((context) =>
            {
                context.AddSource("EnumClassAttribute.g.cs", SourceText.From(Templates.EnumClassAttribute, Encoding.UTF8));
            });

            var incrementalEnumDeclarationProps = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: CheckIfApplicable,
                transform: ConstructModels);

            context.RegisterSourceOutput(incrementalEnumDeclarationProps, (context, props) =>
            {
                try
                {
                    if (props.Status is not EnumClass.Definition.StatusCode.Ok)
                    {
                        ReportErrors(context, props);
                    }

                    if (props.Status is not (EnumClass.Definition.StatusCode.Ok
                        or EnumClass.Definition.StatusCode.InvalidValues)) // 
                    {
                        return;
                    }

                    if (props.Config.GenerateJsonConverter)
                    {
                        context.AddSource($"{props.DeclarationName}JsonConverter.g.cs",
                            SourceText.From(Templates.BuildEnumClassSerializationConvertedDefinition(props), Encoding.UTF8));
                    }
                    context.AddSource($"{props.DeclarationName}.g.cs",
                        SourceText.From(Templates.BuildEnumClassDeclaration(props), Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnexpectedException, props.Location, ex.Message));
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
            switch (props.Status)
            {
                default:
                case EnumClass.Definition.StatusCode.Ok:
                case EnumClass.Definition.StatusCode.NonApplicable:
                    return;
                case EnumClass.Definition.StatusCode.NamespaceNotFound:
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NamespaceNotFound, props.Location));
                    return;
                case EnumClass.Definition.StatusCode.InvalidModifiers:
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidModifiers, props.Location));
                    return;
                case EnumClass.Definition.StatusCode.InvalidValues:
                    foreach (var enumValue in props.EnumValues!.Value.Definitions)
                    {
                        if (enumValue.HasInvalidType)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidEnumValueType, enumValue.Location, enumValue.Name, props.DeclarationName));
                        }

                        if (enumValue.HasInvalidAccessors)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidEnumValueAccessors, enumValue.Location, enumValue.Name, props.DeclarationName));
                        }
                    }
                    return;
            }
        }
    }
}
