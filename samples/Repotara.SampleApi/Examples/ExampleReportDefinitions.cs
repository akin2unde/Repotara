using Repotara.Aggregation;
using Repotara.Definition;

namespace Repotara.SampleApi.Examples;

/// <summary>
/// A named catalog of example <see cref="ReportDefinition"/> instances, one per
/// key SDK capability. Exposed via GET /api/reports/examples so a frontend
/// developer can see the exact JSON shape for each scenario, and each can be
/// run directly via POST /api/reports/run.
/// </summary>
public static class ExampleReportDefinitions
{
    /// <summary>Single source, no joins, no aggregation -- the simplest possible report.</summary>
    public static ReportDefinition Basic() => new()
    {
        Types = ["Order"],
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total" },
            new ReportFieldSelection { Field = "Order.PlacedOn", DisplayName = "Placed On" }
        ],
        Take = 50
    };

    /// <summary>Two sources joined together -- Order to Customer.</summary>
    public static ReportDefinition Joined() => new()
    {
        Types = ["Order", "Customer"],
        Joins =
        [
            new JoinDefinition { Left = "Order", LeftKey = "CustomerId", Right = "Customer", RightKey = "Id", Type = JoinType.Inner }
        ],
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Customer.FirstName", DisplayName = "Customer First Name" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total" }
        ],
        Take = 50
    };

    /// <summary>Three sources chained, grouped, and aggregated -- revenue per region.</summary>
    public static ReportDefinition GroupedAggregate() => new()
    {
        Types = ["Order", "Customer", "Region"],
        Joins =
        [
            new JoinDefinition { Left = "Order", LeftKey = "CustomerId", Right = "Customer", RightKey = "Id" },
            new JoinDefinition { Left = "Customer", LeftKey = "RegionId", Right = "Region", RightKey = "Id" }
        ],
        Fields =
        [
            new ReportFieldSelection { Field = "Region.Name", DisplayName = "Region" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total Revenue", Aggregate = AggregateType.Sum }
        ],
        GroupBy = ["Region.Name"],
        Sort = [new SortField { Field = "Total Revenue", Direction = SortDirection.Desc }]
    };

    /// <summary>A nested AND/OR filter tree, using the abbreviated operations.</summary>
    public static ReportDefinition FilterAndOr() => new()
    {
        Types = ["Order", "Customer"],
        Joins = [new JoinDefinition { Left = "Order", LeftKey = "CustomerId", Right = "Customer", RightKey = "Id" }],
        Filter = new SearchParam
        {
            Operator = "And",
            Conditions =
            [
                new SearchParam { Property = "Order.Total", Operation = "GT", Value = 100 },
                new SearchParam
                {
                    Operator = "Or",
                    Conditions =
                    [
                        new SearchParam { Property = "Customer.FirstName", Operation = "EQ", Value = "Acme" },
                        new SearchParam { Property = "Customer.FirstName", Operation = "EQ", Value = "Globex" }
                    ]
                }
            ]
        },
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total" }
        ]
    };

    /// <summary>Aggregate-level filtering: customers whose total revenue exceeds a threshold.</summary>
    public static ReportDefinition HavingExample() => new()
    {
        Types = ["Order", "Customer"],
        Joins = [new JoinDefinition { Left = "Order", LeftKey = "CustomerId", Right = "Customer", RightKey = "Id" }],
        Fields =
        [
            new ReportFieldSelection { Field = "Customer.FirstName", DisplayName = "Customer" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total Revenue", Aggregate = AggregateType.Sum }
        ],
        GroupBy = ["Customer.FirstName"],
        Having = new SearchParam { Property = "Total Revenue", Operation = "GT", Value = 10000 }
    };

    /// <summary>Sort by multiple fields, with pagination via Skip/Take.</summary>
    public static ReportDefinition SortAndPagination() => new()
    {
        Types = ["Order"],
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total" }
        ],
        Sort =
        [
            new SortField { Field = "Total", Direction = SortDirection.Desc },
            new SortField { Field = "Order Number", Direction = SortDirection.Asc }
        ],
        Skip = 0,
        Take = 20
    };

    /// <summary>A computed Concat field -- FirstName + " " + LastName -> "Full Name".</summary>
    public static ReportDefinition ConcatExample() => new()
    {
        Types = ["Order", "Customer"],
        Joins = [new JoinDefinition { Left = "Order", LeftKey = "CustomerId", Right = "Customer", RightKey = "Id" }],
        Fields =
        [
            new ReportFieldSelection
            {
                DisplayName = "Full Name",
                Concat = new ConcatDefinition { Fields = ["Customer.FirstName", "Customer.LastName"], Delimiter = " " }
            },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total" }
        ]
    };

    /// <summary>Filtering with a relative date keyword -- orders placed this month.</summary>
    public static ReportDefinition RelativeDateExample() => new()
    {
        Types = ["Order"],
        Filter = new SearchParam { Property = "Order.PlacedOn", Operation = "EQ", Value = "THIS_MONTH" },
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.PlacedOn", DisplayName = "Placed On" }
        ]
    };

    /// <summary>Comparing two columns on the same row -- late shipments.</summary>
    public static ReportDefinition ColumnToColumnExample() => new()
    {
        Types = ["Order"],
        Filter = new SearchParam { Property = "Order.ShippedDate", Operation = "GT", ValueProperty = "Order.PromisedDate" },
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.ShippedDate", DisplayName = "Shipped" },
            new ReportFieldSelection { Field = "Order.PromisedDate", DisplayName = "Promised" }
        ]
    };

    /// <summary>Same shape as GroupedAggregate, requested with OutputFormat.Chart for direct charting-library use.</summary>
    public static ReportDefinition ChartExample() => GroupedAggregate();

    /// <summary>
    /// A left join: every customer is returned even if they have no orders yet,
    /// unlike Joined() above which uses the inner-join default.
    /// </summary>
    public static ReportDefinition LeftJoinExample() => new()
    {
        Types = ["Customer", "Order"],
        Joins =
        [
            new JoinDefinition { Left = "Customer", LeftKey = "Id", Right = "Order", RightKey = "CustomerId", Type = JoinType.Left }
        ],
        Fields =
        [
            new ReportFieldSelection { Field = "Customer.FirstName", DisplayName = "Customer" },
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" }
        ]
    };

    /// <summary>Every built-in aggregate (Sum, Avg, Count, Min, Max) side by side over the same field.</summary>
    public static ReportDefinition AggregateShowcase() => new()
    {
        Types = ["Order"],
        Fields =
        [
            new ReportFieldSelection { Field = "Order.CustomerId", DisplayName = "Customer" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total", Aggregate = AggregateType.Sum },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Average", Aggregate = AggregateType.Avg },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Smallest", Aggregate = AggregateType.Min },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Largest", Aggregate = AggregateType.Max },
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Count", Aggregate = AggregateType.Count }
        ],
        GroupBy = ["Order.CustomerId"]
    };

    /// <summary>The IN operation -- matches any value in a list.</summary>
    public static ReportDefinition FilterInExample() => new()
    {
        Types = ["Order"],
        Filter = new SearchParam { Property = "Order.CustomerId", Operation = "IN", Value = new[] { 1, 2, 3 } },
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.CustomerId", DisplayName = "Customer Id" }
        ]
    };

    /// <summary>The CONTAINS operation -- a case-insensitive partial text match.</summary>
    public static ReportDefinition FilterContainsExample() => new()
    {
        Types = ["Order", "Customer"],
        Joins = [new JoinDefinition { Left = "Order", LeftKey = "CustomerId", Right = "Customer", RightKey = "Id" }],
        Filter = new SearchParam { Property = "Customer.FirstName", Operation = "CONTAINS", Value = "acm" },
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Customer.FirstName", DisplayName = "Customer" }
        ]
    };

    /// <summary>
    /// An HTML report using a custom {{DisplayName}} template instead of the default
    /// table -- only meaningful when requested with OutputFormat.Html.
    /// </summary>
    public static ReportDefinition HtmlTemplateExample() => new()
    {
        Types = ["Order"],
        Fields =
        [
            new ReportFieldSelection { Field = "Order.Id", DisplayName = "Order Number" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total" }
        ],
        Template = "<div class=\"order-card\">Order #{{Order Number}} -- ${{Total}}</div>",
        Take = 10
    };

    /// <summary>
    /// Intentionally invalid: Total does not allow Count, and CustomerId is
    /// selected without being grouped or aggregated alongside a grouped report.
    /// Demonstrates the structured 400 response returned for a bad definition.
    /// </summary>
    public static ReportDefinition InvalidExample() => new()
    {
        Types = ["Order"],
        Fields =
        [
            new ReportFieldSelection { Field = "Order.CustomerId", DisplayName = "Customer" },
            new ReportFieldSelection { Field = "Order.Total", DisplayName = "Total", Aggregate = AggregateType.Count }
        ],
        GroupBy = ["Order.CompanyId"]
    };
}
