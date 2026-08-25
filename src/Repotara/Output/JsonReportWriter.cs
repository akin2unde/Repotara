using System.Text.Json;
using Repotara.Definition;

namespace Repotara.Output;

/// <summary>Serializes result rows as a JSON array of objects, keyed by column name.</summary>
public sealed class JsonReportWriter : IReportWriter
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Json;

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public string Write(IReadOnlyList<ReportRow> rows, ReportDefinition definition)
    {
        var payload = new List<Dictionary<string, object?>>();

        foreach (var row in rows)
        {
            var item = new Dictionary<string, object?>();
            foreach (var (column, value) in row.Values)
            {
                item[column] = value;
            }
            payload.Add(item);
        }

        return JsonSerializer.Serialize(payload);
    }
}
