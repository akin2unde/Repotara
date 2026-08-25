using Repotara.Attributes;

namespace Repotara.SampleApi.Models;

/// <summary>
/// A shared lookup table with no tenant column at all -- opts out of
/// multi-tenant scoping via IgnoreTenant, since it has no CompanyId to filter on.
/// Deliberately does NOT derive from DbModel, to demonstrate registering a
/// one-off reportable class via RegisterType&lt;Region&gt;() in Program.cs.
/// </summary>
[Reportable(Source = "regions", IgnoreTenant = true)]
public class Region
{
    public int Id { get; set; }

    [ReportField(DisplayName = "Region", Column = "region_name")]
    public string Name { get; set; } = "";
}
