using Repotara.Definition;
using Repotara.Metadata;

namespace Repotara.Providers;

/// <summary>
/// Executes a validated <see cref="ReportDefinition"/> against a physical
/// database and returns the result rows. Exactly one implementation is active
/// per project, selected at startup via <c>RepotaraOptions.Provider</c>.
/// </summary>
public interface IReportDataProvider
{
    /// <summary>
    /// Executes the given definition and returns the resulting rows, already
    /// filtered, joined, grouped, aggregated, sorted, and paginated as specified.
    /// </summary>
    Task<List<ReportRow>> ExecuteAsync(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> sourceMetadata,
        CancellationToken cancellationToken = default);
}
