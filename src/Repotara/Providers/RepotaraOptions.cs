using Repotara.Providers.Sql;

using System.Reflection;
using Repotara.Providers.Sql;

namespace Repotara.Providers;

/// <summary>
/// Configuration for Repotara, registered once at startup via
/// <c>services.AddRepotara(options => ...)</c>. A single project is assumed to
/// use exactly one database throughout, so this is configured once, not per request.
/// </summary>
public sealed class RepotaraOptions
{
    /// <summary>Which database technology to run report queries against.</summary>
    public ProviderType Provider { get; set; }

    /// <summary>The relational dialect to use when <see cref="Provider"/> is Sql.</summary>
    public SqlOption Options { get; set; }

    /// <summary>Database server hostname.</summary>
    public required string Host { get; set; }

    /// <summary>Database server port.</summary>
    public int Port { get; set; }

    /// <summary>Database or schema name.</summary>
    public required string DatabaseName { get; set; }

    /// <summary>Database username.</summary>
    public string? Username { get; set; }

    /// <summary>Database password.</summary>
    public string? Password { get; set; }

    /// <summary>
    /// When true, every query is automatically scoped to the current tenant
    /// using <see cref="TenantColumn"/>, resolved via the registered
    /// <c>ITenantContext</c>. Defaults to false (no behavior change for
    /// single-tenant projects).
    /// </summary>
    public bool EnableMultiTenancy { get; set; }

    /// <summary>
    /// The physical column/field name holding the tenant identifier, applied to
    /// every reportable class unless it opts out via <c>[Reportable(IgnoreTenant = true)]</c>.
    /// </summary>
    public string TenantColumn { get; set; } = "TenantId";

    /// <summary>
    /// The row limit applied when a <see cref="Repotara.Definition.ReportDefinition"/>
    /// does not specify its own Take value, so no report runs unbounded by accident.
    /// </summary>
    public int DefaultRowLimit { get; set; } = 10000;

    /// <summary>
    /// Assemblies to scan for every <c>[Reportable]</c> class, added via <see cref="RegisterAssembly"/>.
    /// </summary>
    internal List<Assembly> Assemblies { get; } = [];

    /// <summary>
    /// Individual classes registered explicitly via <see cref="RegisterType{T}"/>,
    /// without scanning an entire assembly.
    /// </summary>
    internal List<Type> ExplicitTypes { get; } = [];

    /// <summary>
    /// Base type + assembly pairs registered via <see cref="RegisterDerivedFrom{TBase}"/>:
    /// every type deriving from the base type and marked <c>[Reportable]</c> is included.
    /// </summary>
    internal List<(Type BaseType, Assembly Assembly)> DerivedFromRegistrations { get; } = [];

    /// <summary>
    /// Scans every type in the given assembly and registers every one marked
    /// <c>[Reportable]</c>, regardless of base type. Use when your reportable
    /// models don't share a common base class.
    /// </summary>
    public void RegisterAssembly(Assembly assembly)
    {
        Assemblies.Add(assembly);
    }

    /// <summary>
    /// Scans for every type deriving from <typeparamref name="TBase"/> that is
    /// marked <c>[Reportable]</c>, e.g. <c>RegisterDerivedFrom&lt;DbModel&gt;()</c>
    /// finds Order, Customer, etc. without listing each one individually.
    /// Scans <typeparamref name="TBase"/>'s own assembly unless a different one is supplied.
    /// </summary>
    public void RegisterDerivedFrom<TBase>(Assembly? assembly = null)
    {
        DerivedFromRegistrations.Add((typeof(TBase), assembly ?? typeof(TBase).Assembly));
    }

    /// <summary>
    /// Registers a single class explicitly, without scanning its assembly for
    /// other reportable types. Useful for a one-off exception to your usual pattern.
    /// </summary>
    public void RegisterType<T>()
    {
        ExplicitTypes.Add(typeof(T));
    }
}
