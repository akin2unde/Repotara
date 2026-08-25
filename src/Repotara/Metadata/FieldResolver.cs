namespace Repotara.Metadata;

/// <summary>
/// Resolves "Source.Property" paths against cached type metadata. Used by
/// <c>ReportDefinitionValidator</c> and both query builders so field resolution
/// logic exists in exactly one place.
/// </summary>
public static class FieldResolver
{
    /// <summary>
    /// Splits a "Source.Property" path and returns the source name and property name.
    /// </summary>
    public static (string SourceName, string PropertyName) SplitPath(string path)
    {
        var separatorIndex = path.IndexOf('.');
        if (separatorIndex < 0)
        {
            throw new InvalidOperationException(
                "Field '" + path + "' must be in 'Source.Property' format.");
        }

        var sourceName = path.Substring(0, separatorIndex);
        var propertyName = path.Substring(separatorIndex + 1);
        return (sourceName, propertyName);
    }

    /// <summary>
    /// Resolves a "Source.Property" path to its <see cref="FieldMetadata"/>, throwing
    /// a clear error if the source or property is not reportable.
    /// </summary>
    public static FieldMetadata ResolveField(string path, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var (sourceName, propertyName) = SplitPath(path);

        if (metadata.TryGetValue(sourceName, out var sourceMetadata) == false)
        {
            throw new InvalidOperationException("Unknown report source: " + sourceName);
        }

        var field = sourceMetadata.GetField(propertyName);
        if (field == null)
        {
            throw new InvalidOperationException(
                "Field '" + propertyName + "' is not reportable on source '" + sourceName + "'.");
        }

        return field;
    }

    /// <summary>
    /// Resolves a "Source.Property" path to its physical "source.column" form,
    /// e.g. "Order.Total" -> "Order.order_total". Used when building native queries.
    /// </summary>
    public static string ResolveColumn(string path, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var (sourceName, _) = SplitPath(path);
        var field = ResolveField(path, metadata);
        return sourceName + "." + field.Column;
    }
}
