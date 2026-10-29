// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>Every property a series draws travels through the spec reader too. An agent hands back the JSON a
/// figure writes; a property the reader refused, or let through misspelt to a serializer that ignores the miss, would
/// be a chart the agent cannot send back as it was drawn.</summary>
public class SpecDrawnPropertiesTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    private static string Spec(string series) =>
        $$"""{"width":400,"height":300,"subPlots":[{"series":[{{series}}]}]}""";

    private static readonly ChartTypeCatalog Catalog = new();

    /// <summary>The rows of the core contract, less the types the catalog does not list because their JSON reader builds a
    /// placeholder (the treemap and the sunburst): a roadmap row of its own, not a property the reader drops.</summary>
    public static TheoryData<string, string> ReaderRows { get; } =
        new(DrawnSeriesFixtures.DrawnTypes
            .Where(type => !Catalog.Excluded.ContainsKey(DiscriminatorOf(type)))
            .SelectMany(type => DrawnSeriesFixtures.DrawnProperties(type).Select(property => (type.Name, property.Name))));

    /// <summary>The <c>type</c> the series of <paramref name="type"/> is written as.</summary>
    private static string DiscriminatorOf(Type type) =>
        DrawnSeriesFixtures.TheSeries(DrawnSeriesFixtures.Build(type.Name, everyPropertySet: false)).ToSeriesDto().Type!;

    /// <summary>The same rows as the core contract, read through the reader instead of the serializer alone.</summary>
    [Theory]
    [MemberData(nameof(ReaderRows))]
    public void ADrawnProperty_PassesTheSpecReader_WithTheValueItWasGiven(string type, string property)
    {
        var figure = DrawnSeriesFixtures.Build(type, everyPropertySet: true);
        object? written = DrawnSeriesFixtures.ValueOf(DrawnSeriesFixtures.TheSeries(figure), property);

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        object? readBack = DrawnSeriesFixtures.ValueOf(DrawnSeriesFixtures.TheSeries(read), property);
        Assert.True(DrawnSeriesFixtures.SameValue(written, readBack),
            $"{type}.{property}: set to {DrawnSeriesFixtures.Show(written)}, read back through the spec reader as {DrawnSeriesFixtures.Show(readBack)}.");
    }

    [Theory]
    [InlineData("""{"type":"line","xData":[1,2],"yData":[1,2],"marker":"blob"}""", "blob", "Diamond")]
    [InlineData("""{"type":"line","xData":[1,2],"yData":[1,2],"drawStyle":"zigzag"}""", "zigzag", "StepsPost")]
    [InlineData("""{"type":"bar","categories":["a","b"],"values":[1,2],"align":"middle"}""", "middle", "Edge")]
    [InlineData("""{"type":"bar","categories":["a","b"],"values":[1,2],"hatch":"zigzag"}""", "zigzag", "DiagonalCross")]
    [InlineData("""{"type":"stackedarea","xData":[1,2],"datasets":[[1,2]],"baseline":"flat"}""", "flat", "Wiggle")]
    [InlineData("""{"type":"violin","datasets":[[1,2,3]],"side":"left"}""", "left", "High")]
    [InlineData("""{"type":"image","heatmapData":[[1,2],[3,4]],"blendMode":"glow"}""", "glow", "Overlay")]
    [InlineData("""{"type":"contourf","xData":[1,2],"yData":[1,2],"heatmapData":[[1,2],[3,4]],"hatches":["Horizontal","zigzag"]}""", "zigzag", "DiagonalCross")]
    [InlineData("""{"type":"treemap","labelFit":"clip"}""", "clip", "Truncate")]
    [InlineData("""{"type":"histogram","data":[1,2,3],"histType":"outline"}""", "outline", "StepFilled")]
    [InlineData("""{"type":"sankey","nodeAlignment":"middle"}""", "middle", "Justify")]
    [InlineData("""{"type":"sankey","linkColorMode":"both"}""", "both", "Gradient")]
    [InlineData("""{"type":"sankey","sankeyOrientation":"diagonal"}""", "diagonal", "Vertical")]
    [InlineData("""{"type":"dendrogram","dendrogramOrientation":"up"}""", "up", "Bottom")]
    [InlineData("""{"type":"stattile","gaugeValue":1,"conditionSeverity":"critcal"}""", "critcal", "Critical")]
    [InlineData("""{"type":"stattile","gaugeValue":1,"conditionVisibility":"muted"}""", "muted", "Shelved")]
    [InlineData("""{"type":"statetimeline","stateSegmentHatches":["Horizontal","zigzag"]}""", "zigzag", "DiagonalCross")]
    [InlineData("""{"type":"statetimeline","stateSegmentSeverities":["Normal","bad"]}""", "bad", "Critical")]
    [InlineData("""{"type":"statetimeline","stateSegmentVisibilities":["Observed","bad"]}""", "bad", "Shelved")]
    public void AMisspelledDrawnEnumValue_IsRefusedWithTheAcceptedValues(string series, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(series))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    [Theory]
    [InlineData("diamond")]
    [InlineData("DIAMOND")]
    [InlineData("Diamond")]
    public void AnAcceptedMarker_ReachesTheLine_InAnyCase(string written)
    {
        var figure = Reader.Read(new ChartSpec(Spec($$"""{"type":"line","xData":[1,2],"yData":[1,2],"marker":"{{written}}","drawStyle":"stepspost"}""")));

        var line = Assert.IsType<LineSeries>(figure.SubPlots[0].Series[0]);
        Assert.Equal(MarkerStyle.Diamond, line.Marker);
        Assert.Equal(DrawStyle.StepsPost, line.DrawStyle);
    }

    /// <summary>An image's blend mode and a filled contour's hatches are accepted in any case, the hatches one by one,
    /// and reach the series spelt as the serializer reads them.</summary>
    [Fact]
    public void AnAcceptedBlendModeAndHatchList_ReachTheSeries_InAnyCase()
    {
        var image = Reader.Read(new ChartSpec(Spec("""{"type":"image","heatmapData":[[1,2],[3,4]],"blendMode":"multiply"}""")));
        var contourf = Reader.Read(new ChartSpec(Spec(
            """{"type":"contourf","xData":[1,2],"yData":[1,2],"heatmapData":[[1,2],[3,4]],"hatches":["horizontal","DIAGONALCROSS"]}""")));

        Assert.Equal(BlendMode.Multiply, Assert.IsType<ImageSeries>(image.SubPlots[0].Series[0]).BlendMode);
        Assert.Equal([HatchPattern.Horizontal, HatchPattern.DiagonalCross], Assert.IsType<ContourfSeries>(contourf.SubPlots[0].Series[0]).Hatches);
    }

    [Fact]
    public void ColourNamesInAPerPointColourList_AreNormalisedToo()
    {
        var figure = Reader.Read(new ChartSpec(Spec(
            """{"type":"scatter","xData":[1,2],"yData":[1,2],"colors":["red","#00f"],"edgeColors":["white","black"]}""")));

        var scatter = Assert.IsType<ScatterSeries>(figure.SubPlots[0].Series[0]);
        Assert.NotNull(scatter.Colors);
        Assert.NotNull(scatter.EdgeColors);
        Assert.Equal([Color.FromHex("#FF0000"), Color.FromHex("#0000FF")], scatter.Colors);
        Assert.Equal([Color.FromHex("#FFFFFF"), Color.FromHex("#000000")], scatter.EdgeColors);
    }

    [Fact]
    public void AnUnknownColourInAPerPointColourList_IsRefusedByPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"type":"scatter","xData":[1,2],"yData":[1,2],"edgeColors":["white","reddish"]}"""))));

        Assert.Contains("'reddish'", refusal.Message);
        Assert.Contains("subPlots[0].series[0].edgeColors[1]", refusal.Message);
    }
}
