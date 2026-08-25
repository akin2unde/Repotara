namespace Repotara.Attributes;

/// <summary>
/// Excludes a public property from reporting. All public properties on a
/// <see cref="ReportableAttribute"/>-marked class are reportable by default;
/// apply this to opt a specific property out.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ReportIgnoreAttribute : Attribute
{
}
