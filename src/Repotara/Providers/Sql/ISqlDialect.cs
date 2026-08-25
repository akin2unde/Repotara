namespace Repotara.Providers.Sql;

/// <summary>
/// Handles syntax differences between relational databases (identifier
/// quoting, parameter style, paging syntax) so <see cref="SqlQueryBuilder"/>
/// can stay dialect-agnostic.
/// </summary>
public interface ISqlDialect
{
    /// <summary>Quotes an identifier (table or column alias) for safe inclusion in a query.</summary>
    string QuoteIdentifier(string name);

    /// <summary>The parameter placeholder prefix, e.g. "@".</summary>
    string ParameterPrefix { get; }

    /// <summary>Builds the paging clause (e.g. OFFSET/FETCH or LIMIT/OFFSET) for this dialect.</summary>
    string BuildPaging(int skip, int take);
}
