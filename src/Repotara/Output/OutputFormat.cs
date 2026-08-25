namespace Repotara.Output;

/// <summary>The output format a report can be written as.</summary>
public enum OutputFormat
{
    /// <summary>A JSON array of row objects.</summary>
    Json,

    /// <summary>An XML document with one Row element per record.</summary>
    Xml,

    /// <summary>An HTML table, or a rendered custom template if one is supplied.</summary>
    Html,

    /// <summary>A chart-ready { labels, datasets } JSON shape for charting libraries.</summary>
    Chart
}
