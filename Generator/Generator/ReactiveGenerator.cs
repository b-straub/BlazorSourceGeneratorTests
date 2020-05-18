using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ReactivePropertyGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        private readonly List<(string fieldName, string propertyName, ITypeSymbol propertyType)> _fieldList = new List<(string fieldName, string propertyName, ITypeSymbol propertyType)>();
        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(SourceGeneratorContext context)
        {
            // add the attribute text
            context.AddSource("ReactivePropertyAttribute", SourceTextProvider.AttributeSource());

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
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceTextProvider.AttributeSource(), options));

            // get the newly bound attribute, and IReactiveProperty
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("ReactiveProperty.ReactivePropertyAttribute");
            INamedTypeSymbol baseSymbol = compilation.GetTypeByMetadataName("ReactiveProperty.ReactivePropertyImpl");
            INamedTypeSymbol interfaceSymbol = compilation.GetTypeByMetadataName("ReactiveProperty.IReactiveProperty");

            List<(IFieldSymbol, Location)> fieldSymbols = new List<(IFieldSymbol, Location)>();
            foreach (FieldDeclarationSyntax fieldDeclaration in receiver.CandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(fieldDeclaration.SyntaxTree);

                foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;

                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add((fieldSymbol, fieldDeclaration.GetLocation()));
                    }
                }
            }

            List<INamedTypeSymbol> classSymbols = new List<INamedTypeSymbol>();
            foreach (ClassDeclarationSyntax classDeclaration in receiver.CandidateClasses)
            {
                classSymbols.Add(classDeclaration.NamedTypeSymbol(compilation));
            }

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, (IFieldSymbol, Location)> group in fieldSymbols.GroupBy(f => f.Item1.ContainingType))
            {
                try
                {
                    var classDeclarationList = receiver.CandidateClasses
                        .Where(cd =>
                        {
                            return cd.NamedTypeSymbol(compilation).Equals(group.Key, SymbolEqualityComparer.Default);
                        });

                    var classDeclaration = classDeclarationList.FirstOrDefault();

                    if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        throw new GeneratorException(GeneratorException.Reason.Partial, classDeclaration.GetLocation(), group.Key.Name);
                    }

                    ProcessClass(group.Key.Name, (group.Key, classDeclaration.GetLocation()), group.ToList(), attributeSymbol, baseSymbol, interfaceSymbol, context);
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG000ClassGenerated(group.Key.Name), classDeclaration.GetLocation()));
                }
                catch (GeneratorException ex)
                {
                    ex.ReportDiagnostic(context, DiagnosticDescriptors.FilePath());
                }
            }
        }

        private void ProcessClass(string groupName, (INamedTypeSymbol classSymbol, Location location) cd, List<(IFieldSymbol, Location)> fields, ISymbol attributeSymbol, ISymbol baseSymbol, ISymbol interfaceSymbol, SourceGeneratorContext context)
        {
            // class must be top level
            if (!cd.classSymbol.ContainingSymbol.Equals(cd.classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                throw new GeneratorException(GeneratorException.Reason.TopLevel, cd.location, groupName);
            }

            // class must be derived from known base
            if (!cd.classSymbol.Interfaces.Contains(interfaceSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(GeneratorException.CreateDescriptor(GeneratorException.Reason.Interface, groupName), cd.location));
            }

            string namespaceName = cd.classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;

namespace {namespaceName}
{{
    public partial class {cd.classSymbol.Name} : {baseSymbol.ToDisplayString()}
    {{
");

            // create properties for each field
            _fieldList.Clear();
            foreach ((IFieldSymbol, Location) fieldDesriptor in fields)
            {
                ProcessField(source, fieldDesriptor, attributeSymbol);
            }

            source.Append("\r\n");

            source.Append(SourceTextProvider.ReactiveSource(_fieldList));
            source.Append("\t}\r\n}");

            var sourceText = SourceText.From(source.ToString(), Encoding.UTF8);

            //ReactivePropertyGeneratorExtensions.DumpGeneratedSource(sourceText, context);

            context.AddSource($"{groupName}_reactiveProperty.cs", sourceText);
        }

        private void ProcessField(StringBuilder source, (IFieldSymbol fieldSymbol, Location location) fd, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fd.fieldSymbol.Name;
            ITypeSymbol fieldType = fd.fieldSymbol.Type;

            // get the ReactiveAttribute attribute from the field, and any associated data
            AttributeData attributeData = fd.fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0)
            {
                throw new GeneratorException(GeneratorException.Reason.FieldEmpty, fd.location, fieldName);
            }
            if (propertyName == fieldName)
            {
                throw new GeneratorException(GeneratorException.Reason.FieldDuplicate, fd.location, propertyName);
            }

            _fieldList.Add((fieldName, propertyName, fieldType));

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

                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
                {
                    CandidateClasses.Add(classDeclarationSyntax);
                }
            }
        }
    }
}
