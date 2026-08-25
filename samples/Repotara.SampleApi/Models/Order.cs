using Repotara.Aggregation;
using Repotara.Attributes;

namespace Repotara.SampleApi.Models;

/// <summary>
/// Example reportable class. Every public property is reportable by default
/// (the opt-out model) -- InternalNotes below is the one deliberately excluded.
/// Inherits DbModel so it's picked up by RegisterDerivedFrom&lt;DbModel&gt;() in Program.cs.
/// </summary>
[Reportable(Source = "orders")]
public class Order : DbModel
{
    public int CustomerId { get; set; }

    [ReportField(DisplayName = "Order Total", Column = "order_total",
                 AllowedAggregates = [AggregateType.Sum, AggregateType.Avg, AggregateType.Min, AggregateType.Max])]
    public decimal Total { get; set; }

    [ReportField(DisplayName = "Order Date", Column = "placed_on")]
    public DateTime PlacedOn { get; set; }

    [ReportField(DisplayName = "Shipped Date", Column = "shipped_date")]
    public DateTime? ShippedDate { get; set; }

    [ReportField(DisplayName = "Promised Date", Column = "promised_date")]
    public DateTime? PromisedDate { get; set; }

    /// <summary>Multi-tenant demo column -- see appsettings.json "Repotara:TenantColumn".</summary>
    public int CompanyId { get; set; }

    [ReportIgnore]
    public string? InternalNotes { get; set; }
}
