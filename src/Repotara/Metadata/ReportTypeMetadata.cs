namespace Repotara.Metadata;

/// <summary>
/// Resolved, cached metadata for one <c>[Reportable]</c> class: its physical
/// source name, its tenant opt-out flag, and metadata for every reportable field.
/// </summary>
public sealed class ReportTypeMetadata
{
    /// <summary>The CLR type this metadata was built from.</summary>
    public required Type ClrType { get; init; }

    /// <summary>The physical table/collection name.</summary>
    public required string Source { get; init; }

    /// <summary>Whether this class is excluded from multi-tenant filtering.</summary>
    public required bool IgnoreTenant { get; init; }

    /// <summary>Metadata for every reportable field, keyed by C# property name.</summary>
    public required IReadOnlyDictionary<string, FieldMetadata> Fields { get; init; }

    /// <summary>
    /// Looks up field metadata by property name, or returns null if the
    /// property does not exist or is not reportable.
    /// </summary>
    public FieldMetadata? GetField(string propertyName)
    {
        return Fields.TryGetValue(propertyName, out var field) ? field : null;
    }
}
