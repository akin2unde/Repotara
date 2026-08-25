namespace Repotara.Definition;

/// <summary>The kind of join between two sources.</summary>
public enum JoinType
{
    /// <summary>Only rows with a match on both sides are included.</summary>
    Inner,

    /// <summary>All rows from the left source are included, matched or not.</summary>
    Left
}
