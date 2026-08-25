namespace Repotara.Providers.Sql;

/// <summary>SQL syntax rules for Microsoft SQL Server.</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    /// <inheritdoc />
    public string QuoteIdentifier(string name) => "[" + name + "]";

    /// <inheritdoc />
    public string ParameterPrefix => "@";

    /// <inheritdoc />
    public string BuildPaging(int skip, int take)
    {
        return "OFFSET " + skip + " ROWS FETCH NEXT " + take + " ROWS ONLY";
    }
}
