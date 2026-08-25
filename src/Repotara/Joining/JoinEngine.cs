using Repotara.Definition;

namespace Repotara.Joining;

/// <summary>
/// Performs an in-memory equi-join across any number of chained sources. Used
/// as the fallback path when a provider cannot express a particular join
/// natively; the SQL and MongoDB providers normally push joins down instead.
/// </summary>
public static class JoinEngine
{
    /// <summary>
    /// Flattens the given per-source object lists into composite rows according
    /// to the join chain. Each output row exposes values under
    /// "SourceName.PropertyName" keys via a lookup delegate supplied by the caller.
    /// </summary>
    public static List<Dictionary<string, object?>> Flatten(
        Dictionary<string, List<Dictionary<string, object?>>> sources,
        List<JoinDefinition> joins)
    {
        if (joins.Count == 0)
        {
            var onlySourceName = sources.Keys.First();
            return sources[onlySourceName];
        }

        var firstJoin = joins[0];
        var combined = JoinTwo(sources[firstJoin.Left], firstJoin.Left, sources[firstJoin.Right], firstJoin.Right,
            firstJoin.LeftKey, firstJoin.RightKey, firstJoin.Type);

        for (var i = 1; i < joins.Count; i++)
        {
            var join = joins[i];
            var rightRows = sources[join.Right];
            combined = JoinTwo(combined, null, rightRows, join.Right, join.LeftKey, join.RightKey, join.Type);
        }

        return combined;
    }

    private static List<Dictionary<string, object?>> JoinTwo(
        List<Dictionary<string, object?>> leftRows,
        string? leftPrefix,
        List<Dictionary<string, object?>> rightRows,
        string rightPrefix,
        string leftKey,
        string rightKey,
        JoinType joinType)
    {
        var leftKeyColumn = leftPrefix == null ? leftKey : leftPrefix + "." + leftKey;
        var rightKeyColumn = rightPrefix + "." + rightKey;

        var rightIndex = new Dictionary<object, List<Dictionary<string, object?>>>();
        foreach (var rightRow in rightRows)
        {
            var key = rightRow[rightKeyColumn];
            if (key == null)
            {
                continue;
            }

            if (rightIndex.TryGetValue(key, out var bucket) == false)
            {
                bucket = [];
                rightIndex[key] = bucket;
            }
            bucket.Add(rightRow);
        }

        var result = new List<Dictionary<string, object?>>();

        foreach (var leftRow in leftRows)
        {
            var key = leftRow.TryGetValue(leftKeyColumn, out var value) ? value : null;
            var matches = key != null && rightIndex.TryGetValue(key, out var bucket) ? bucket : null;

            if (matches == null || matches.Count == 0)
            {
                if (joinType == JoinType.Left)
                {
                    result.Add(new Dictionary<string, object?>(leftRow));
                }
                continue;
            }

            foreach (var rightRow in matches)
            {
                var merged = new Dictionary<string, object?>(leftRow);
                foreach (var (column, columnValue) in rightRow)
                {
                    merged[column] = columnValue;
                }
                result.Add(merged);
            }
        }

        return result;
    }
}
