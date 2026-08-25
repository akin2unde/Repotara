using System.Text;
using System.Text.RegularExpressions;

namespace Repotara.Templating;

/// <summary>
/// Renders a template string by replacing each <c>{{ColumnName}}</c> tag with
/// the corresponding value from a <see cref="ReportRow"/>.
/// </summary>
public static partial class TemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([^{}]+?)\s*\}\}")]
    private static partial Regex TagPattern();

    /// <summary>Renders the template for a single row.</summary>
    public static string Render(string template, ReportRow row)
    {
        return TagPattern().Replace(template, match =>
        {
            var columnName = match.Groups[1].Value;
            var value = row.Get(columnName);
            return value?.ToString() ?? string.Empty;
        });
    }

    /// <summary>Renders the template once per row and concatenates the results.</summary>
    public static string RenderAll(string template, IEnumerable<ReportRow> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.Append(Render(template, row));
        }
        return builder.ToString();
    }
}
