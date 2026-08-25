namespace Repotara;

/// <summary>
/// The final report output, ready to be returned directly from a controller
/// action as the HTTP response body.
/// </summary>
public sealed class ReportResult
{
    /// <summary>The serialized report content (JSON, XML, or HTML text).</summary>
    public required string Content { get; init; }

    /// <summary>The MIME content type matching <see cref="Content"/>.</summary>
    public required string ContentType { get; init; }
}
