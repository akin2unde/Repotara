using Repotara.Attributes;

namespace Repotara.SampleApi.Models;

[Reportable(Source = "customers")]
public class Customer : DbModel
{
    [ReportField(DisplayName = "First Name", Column = "first_name")]
    public string FirstName { get; set; } = "";

    [ReportField(DisplayName = "Last Name", Column = "last_name")]
    public string LastName { get; set; } = "";

    public int RegionId { get; set; }

    public int CompanyId { get; set; }
}
