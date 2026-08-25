using Repotara.Definition;

namespace Repotara.Output;

/// <summary>
/// Writes a set of result rows out as a specific format. Implementations are
/// pluggable -- consumers can register a custom writer for a format not
/// built in (e.g. CSV) without touching the core engine.
/// </summary>
public interface IReportWriter
{
    /// <summary>The output format this writer produces.</summary>
    OutputFormat Format { get; }

    /// <summary>The MIME content type of the produced output.</summary>
    string ContentType { get; }

    /// <summary>Writes the rows as a string in this writer's format.</summary>
    string Write(IReadOnlyList<ReportRow> rows, ReportDefinition definition);
}
