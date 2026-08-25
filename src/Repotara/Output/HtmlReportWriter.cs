using System.Text;
using Repotara.Definition;
using Repotara.Templating;

namespace Repotara.Output;

/// <summary>
/// Renders result rows as HTML. If <see cref="ReportDefinition.Template"/> is
/// set, renders that template once per row and concatenates the output.
/// Otherwise falls back to a plain HTML table.
/// </summary>
public sealed class HtmlReportWriter : IReportWriter
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Html;

    /// <inheritdoc />
    public string ContentType => "text/html";

    /// <inheritdoc />
    public string Write(IReadOnlyList<ReportRow> rows, ReportDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Template) == false)
        {
            return TemplateRenderer.RenderAll(definition.Template, rows);
        }

        return RenderDefaultTable(rows);
    }

    private static string RenderDefaultTable(IReadOnlyList<ReportRow> rows)
    {
        var builder = new StringBuilder();
        builder.Append("<table>");

        if (rows.Count > 0)
        {
            builder.Append("<thead><tr>");
            foreach (var (column, _) in rows[0].Values)
            {
                builder.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(column)).Append("</th>");
            }
            builder.Append("</tr></thead>");
        }

        builder.Append("<tbody>");
        foreach (var row in rows)
        {
            builder.Append("<tr>");
            foreach (var (_, value) in row.Values)
            {
                builder.Append("<td>").Append(System.Net.WebUtility.HtmlEncode(value?.ToString() ?? "")).Append("</td>");
            }
            builder.Append("</tr>");
        }
        builder.Append("</tbody></table>");

        return builder.ToString();
    }
}
