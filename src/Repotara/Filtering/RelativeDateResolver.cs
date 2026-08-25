using Repotara.Definition;

namespace Repotara.Filtering;

/// <summary>
/// Walks a <see cref="SearchParam"/> tree and rewrites any leaf whose value is a
/// relative date keyword (e.g. "TODAY") into a concrete GTE/LT range pair. Runs
/// before validation and query building, so downstream code only ever sees
/// concrete literal values.
/// </summary>
public static class RelativeDateResolver
{
    /// <summary>
    /// Returns a new tree with all relative date keywords resolved against the
    /// given "current" instant. Passing the current time explicitly (rather than
    /// reading it internally) keeps this method easy to unit test.
    /// </summary>
    public static SearchParam? Resolve(SearchParam? node, DateTime now)
    {
        if (node == null)
        {
            return null;
        }

        if (node.IsBranch)
        {
            var resolvedConditions = new List<SearchParam>();
            foreach (var condition in node.Conditions!)
            {
                var resolved = Resolve(condition, now);
                if (resolved != null)
                {
                    resolvedConditions.Add(resolved);
                }
            }

            return new SearchParam
            {
                Operator = node.Operator,
                Conditions = resolvedConditions
            };
        }

        if (node.Value is string text && RelativeDateKeyword.IsKeyword(text))
        {
            var range = ResolveRange(text, now);

            var startCondition = new SearchParam
            {
                Property = node.Property,
                Operation = "GTE",
                Value = range.Start
            };

            var endCondition = new SearchParam
            {
                Property = node.Property,
                Operation = "LT",
                Value = range.End
            };

            return new SearchParam
            {
                Operator = "And",
                Conditions = [startCondition, endCondition]
            };
        }

        return node;
    }

    private static (DateTime Start, DateTime End) ResolveRange(string keyword, DateTime now)
    {
        var today = now.Date;

        if (keyword == RelativeDateKeyword.Today)
        {
            return (today, today.AddDays(1));
        }

        if (keyword == RelativeDateKeyword.Yesterday)
        {
            return (today.AddDays(-1), today);
        }

        if (keyword == RelativeDateKeyword.ThisWeek)
        {
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            return (startOfWeek, startOfWeek.AddDays(7));
        }

        if (keyword == RelativeDateKeyword.LastWeek)
        {
            var startOfThisWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfLastWeek = startOfThisWeek.AddDays(-7);
            return (startOfLastWeek, startOfThisWeek);
        }

        if (keyword == RelativeDateKeyword.ThisMonth)
        {
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            return (startOfMonth, startOfMonth.AddMonths(1));
        }

        if (keyword == RelativeDateKeyword.LastMonth)
        {
            var startOfThisMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);
            return (startOfLastMonth, startOfThisMonth);
        }

        throw new NotSupportedException("Unknown relative date keyword: " + keyword);
    }
}
