// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The catalog is what <c>list_chart_types</c> shows and what the reader validates against. It is
/// READ off the serializer's registry, never kept by hand, minus the types whose JSON reader ignores the
/// document and substitutes placeholder data — listing those would advertise a chart that renders someone
/// else's numbers with a success flag.</summary>
public class ChartTypeCatalogTests
{
    private readonly ChartTypeCatalog _catalog = new();

    [Fact]
    public void ListingTheTypes_NeverReturnsAnEmptyList() => Assert.NotEmpty(_catalog.Listed);

    [Fact]
    public void TheListedAndTheExcludedTogether_AreExactlyTheRegistry()
    {
        var union = _catalog.Listed.Concat(_catalog.Excluded.Keys).OrderBy(t => t, StringComparer.Ordinal);

        Assert.Equal(SeriesRegistry.Discriminators, union);
    }

    [Fact]
    public void TheCountIsDerived_NotLiteral() =>
        Assert.Equal(SeriesRegistry.Discriminators.Count - _catalog.Excluded.Count, _catalog.Listed.Count);

    [Fact]
    public void TheListedTypes_AreSorted_AndUnique()
    {
        Assert.Equal(_catalog.Listed.OrderBy(t => t, StringComparer.Ordinal), _catalog.Listed);
        Assert.Equal(_catalog.Listed.Count, _catalog.Listed.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void GeoPolygon_IsNotListed_BecauseNothingRegistersIt()
    {
        Assert.DoesNotContain("geo_polygon", _catalog.Listed);
        Assert.DoesNotContain("geo_polygon", _catalog.Excluded.Keys);
    }

    /// <summary>Each of these used to build a fixed placeholder instead of reading its document and was excluded
    /// with a reason; each now has a real <c>FromSeriesDto</c> (JSON-roundtrip plan) and is listed like any other
    /// registered type.</summary>
    [Theory]
    [InlineData("sankey")]
    [InlineData("sunburst")]
    [InlineData("treemap")]
    [InlineData("polarbar")]
    [InlineData("polarline")]
    [InlineData("polarscatter")]
    [InlineData("treegrid")]
    [InlineData("dendrogram")]
    public void AFormerlyPlaceholderType_IsListedNow_NotExcluded(string type)
    {
        Assert.True(_catalog.IsListed(type));
        Assert.DoesNotContain(type, _catalog.Excluded.Keys);
    }

    [Fact]
    public void NoTypeIsExcludedToday() => Assert.Empty(_catalog.Excluded);

    /// <summary>The exclusion mechanism itself, independent of which real type happens to be excluded on any
    /// given day — exercised through the constructor <see cref="ChartTypeCatalog"/> keeps for this purpose.</summary>
    [Fact]
    public void AnInjectedExclusion_IsExcludedWithItsReason_AndNotListed()
    {
        var catalog = new ChartTypeCatalog(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["line"] = "a fake reason for this test only",
        });

        Assert.False(catalog.IsListed("line"));
        Assert.Equal("a fake reason for this test only", catalog.Excluded["line"]);
    }

    [Theory]
    [InlineData("line", true)]
    [InlineData("candlestick", true)]
    [InlineData("Line", false)]
    [InlineData("nope", false)]
    public void IsListed_IsAnExactOrdinalMatch(string type, bool listed) => Assert.Equal(listed, _catalog.IsListed(type));

    [Theory]
    [InlineData("Line", "line")]
    [InlineData("lien", "line")]
    [InlineData("scater", "scatter")]
    [InlineData("BAR", "bar")]
    [InlineData("zzzzzzzz", null)]
    public void ASuggestion_IsTheNearestListedType_OrNothing(string typed, string? expected) =>
        Assert.Equal(expected, _catalog.Suggest(typed));
}
