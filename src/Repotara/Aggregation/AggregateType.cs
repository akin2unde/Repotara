namespace Repotara.Aggregation;

/// <summary>
/// Supported aggregate operations. Every value here translates directly to a native
/// SQL aggregate function and a native MongoDB accumulator, so no in-memory
/// fallback is required for any of them.
/// </summary>
public enum AggregateType
{
    /// <summary>Sum of all values in the group.</summary>
    Sum,

    /// <summary>Average of all values in the group.</summary>
    Avg,

    /// <summary>Count of rows in the group.</summary>
    Count,

    /// <summary>Smallest value in the group.</summary>
    Min,

    /// <summary>Largest value in the group.</summary>
    Max
}
