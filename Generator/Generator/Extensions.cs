using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    }
}
