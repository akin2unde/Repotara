using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Npgsql;
using Repotara.Definition;
using Repotara.Metadata;

namespace Repotara.Providers.Sql;

/// <summary>
/// Executes a <see cref="ReportDefinition"/> against a relational database
/// (SQL Server, PostgreSQL, or MySQL) by translating it to parameterized SQL
/// via <see cref="SqlQueryBuilder"/> and running it with ADO.NET.
/// </summary>
public sealed class SqlReportProvider : IReportDataProvider
{
    private readonly RepotaraOptions _options;
    private readonly SqlQueryBuilder _queryBuilder;

    /// <summary>Creates the provider using the given configured options.</summary>
    public SqlReportProvider(IOptions<RepotaraOptions> options)
    {
        _options = options.Value;
        _queryBuilder = new SqlQueryBuilder(ResolveDialect(_options.Options));
    }

    /// <inheritdoc />
    public async Task<List<ReportRow>> ExecuteAsync(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> sourceMetadata,
        CancellationToken cancellationToken = default)
    {
        var (sql, parameters) = _queryBuilder.Build(definition, sourceMetadata, _options.DefaultRowLimit);

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            var rows = new List<ReportRow>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new ReportRow();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row.Set(columnName, value);
                }
                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private DbConnection CreateConnection()
    {
        var connectionString = ConnectionStringFactory.Build(_options);

        return _options.Options switch
        {
            SqlOption.SqlServer => new SqlConnection(connectionString),
            SqlOption.PostgreSql => new NpgsqlConnection(connectionString),
            SqlOption.MySql => new MySqlConnection(connectionString),
            _ => throw new NotSupportedException("Unsupported SQL option: " + _options.Options)
        };
    }

    private static ISqlDialect ResolveDialect(SqlOption option)
    {
        return option switch
        {
            SqlOption.SqlServer => new SqlServerDialect(),
            SqlOption.PostgreSql => new PostgreSqlDialect(),
            SqlOption.MySql => new MySqlDialect(),
            _ => throw new NotSupportedException("Unsupported SQL option: " + option)
        };
    }
}
