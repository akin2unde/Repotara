namespace Repotara.Providers.Sql;

/// <summary>SQL syntax rules for MySQL.</summary>
public sealed class MySqlDialect : ISqlDialect
{
    /// <inheritdoc />
    public string QuoteIdentifier(string name) => "`" + name + "`";

    /// <inheritdoc />
    public string ParameterPrefix => "@";

    /// <inheritdoc />
    public string BuildPaging(int skip, int take)
    {
        return "LIMIT " + take + " OFFSET " + skip;
    }
}
