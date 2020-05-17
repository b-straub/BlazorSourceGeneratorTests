using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ReactivePropertyGenerator
{
    public class GeneratorException : Exception
    {
        public enum Reason
        {
            Unkown,
            [Description("RPG100")]
            Interface,
            [Description("RPG101")]
            Partial,
            [Description("RPG102")]
            TopLevel,
            [Description("RPG103")]
            FieldEmpty,
            [Description("RPG104")]
            FieldDuplicate
        }

        private Reason _reason = Reason.Unkown;
        private Location _location = Location.None;
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
        public GeneratorException(Reason reason, Location location, string reasonContext = "")
        {
            _reason = reason;
            _location = location;
            _reasonContext = reasonContext;
        }

        public void ReportDiagnostic(SourceGeneratorContext context, string filePath = "")
        {
            var location = _location;

            if (_location == Location.None)
            {
                location = DiagnosticDescriptors.GetLocation(filePath, LineNumber());
            }

            context.ReportDiagnostic(Diagnostic.Create(CreateDescriptor(_reason, _reasonContext), location));
        }

        public static DiagnosticDescriptor CreateDescriptor(Reason reason, string context) => reason switch
        {
            Reason.Unkown => throw new ArgumentOutOfRangeException(reason.ToString()),
            Reason.Interface => DiagnosticDescriptors.Interface(context, reason.Description()),
            Reason.Partial => DiagnosticDescriptors.Partial(context, reason.Description()),
            Reason.TopLevel => DiagnosticDescriptors.TopLevel(context, reason.Description()),
            Reason.FieldEmpty => DiagnosticDescriptors.FieldEmpty(context, reason.Description()),
            Reason.FieldDuplicate => DiagnosticDescriptors.FieldDuplicate(context, reason.Description()),
            _ => throw new ArgumentOutOfRangeException(reason.ToString())
        };

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
                "RPG1",
                "No SyntaxReceiver",
                "Can not populate SyntaxReceiver",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor Interface(string className, string id)
        {
            return new DiagnosticDescriptor(
                 $"{id}",
                "Base",
                $"{className} should mention interface IReactiveProperty",
                "Compilation",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor Partial(string className, string id)
        {
            return new DiagnosticDescriptor(
                $"{id}",
                "Partial",
                $"{className} must be declared partial",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor TopLevel(string className, string id)
        {
            return new DiagnosticDescriptor(
                $"{id}",
                "Top level",
                $"{className} must be at top level",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }

        public static DiagnosticDescriptor FieldEmpty(string fieldName, string id)
        {
            return new DiagnosticDescriptor(
                $"{id}",
                "PropertyName",
                $"Empty PropertyName {fieldName}",
                "Compilation",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
        }
        public static DiagnosticDescriptor FieldDuplicate(string fieldName, string id)
        {
            return new DiagnosticDescriptor(
                $"{id}",
                "PropertyName",
                $"PropertyName same as backing field {fieldName}",
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
