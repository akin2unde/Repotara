namespace Repotara.Aggregation;

/// <summary>Built-in aggregator for <see cref="AggregateType.Sum"/>.</summary>
public sealed class SumAggregator : IAggregator
{
    /// <inheritdoc />
    public AggregateType Type => AggregateType.Sum;

    /// <inheritdoc />
    public object? Compute(IEnumerable<object?> values)
    {
        decimal total = 0;
        foreach (var value in values)
        {
            if (value != null)
            {
                total += Convert.ToDecimal(value);
            }
        }
        return total;
    }
}

/// <summary>Built-in aggregator for <see cref="AggregateType.Avg"/>.</summary>
public sealed class AvgAggregator : IAggregator
{
    /// <inheritdoc />
    public AggregateType Type => AggregateType.Avg;

    /// <inheritdoc />
    public object? Compute(IEnumerable<object?> values)
    {
        decimal total = 0;
        var count = 0;

        foreach (var value in values)
        {
            if (value != null)
            {
                total += Convert.ToDecimal(value);
                count++;
            }
        }

        return count == 0 ? null : total / count;
    }
}

/// <summary>Built-in aggregator for <see cref="AggregateType.Count"/>.</summary>
public sealed class CountAggregator : IAggregator
{
    /// <inheritdoc />
    public AggregateType Type => AggregateType.Count;

    /// <inheritdoc />
    public object? Compute(IEnumerable<object?> values)
    {
        var count = 0;
        foreach (var _ in values)
        {
            count++;
        }
        return count;
    }
}

/// <summary>Built-in aggregator for <see cref="AggregateType.Min"/>.</summary>
public sealed class MinAggregator : IAggregator
{
    /// <inheritdoc />
    public AggregateType Type => AggregateType.Min;

    /// <inheritdoc />
    public object? Compute(IEnumerable<object?> values)
    {
        object? min = null;
        foreach (var value in values)
        {
            if (value is IComparable comparable && (min == null || comparable.CompareTo(min) < 0))
            {
                min = value;
            }
        }
        return min;
    }
}

/// <summary>Built-in aggregator for <see cref="AggregateType.Max"/>.</summary>
public sealed class MaxAggregator : IAggregator
{
    /// <inheritdoc />
    public AggregateType Type => AggregateType.Max;

    /// <inheritdoc />
    public object? Compute(IEnumerable<object?> values)
    {
        object? max = null;
        foreach (var value in values)
        {
            if (value is IComparable comparable && (max == null || comparable.CompareTo(max) > 0))
            {
                max = value;
            }
        }
        return max;
    }
}

/// <summary>Resolves the built-in aggregator for a given <see cref="AggregateType"/>.</summary>
public static class AggregatorFactory
{
    /// <summary>Returns the built-in aggregator implementation for the given type.</summary>
    public static IAggregator Create(AggregateType type)
    {
        return type switch
        {
            AggregateType.Sum => new SumAggregator(),
            AggregateType.Avg => new AvgAggregator(),
            AggregateType.Count => new CountAggregator(),
            AggregateType.Min => new MinAggregator(),
            AggregateType.Max => new MaxAggregator(),
            _ => throw new NotSupportedException("Unsupported aggregate type: " + type)
        };
    }
}
