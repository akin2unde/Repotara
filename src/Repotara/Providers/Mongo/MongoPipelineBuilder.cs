using MongoDB.Bson;
using Repotara.Aggregation;
using Repotara.Definition;
using Repotara.Metadata;

namespace Repotara.Providers.Mongo;

/// <summary>
/// Translates a validated <see cref="ReportDefinition"/> into a MongoDB
/// aggregation pipeline: <c>$lookup</c> for joins, <c>$match</c> for filters,
/// <c>$group</c> for aggregation, a second <c>$match</c> for having, <c>$sort</c>,
/// then <c>$skip</c>/<c>$limit</c> for pagination.
/// </summary>
public static class MongoPipelineBuilder
{
    /// <summary>Builds the full pipeline for the given definition.</summary>
    public static List<BsonDocument> Build(
        ReportDefinition definition,
        IReadOnlyDictionary<string, ReportTypeMetadata> metadata,
        int defaultRowLimit)
    {
        var rootSource = definition.Types[0];
        var stages = new List<BsonDocument>();

        if (definition.Joins != null)
        {
            foreach (var join in definition.Joins)
            {
                stages.Add(BuildLookup(join, metadata));
                stages.Add(BuildUnwind(join));
            }
        }

        var filter = BuildMatch(definition.Filter, metadata, rootSource);
        if (filter != null)
        {
            stages.Add(new BsonDocument("$match", filter));
        }

        var hasAggregates = definition.Fields.Any(f => f.Aggregate != null);

        if (hasAggregates)
        {
            stages.Add(BuildGroup(definition, metadata, rootSource));
            stages.Add(BuildProjectAfterGroup(definition));

            if (definition.Having != null)
            {
                var having = BuildHavingMatch(definition.Having);
                stages.Add(new BsonDocument("$match", having));
            }
        }
        else
        {
            stages.Add(BuildProject(definition, metadata, rootSource));
        }

        if (definition.Sort != null && definition.Sort.Count > 0)
        {
            stages.Add(BuildSort(definition));
        }

        var skip = definition.Skip ?? 0;
        var take = definition.Take ?? defaultRowLimit;
        stages.Add(new BsonDocument("$skip", skip));
        stages.Add(new BsonDocument("$limit", take));

        return stages;
    }

    private static BsonDocument BuildLookup(JoinDefinition join, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        var rightCollection = metadata[join.Right].Source;
        var leftField = ResolveMongoField(join.Left + "." + join.LeftKey, metadata, join.Left);
        var rightField = metadata[join.Right].GetField(join.RightKey)!.Column;

        return new BsonDocument("$lookup", new BsonDocument
        {
            { "from", rightCollection },
            { "localField", leftField },
            { "foreignField", rightField },
            { "as", join.Right }
        });
    }

    private static BsonDocument BuildUnwind(JoinDefinition join)
    {
        return new BsonDocument("$unwind", new BsonDocument
        {
            { "path", "$" + join.Right },
            { "preserveNullAndEmptyArrays", join.Type == JoinType.Left }
        });
    }

    private static BsonDocument? BuildMatch(SearchParam? node, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, string rootSource)
    {
        if (node == null)
        {
            return null;
        }

        if (node.IsBranch)
        {
            var operatorKeyword = string.Equals(node.Operator, "Or", StringComparison.OrdinalIgnoreCase) ? "$or" : "$and";
            var parts = new BsonArray();
            foreach (var condition in node.Conditions!)
            {
                parts.Add(BuildMatch(condition, metadata, rootSource));
            }
            return new BsonDocument(operatorKeyword, parts);
        }

        var field = "$" + ResolveMongoField(node.Property!, metadata, rootSource);

        if (node.ValueProperty != null)
        {
            var valueField = "$" + ResolveMongoField(node.ValueProperty, metadata, rootSource);
            var exprOperator = ResolveExprOperator(node.Operation!);
            return new BsonDocument("$expr", new BsonDocument(exprOperator, new BsonArray { field, valueField }));
        }

        var matchField = ResolveMongoField(node.Property!, metadata, rootSource);
        var matchOperator = ResolveMatchOperator(node.Operation!);
        return new BsonDocument(matchField, new BsonDocument(matchOperator, BsonValue.Create(node.Value)));
    }

    private static BsonDocument BuildGroup(ReportDefinition definition, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, string rootSource)
    {
        var idFields = new BsonDocument();
        if (definition.GroupBy != null)
        {
            foreach (var groupField in definition.GroupBy)
            {
                idFields[SafeKey(groupField)] = "$" + ResolveMongoField(groupField, metadata, rootSource);
            }
        }

        var group = new BsonDocument
        {
            { "_id", idFields.ElementCount == 0 ? BsonNull.Value : idFields }
        };

        foreach (var field in definition.Fields)
        {
            if (field.Aggregate == null)
            {
                continue;
            }

            var displayName = field.DisplayName ?? field.Field ?? "Value";

            if (field.Aggregate == AggregateType.Count)
            {
                group[displayName] = new BsonDocument("$sum", 1);
                continue;
            }

            var expression = "$" + ResolveMongoField(field.Field!, metadata, rootSource);
            var accumulator = ResolveAccumulator(field.Aggregate.Value);
            group[displayName] = new BsonDocument(accumulator, expression);
        }

        return new BsonDocument("$group", group);
    }

    private static BsonDocument BuildProjectAfterGroup(ReportDefinition definition)
    {
        var project = new BsonDocument { { "_id", 0 } };

        if (definition.GroupBy != null)
        {
            foreach (var groupField in definition.GroupBy)
            {
                var matchingSelection = definition.Fields.FirstOrDefault(f => f.Field == groupField);
                var displayName = matchingSelection?.DisplayName ?? groupField;
                project[displayName] = "$_id." + SafeKey(groupField);
            }
        }

        foreach (var field in definition.Fields)
        {
            if (field.Aggregate == null)
            {
                continue;
            }

            var displayName = field.DisplayName ?? field.Field ?? "Value";
            project[displayName] = 1;
        }

        return new BsonDocument("$project", project);
    }

    private static BsonDocument BuildHavingMatch(SearchParam node)
    {
        if (node.IsBranch)
        {
            var operatorKeyword = string.Equals(node.Operator, "Or", StringComparison.OrdinalIgnoreCase) ? "$or" : "$and";
            var parts = new BsonArray();
            foreach (var condition in node.Conditions!)
            {
                parts.Add(BuildHavingMatch(condition));
            }
            return new BsonDocument(operatorKeyword, parts);
        }

        // Having references the already-projected output display name directly --
        // no source prefix needed, unlike a pre-aggregation Filter.
        var matchOperator = ResolveMatchOperator(node.Operation!);
        return new BsonDocument(node.Property!, new BsonDocument(matchOperator, BsonValue.Create(node.Value)));
    }

    private static BsonDocument BuildProject(ReportDefinition definition, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, string rootSource)
    {
        var project = new BsonDocument { { "_id", 0 } };

        foreach (var field in definition.Fields)
        {
            var displayName = ResolveDisplayName(field, metadata);

            if (field.Concat != null)
            {
                var parts = new BsonArray();
                for (var i = 0; i < field.Concat.Fields.Count; i++)
                {
                    parts.Add("$" + ResolveMongoField(field.Concat.Fields[i], metadata, rootSource));

                    var isLast = i == field.Concat.Fields.Count - 1;
                    if (isLast == false)
                    {
                        parts.Add(field.Concat.Delimiter);
                    }
                }
                project[displayName] = new BsonDocument("$concat", parts);
                continue;
            }

            project[displayName] = "$" + ResolveMongoField(field.Field!, metadata, rootSource);
        }

        return new BsonDocument("$project", project);
    }

    private static BsonDocument BuildSort(ReportDefinition definition)
    {
        var sort = new BsonDocument();
        foreach (var sortField in definition.Sort!)
        {
            sort[sortField.Field] = sortField.Direction == SortDirection.Desc ? -1 : 1;
        }
        return new BsonDocument("$sort", sort);
    }

    private static string ResolveDisplayName(ReportFieldSelection field, IReadOnlyDictionary<string, ReportTypeMetadata> metadata)
    {
        if (string.IsNullOrWhiteSpace(field.DisplayName) == false)
        {
            return field.DisplayName;
        }

        if (field.Field != null)
        {
            return FieldResolver.ResolveField(field.Field, metadata).DefaultDisplayName;
        }

        return "Value";
    }

    /// <summary>
    /// Resolves a "Source.Property" path to its physical Mongo field path. Root
    /// source fields are top-level (no prefix); joined sources are nested under
    /// the $lookup "as" alias, so they resolve to "Alias.field".
    /// </summary>
    private static string ResolveMongoField(string path, IReadOnlyDictionary<string, ReportTypeMetadata> metadata, string rootSource)
    {
        var (sourceName, _) = FieldResolver.SplitPath(path);
        var field = FieldResolver.ResolveField(path, metadata);

        return sourceName == rootSource ? field.Column : sourceName + "." + field.Column;
    }

    private static string SafeKey(string path) => path.Replace('.', '_');

    private static string ResolveAccumulator(AggregateType type)
    {
        return type switch
        {
            AggregateType.Sum => "$sum",
            AggregateType.Avg => "$avg",
            AggregateType.Min => "$min",
            AggregateType.Max => "$max",
            AggregateType.Count => "$sum",
            _ => throw new NotSupportedException("Unsupported aggregate type: " + type)
        };
    }

    private static string ResolveMatchOperator(string operation)
    {
        return operation switch
        {
            "EQ" => "$eq",
            "NEQ" => "$ne",
            "GT" => "$gt",
            "GTE" => "$gte",
            "LT" => "$lt",
            "LTE" => "$lte",
            "IN" => "$in",
            "CONTAINS" => "$regex",
            _ => throw new NotSupportedException("Unsupported filter operation: " + operation)
        };
    }

    private static string ResolveExprOperator(string operation)
    {
        return operation switch
        {
            "EQ" => "$eq",
            "NEQ" => "$ne",
            "GT" => "$gt",
            "GTE" => "$gte",
            "LT" => "$lt",
            "LTE" => "$lte",
            _ => throw new NotSupportedException("Unsupported column-to-column operation: " + operation)
        };
    }
}
