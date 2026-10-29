// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>What an axes draws besides its series travels through the spec reader too: the ticks and their labels, the
/// axis settings, the legend, the colour bar, the grid, the spines and the overlays. An agent hands back the JSON a
/// figure writes; a part the reader refused, or let through misspelt to a serializer that skips the miss, would be a
/// chart the agent cannot send back as it was drawn.</summary>
public class SpecAxesPartsTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    /// <summary>A spec with one subplot: the fields of <paramref name="subPlot"/> (a JSON object) and one line.</summary>
    private static string Spec(string subPlot)
    {
        var axes = JsonNode.Parse(subPlot)!.AsObject();
        axes["series"] = JsonNode.Parse("""[{"type":"line","xData":[1,2],"yData":[1,2]}]""");
        return new JsonObject { ["width"] = 400, ["height"] = 300, ["subPlots"] = new JsonArray(axes) }.ToJsonString();
    }

    /// <summary>The rows of the core axes contract, read through the reader instead of the serializer alone.</summary>
    [Theory]
    [MemberData(nameof(DrawnAxesFixtures.Rows), MemberType = typeof(DrawnAxesFixtures))]
    public void ADrawnAxesProperty_PassesTheSpecReader_WithTheValueItWasGiven(string part, string path)
    {
        var figure = DrawnAxesFixtures.Build(everyPropertySet: true);
        object? written = DrawnAxesFixtures.ValueOf(figure, part, path);

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        object? readBack = DrawnAxesFixtures.ValueOf(read, part, path);
        Assert.True(DrawnSeriesFixtures.SameValue(written, readBack),
            $"{part}.{path}: set to {DrawnSeriesFixtures.Show(written)}, read back through the spec reader as {DrawnSeriesFixtures.Show(readBack)}.");
    }

    [Theory]
    [InlineData("""{"legend":{"position":"topRight"}}""", "topRight", "UpperRight")]
    [InlineData("""{"colorBar":{"orientation":"diagonal"}}""", "diagonal", "Horizontal")]
    [InlineData("""{"colorBar":{"extend":"up"}}""", "up", "Both")]
    [InlineData("""{"grid":{"visible":true,"axis":"z"}}""", "z", "Both")]
    [InlineData("""{"grid":{"visible":true,"which":"all"}}""", "all", "Minor")]
    [InlineData("""{"grid":{"visible":true,"lineStyle":"wavy"}}""", "wavy", "Dotted")]
    [InlineData("""{"gridChange":{"axis":"z"}}""", "z", "Both")]
    [InlineData("""{"gridChange":{"which":"all"}}""", "all", "Minor")]
    [InlineData("""{"gridChange":{"lineStyle":"wavy"}}""", "wavy", "Dotted")]
    [InlineData("""{"spines":{"left":{"lineStyle":"wavy"}}}""", "wavy", "Dashed")]
    [InlineData("""{"titleLoc":"middle"}""", "middle", "Center")]
    [InlineData("""{"titleStyle":{"fontWeight":"heavy"}}""", "heavy", "Bold")]
    [InlineData("""{"xAxis":{"majorTicks":{"direction":"sideways"}}}""", "sideways", "InOut")]
    [InlineData("""{"secondaryXAxis":{"minorTicks":{"direction":"sideways"}}}""", "sideways", "InOut")]
    [InlineData("""{"yAxis":{"labelStyle":{"fontSlant":"slanted"}}}""", "slanted", "Oblique")]
    [InlineData("""{"annotations":[{"text":"a","x":1,"y":1,"alignment":"justify"}]}""", "justify", "Right")]
    [InlineData("""{"annotations":[{"text":"a","x":1,"y":1,"coordinates":"figure"}]}""", "figure", "AxesFraction")]
    [InlineData("""{"annotations":[{"text":"a","x":1,"y":1,"arrowStyle":"spear"}]}""", "spear", "Wedge")]
    [InlineData("""{"annotations":[{"text":"a","x":1,"y":1,"font":{"weight":"heavy"}}]}""", "heavy", "Bold")]
    [InlineData("""{"xAxis":{"tickLocator":{"type":"spiral"}}}""", "spiral", "multiple")]
    [InlineData("""{"yAxis":{"tickFormatter":{"type":"roman"}}}""", "roman", "category")]
    public void AMisspelledValueOfAnAxesPart_IsRefusedWithTheAcceptedValues(string subPlot, string wrong, string accepted)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(subPlot))));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains(accepted, refusal.Message);
    }

    /// <summary>A grid change is the properties an agent names over the theme's grid. Read through the spec reader and written
    /// back it is the same change: the properties it named, spelt as the library writes them, and no others.</summary>
    [Fact]
    public void AGridChange_PassesTheSpecReader_AndIsWrittenBackAsTheChangeItWas()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"gridChange":{"visible":true,"alpha":0.4,"lineStyle":"Dashed"}}""")));

        using var written = System.Text.Json.JsonDocument.Parse(figure.ToJson());
        var change = written.RootElement.GetProperty("subPlots")[0].GetProperty("gridChange");

        Assert.True(change.GetProperty("visible").GetBoolean());
        Assert.Equal(0.4, change.GetProperty("alpha").GetDouble());
        Assert.Equal("dashed", change.GetProperty("lineStyle").GetString()!.ToLowerInvariant());
        Assert.False(change.TryGetProperty("color", out _));
        Assert.False(change.TryGetProperty("lineWidth", out _));
    }

    /// <summary>The neutral grey a theme names, and the role of a series that wears it, travel through the spec reader as the
    /// colour and the flag they are, and the chart draws the theme's grey.</summary>
    [Fact]
    public void ANeutralColourAndANeutralRole_PassTheSpecReader_AndDraw()
    {
        const string spec = """
            {"width":400,"height":300,"theme":{"neutralColor":"#112233"},
             "subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]},{"type":"errorbar","xData":[1,2],"yData":[1,2],"yErrorLow":[0,0],"yErrorHigh":[1,1],"wearsNeutral":true}],
             "referenceLines":[{"value":1.5,"orientation":"horizontal"}]}]}
            """;

        var figure = Reader.Read(new ChartSpec(spec));
        string svg = figure.ToSvg();

        Assert.Equal(Color.FromHex("#112233"), figure.Theme.NeutralColor);
        Assert.Contains("stroke=\"#112233\"", svg);
        Assert.Contains("\"wearsNeutral\":true", figure.ToJson());
    }

    /// <summary>A heatmap that says where its calendar begins passes the spec reader, and the table of the chart gives each
    /// cell the date it stands for.</summary>
    [Fact]
    public void AHeatmapWithAFirstCellDate_PassesTheSpecReader_AndItsTableNamesTheDates()
    {
        const string spec = """
            {"width":400,"height":300,"subPlots":[{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4],[5,6],[7,8],[9,10],[11,12],[13,14]],"firstCellDate":"2025-03-03"}]}]}
            """;

        var figure = Reader.Read(new ChartSpec(spec));

        var table = Assert.Single(figure.ToDataTables());
        Assert.Equal(["row", "column", "date", "value"], table.Columns.Select(c => c.Header));
        Assert.Equal(new DateTime(2025, 3, 3), DateTime.FromOADate(table.Rows[0][2].Number));
        Assert.Equal(new DateTime(2025, 3, 10), DateTime.FromOADate(table.Rows[1][2].Number));    // row 0, column 1: a week later
        Assert.Equal(new DateTime(2025, 3, 13), DateTime.FromOADate(table.Rows[7][2].Number));    // row 3, column 1
    }

    [Fact]
    public void AMisspelledFigureColourBarOrientation_IsRefusedWithTheAcceptedValues()
    {
        const string spec = """{"width":400,"height":300,"figureColorBar":{"orientation":"diagonal"},"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}""";

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(spec)));

        Assert.Contains("'diagonal'", refusal.Message);
        Assert.Contains("Horizontal", refusal.Message);
    }

    [Fact]
    public void AMisspelledNormaliserKind_IsRefusedWithTheAcceptedKinds()
    {
        const string spec = """{"width":400,"height":300,"subPlots":[{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"normalizer":{"type":"cubic"}}]}]}""";

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(spec)));

        Assert.Contains("'cubic'", refusal.Message);
        Assert.Contains("twoSlope", refusal.Message);
    }

    [Fact]
    public void ALocatorWithoutAKind_IsRefusedWithTheAcceptedKinds()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec("""{"xAxis":{"tickLocator":{"base":2}}}"""))));

        Assert.Contains("tickLocator", refusal.Message);
        Assert.Contains("multiple", refusal.Message);
    }

    /// <summary>An agent writes the fields of an object in any order; the kind is found wherever it stands, and spelt in
    /// any case.</summary>
    [Fact]
    public void AKindWrittenAfterItsArguments_AndInAnyCase_IsTakenAsWritten()
    {
        var figure = Reader.Read(new ChartSpec(Spec(
            """{"xAxis":{"tickLocator":{"base":2,"offset":0.5,"type":"Multiple"},"tickFormatter":{"labels":["a","b"],"reversed":true,"type":"CATEGORY"}}}""")));

        var axis = figure.SubPlots[0].XAxis;
        Assert.True(DrawnSeriesFixtures.SameValue(new MultipleLocator(2, 0.5), axis.TickLocator));
        Assert.True(DrawnSeriesFixtures.SameValue(new CategoryFormatter(["a", "b"], reversed: true), axis.TickFormatter));
    }

    /// <summary>Names at positions of their own (the columns of a Marimekko chart) pass the reader, and names whose positions
    /// do not pair up with them are refused with the field named.</summary>
    [Fact]
    public void ACategoryFormatterAtPositions_IsTakenAsWritten_AndPositionsThatDoNotPairUpAreRefused()
    {
        var figure = Reader.Read(new ChartSpec(Spec(
            """{"xAxis":{"tickFormatter":{"type":"category","labels":["a","b"],"positions":[12.5,62.5]}}}""")));

        Assert.True(DrawnSeriesFixtures.SameValue(new CategoryFormatter(["a", "b"], [12.5, 62.5]), figure.SubPlots[0].XAxis.TickFormatter));
        Assert.Equal("b", figure.SubPlots[0].XAxis.TickFormatter!.Format(62.5));

        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"xAxis":{"tickFormatter":{"type":"category","labels":["a","b"],"positions":[12.5]}}}"""))));
        Assert.Contains("positions", refusal.Message);
    }

    private static readonly double[] Three = [1, 2, 3];
    private static readonly double[] Two = [1, 2];
    private static readonly double[] Flat = [0, 0, 1];
    private static readonly double[,] TwoByTwo = { { 1, 2 }, { 3, 4 } };

    /// <summary>One series of every chart type this server lists that maps its values onto a colour map, by the type name
    /// a spec writes it with.</summary>
    private static readonly IReadOnlyDictionary<string, Func<ISeries>> NormalizableSeries = new Dictionary<string, Func<ISeries>>(StringComparer.Ordinal)
    {
        ["scatter"] = () => new ScatterSeries(Three, Three),
        ["heatmap"] = () => new HeatmapSeries(TwoByTwo),
        ["clustermap"] = () => new ClustermapSeries(TwoByTwo),
        ["contourf"] = () => new ContourfSeries(Two, Two, TwoByTwo),
        ["hexbin"] = () => new HexbinSeries(Three, Three),
        ["histogram2d"] = () => new Histogram2DSeries(Three, Flat),
        ["image"] = () => new ImageSeries(TwoByTwo),
        ["pcolormesh"] = () => new PcolormeshSeries(Three, Three, TwoByTwo),
        ["spectrogram"] = () => new SpectrogramSeries(new double[] { 1.0, 0.5, -0.5, -1.0, 0.0, 0.5, 1.0, 0.5 }),
        ["tricontour"] = () => new TricontourSeries(Three, Flat, Three),
        ["tripcolor"] = () => new TripcolorSeries(Three, Flat, Three),
        ["polarheatmap"] = () => new PolarHeatmapSeries(TwoByTwo, thetaBins: 2, rBins: 2),
        ["scatter3d"] = () => new Scatter3DSeries(Three, Three, Three),
        ["surface"] = () => new SurfaceSeries(Two, Two, TwoByTwo),
        ["trisurf"] = () => new Trisurf3DSeries(Three, Flat, Three),
    };

    /// <summary>The chart types of <see cref="NormalizableSeries"/>.</summary>
    public static TheoryData<string> NormalizableTypes { get; } = new(NormalizableSeries.Keys);

    /// <summary>A normaliser on any chart type the server lists is a field the reader lets through and the serializer
    /// reads: an agent that hands back the JSON of a contour, a hexbin or a 3-D scatter gets the same colouring.</summary>
    [Theory]
    [MemberData(nameof(NormalizableTypes))]
    public void ANormaliser_OnAListedChartTypeThatTakesOne_PassesTheSpecReader(string type)
    {
        var series = NormalizableSeries[type]();
        ((INormalizable)series).Normalizer = new TwoSlopeNormalizer(2.5);
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();

        var read = Reader.Read(new ChartSpec(figure.ToJson()));

        var readSeries = read.SubPlots[0].Series.Single();
        Assert.IsType(series.GetType(), readSeries);
        Assert.True(new ChartTypeCatalog().IsListed(type), $"'{type}' is not a chart type this server lists.");
        Assert.True(DrawnSeriesFixtures.SameValue(new TwoSlopeNormalizer(2.5), ((INormalizable)readSeries).Normalizer),
            $"{type}: the normaliser read back as {DrawnSeriesFixtures.Show(((INormalizable)readSeries).Normalizer)}.");
    }

    [Fact]
    public void ANormaliserWrittenAfterItsArguments_IsTakenAsWritten()
    {
        var figure = Reader.Read(new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"heatmap","heatmapData":[[1,2],[3,4]],"normalizer":{"center":2.5,"type":"twoSlope"}}]}]}"""));

        var heatmap = Assert.IsType<HeatmapSeries>(figure.SubPlots[0].Series[0]);
        Assert.True(DrawnSeriesFixtures.SameValue(new TwoSlopeNormalizer(2.5), heatmap.Normalizer));
    }

    [Fact]
    public void AnAcceptedLegendPosition_ReachesTheLegend_InAnyCase()
    {
        var figure = Reader.Read(new ChartSpec(Spec("""{"legend":{"position":"outsideright"}}""")));

        Assert.Equal(LegendPosition.OutsideRight, figure.SubPlots[0].Legend.Position);
    }

    /// <summary>A series on the top axis is checked the way a series on the other axes is.</summary>
    [Fact]
    public void AMisspelledMarkerOfATopAxisSeries_IsRefusedWithTheAcceptedValues()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(Spec(
            """{"secondaryXAxis":{},"xSecondarySeries":[{"type":"line","xData":[1,2],"yData":[1,2],"marker":"blob"}]}"""))));

        Assert.Contains("'blob'", refusal.Message);
        Assert.Contains("Diamond", refusal.Message);
    }
}
