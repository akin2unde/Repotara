namespace Repotara.Definition;

/// <summary>One field-level validation failure found in a <see cref="ReportDefinition"/>.</summary>
public sealed class ValidationError
{
    /// <summary>The field or path the error relates to.</summary>
    public required string Field { get; init; }

    /// <summary>A human-readable explanation of what's wrong.</summary>
    public required string Reason { get; init; }
}
