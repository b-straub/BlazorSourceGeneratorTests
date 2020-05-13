using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public class ReactivePropertyGenerator : ISourceGenerator
    {
        private string attributeText = new StringBuilder().Append(@"
using System;

namespace ReactiveProperty
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class ReactivePropertyAttribute : Attribute
    {
        public ReactivePropertyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
").ToString();

        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(SourceGeneratorContext context)
        {
            // add the attribute text
            var attributeInterfaceSource = SourceText.From(attributeText, Encoding.UTF8);
            context.AddSource("ReactivePropertyAttribute", attributeInterfaceSource);

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                var loc = DiagnosticDescriptors.GetLocation(DiagnosticDescriptors.FilePath(), DiagnosticDescriptors.LineNumber());
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG100SyntaxReceiver(), loc));
                return;
            }

            // we're going to create a new compilation that contains the attribute and interface.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));
           
            // get the newly bound attribute, and IReactiveProperty
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("ReactiveProperty.ReactivePropertyAttribute");
            INamedTypeSymbol baseSymbol = compilation.GetTypeByMetadataName("ReactiveProperty.ReactivePropertyBase");

            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (FieldDeclarationSyntax field in receiver.CandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;

                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }

            List<INamedTypeSymbol> classSymbols = new List<INamedTypeSymbol>();
            foreach (ClassDeclarationSyntax partialClass in receiver.CandidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(partialClass.SyntaxTree);
                INamedTypeSymbol classSymbol = model.GetDeclaredSymbol(partialClass) as INamedTypeSymbol;
                classSymbols.Add(classSymbol);
            }

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                try
                {
                    if (!classSymbols.Any(cs => cs.Equals(group.Key, SymbolEqualityComparer.Default)))
                    {
                        throw new GeneratorException(GeneratorException.Reason.Partial, group.Key.Name);
                    }

                    ProcessClass(group.Key.Name, group.Key, group.ToList(), attributeSymbol, baseSymbol, context);
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG000ClassGenerated(group.Key.Name), Location.None));
                }
                catch(GeneratorException ex)
                {
                    ex.ReportDiagnostic(context, DiagnosticDescriptors.FilePath());
                }
            }
        }

        private static void ProcessClass(string groupName, INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol baseSymbol, SourceGeneratorContext context)
        {
            // class must be top level
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                throw new GeneratorException(GeneratorException.Reason.TopLevel, groupName);
            }

            // class must be derived from known base
            if (classSymbol.BaseType == null || !classSymbol.BaseType.Equals(baseSymbol, SymbolEqualityComparer.Default))
            {
                throw new GeneratorException(GeneratorException.Reason.Base, groupName);
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {baseSymbol.ToDisplayString()}
    {{
");
            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
            }

            source.Append("\t}\r\n}");

            var sourceText = SourceText.From(source.ToString(), Encoding.UTF8);

            context.AddSource($"{groupName}_reactiveProperty.cs", sourceText);
        }

        private static void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                throw new GeneratorException(GeneratorException.Reason.Field, propertyName);
            }

            source.Append($@"
        public {fieldType} {propertyName} 
        {{
            get 
            {{
                return this.{fieldName};
            }}

            set
            {{
                if (this.{fieldName} != value)
                {{
                    this.{fieldName} = value;
                    NotifyChange(nameof({propertyName}));
                }}    
            }}
        }}
");

            string chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper(CultureInfo.InvariantCulture);

                return fieldName.Substring(0, 1).ToUpper(CultureInfo.InvariantCulture) + fieldName.Substring(1);
            }
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields { get; } = new List<FieldDeclarationSyntax>();
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new List<ClassDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateFields.Add(fieldDeclarationSyntax);
                }

                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                   && classDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    CandidateClasses.Add(classDeclarationSyntax);
                }
            }
        }
    }
}
