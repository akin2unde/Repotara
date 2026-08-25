using Repotara.Aggregation;

namespace Repotara.Metadata;

/// <summary>
/// Resolved, cached metadata for a single reportable property: its C# name,
/// its physical column name, its default display name, which aggregates are
/// allowed on it, and a compiled accessor for fast in-memory access.
/// </summary>
public sealed class FieldMetadata
{
    /// <summary>The C# property name, e.g. "Total".</summary>
    public required string PropertyName { get; init; }

    /// <summary>The physical column name (SQL) or field name (MongoDB).</summary>
    public required string Column { get; init; }

    /// <summary>The default display name used when no per-request override is supplied.</summary>
    public required string DefaultDisplayName { get; init; }

    /// <summary>
    /// Aggregate operations allowed against this field. Empty means any aggregate is allowed.
    /// </summary>
    public required AggregateType[] AllowedAggregates { get; init; }

    /// <summary>Compiled, reflection-free getter for this property.</summary>
    public required FieldAccessor Accessor { get; init; }

    /// <summary>The declared .NET type of the property.</summary>
    public required Type PropertyType { get; init; }
}
