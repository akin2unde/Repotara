using System.Linq.Expressions;
using System.Reflection;

namespace Repotara.Metadata;

/// <summary>
/// A pre-compiled, reflection-free getter for a single property. Built once per
/// property when a type is first scanned by <see cref="ReportMetadataCache"/>,
/// then reused for every row afterward.
/// </summary>
public sealed class FieldAccessor
{
    /// <summary>The compiled getter delegate.</summary>
    public Func<object, object?> Getter { get; }

    private FieldAccessor(Func<object, object?> getter)
    {
        Getter = getter;
    }

    /// <summary>
    /// Compiles a getter for the given property using an expression tree,
    /// paying the reflection/JIT cost once instead of on every access.
    /// </summary>
    public static FieldAccessor Compile(PropertyInfo property)
    {
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instanceParam, property.DeclaringType!);
        var propertyAccess = Expression.Property(typedInstance, property);
        var boxedResult = Expression.Convert(propertyAccess, typeof(object));

        var lambda = Expression.Lambda<Func<object, object?>>(boxedResult, instanceParam);
        return new FieldAccessor(lambda.Compile());
    }
}
