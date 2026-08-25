using System.Reflection;
using Repotara.Attributes;

namespace Repotara.Providers;

/// <summary>
/// Resolves the source names used in a <see cref="Repotara.Definition.ReportDefinition"/>
/// (e.g. "Order") to their CLR <see cref="Type"/>, built once at startup from
/// whatever assemblies, base types, or individual types were registered via
/// <see cref="RepotaraOptions.RegisterAssembly"/>, <see cref="RepotaraOptions.RegisterDerivedFrom{TBase}"/>,
/// and <see cref="RepotaraOptions.RegisterType{T}"/>.
/// </summary>
public sealed class ReportableTypeRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _typesByName;

    private ReportableTypeRegistry(IReadOnlyDictionary<string, Type> typesByName)
    {
        _typesByName = typesByName;
    }

    /// <summary>
    /// Resolves a source name (matching a class name) to its registered CLR type,
    /// throwing a clear error if it was never registered.
    /// </summary>
    public Type Resolve(string sourceName)
    {
        if (_typesByName.TryGetValue(sourceName, out var type) == false)
        {
            throw new InvalidOperationException(
                "Unknown report source '" + sourceName + "'. Ensure the class is marked " +
                "[Reportable] and registered via RepotaraOptions.RegisterAssembly, " +
                "RegisterDerivedFrom, or RegisterType.");
        }

        return type;
    }

    /// <summary>Builds the registry from the given options, scanning every registered source.</summary>
    public static ReportableTypeRegistry Build(RepotaraOptions options)
    {
        var typesByName = new Dictionary<string, Type>();

        foreach (var assembly in options.Assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ReportableAttribute>() != null)
                {
                    AddType(typesByName, type);
                }
            }
        }

        foreach (var (baseType, assembly) in options.DerivedFromRegistrations)
        {
            foreach (var type in assembly.GetTypes())
            {
                var isDerived = baseType.IsAssignableFrom(type) && type != baseType;
                var isReportable = type.GetCustomAttribute<ReportableAttribute>() != null;

                if (isDerived && isReportable)
                {
                    AddType(typesByName, type);
                }
            }
        }

        foreach (var type in options.ExplicitTypes)
        {
            AddType(typesByName, type);
        }

        return new ReportableTypeRegistry(typesByName);
    }

    private static void AddType(Dictionary<string, Type> typesByName, Type type)
    {
        if (typesByName.TryGetValue(type.Name, out var existing) && existing != type)
        {
            throw new InvalidOperationException(
                "Two different [Reportable] classes are both named '" + type.Name + "' " +
                "(" + existing.FullName + " and " + type.FullName + "). Report source names must be unique.");
        }

        typesByName[type.Name] = type;
    }
}
