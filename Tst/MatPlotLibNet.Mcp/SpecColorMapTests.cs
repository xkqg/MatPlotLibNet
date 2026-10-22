// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A colour map built in code travels in a spec as what it is made of (<c>colorMap</c>, written by kind), and
/// the reader checks it as it checks a normaliser: the kind must be one the serializer reads, wherever it stands and in
/// any case, nested inside a reversed map too, and every colour in it may be a CSS4 name.</summary>
public class SpecColorMapTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    private static string HeatmapSpec(string colorMap, string subPlotFields = "") =>
        $$"""{"width":400,"height":300,"subPlots":[{{{subPlotFields}}"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"colorMap":{{colorMap}}}]}]}""";

    private static IColorMap? MapOf(string spec) =>
        Assert.IsType<HeatmapSeries>(Reader.Read(new ChartSpec(spec)).SubPlots[0].Series[0]).ColorMap;

    [Theory]
    [InlineData("""{"type":"gradient","colors":["red","blue"]}""")]
    [InlineData("""{"type":"reversed","map":{"type":"gradient","colors":["red","blue"]}}""")]
    public void AMisspelledColourMapKind_IsRefusedWithTheAcceptedKinds(string colorMap)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(HeatmapSpec(colorMap))));

        Assert.Contains("'gradient'", refusal.Message);
        Assert.Contains("linear", refusal.Message);
        Assert.Contains("listed", refusal.Message);
    }

    [Fact]
    public void AColourMapWithoutAKind_IsRefusedWithTheAcceptedKinds()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(HeatmapSpec("""{"colors":["red","blue"]}"""))));

        Assert.Contains("colorMap", refusal.Message);
        Assert.Contains("reversed", refusal.Message);
    }

    [Fact]
    public void AMisspelledColourMapKindOnAColourBar_IsRefusedWithTheAcceptedKinds()
    {
        string spec = HeatmapSpec("""{"type":"listed","colors":["red"]}""", subPlotFields: """
            "colorBar":{"colorMap":{"type":"spectrum"}},
            """.Trim());

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(spec)));

        Assert.Contains("'spectrum'", refusal.Message);
        Assert.Contains("named", refusal.Message);
    }

    /// <summary>An agent writes the kind after the fields, in any case, and colours by name: the map is taken as written.</summary>
    [Fact]
    public void AColourMapWithItsKindLastAndColourNames_IsTakenAsWritten()
    {
        var map = MapOf(HeatmapSpec("""{"colors":["white","darkgreen"],"badColor":"lightgray","name":"days","type":"LINEAR"}"""));

        Assert.NotNull(map);
        Assert.Equal("days", map.Name);
        Assert.Equal(Color.FromName("white"), map.GetColor(0));
        Assert.Equal(Color.FromName("darkgreen"), map.GetColor(1));
        Assert.Equal(Color.FromName("lightgray"), map.GetBadColor());
    }

    [Fact]
    public void AReversedListedMapWrittenByHand_IsTakenAsWritten()
    {
        var map = MapOf(HeatmapSpec("""{"map":{"colors":["red","#00f"],"type":"Listed"},"type":"reversed"}"""));

        Assert.NotNull(map);
        Assert.Equal(Color.FromHex("#0000FF"), map.GetColor(0));
        Assert.Equal(Color.FromHex("#FF0000"), map.GetColor(1));
    }

    /// <summary>A description that cannot make a map is refused with the field it lacks.</summary>
    [Fact]
    public void AColourMapWithoutColours_IsRefused()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(HeatmapSpec("""{"type":"linear","colors":[]}"""))));

        Assert.Contains("colors", refusal.Message);
    }
}
