using Repotara.Aggregation;
using Repotara.Attributes;

namespace Repotara.Tests.Fixtures;

[Reportable(Source = "orders")]
public class TestOrder
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [ReportField(DisplayName = "Order Total", Column = "order_total",
                 AllowedAggregates = [AggregateType.Sum, AggregateType.Avg])]
    public decimal Total { get; set; }

    [ReportField(DisplayName = "Placed On", Column = "placed_on")]
    public DateTime PlacedOn { get; set; }

    public int CompanyId { get; set; }

    [ReportIgnore]
    public string? InternalNotes { get; set; }
}

[Reportable(Source = "customers")]
public class TestCustomer
{
    public int Id { get; set; }

    [ReportField(DisplayName = "First Name", Column = "first_name")]
    public string FirstName { get; set; } = "";

    [ReportField(DisplayName = "Last Name", Column = "last_name")]
    public string LastName { get; set; } = "";

    public int CompanyId { get; set; }
}

[Reportable(Source = "regions", IgnoreTenant = true)]
public class TestRegion
{
    public int Id { get; set; }

    [ReportField(DisplayName = "Region", Column = "region_name")]
    public string Name { get; set; } = "";
}
