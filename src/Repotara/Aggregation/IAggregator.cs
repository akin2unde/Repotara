namespace Repotara.Aggregation;

/// <summary>
/// Computes a single aggregate value over a set of raw values. Used by the
/// in-memory fallback grouping engine; native providers use SQL/Mongo aggregate
/// functions directly and do not go through this interface.
/// </summary>
public interface IAggregator
{
    /// <summary>The aggregate operation this instance computes.</summary>
    AggregateType Type { get; }

    /// <summary>Computes the aggregate over the given values.</summary>
    object? Compute(IEnumerable<object?> values);
}
