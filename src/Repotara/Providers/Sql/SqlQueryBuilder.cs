using System.Text;
using Repotara.Definition;
using Repotara.Metadata;

namespace Repotara.Providers.Sql;

/// <summary>
/// Translates a validated <see cref="ReportDefinition"/> into parameterized SQL
/// for the configured dialect. Every value in the definition's filters is bound
/// as a query parameter -- never string-concatenated -- to prevent SQL injection.
/// </summary>
public sealed class SqlQueryBuilder
{
    private readonly ISqlDialect _dialect;

    /// <summary>Creates a query builder for the given dialect.</summary>
    public SqlQueryBuilder(ISqlDialect dialect)
    {
        _dialect = dialect;
    }

    /// <summary>Builds the SQL text and its bound parameters for the given definition.</summary>
    public (string Sql, Dictionary<string, object?> Parameters) Build(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> metadata,
        int defaultRowLimit)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = new StringBuilder();

        sql.Append("SELECT ").Append(BuildSelect(definition, metadata));
        sql.Append(" FROM ").Append(BuildFromAndJoins(definition, metadata));

        var where = BuildCondition(definition.Filter, metadata, parameters, useOutputNames: false);
        if (where != null)
        {
            sql.Append(" WHERE ").Append(where);
        }

        var groupBy = BuildGroupBy(definition, metadata);
        if (groupBy != null)
        {
            sql.Append(" GROUP BY ").Append(groupBy);
        }

        var having = BuildHaving(definition, metadata, parameters);
        if (having != null)
        {
            sql.Append(" HAVING ").Append(having);
        }

        var orderBy = BuildOrderBy(definition);
        if (orderBy != null)
        {
            sql.Append(" ORDER BY ").Append(orderBy);
        }

        var skip = definition.Skip ?? 0;
        var take = definition.Take ?? defaultRowLimit;
        sql.Append(' ').Append(_dialect.BuildPaging(skip, take));

        return (sql.ToString(), parameters);
    }

    private string BuildSelect(ReportDefinition definition, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var items = new List<string>();

        foreach (var field in definition.Fields)
        {
            items.Add(BuildSelectItem(field, metadata));
        }

        return string.Join(", ", items);
    }

    private string BuildSelectItem(ReportFieldSelection field, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var displayName = ResolveDisplayName(field, metadata);
        var expression = BuildFieldExpression(field, metadata);
        return expression + " AS " + _dialect.QuoteIdentifier(displayName);
    }

    private string BuildFieldExpression(ReportFieldSelection field, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        if (field.Concat != null)
        {
            var parts = new List<string>();
            for (var i = 0; i < field.Concat.Fields.Count; i++)
            {
                parts.Add(FieldResolver.ResolveColumn(field.Concat.Fields[i], metadata));

                var isLast = i == field.Concat.Fields.Count - 1;
                if (isLast == false)
                {
                    parts.Add("'" + field.Concat.Delimiter.Replace("'", "''") + "'");
                }
            }

            return "CONCAT(" + string.Join(", ", parts) + ")";
        }

        var column = FieldResolver.ResolveColumn(field.Field!, metadata);

        if (field.Aggregate != null)
        {
            var function = field.Aggregate.Value.ToString().ToUpperInvariant();
            return function + "(" + column + ")";
        }

        return column;
    }

    private static string ResolveDisplayName(ReportFieldSelection field, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        if (string.IsNullOrWhiteSpace(field.DisplayName) == false)
        {
            return field.DisplayName;
        }

        if (field.Field != null)
        {
            var resolved = FieldResolver.ResolveField(field.Field, metadata);
            return resolved.DefaultDisplayName;
        }

        return "Value";
    }

    private static string BuildFromAndJoins(ReportDefinition definition, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var rootSourceName = definition.Types[0];
        var rootTable = metadata[rootSourceName].Source;

        var builder = new StringBuilder();
        builder.Append(rootTable).Append(" AS ").Append(rootSourceName);

        if (definition.Joins == null)
        {
            return builder.ToString();
        }

        foreach (var join in definition.Joins)
        {
            var rightTable = metadata[join.Right].Source;
            var joinKeyword = join.Type == JoinType.Left ? " LEFT JOIN " : " JOIN ";

            builder.Append(joinKeyword).Append(rightTable).Append(" AS ").Append(join.Right)
                   .Append(" ON ").Append(join.Left).Append('.').Append(metadata[join.Left].GetField(join.LeftKey)!.Column)
                   .Append(" = ").Append(join.Right).Append('.').Append(metadata[join.Right].GetField(join.RightKey)!.Column);
        }

        return builder.ToString();
    }

    private string? BuildCondition(
        SearchParam? node,
        IReadOnlyDictionary<string, ReportTypeMetadata> metadata,
        Dictionary<string, object?> parameters,
        bool useOutputNames)
    {
        if (node == null)
        {
            return null;
        }

        if (node.IsBranch)
        {
            var parts = new List<string>();
            foreach (var condition in node.Conditions!)
            {
                parts.Add(BuildCondition(condition, metadata, parameters, useOutputNames)!);
            }

            var joinWord = string.Equals(node.Operator, "Or", StringComparison.OrdinalIgnoreCase) ? " OR " : " AND ";
            return "(" + string.Join(joinWord, parts) + ")";
        }

        var left = useOutputNames
            ? _dialect.QuoteIdentifier(node.Property!)
            : FieldResolver.ResolveColumn(node.Property!, metadata);

        var sqlOperator = ResolveSqlOperator(node.Operation!);

        string right;
        if (node.ValueProperty != null)
        {
            right = useOutputNames
                ? _dialect.QuoteIdentifier(node.ValueProperty)
                : FieldResolver.ResolveColumn(node.ValueProperty, metadata);
        }
        else
        {
            var paramName = _dialect.ParameterPrefix + "p" + parameters.Count;
            parameters[paramName] = node.Value;
            right = paramName;
        }

        return left + " " + sqlOperator + " " + right;
    }

    private string? BuildGroupBy(ReportDefinition definition, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        if (definition.GroupBy == null || definition.GroupBy.Count == 0)
        {
            return null;
        }

        var columns = new List<string>();
        foreach (var groupField in definition.GroupBy)
        {
            columns.Add(FieldResolver.ResolveColumn(groupField, metadata));
        }

        return string.Join(", ", columns);
    }

    private string? BuildHaving(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> metadata,
        Dictionary<string, object?> parameters)
    {
        if (definition.Having == null)
        {
            return null;
        }

        return BuildHavingCondition(definition.Having, definition, metadata, parameters);
    }

    private string BuildHavingCondition(
        SearchParam node,
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> metadata,
        Dictionary<string, object?> parameters)
    {
        if (node.IsBranch)
        {
            var parts = new List<string>();
            foreach (var condition in node.Conditions!)
            {
                parts.Add(BuildHavingCondition(condition, definition, metadata, parameters));
            }

            var joinWord = string.Equals(node.Operator, "Or", StringComparison.OrdinalIgnoreCase) ? " OR " : " AND ";
            return "(" + string.Join(joinWord, parts) + ")";
        }

        // Having references the output display name; re-resolve the raw aggregate
        // expression, since most dialects do not allow referencing a SELECT alias in HAVING.
        var matchingField = definition.Fields.FirstOrDefault(f => ResolveDisplayName(f, metadata) == node.Property);
        if (matchingField == null)
        {
            throw new InvalidOperationException("Having references unknown output field: " + node.Property);
        }

        var left = BuildFieldExpression(matchingField, metadata);
        var sqlOperator = ResolveSqlOperator(node.Operation!);

        var paramName = _dialect.ParameterPrefix + "p" + parameters.Count;
        parameters[paramName] = node.Value;

        return left + " " + sqlOperator + " " + paramName;
    }

    private string? BuildOrderBy(ReportDefinition definition)
    {
        if (definition.Sort == null || definition.Sort.Count == 0)
        {
            return null;
        }

        var parts = new List<string>();
        foreach (var sortField in definition.Sort)
        {
            var direction = sortField.Direction == SortDirection.Desc ? "DESC" : "ASC";
            parts.Add(_dialect.QuoteIdentifier(sortField.Field) + " " + direction);
        }

        return string.Join(", ", parts);
    }

    private static string ResolveSqlOperator(string operation)
    {
        return operation switch
        {
            "EQ" => "=",
            "NEQ" => "<>",
            "GT" => ">",
            "GTE" => ">=",
            "LT" => "<",
            "LTE" => "<=",
            "IN" => "IN",
            "CONTAINS" => "LIKE",
            _ => throw new NotSupportedException("Unsupported filter operation: " + operation)
        };
    }
}
