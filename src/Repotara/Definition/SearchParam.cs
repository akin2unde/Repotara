namespace Repotara.Definition;

/// <summary>
/// A recursive filter condition node, used for both <see cref="ReportDefinition.Filter"/>
/// (row-level, applied before aggregation) and <see cref="ReportDefinition.Having"/>
/// (aggregate-level, applied after aggregation).
/// <para>
/// A node is either a leaf condition (<see cref="Property"/> + <see cref="Operation"/> set)
/// or a branch (<see cref="Operator"/> + <see cref="Conditions"/> set). Branches nest to
/// any depth, the same way SQL AND/OR groups nest.
/// </para>
/// </summary>
public sealed class SearchParam
{
    /// <summary>
    /// Leaf only. The property being compared. Before aggregation this is a
    /// "Source.Property" path; in a <see cref="ReportDefinition.Having"/> clause
    /// this is an output display name instead, since aggregated values don't
    /// belong to a single source.
    /// </summary>
    public string? Property { get; set; }

    /// <summary>
    /// Leaf only. The comparison operation: EQ, NEQ, GT, GTE, LT, LTE, IN, CONTAINS.
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Leaf only. A literal value to compare against. May also be a relative date
    /// keyword (e.g. "TODAY", "THIS_MONTH") when the target property is a date.
    /// Mutually exclusive with <see cref="ValueProperty"/>.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Leaf only. A "Source.Property" path to compare against instead of a literal
    /// value, e.g. comparing two columns on the same row. Mutually exclusive with <see cref="Value"/>.
    /// </summary>
    public string? ValueProperty { get; set; }

    /// <summary>Branch only. "And" or "Or".</summary>
    public string? Operator { get; set; }

    /// <summary>Branch only. The nested conditions combined by <see cref="Operator"/>.</summary>
    public List<SearchParam>? Conditions { get; set; }

    /// <summary>True if this node is a branch (has nested conditions) rather than a leaf.</summary>
    public bool IsBranch => Conditions != null && Conditions.Count > 0;
}
