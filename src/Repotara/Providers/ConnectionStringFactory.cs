using Repotara.Providers.Sql;

namespace Repotara.Providers;

/// <summary>
/// Builds a provider-specific connection string from the discrete Host/Port/
/// DatabaseName/Username/Password fields in <see cref="RepotaraOptions"/>, so
/// consumers never need to know provider-specific connection string syntax.
/// </summary>
public static class ConnectionStringFactory
{
    /// <summary>Builds the connection string appropriate for the configured provider and dialect.</summary>
    public static string Build(RepotaraOptions options)
    {
        if (options.Provider == ProviderType.MongoDb)
        {
            return BuildMongo(options);
        }

        return options.Options switch
        {
            SqlOption.SqlServer => BuildSqlServer(options),
            SqlOption.PostgreSql => BuildPostgreSql(options),
            SqlOption.MySql => BuildMySql(options),
            _ => throw new NotSupportedException("Unsupported SQL option: " + options.Options)
        };
    }

    private static string BuildSqlServer(RepotaraOptions options)
    {
        return "Server=" + options.Host + "," + options.Port + ";"
             + "Database=" + options.DatabaseName + ";"
             + "User=" + options.Username + ";"
             + "Password=" + options.Password + ";"
             + "timeout=60;"
             + "TrustServerCertificate=True;";
    }

    private static string BuildPostgreSql(RepotaraOptions options)
    {
        return "Host=" + options.Host + ";"
             + "Port=" + options.Port + ";"
             + "Database=" + options.DatabaseName + ";"
             + "Username=" + options.Username + ";"
             + "Password=" + options.Password + ";";
    }

    private static string BuildMySql(RepotaraOptions options)
    {
        return "Server=" + options.Host + ";"
             + "Port=" + options.Port + ";"
             + "Database=" + options.DatabaseName + ";"
             + "Uid=" + options.Username + ";"
             + "Pwd=" + options.Password + ";";
    }

    private static string BuildMongo(RepotaraOptions options)
    {
        return "mongodb://" + options.Username + ":" + options.Password
             + "@" + options.Host + ":" + options.Port
             + "/" + options.DatabaseName;
    }
}
