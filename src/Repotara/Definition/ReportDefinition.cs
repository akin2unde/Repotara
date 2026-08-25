namespace Repotara.Definition;

/// <summary>
/// The JSON contract sent by the frontend describing what report to run: which
/// sources, how they join, how to filter/group/aggregate/sort them, and how many
/// rows to return. This is the only thing the frontend needs to know about --
/// it never needs to know the underlying database, tables, or physical columns.
/// </summary>
public sealed class ReportDefinition
{
    /// <summary>
    /// The reportable source names involved in this report, e.g. ["Order", "Customer"].
    /// Each name must correspond to a class registered with the engine.
    /// </summary>
    public required List<string> Types { get; set; }

    /// <summary>
    /// Join instructions chaining the sources together. Required when
    /// <see cref="Types"/> has more than one entry.
    /// </summary>
    public List<JoinDefinition>? Joins { get; set; }

    /// <summary>The fields to include in the output, in the order they should appear.</summary>
    public required List<ReportFieldSelection> Fields { get; set; }

    /// <summary>
    /// Row-level filter, applied before aggregation. See <see cref="SearchParam"/>.
    /// </summary>
    public SearchParam? Filter { get; set; }

    /// <summary>
    /// Aggregate-level filter, applied after grouping/aggregation. See <see cref="SearchParam"/>.
    /// Conditions here reference output display names, not source properties.
    /// </summary>
    public SearchParam? Having { get; set; }

    /// <summary>Properties to group by, as "Source.Property" paths. Optional.</summary>
    public List<string>? GroupBy { get; set; }

    /// <summary>Sort instructions, applied in list order. Optional.</summary>
    public List<SortField>? Sort { get; set; }

    /// <summary>Number of rows to skip, for pagination. Optional.</summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Maximum number of rows to return. If not supplied, falls back to
    /// <c>RepotaraOptions.DefaultRowLimit</c> so no report runs unbounded by accident.
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// An HTML template containing simple <c>{{DisplayName}}</c> substitution tags.
    /// Used only when the requested Repotara.Output.OutputFormat is Html; ignored otherwise.
    /// </summary>
    public string? Template { get; set; }
}
