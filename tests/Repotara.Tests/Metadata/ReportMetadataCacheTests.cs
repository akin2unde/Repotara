using Repotara.Metadata;
using Repotara.Tests.Fixtures;

namespace Repotara.Tests.Metadata;

public class ReportMetadataCacheTests
{
    [Fact]
    public void Get_IncludesAllPublicPropertiesByDefault()
    {
        var metadata = ReportMetadataCache.Get(typeof(TestOrder));

        Assert.NotNull(metadata.GetField("Id"));
        Assert.NotNull(metadata.GetField("CustomerId"));
        Assert.NotNull(metadata.GetField("Total"));
        Assert.NotNull(metadata.GetField("CompanyId"));
    }

    [Fact]
    public void Get_ExcludesReportIgnoreProperties()
    {
        var metadata = ReportMetadataCache.Get(typeof(TestOrder));

        Assert.Null(metadata.GetField("InternalNotes"));
    }

    [Fact]
    public void Get_UsesAttributeColumnAndDisplayNameWhenSet()
    {
        var metadata = ReportMetadataCache.Get(typeof(TestOrder));
        var field = metadata.GetField("Total");

        Assert.NotNull(field);
        Assert.Equal("order_total", field!.Column);
        Assert.Equal("Order Total", field.DefaultDisplayName);
    }

    [Fact]
    public void Get_FallsBackToPropertyNameWhenNoAttribute()
    {
        var metadata = ReportMetadataCache.Get(typeof(TestOrder));
        var field = metadata.GetField("Id");

        Assert.NotNull(field);
        Assert.Equal("Id", field!.Column);
        Assert.Equal("Id", field.DefaultDisplayName);
    }

    [Fact]
    public void Get_ResolvesSourceNameFromAttribute()
    {
        var metadata = ReportMetadataCache.Get(typeof(TestOrder));

        Assert.Equal("orders", metadata.Source);
    }

    [Fact]
    public void Get_IsCachedAcrossCalls()
    {
        var first = ReportMetadataCache.Get(typeof(TestOrder));
        var second = ReportMetadataCache.Get(typeof(TestOrder));

        Assert.Same(first, second);
    }

    [Fact]
    public void Get_ReadsIgnoreTenantFlag()
    {
        var metadata = ReportMetadataCache.Get(typeof(TestRegion));

        Assert.True(metadata.IgnoreTenant);
    }
}

public class FieldResolverTests
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
    public void ResolveColumn_ReturnsSourceDotPhysicalColumn()
    {
        var metadata = BuildMetadata();

        var column = FieldResolver.ResolveColumn("TestOrder.Total", metadata);

        Assert.Equal("TestOrder.order_total", column);
    }

    [Fact]
    public void ResolveField_ThrowsForUnknownSource()
    {
        var metadata = BuildMetadata();

        Assert.Throws<InvalidOperationException>(() => FieldResolver.ResolveField("Unknown.Field", metadata));
    }

    [Fact]
    public void ResolveField_ThrowsForNonReportableProperty()
    {
        var metadata = BuildMetadata();

        Assert.Throws<InvalidOperationException>(() => FieldResolver.ResolveField("TestOrder.InternalNotes", metadata));
    }

    [Fact]
    public void SplitPath_ThrowsWhenNoDotPresent()
    {
        Assert.Throws<InvalidOperationException>(() => FieldResolver.SplitPath("NoDotHere"));
    }
}
