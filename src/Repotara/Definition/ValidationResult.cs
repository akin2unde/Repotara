namespace Repotara.Definition;

/// <summary>The outcome of validating a <see cref="ReportDefinition"/> against source metadata.</summary>
public sealed class ValidationResult
{
    /// <summary>True if no errors were found.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Every validation failure found, empty if valid.</summary>
    public required List<ValidationError> Errors { get; init; }

    /// <summary>A result with no errors.</summary>
    public static ValidationResult Success() => new() { Errors = [] };

    /// <summary>A result carrying the given errors.</summary>
    public static ValidationResult Failure(List<ValidationError> errors) => new() { Errors = errors };
}
