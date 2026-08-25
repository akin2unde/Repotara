namespace Repotara.Attributes;

/// <summary>
/// Marks a class as an eligible report data source.
/// All public properties are reportable by default unless marked with <see cref="ReportIgnoreAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ReportableAttribute : Attribute
{
    /// <summary>
    /// The physical table name (SQL) or collection name (MongoDB) that backs this class.
    /// Defaults to the class name if not set.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// When true, this class is excluded from multi-tenant filtering even if
    /// <c>RepotaraOptions.EnableMultiTenancy</c> is on. Use for shared/global data
    /// (lookup tables) or classes that use a different tenant convention.
    /// </summary>
    public bool IgnoreTenant { get; set; }
}
