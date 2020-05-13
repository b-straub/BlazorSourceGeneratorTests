using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

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
        public void TestTopLevel()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(topLevel: false));

            Assert.True(generatorDiagnostics.Any(x => x.Id == "RPG102"));
        }

        [Fact]
        public void TestMissingPartial()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(partial: false));

            Assert.True(generatorDiagnostics.Any(x => x.Id == "RPG101"));
        }

        [Fact]
        public void TestMissingBase()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(baseClass: false));

            Assert.True(generatorDiagnostics.Any(x => x.Id == "RPG104"));
        }

        [Fact]
        public void TestDuplicateName()
        {
            var generatorDiagnostics = GeneratorTestFactory
                .RunGenerator(GeneratorTestFactory.GenerateTestClass(attribute: @"[ReactiveProperty(PropertyName = ""_test"")]"));

            Assert.True(generatorDiagnostics.Any(x => x.Id == "RPG103"));
        }

        [Fact]
        public void TestEmptyName()
        {
            var generatorDiagnostics = GeneratorTestFactory
               .RunGenerator(GeneratorTestFactory.GenerateTestClass(attribute: @"[ReactiveProperty(PropertyName = """")]"));

            Assert.True(generatorDiagnostics.Any(x => x.Id == "RPG103"));
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
