using Repotara.Aggregation;
using Repotara.Definition;
using Repotara.Metadata;
using Repotara.Tests.Fixtures;

namespace Repotara.Tests.Definition;

public class ReportDefinitionValidatorTests
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
    public void Validate_AcceptsSimpleValidDefinition()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsUnknownSource()
    {
        var definition = new ReportDefinition
        {
            Types = ["NotReal"],
            Fields = [new ReportFieldSelection { Field = "NotReal.Id" }]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsFieldSelectionWithNeitherFieldNorConcat()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection()]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsFieldSelectionWithBothFieldAndConcat()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields =
            [
                new ReportFieldSelection
                {
                    Field = "TestOrder.Id",
                    Concat = new ConcatDefinition { Fields = ["TestOrder.Id", "TestOrder.CustomerId"] }
                }
            ]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsConcatWithFewerThanTwoFields()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields =
            [
                new ReportFieldSelection
                {
                    DisplayName = "Bad",
                    Concat = new ConcatDefinition { Fields = ["TestOrder.Id"] }
                }
            ]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AllowsAggregateOnFieldWithNoRestriction()
    {
        // Id has an empty AllowedAggregates array, which means "any aggregate is allowed".
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id", Aggregate = AggregateType.Count }]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsDisallowedAggregate()
    {
        // Total only allows Sum and Avg -- Count should be rejected.
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Total", Aggregate = AggregateType.Count }]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsUngroupedUnaggregatedFieldWhenGroupByIsUsed()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields =
            [
                new ReportFieldSelection { Field = "TestOrder.Id" },
                new ReportFieldSelection { Field = "TestOrder.Total", Aggregate = AggregateType.Sum }
            ],
            GroupBy = ["TestOrder.CustomerId"]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsJoinWithUnknownKey()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder", "TestCustomer"],
            Joins = [new JoinDefinition { Left = "TestOrder", LeftKey = "NotAField", Right = "TestCustomer", RightKey = "Id" }],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }]
        };

        var result = ReportDefinitionValidator.Validate(definition, BuildMetadata());

        Assert.False(result.IsValid);
    }
}
