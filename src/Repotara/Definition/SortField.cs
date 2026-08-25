namespace Repotara.Definition;

/// <summary>
/// One entry in a <see cref="ReportDefinition"/>'s sort list. Multiple entries
/// are applied in order, like a SQL <c>ORDER BY a, b</c>.
/// </summary>
public sealed class SortField
{
    /// <summary>
    /// The field to sort by. Before aggregation this is a "Source.Property" path;
    /// after aggregation it may be an output display name instead.
    /// </summary>
    public required string Field { get; set; }

    /// <summary>The sort direction. Defaults to ascending.</summary>
    public SortDirection Direction { get; set; } = SortDirection.Asc;
}
