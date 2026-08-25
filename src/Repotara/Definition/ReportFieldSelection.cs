using Repotara.Aggregation;

namespace Repotara.Definition;

/// <summary>
/// One field selected by the frontend for output. Exactly one of
/// <see cref="Field"/> or <see cref="Concat"/> must be set.
/// </summary>
public sealed class ReportFieldSelection
{
    /// <summary>
    /// A plain "Source.Property" reference, e.g. "Order.Total".
    /// Mutually exclusive with <see cref="Concat"/>.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// A computed concatenation of multiple fields. Mutually exclusive with <see cref="Field"/>.
    /// Cannot be combined with <see cref="Aggregate"/>.
    /// </summary>
    public ConcatDefinition? Concat { get; set; }

    /// <summary>
    /// The output column name. Falls back to the field's attribute-level
    /// DisplayName, then to the raw property name, if not supplied.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The aggregate operation to apply to this field. Required when the
    /// definition specifies <see cref="ReportDefinition.GroupBy"/> and this
    /// field is not one of the group-by fields.
    /// </summary>
    public AggregateType? Aggregate { get; set; }
}
