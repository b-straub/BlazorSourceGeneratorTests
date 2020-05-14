using System.Collections.Immutable;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Generator;
using ReactiveProperty;

namespace Tests
{
    public static class GeneratorTestFactory
    {
        public static string GenerateTestClass(bool baseClass = true, bool partial = true, bool topLevel = true, string attribute = "[ReactiveProperty]")
        {
            var sb = new StringBuilder();

            sb.Append("using System;\r\n");
            sb.Append("using ReactiveProperty;\r\n\r\n");
            sb.Append("namespace ReactivePropertyTest\r\n");
            sb.Append("{\r\n");

            if (!topLevel)
            {
                partial = true;
                sb.Append("\tpublic partial class Foo\r\n");
                sb.Append("\t{\r\n");
            }

            sb.Append($"\t\tpublic {(partial ? "partial" : "")} class TestClass {(baseClass ? " : ReactivePropertyBase" : "")}\r\n");
            sb.Append("\t\t{\r\n");
            sb.Append($"\t\t\t{attribute}\r\n");
            sb.Append("\t\t\tprivate string _testString = \"Test\";\r\n");
            sb.Append("\r\n");
            sb.Append($"\t\t\t{attribute}\r\n");
            sb.Append("\t\t\tprivate int _testNumber = 42;\r\n");
            sb.Append("\t\t}\r\n");

            if (!topLevel)
            {
                sb.Append("\t}\r\n");
                sb.Append("}");
            }
            else
            {
                sb.Append("}");
            }

            return sb.ToString();
        }


        public static ImmutableArray<Diagnostic> RunGenerator(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source, Encoding.UTF8));

            //var parseOptions = TestOptions.Regular;
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Default);

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ReactivePropertyBase).Assembly.Location)
            };

            Compilation compilation = CSharpCompilation.Create("testgenerator", new[] { syntaxTree }, references, compilationOptions);
            var diagnostics = compilation.GetDiagnostics();
            if (!VerifyDiagnostics(diagnostics, new[] { "CS0012", "CS0616", "CS0246" }))
            {
                // this will make the test fail, check the input source code!
                return diagnostics;
            }

            var generator = new ReactivePropertyGenerator();
            var parseOptions = syntaxTree.Options as CSharpParseOptions;

            GeneratorDriver driver = new CSharpGeneratorDriver(parseOptions, ImmutableArray.Create<ISourceGenerator>(generator), ImmutableArray<AdditionalText>.Empty);
            driver.RunFullGeneration(compilation, out var outputCompilation, out var generatorDiagnostics);

            return generatorDiagnostics;
        }

        public static bool VerifyDiagnostics(ImmutableArray<Diagnostic> actual, string [] expected)
        {
            return actual.Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.Id.ToString())
                    .All(id => expected.Contains(id)); ;
        }
    }
}
