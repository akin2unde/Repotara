using Repotara.Definition;

namespace Repotara.Aggregation;

/// <summary>
/// Groups already-flattened rows by the requested group-by columns and applies
/// aggregators to the remaining fields. Used as the in-memory fallback path;
/// native providers perform grouping in the database instead.
/// </summary>
public static class GroupingEngine
{
    /// <summary>
    /// Groups and aggregates rows according to the definition's GroupBy and
    /// field aggregate settings. Rows must already carry values keyed by output
    /// display name matching <paramref name="definition"/>'s field list.
    /// </summary>
    public static List<ReportRow> GroupAndAggregate(List<ReportRow> rows, ReportDefinition definition)
    {
        if (definition.GroupBy == null || definition.GroupBy.Count == 0)
        {
            return rows;
        }

        var groups = new Dictionary<string, List<ReportRow>>();
        var groupKeyValues = new Dictionary<string, List<object?>>();

        foreach (var row in rows)
        {
            var keyParts = new List<string>();
            var keyValues = new List<object?>();

            foreach (var groupColumn in definition.GroupBy)
            {
                var value = row.Get(groupColumn);
                keyValues.Add(value);
                keyParts.Add(value?.ToString() ?? "\u0000null\u0000");
            }

            var key = string.Join("\u0001", keyParts);

            if (groups.TryGetValue(key, out var bucket) == false)
            {
                bucket = [];
                groups[key] = bucket;
                groupKeyValues[key] = keyValues;
            }

            bucket.Add(row);
        }

        var result = new List<ReportRow>();

        foreach (var (key, bucket) in groups)
        {
            var outputRow = new ReportRow();
            var keyValues = groupKeyValues[key];

            for (var i = 0; i < definition.GroupBy.Count; i++)
            {
                outputRow.Set(definition.GroupBy[i], keyValues[i]);
            }

            foreach (var field in definition.Fields)
            {
                if (field.Aggregate == null)
                {
                    continue;
                }

                var columnName = field.DisplayName ?? field.Field ?? "Value";
                var rawValues = new List<object?>();

                foreach (var bucketRow in bucket)
                {
                    rawValues.Add(bucketRow.Get(columnName));
                }

                var aggregator = AggregatorFactory.Create(field.Aggregate.Value);
                outputRow.Set(columnName, aggregator.Compute(rawValues));
            }

            result.Add(outputRow);
        }

        return result;
    }
}
