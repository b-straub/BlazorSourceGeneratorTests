using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace ReactivePropertyGenerator
{
    public static class ReactivePropertyGeneratorExtensions
    {
        public static INamedTypeSymbol NamedTypeSymbol(this ClassDeclarationSyntax cd, Compilation compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            SemanticModel model = compilation.GetSemanticModel(cd.SyntaxTree);
            return model.GetDeclaredSymbol(cd) as INamedTypeSymbol;
        }

        public static string Description(this Enum value)
        {
            return
                value
                    .GetType()
                    .GetMember(value.ToString())
                    .FirstOrDefault()
                    ?.GetCustomAttribute<DescriptionAttribute>()
                    ?.Description
                ?? value.ToString();
        }

        public static void DumpGeneratedSource(this SourceText sourceText, SourceGeneratorContext context)
        {
            if (sourceText == null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, options);
            var root = syntaxTree.GetRoot(context.CancellationToken).WithAdditionalAnnotations(Formatter.Annotation);

            using (Workspace adHoc = new AdhocWorkspace())
            {
                var formatOptions = adHoc.Options;
                //formatOptions = formatOptions.WithChangedOption(CSharpFormattingOptions.IndentBlock, true);
                //formatOptions = formatOptions.WithChangedOption(CSharpFormattingOptions.IndentBraces, true);

                var changes = Formatter.GetFormattedTextChanges(root, adHoc);

                root = Formatter.Format(root, Formatter.Annotation, adHoc, formatOptions, context.CancellationToken);
                changes = Formatter.GetFormattedTextChanges(root, adHoc);
            }

            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Write the sourcetext to a new file named "GS_Date_Time".
            var fileDate = DateTime.Now.ToString("dd-M-yyyy_HH-mm-ss", CultureInfo.InvariantCulture);
            var filename = $"GS_{fileDate}.cs";
         
            using (var outputFile = new StreamWriter(Path.Combine(docPath, filename)))
            {
                outputFile.Write(root.GetText(Encoding.UTF8).ToString());
            }
        }
    }
}
