namespace Repotara.Providers;

/// <summary>The database technology Repotara runs report queries against.</summary>
public enum ProviderType
{
    /// <summary>A relational database, selected via <see cref="SqlOption"/>.</summary>
    Sql,

    /// <summary>MongoDB.</summary>
    MongoDb
}
