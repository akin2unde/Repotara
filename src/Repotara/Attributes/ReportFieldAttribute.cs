using Repotara.Aggregation;

namespace Repotara.Attributes;

/// <summary>
/// Optional attribute to customize how a property is exposed for reporting.
/// A property does not need this attribute to be reportable -- it is only
/// required when the defaults (property name as column, no aggregate restriction)
/// need to be overridden.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ReportFieldAttribute : Attribute
{
    /// <summary>
    /// The default output column name used when the frontend's field selection
    /// does not supply its own <c>DisplayName</c>. Falls back to the property name if not set.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The physical column name (SQL) or field name (MongoDB). Falls back to the
    /// property name if not set.
    /// </summary>
    public string? Column { get; set; }

    /// <summary>
    /// Restricts which aggregate operations may be requested against this field.
    /// An empty array (the default) means any aggregate is allowed.
    /// </summary>
    public AggregateType[] AllowedAggregates { get; set; } = [];
}
