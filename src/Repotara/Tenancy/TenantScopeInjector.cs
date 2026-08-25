using Repotara.Definition;
using Repotara.Metadata;
using Repotara.Providers;

namespace Repotara.Tenancy;

/// <summary>
/// Injects a mandatory tenant-scoping condition into a <see cref="ReportDefinition"/>'s
/// filter tree, server-side, before validation or query building ever run. The
/// frontend's JSON can never see, set, or override this condition.
/// </summary>
public static class TenantScopeInjector
{
    /// <summary>
    /// Returns a filter tree with tenant conditions merged in for every source
    /// that has a tenant column and has not opted out, or the original filter
    /// unchanged if multi-tenancy is disabled or no source needs scoping.
    /// </summary>
    public static SearchParam? Apply(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> metadata,
        RepotaraOptions options,
        string tenantId)
    {
        if (options.EnableMultiTenancy == false)
        {
            return definition.Filter;
        }

        var tenantConditions = new List<SearchParam>();

        foreach (var sourceName in definition.Types)
        {
            var sourceMetadata = metadata[sourceName];

            if (sourceMetadata.IgnoreTenant)
            {
                continue;
            }

            if (sourceMetadata.GetField(options.TenantColumn) == null)
            {
                continue;
            }

            tenantConditions.Add(new SearchParam
            {
                Property = sourceName + "." + options.TenantColumn,
                Operation = "EQ",
                Value = tenantId
            });
        }

        if (tenantConditions.Count == 0)
        {
            return definition.Filter;
        }

        var mandatoryFilter = new SearchParam { Operator = "And", Conditions = tenantConditions };

        if (definition.Filter == null)
        {
            return mandatoryFilter;
        }

        return new SearchParam
        {
            Operator = "And",
            Conditions = [mandatoryFilter, definition.Filter]
        };
    }
}
