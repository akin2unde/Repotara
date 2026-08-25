using Repotara.Definition;
using Repotara.Filtering;

namespace Repotara.Tests.Filtering;

public class FilterEvaluatorTests
{
    private static ReportRow BuildRow(decimal total, string name)
    {
        var row = new ReportRow();
        row.Set("Total", total);
        row.Set("Name", name);
        return row;
    }

    [Fact]
    public void Matches_ReturnsTrueForNullNode()
    {
        var row = BuildRow(100, "Acme");

        Assert.True(FilterEvaluator.Matches(null, row));
    }

    [Fact]
    public void Matches_EvaluatesEqLeaf()
    {
        var row = BuildRow(100, "Acme");
        var node = new SearchParam { Property = "Name", Operation = "EQ", Value = "Acme" };

        Assert.True(FilterEvaluator.Matches(node, row));
    }

    [Fact]
    public void Matches_EvaluatesGtLeaf()
    {
        var row = BuildRow(100, "Acme");
        var node = new SearchParam { Property = "Total", Operation = "GT", Value = 50m };

        Assert.True(FilterEvaluator.Matches(node, row));
    }

    [Fact]
    public void Matches_CombinesAndBranchCorrectly()
    {
        var row = BuildRow(100, "Acme");
        var node = new SearchParam
        {
            Operator = "And",
            Conditions =
            [
                new SearchParam { Property = "Total", Operation = "GT", Value = 50m },
                new SearchParam { Property = "Name", Operation = "EQ", Value = "Globex" }
            ]
        };

        Assert.False(FilterEvaluator.Matches(node, row));
    }

    [Fact]
    public void Matches_CombinesOrBranchCorrectly()
    {
        var row = BuildRow(100, "Acme");
        var node = new SearchParam
        {
            Operator = "Or",
            Conditions =
            [
                new SearchParam { Property = "Total", Operation = "GT", Value = 999m },
                new SearchParam { Property = "Name", Operation = "EQ", Value = "Acme" }
            ]
        };

        Assert.True(FilterEvaluator.Matches(node, row));
    }

    [Fact]
    public void Matches_ComparesTwoColumnsViaValueProperty()
    {
        var row = new ReportRow();
        row.Set("Shipped", new DateTime(2026, 2, 1));
        row.Set("Promised", new DateTime(2026, 1, 1));

        var node = new SearchParam { Property = "Shipped", Operation = "GT", ValueProperty = "Promised" };

        Assert.True(FilterEvaluator.Matches(node, row));
    }

    [Fact]
    public void Matches_ContainsIsCaseInsensitive()
    {
        var row = BuildRow(100, "Acme Corp");
        var node = new SearchParam { Property = "Name", Operation = "CONTAINS", Value = "acme" };

        Assert.True(FilterEvaluator.Matches(node, row));
    }
}

public class RelativeDateResolverTests
{
    [Fact]
    public void Resolve_TodayProducesFullCalendarDayRange()
    {
        var now = new DateTime(2026, 8, 25, 14, 30, 0);
        var node = new SearchParam { Property = "PlacedOn", Operation = "EQ", Value = "TODAY" };

        var resolved = RelativeDateResolver.Resolve(node, now);

        Assert.NotNull(resolved);
        Assert.True(resolved!.IsBranch);
        var start = resolved.Conditions!.Single(c => c.Operation == "GTE").Value;
        var end = resolved.Conditions!.Single(c => c.Operation == "LT").Value;

        Assert.Equal(new DateTime(2026, 8, 25), start);
        Assert.Equal(new DateTime(2026, 8, 26), end);
    }

    [Fact]
    public void Resolve_ThisMonthProducesFullCalendarMonthRange()
    {
        var now = new DateTime(2026, 8, 25);
        var node = new SearchParam { Property = "PlacedOn", Operation = "EQ", Value = "THIS_MONTH" };

        var resolved = RelativeDateResolver.Resolve(node, now);

        var start = resolved!.Conditions!.Single(c => c.Operation == "GTE").Value;
        var end = resolved.Conditions!.Single(c => c.Operation == "LT").Value;

        Assert.Equal(new DateTime(2026, 8, 1), start);
        Assert.Equal(new DateTime(2026, 9, 1), end);
    }

    [Fact]
    public void Resolve_LeavesNonKeywordLeafUnchanged()
    {
        var node = new SearchParam { Property = "Name", Operation = "EQ", Value = "Acme" };

        var resolved = RelativeDateResolver.Resolve(node, DateTime.UtcNow);

        Assert.False(resolved!.IsBranch);
        Assert.Equal("Acme", resolved.Value);
    }

    [Fact]
    public void Resolve_RecursesThroughBranches()
    {
        var now = new DateTime(2026, 8, 25);
        var node = new SearchParam
        {
            Operator = "And",
            Conditions =
            [
                new SearchParam { Property = "PlacedOn", Operation = "EQ", Value = "TODAY" },
                new SearchParam { Property = "Name", Operation = "EQ", Value = "Acme" }
            ]
        };

        var resolved = RelativeDateResolver.Resolve(node, now);

        Assert.Equal(2, resolved!.Conditions!.Count);
        Assert.True(resolved.Conditions[0].IsBranch);
        Assert.False(resolved.Conditions[1].IsBranch);
    }
}
