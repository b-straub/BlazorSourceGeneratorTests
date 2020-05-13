using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    public class GeneratorException : Exception
    {
        public enum Reason
        {
            Unkown,
            Partial,
            TopLevel,
            Field,
            Base
        }

        private Reason _reason = Reason.Unkown;
        private string _reasonContext = "";
     
        public GeneratorException()
        {
        }

        public GeneratorException(string message)
            : base(message)
        {
        }

        public GeneratorException(string message, Exception inner)
            : base(message, inner)
        {
        }
        public GeneratorException(Reason reason, string reasonContext = "")
        {
            _reason = reason;
            _reasonContext = reasonContext;
        }
        public void ReportDiagnostic(SourceGeneratorContext context, string filePath = "")
        {
            var loc = DiagnosticDescriptors.GetLocation(filePath, LineNumber());

            switch (_reason)
            {
                case Reason.Unkown: 
                    throw new ArgumentOutOfRangeException(_reason.ToString());
                case Reason.Partial:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG101Partial(_reasonContext), loc));
                    break;
                case Reason.TopLevel:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG102TopLevel(_reasonContext), loc));
                    break;
                case Reason.Field:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG103Field(_reasonContext), loc));
                    break;
                case Reason.Base:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.RPG104Base(_reasonContext), loc));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(_reason.ToString());
            }
        }

        private int LineNumber()
        {
            Match match = Regex.Match(StackTrace, @":\w*\s(?<line>\d+)");
            if (match.Success)
            {
                return Convert.ToInt32(match.Groups["line"].Value, CultureInfo.InvariantCulture);
            }

            return -1;
        }
    }

    public static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor RPG100SyntaxReceiver()
        {
            return new DiagnosticDescriptor(
                "RPG100",
                "No SyntaxReceiver",
                "Can not populate SyntaxReceiver",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor RPG101Partial(string className)
        {
            return new DiagnosticDescriptor(
                "RPG101",
                "Partial",
                $"{className} must be declared partial",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor RPG102TopLevel(string className)
        {
            return new DiagnosticDescriptor(
                "RPG102",
                "Top level",
                $"{className} must be at top level",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor RPG103Field(string fieldName)
        {
            return new DiagnosticDescriptor(
                "RPG103",
                "Property generation",
                $"Invalid property {fieldName}",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }
        public static DiagnosticDescriptor RPG104Base(string className)
        {
            return new DiagnosticDescriptor(
                "RPG104",
                "Base",
                $"{className} must be derived from ReactivePropertyBase",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor RPG000ClassGenerated(string className)
        {
            return new DiagnosticDescriptor(
                "RPG000",
                "Class generation",
                $"Sucessfully generated reactive properties for {className}",
                "Compilation",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor Info(string title, string message)
        {
            return new DiagnosticDescriptor(
                "Info",
                title,
                message,
                "Compilation",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: false);
        }

        public static string FilePath([System.Runtime.CompilerServices.CallerFilePath] string filePath = "")
        {
            return filePath;
        }

        public static int LineNumber([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return lineNumber;
        }

        public static Location GetLocation(string path, int line)
        {
            var linePosition = new LinePosition(line, 0);
            return Location.Create(path, new TextSpan(0, 0), new LinePositionSpan(linePosition, linePosition));
        }
    }
}
