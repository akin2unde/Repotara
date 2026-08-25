using Repotara.Aggregation;
using Repotara.Definition;
using Repotara.Metadata;
using Repotara.Providers.Sql;
using Repotara.Tests.Fixtures;

namespace Repotara.Tests.Providers;

public class SqlQueryBuilderTests
{
    private static Dictionary<string, ReportTypeMetadata> BuildMetadata()
    {
        return new Dictionary<string, ReportTypeMetadata>
        {
            ["TestOrder"] = ReportMetadataCache.Get(typeof(TestOrder)),
            ["TestCustomer"] = ReportMetadataCache.Get(typeof(TestCustomer))
        };
    }

    private static SqlQueryBuilder BuildPostgresBuilder() => new(new PostgreSqlDialect());

    [Fact]
    public void Build_ProducesSelectFromForSimpleDefinition()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id", DisplayName = "Order Number" }]
        };

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("SELECT TestOrder.Id AS \"Order Number\"", sql);
        Assert.Contains("FROM orders AS TestOrder", sql);
    }

    [Fact]
    public void Build_BindsFilterValuesAsParametersNotLiterals()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Filter = new SearchParam { Property = "TestOrder.Total", Operation = "GT", Value = 100m }
        };

        var (sql, parameters) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("@p0", sql);
        Assert.Contains("TestOrder.order_total > @p0", sql);
        Assert.DoesNotContain("> 100 ", sql);
        Assert.Equal(100m, parameters["@p0"]);
    }

    [Fact]
    public void Build_ProducesAndOrForNestedFilterTree()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Filter = new SearchParam
            {
                Operator = "And",
                Conditions =
                [
                    new SearchParam { Property = "TestOrder.Total", Operation = "GT", Value = 100m },
                    new SearchParam
                    {
                        Operator = "Or",
                        Conditions =
                        [
                            new SearchParam { Property = "TestOrder.CustomerId", Operation = "EQ", Value = 1 },
                            new SearchParam { Property = "TestOrder.CustomerId", Operation = "EQ", Value = 2 }
                        ]
                    }
                ]
            }
        };

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("WHERE (", sql);
        Assert.Contains(" AND (", sql);
        Assert.Contains(" OR ", sql);
    }

    [Fact]
    public void Build_ComparesTwoColumnsWithoutBindingAParameter()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Filter = new SearchParam { Property = "TestOrder.Id", Operation = "GT", ValueProperty = "TestOrder.CustomerId" }
        };

        var (sql, parameters) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("TestOrder.Id > TestOrder.CustomerId", sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Build_ProducesJoinClause()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder", "TestCustomer"],
            Joins = [new JoinDefinition { Left = "TestOrder", LeftKey = "CustomerId", Right = "TestCustomer", RightKey = "Id" }],
            Fields = [new ReportFieldSelection { Field = "TestCustomer.FirstName" }]
        };

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("JOIN customers AS TestCustomer ON TestOrder.CustomerId = TestCustomer.Id", sql);
    }

    [Fact]
    public void Build_UsesLeftJoinKeywordWhenSpecified()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder", "TestCustomer"],
            Joins = [new JoinDefinition { Left = "TestOrder", LeftKey = "CustomerId", Right = "TestCustomer", RightKey = "Id", Type = JoinType.Left }],
            Fields = [new ReportFieldSelection { Field = "TestCustomer.FirstName" }]
        };

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("LEFT JOIN customers AS TestCustomer", sql);
    }

    [Fact]
    public void Build_ProducesGroupByAndAggregateFunction()
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

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("SUM(TestOrder.order_total) AS \"Total Revenue\"", sql);
        Assert.Contains("GROUP BY TestOrder.CustomerId", sql);
    }

    [Fact]
    public void Build_ProducesHavingClauseReferencingAggregateExpression()
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
            Having = new SearchParam { Property = "Total Revenue", Operation = "GT", Value = 10000m }
        };

        var (sql, parameters) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("HAVING SUM(TestOrder.order_total) >", sql);
        Assert.Contains(10000m, parameters.Values);
    }

    [Fact]
    public void Build_ProducesConcatExpression()
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

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("CONCAT(TestCustomer.first_name, ' ', TestCustomer.last_name) AS \"Full Name\"", sql);
    }

    [Fact]
    public void Build_ProducesOrderByForSortFields()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id", DisplayName = "Id" }],
            Sort = [new SortField { Field = "Id", Direction = SortDirection.Desc }]
        };

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 10000);

        Assert.Contains("ORDER BY \"Id\" DESC", sql);
    }

    [Fact]
    public void Build_UsesDefaultRowLimitWhenTakeNotSpecified()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }]
        };

        var (sql, _) = BuildPostgresBuilder().Build(definition, BuildMetadata(), 500);

        Assert.Contains("LIMIT 500 OFFSET 0", sql);
    }

    [Fact]
    public void Build_UsesSqlServerPagingSyntaxForThatDialect()
    {
        var definition = new ReportDefinition
        {
            Types = ["TestOrder"],
            Fields = [new ReportFieldSelection { Field = "TestOrder.Id" }],
            Take = 25
        };

        var builder = new SqlQueryBuilder(new SqlServerDialect());
        var (sql, _) = builder.Build(definition, BuildMetadata(), 10000);

        Assert.Contains("OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY", sql);
    }
}
