using System.Collections.Concurrent;
using System.Reflection;
using Repotara.Attributes;

namespace Repotara.Metadata;

/// <summary>
/// Reflects each <c>[Reportable]</c> type exactly once and caches the result for
/// the lifetime of the process. Every subsequent request for the same type is a
/// dictionary lookup, not a reflection scan.
/// </summary>
public static class ReportMetadataCache
{
    private static readonly ConcurrentDictionary<Type, ReportTypeMetadata> Cache = new();

    /// <summary>
    /// Gets the cached metadata for a type, building and caching it on first use.
    /// </summary>
    public static ReportTypeMetadata Get(Type type)
    {
        return Cache.GetOrAdd(type, Build);
    }

    /// <summary>
    /// Clears all cached metadata. Intended only for scenarios involving dynamic
    /// assembly loading or hot-reload of reportable types; not needed in normal use.
    /// </summary>
    public static void Clear()
    {
        Cache.Clear();
    }

    private static ReportTypeMetadata Build(Type type)
    {
        var reportable = type.GetCustomAttribute<ReportableAttribute>();
        if (reportable == null)
        {
            throw new InvalidOperationException(
                "Type '" + type.Name + "' is not marked with [Reportable] and cannot be used as a report source.");
        }

        var source = reportable.Source ?? type.Name;
        var fields = new Dictionary<string, FieldMetadata>();

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<ReportIgnoreAttribute>() != null)
            {
                continue;
            }

            if (property.CanRead == false)
            {
                continue;
            }

            var reportField = property.GetCustomAttribute<ReportFieldAttribute>();

            var fieldMetadata = new FieldMetadata
            {
                PropertyName = property.Name,
                Column = reportField?.Column ?? property.Name,
                DefaultDisplayName = reportField?.DisplayName ?? property.Name,
                AllowedAggregates = reportField?.AllowedAggregates ?? [],
                Accessor = FieldAccessor.Compile(property),
                PropertyType = property.PropertyType
            };

            fields[property.Name] = fieldMetadata;
        }

        return new ReportTypeMetadata
        {
            ClrType = type,
            Source = source,
            IgnoreTenant = reportable.IgnoreTenant,
            Fields = fields
        };
    }
}
