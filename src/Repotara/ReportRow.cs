namespace Repotara;

/// <summary>
/// A single output row: an ordered set of column/value pairs. Column order
/// always matches the order fields were requested in the
/// <see cref="Repotara.Definition.ReportDefinition"/>. Lookups by column name
/// are O(1) via a parallel index, while iteration for output preserves order.
/// </summary>
public sealed class ReportRow
{
    private readonly List<KeyValuePair<string, object?>> _values = [];
    private readonly Dictionary<string, int> _index = [];

    /// <summary>Sets (or appends) a column value, preserving insertion order.</summary>
    public void Set(string column, object? value)
    {
        if (_index.TryGetValue(column, out var existingIndex))
        {
            _values[existingIndex] = new KeyValuePair<string, object?>(column, value);
            return;
        }

        _index[column] = _values.Count;
        _values.Add(new KeyValuePair<string, object?>(column, value));
    }

    /// <summary>Gets a column value, or null if the column does not exist on this row.</summary>
    public object? Get(string column)
    {
        return _index.TryGetValue(column, out var i) ? _values[i].Value : null;
    }

    /// <summary>All column/value pairs, in the order they were set.</summary>
    public IReadOnlyList<KeyValuePair<string, object?>> Values => _values;
}
