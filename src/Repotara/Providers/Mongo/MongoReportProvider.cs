using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Repotara.Definition;
using Repotara.Metadata;

namespace Repotara.Providers.Mongo;

/// <summary>
/// Executes a <see cref="ReportDefinition"/> against MongoDB by translating it
/// to an aggregation pipeline via <see cref="MongoPipelineBuilder"/>.
/// </summary>
public sealed class MongoReportProvider : IReportDataProvider
{
    private readonly IMongoDatabase _database;
    private readonly RepotaraOptions _options;

    /// <summary>Creates the provider using the given configured options.</summary>
    public MongoReportProvider(IOptions<RepotaraOptions> options)
    {
        _options = options.Value;
        var connectionString = ConnectionStringFactory.Build(_options);
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(_options.DatabaseName);
    }

    /// <inheritdoc />
    public async Task<List<ReportRow>> ExecuteAsync(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> sourceMetadata,
        CancellationToken cancellationToken = default)
    {
        var rootSourceName = definition.Types[0];
        var rootCollectionName = sourceMetadata[rootSourceName].Source;
        var collection = _database.GetCollection<BsonDocument>(rootCollectionName);

        var stages = MongoPipelineBuilder.Build(definition, sourceMetadata, _options.DefaultRowLimit);
        var pipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(stages);

        var cursor = await collection.AggregateAsync(pipeline, cancellationToken: cancellationToken);
        var documents = await cursor.ToListAsync(cancellationToken);

        var rows = new List<ReportRow>();
        foreach (var document in documents)
        {
            var row = new ReportRow();
            foreach (var element in document.Elements)
            {
                row.Set(element.Name, BsonTypeMapper.MapToDotNetValue(element.Value));
            }
            rows.Add(row);
        }

        return rows;
    }
}
