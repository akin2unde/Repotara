namespace Repotara.Definition;

/// <summary>
/// Describes how two sources in a <see cref="ReportDefinition"/> are joined.
/// Multiple joins chain together to support any number of sources.
/// </summary>
public sealed class JoinDefinition
{
    /// <summary>The source name on the left side of the join, e.g. "Order".</summary>
    public required string Left { get; set; }

    /// <summary>The property on the left source used as the join key, e.g. "CustomerId".</summary>
    public required string LeftKey { get; set; }

    /// <summary>The source name on the right side of the join, e.g. "Customer".</summary>
    public required string Right { get; set; }

    /// <summary>The property on the right source used as the join key, e.g. "Id".</summary>
    public required string RightKey { get; set; }

    /// <summary>The join type. Defaults to inner.</summary>
    public JoinType Type { get; set; } = JoinType.Inner;
}
