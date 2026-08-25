using Repotara.Definition;

namespace Repotara.Filtering;

/// <summary>
/// Evaluates a <see cref="SearchParam"/> tree against an in-memory
/// <see cref="ReportRow"/>. Used as a fallback for scenarios a provider cannot
/// push all the way down to the database (e.g. the post-join, pre-aggregation
/// step when working with already-fetched rows).
/// </summary>
public static class FilterEvaluator
{
    /// <summary>Evaluates the given node against a row. A null node always matches.</summary>
    public static bool Matches(SearchParam? node, ReportRow row)
    {
        if (node == null)
        {
            return true;
        }

        if (node.IsBranch)
        {
            var isOr = string.Equals(node.Operator, "Or", StringComparison.OrdinalIgnoreCase);

            foreach (var condition in node.Conditions!)
            {
                var conditionMatches = Matches(condition, row);

                if (isOr && conditionMatches)
                {
                    return true;
                }

                if (isOr == false && conditionMatches == false)
                {
                    return false;
                }
            }

            return isOr == false;
        }

        var left = row.Get(node.Property!);
        var right = node.ValueProperty != null ? row.Get(node.ValueProperty) : node.Value;

        return Compare(left, right, node.Operation!);
    }

    private static bool Compare(object? left, object? right, string operation)
    {
        if (operation == "EQ")
        {
            return Equals(left, right);
        }

        if (operation == "NEQ")
        {
            return Equals(left, right) == false;
        }

        if (operation == "IN")
        {
            if (right is System.Collections.IEnumerable values and not string)
            {
                foreach (var value in values)
                {
                    if (Equals(left, value))
                    {
                        return true;
                    }
                }
                return false;
            }
            return Equals(left, right);
        }

        if (operation == "CONTAINS")
        {
            var leftText = left?.ToString() ?? "";
            var rightText = right?.ToString() ?? "";
            return leftText.Contains(rightText, StringComparison.OrdinalIgnoreCase);
        }

        if (left is IComparable leftComparable && right != null)
        {
            var comparison = leftComparable.CompareTo(Convert.ChangeType(right, left.GetType()));

            if (operation == "GT")
            {
                return comparison > 0;
            }
            if (operation == "GTE")
            {
                return comparison >= 0;
            }
            if (operation == "LT")
            {
                return comparison < 0;
            }
            if (operation == "LTE")
            {
                return comparison <= 0;
            }
        }

        throw new NotSupportedException("Unsupported filter operation: " + operation);
    }
}
