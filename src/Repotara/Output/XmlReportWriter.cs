using System.Text;
using System.Xml;
using Repotara.Definition;

namespace Repotara.Output;

/// <summary>Serializes result rows as an XML document with one &lt;Row&gt; element per record.</summary>
public sealed class XmlReportWriter : IReportWriter
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Xml;

    /// <inheritdoc />
    public string ContentType => "application/xml";

    /// <inheritdoc />
    public string Write(IReadOnlyList<ReportRow> rows, ReportDefinition definition)
    {
        var builder = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false };

        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartElement("Report");

            foreach (var row in rows)
            {
                writer.WriteStartElement("Row");

                foreach (var (column, value) in row.Values)
                {
                    var elementName = SanitizeElementName(column);
                    writer.WriteElementString(elementName, value?.ToString() ?? string.Empty);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        return builder.ToString();
    }

    private static string SanitizeElementName(string column)
    {
        var chars = column.Where(c => char.IsLetterOrDigit(c)).ToArray();
        return chars.Length == 0 ? "Field" : new string(chars);
    }
}
