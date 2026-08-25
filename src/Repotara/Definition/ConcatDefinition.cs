namespace Repotara.Definition;

/// <summary>
/// Describes a computed field made by joining two or more source fields with a
/// delimiter, e.g. FirstName + " " + LastName -> "Full Name". Does not support
/// mixing in literal text beyond the single delimiter.
/// </summary>
public sealed class ConcatDefinition
{
    /// <summary>
    /// The "Source.Property" paths to concatenate, in order. Must contain at least two entries.
    /// </summary>
    public required List<string> Fields { get; set; }

    /// <summary>The delimiter placed between each field. Defaults to a single space.</summary>
    public string Delimiter { get; set; } = " ";
}
