using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;
using ReactivePropertyGenerator;

namespace Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void TestNoAttribute()
        {
            var source = @"
class C { }
";
            var generatorDiagnostics = GeneratorTestFactory.RunGenerator(source);
            Assert.False(generatorDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public void TestMissingBase()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(useInterface: false));

            Assert.True(generatorDiagnostics.Any(x => x.Id == GeneratorException.Reason.Interface.Description()));
        }

        [Fact]
        public void TestMissingPartial()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(partial: false));

            Assert.True(generatorDiagnostics.Any(x => x.Id == GeneratorException.Reason.Partial.Description()));
        }

        [Fact]
        public void TestTopLevel()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(topLevel: false));

            Assert.True(generatorDiagnostics.Any(x => x.Id == GeneratorException.Reason.TopLevel.Description()));
        }

        [Fact]
        public void TestFieldEmpty()
        {
            var generatorDiagnostics = GeneratorTestFactory
               .RunGenerator(GeneratorTestFactory.GenerateTestClass(attribute: @"[ReactiveProperty(PropertyName = """")]"));

            Assert.True(generatorDiagnostics.Any(x => x.Id == GeneratorException.Reason.FieldEmpty.Description()));
        }

        [Fact]
        public void TestFieldDuplicate()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(attribute: @"[ReactiveProperty(PropertyName = ""_testNumber1"")]"));

            Assert.True(generatorDiagnostics.Any(x => x.Id == GeneratorException.Reason.FieldDuplicate.Description()));
        }

        [Fact]
        public void TestSuccess()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass());

            Assert.True(generatorDiagnostics.Any(x => x.Id == "RPG000"));
        }
    }
}
