using Microsoft.Extensions.Options;
using Repotara.Definition;
using Repotara.Filtering;
using Repotara.Metadata;
using Repotara.Output;
using Repotara.Providers;
using Repotara.Tenancy;

namespace Repotara;

/// <summary>
/// The single entry point for running a report. Applies tenant scoping,
/// resolves relative date keywords, validates the definition against source
/// metadata, executes it via the configured <see cref="IReportDataProvider"/>,
/// and writes the result in the requested <see cref="OutputFormat"/>.
/// </summary>
public sealed class ReportEngine
{
    private readonly IReportDataProvider _provider;
    private readonly RepotaraOptions _options;
    private readonly ReportableTypeRegistry _registry;
    private readonly ITenantContext? _tenantContext;
    private readonly IReadOnlyDictionary<OutputFormat, IReportWriter> _writers;

    /// <summary>Creates the engine with its provider, options, type registry, optional tenant context, and writers.</summary>
    public ReportEngine(
        IReportDataProvider provider,
        IOptions<RepotaraOptions> options,
        ReportableTypeRegistry registry,
        IEnumerable<IReportWriter> writers,
        ITenantContext? tenantContext = null)
    {
        _provider = provider;
        _options = options.Value;
        _registry = registry;
        _tenantContext = tenantContext;
        _writers = writers.ToDictionary(w => w.Format);
    }

    /// <summary>
    /// Runs the given definition and returns the result written in the requested
    /// format. Every source named in <paramref name="definition"/> must already
    /// be registered via RepotaraOptions.RegisterAssembly, RegisterDerivedFrom, or RegisterType.
    /// </summary>
    public async Task<ReportResult> ExecuteAsync(
        ReportDefinition definition,
        OutputFormat format,
        CancellationToken cancellationToken = default)
    {
        var metadata = BuildMetadataMap(definition.Types);

        if (_options.EnableMultiTenancy)
        {
            if (_tenantContext == null)
            {
                throw new InvalidOperationException(
                    "EnableMultiTenancy is on, but no ITenantContext was registered. " +
                    "Register an implementation via services.AddScoped<ITenantContext, YourImplementation>().");
            }

            definition.Filter = TenantScopeInjector.Apply(definition, metadata, _options, _tenantContext.TenantId);
        }

        definition.Filter = RelativeDateResolver.Resolve(definition.Filter, DateTime.UtcNow);
        definition.Having = RelativeDateResolver.Resolve(definition.Having, DateTime.UtcNow);

        var validation = ReportDefinitionValidator.Validate(definition, metadata);
        if (validation.IsValid == false)
        {
            var reasons = string.Join("; ", validation.Errors.Select(e => e.Field + ": " + e.Reason));
            throw new InvalidOperationException("Report definition is invalid: " + reasons);
        }

        var rows = await _provider.ExecuteAsync(definition, metadata, cancellationToken);

        if (_writers.TryGetValue(format, out var writer) == false)
        {
            throw new NotSupportedException("No writer registered for output format: " + format);
        }

        var content = writer.Write(rows, definition);

        return new ReportResult
        {
            Content = content,
            ContentType = writer.ContentType
        };
    }

    private Dictionary<string, ReportTypeMetadata> BuildMetadataMap(IEnumerable<string> sourceNames)
    {
        var map = new Dictionary<string, ReportTypeMetadata>();
        foreach (var sourceName in sourceNames)
        {
            var type = _registry.Resolve(sourceName);
            map[sourceName] = ReportMetadataCache.Get(type);
        }
        return map;
    }
}
