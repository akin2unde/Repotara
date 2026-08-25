namespace Repotara.Providers.Sql;

/// <summary>The specific relational database dialect to use when <see cref="ProviderType.Sql"/> is selected.</summary>
public enum SqlOption
{
    /// <summary>Microsoft SQL Server.</summary>
    SqlServer,

    /// <summary>PostgreSQL.</summary>
    PostgreSql,

    /// <summary>MySQL.</summary>
    MySql
}
