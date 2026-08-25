using MongoDB.Bson;
using Repotara.Aggregation;
using Repotara.Definition;
using Repotara.Metadata;
using Repotara.Providers.Mongo;
using Repotara.Tests.Fixtures;

namespace Repotara.Tests.Providers;

public class MongoPipelineBuilderTests
{
    private static Dictionary<string, ReportTypeMetadata> BuildMetadata()
    {
        return new Dictionary<string, ReportTypeMetadata>
        {
            ["TestOrder"] = ReportMetadataCache.Get(typeof(TestOrder)),
            ["TestCustomer"] = ReportMetadataCache.Get(typeof(TestCustomer))
        };
    }

    [Fact]
    public void Build_ProducesProjectStageForSimpleDefinition()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id", DisplayName = "Order Number" }]
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var project = stages.Single(s => s.Contains("$project"));
        Assert.Equal("$Id", project["$project"]["Order Number"].AsString);
    }

    [Fact]
    public void Build_ProducesLookupAndUnwindForJoin()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder", "TestCustomer"],
            Joins = [new JoinDefinition { Left = "TestOrder", LeftKey = "CustomerId", Right = "TestCustomer", RightKey = "Id" }],
            Fields = [new ReportFieldSelection { Field = "TestCustomer.FirstName" }]
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var lookup = stages.Single(s => s.Contains("$lookup"))["$lookup"].AsBsonDocument;
        Assert.Equal("customers", lookup["from"].AsString);
        Assert.Equal("CustomerId", lookup["localField"].AsString);
        Assert.Equal("Id", lookup["foreignField"].AsString);
        Assert.Equal("TestCustomer", lookup["as"].AsString);

        var unwind = stages.Single(s => s.Contains("$unwind"))["$unwind"].AsBsonDocument;
        Assert.Equal("$TestCustomer", unwind["path"].AsString);
        Assert.False(unwind["preserveNullAndEmptyArrays"].AsBoolean);
    }

    [Fact]
    public void Build_PreservesNullsForLeftJoin()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder", "TestCustomer"],
            Joins = [new JoinDefinition { Left = "TestOrder", LeftKey = "CustomerId", Right = "TestCustomer", RightKey = "Id", Type = JoinType.Left }],
            Fields = [new ReportFieldSelection { Field = "TestCustomer.FirstName" }]
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var unwind = stages.Single(s => s.Contains("$unwind"))["$unwind"].AsBsonDocument;
        Assert.True(unwind["preserveNullAndEmptyArrays"].AsBoolean);
    }

    [Fact]
    public void Build_ProducesMatchStageForFilter()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Filter = new SearchParam { Property = "TestOrder.Total", Operation = "GT", Value = 100 }
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var match = stages.Single(s => s.Contains("$match"))["$match"].AsBsonDocument;
        Assert.Equal(100, match["order_total"]["$gt"].AsInt32);
    }

    [Fact]
    public void Build_UsesExprForColumnToColumnComparison()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Filter = new SearchParam { Property = "TestOrder.Id", Operation = "GT", ValueProperty = "TestOrder.CustomerId" }
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var match = stages.Single(s => s.Contains("$match"))["$match"].AsBsonDocument;
        Assert.True(match.Contains("$expr"));
    }

    [Fact]
    public void Build_ProducesGroupStageWithAccumulator()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields =
            [
                new ReportFieldSelection { Field = "TestOrder.CustomerId", DisplayName = "Customer" },
                new ReportFieldSelection { Field = "TestOrder.Total", DisplayName = "Total Revenue", Aggregate = AggregateType.Sum }
            ],
            GroupBy = ["TestOrder.CustomerId"]
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var group = stages.Single(s => s.Contains("$group"))["$group"].AsBsonDocument;
        Assert.Equal("$order_total", group["Total Revenue"]["$sum"].AsString);
    }

    [Fact]
    public void Build_ProducesSecondMatchStageForHaving()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields =
            [
                new ReportFieldSelection { Field = "TestOrder.CustomerId", DisplayName = "Customer" },
                new ReportFieldSelection { Field = "TestOrder.Total", DisplayName = "Total Revenue", Aggregate = AggregateType.Sum }
            ],
            GroupBy = ["TestOrder.CustomerId"],
            Having = new SearchParam { Property = "Total Revenue", Operation = "GT", Value = 10000 }
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var matchStages = stages.Where(s => s.Contains("$match")).ToList();
        Assert.Single(matchStages);
        Assert.Equal(10000, matchStages[0]["$match"]["Total Revenue"]["$gt"].AsInt32);
    }

    [Fact]
    public void Build_ProducesConcatStageForComputedField()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestCustomer"],
            Fields =
            [
                new ReportFieldSelection
                {
                    DisplayName = "Full Name",
                    Concat = new ConcatDefinition { Fields = ["TestCustomer.FirstName", "TestCustomer.LastName"], Delimiter = " " }
                }
            ]
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        var project = stages.Single(s => s.Contains("$project"))["$project"].AsBsonDocument;
        var concatArray = project["Full Name"]["$concat"].AsBsonArray;

        Assert.Equal("$first_name", concatArray[0].AsString);
        Assert.Equal(" ", concatArray[1].AsString);
        Assert.Equal("$last_name", concatArray[2].AsString);
    }

    [Fact]
    public void Build_AppliesSkipAndLimit()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Skip = 10,
            Take = 20
        };

        var stages = MongoPipelineBuilder.Build(definition, BuildMetadata(), 10000);

        Assert.Equal(10, stages.Single(s => s.Contains("$skip"))["$skip"].AsInt32);
        Assert.Equal(20, stages.Single(s => s.Contains("$limit"))["$limit"].AsInt32);
    }
}
