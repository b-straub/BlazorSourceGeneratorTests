using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactivePropertyGenerator
{
    public static class SourceTextProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public static SourceText AttributeSource()
        {
            return SourceText.From(@"
using System;

namespace ReactiveProperty
{
    /// <summary>
    /// An reactive attribute that can only be attached to classes.
    /// <example>For example:
    /// <code>
    /// [ReactiveProperty(PropertyName = ""MagicNumber"")]
    ///    private int _magicNumberField = 42;
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class ReactivePropertyAttribute : Attribute
    {
        public ReactivePropertyAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
", Encoding.UTF8);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public static string ReactiveSource(IEnumerable<(string fieldName, string propertyName, ITypeSymbol propertyType)> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var fieldsGroup = fields.GroupBy(f => f.propertyType);

            var sb = new StringBuilder();

            sb.Append(@"
public void RegisterReactiveAction(Action action, string propertyName = null)
{
    if (string.IsNullOrEmpty(propertyName))
    {
        AddToDisposeBag(Changed
        .Subscribe(p => action()));
    }
    else
    {
        AddToDisposeBag(Changed
            .Where(p => p == propertyName)
            .Subscribe(p => action()));
    }
}");

            foreach (var group in fieldsGroup)
            {
                sb.Append($@"
public void RegisterReactiveAction(Action<{group.Key}> action, string propertyName)
{{
    AddToDisposeBag(Changed
        .Where(p => p == propertyName)
        .Select(p =>
        {{
            return propertyName switch
            {{");

                foreach (var field in group)
                {
                    sb.Append($@"""{field.propertyName}"" => {field.fieldName},");
                }

                sb.Append(@"
                _ => throw new ArgumentOutOfRangeException(propertyName)
            };
        })
        .Subscribe(v =>
        {
            action(v);
        }));
}");
            }

            sb.Append(@"
/*public void RegisterReactiveProperty<TS, TP>(TS sender, Expression<Func<TS, TP>> outExpr, Expression<Func<ReactiveTestViewModel, TP>> inExpr)
{
    if (outExpr == null)
    {
        throw new ArgumentNullException(nameof(outExpr));
    }

    var exprOut = (MemberExpression)outExpr.Body;
    var propOut = (PropertyInfo)exprOut.Member;

    if (inExpr == null)
    {
        throw new ArgumentNullException(nameof(outExpr));
    }

    var exprIn = (MemberExpression)inExpr.Body;
    var propIn = (PropertyInfo)exprIn.Member;

    AddToDisposeBag(Changed
        .Where(p => p == propIn.Name)
        .Select(_ => propIn.GetValue(this))
        .Subscribe(v =>
        {
            propOut.SetValue(sender, v, null);
        }));
}*/
");
            return sb.ToString();
        }
    }
}
