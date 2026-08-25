using System.Text.Json;
using Repotara.Definition;

namespace Repotara.Output;

/// <summary>
/// Serializes result rows as a chart-ready { labels, datasets } JSON shape,
/// directly usable by charting libraries such as Chart.js or Recharts. Uses
/// the first <see cref="ReportDefinition.GroupBy"/> column as the label and
/// every aggregate field as a dataset.
/// </summary>
public sealed class ChartReportWriter : IReportWriter
{
    /// <inheritdoc />
    public OutputFormat Format => OutputFormat.Chart;

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public string Write(IReadOnlyList<ReportRow> rows, ReportDefinition definition)
    {
        if (definition.GroupBy == null || definition.GroupBy.Count == 0)
        {
            throw new InvalidOperationException("Chart output requires at least one GroupBy field.");
        }

        var labelColumn = definition.GroupBy[0];
        var aggregateFields = definition.Fields.Where(f => f.Aggregate != null).ToList();

        if (aggregateFields.Count == 0)
        {
            throw new InvalidOperationException("Chart output requires at least one aggregate field.");
        }

        var labels = new List<object?>();
        foreach (var row in rows)
        {
            labels.Add(row.Get(labelColumn));
        }

        var datasets = new List<object>();
        foreach (var field in aggregateFields)
        {
            var columnName = field.DisplayName ?? field.Field ?? "Value";
            var data = new List<object?>();

            foreach (var row in rows)
            {
                data.Add(row.Get(columnName));
            }

            datasets.Add(new { label = columnName, data });
        }

        var chart = new { labels, datasets };
        return JsonSerializer.Serialize(chart);
    }
}
