// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using ModelContextProtocol;
using System.Text.Json;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The arms that only a wrong or unusual call reaches: a chart that renders nothing, a path the operating
/// system itself rejects, a chart family whose points are counted differently, an axis that says where it starts.
/// Each of these is something a model can actually produce, and each one has to come back as a sentence.</summary>
[Collection("ChartServicesGlobalState")]
public class ToolEdgeTests : IDisposable
{
    private readonly DirectoryInfo _output = Directory.CreateTempSubdirectory("mcp-edge-tests");

    public void Dispose()
    {
        _output.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private static readonly ChartTypeCatalog Catalog = new();
    private static readonly ChartSpecReader Reader = new(Catalog, RenderLimits.Default);
    private static readonly ChartRendering Rendering = new(Reader, new ChartSummarizer());
    private static readonly ChartTabulation Tabulation = new(Reader, RenderLimits.Default);

    private ChartTools Tools => new(Rendering, Tabulation, Catalog, new ChartSchemaDescription(Catalog), new OutputPathResolver(_output.FullName));

    private static JsonElement Spec(string json) => JsonDocument.Parse(json).RootElement;

    // ---- what the renderer itself can still refuse ------------------------------------------------------------

    [Fact]
    public void AChartTheRendererCannotDraw_IsRefusedDuringRendering()
    {
        // The library refuses what cannot be drawn when the document is read: a probe over every series type it ships
        // (arrays damaged five ways, numbers at zero and below) finds no figure that reads back and then fails while it is
        // drawn. This net catches what no input check can foresee; here the text measurement itself fails.
        var spec = new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2,3],"yData":[1,2,3]}]}]}""");
        Rendering.Render(spec, ChartFormat.Png);   // loads the PNG backend first: its module initializer installs its own text measurement
        var before = FontMetricsDefault.Current;
        FontMetricsDefault.Current = new FailingFontMetrics();
        try
        {
            var refusal = Assert.Throws<ToolRefusalException>(() => Rendering.Render(spec, ChartFormat.Png));

            Assert.Contains("could not be rendered", refusal.Message);
            Assert.Contains("no font here", refusal.Message);
        }
        finally
        {
            FontMetricsDefault.Current = before;
        }
    }

    /// <summary>A 3-D line with one z value for three points used to pass the reader and fail while it was drawn; the library
    /// refuses it while the spec is read, naming the series, so the picture is never attempted.</summary>
    [Fact]
    public void AThreeDLineShortOfZValues_IsRefusedWhenTheSpecIsRead_NamingTheSeries()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Rendering.Render(
            new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line3d","xData":[1,2,3],"yData":[1,2,3],"zData":[1]}]}]}"""),
            ChartFormat.Png));

        Assert.Contains("$.subPlots[0].series[0]", refusal.Message);
        Assert.Contains("Array lengths must match. Got 3 and 1.", refusal.Message);
        Assert.DoesNotContain("could not be rendered", refusal.Message);
    }

    /// <summary>A text measurement that fails whatever it is asked to measure.</summary>
    private sealed class FailingFontMetrics : IFontMetrics
    {
        public Size Measure(string text, Font font) => throw new InvalidOperationException("no font here");
    }

    /// <summary>An empty funnel has nothing to draw: it renders an empty plot, as every other type does with no data (it
    /// used to be refused, because its renderer scaled the stages by Values.Max() over nothing).</summary>
    [Fact]
    public void AnEmptyFunnel_RendersAnEmptyPlot()
    {
        var result = Rendering.Render(
            new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"funnel","pieLabels":[],"values":[]}]}]}"""),
            ChartFormat.Png);

        Assert.NotEmpty(result.Bytes);
        Assert.Equal("funnel", Assert.Single(result.Summary.Series).Discriminator);
    }

    /// <summary>An empty candlestick has nothing to draw and nothing to range over: it renders an empty plot (it used to
    /// be refused, because its range took Low.Min() over nothing).</summary>
    [Fact]
    public void AnEmptyCandlestick_RendersAnEmptyPlot()
    {
        var result = Rendering.Render(
            new ChartSpec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"candlestick","open":[],"high":[],"low":[],"close":[]}]}]}"""),
            ChartFormat.Png);

        Assert.NotEmpty(result.Bytes);
        Assert.Null(Assert.Single(result.Summary.Series).YRange);
    }

    [Theory]
    [InlineData("chart.pdf", ChartFormat.Pdf)]
    [InlineData("chart.svg", ChartFormat.Svg)]
    [InlineData("chart.png", ChartFormat.Png)]
    [InlineData("chart.PNG", ChartFormat.Png)]
    [InlineData("dir.pdf/chart.Svg", ChartFormat.Svg)]
    public void AKnownExtension_NamesItsFormat(string path, ChartFormat format) =>
        Assert.Equal(format, ChartRendering.FormatOfExtension(path));

    /// <summary>The library knows .gif (it refuses it on a figure save), but the server has no animation format, so the
    /// extension names none.</summary>
    [Theory]
    [InlineData("chart.jpg")]
    [InlineData("chart.gif")]
    [InlineData("chart")]
    public void AnExtensionTheServerCannotWrite_NamesNoFormat(string path) =>
        Assert.Null(ChartRendering.FormatOfExtension(path));

    // ---- the boundary that catches what nobody foresaw ---------------------------------------------------------

    [Fact]
    public void APathTheOperatingSystemRejects_StillComesBackAsASentence()
    {
        // Not a refusal the server wrote: the runtime throws on the path itself. It must not escape as a bare
        // exception type, because the SDK would replace it with "An error occurred" and the model learns nothing.
        var error = Assert.Throws<McpException>(() => Tools.SaveChart(
            Spec("""{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2]}]}]}"""),
            "cha\0rt.png",
            ChartFormat.Png));

        Assert.NotEmpty(error.Message);
    }

    // ---- the catalog when there is nothing to exclude -----------------------------------------------------------

    [Fact]
    public void AServerWithNothingToExclude_ListsTheTypesWithoutACaveat()
    {
        var everything = new ChartTypeCatalog(new Dictionary<string, string>());
        var tools = new ChartTools(Rendering, Tabulation, everything, new ChartSchemaDescription(everything), new OutputPathResolver(_output.FullName));

        string listed = tools.ListChartTypes();

        Assert.Contains("sankey", listed);
        Assert.DoesNotContain("Not available", listed);
        Assert.Empty(everything.Excluded);
    }

    [Fact]
    public void AMisspellingFarFromEveryType_GetsNoSuggestion() => Assert.Null(Catalog.Suggest("qqqqqqqqqqqq"));

    // ---- the summary of the chart families that count differently ------------------------------------------------

    [Fact]
    public void APolarSeries_CountsItsRadii()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2, 3], [0.0, 1, 2])).Build();

        Assert.Equal(3, new ChartSummarizer().Describe(figure).Series[0].PointCount);
    }

    [Fact]
    public void ABoxPlot_CountsEveryValueAcrossItsDatasets()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.BoxPlot([[1.0, 2, 3], [4.0, 5]])).Build();

        Assert.Equal(5, new ChartSummarizer().Describe(figure).Series[0].PointCount);
    }

    [Fact]
    public void ACandlestick_CountsItsCloses()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick([1.0, 2], [2.0, 3], [0.0, 1], [1.5, 2.5]))
            .Build();

        Assert.Equal(2, new ChartSummarizer().Describe(figure).Series[0].PointCount);
    }

    [Fact]
    public void ASummaryOfAnUntitledChart_SaysSo() =>
        Assert.Contains("Untitled", new ChartSummarizer().Describe(Plt.Create().Plot([1.0], [1.0]).Build()).ToText());

    [Fact]
    public void TheAxesTheSeriesIsAskedAbout_IsTheOneTheSpecDescribes()
    {
        // Every member of the read-only axes view is something a series may consult while placing itself: a bar
        // asks the bar mode, a log axis changes how a floor is computed, and an explicit limit overrides the data.
        var figure = new ChartSpecReader(Catalog, RenderLimits.Default).Read(new ChartSpec(
            """
            {"width":400,"height":300,"subPlots":[{
              "barMode":"stacked",
              "xAxis":{"min":0,"max":10},
              "yAxis":{"min":1,"max":1000,"scale":"log"},
              "series":[{"type":"bar","categories":["a","b"],"values":[10,100]},
                        {"type":"bar","categories":["a","b"],"values":[20,200]}]}]}
            """));

        var summary = new ChartSummarizer().Describe(figure);

        Assert.Equal(2, summary.Series.Count);
        Assert.All(summary.Series, s => Assert.Equal("bar", s.Discriminator));
    }

    [Fact]
    public void AGanttChart_AsksItsAxesWhereTheRowsStart()
    {
        // Gantt is the series that reads the Y limits off the axes context rather than off its own data, so this
        // is what proves the summary hands a series the axes the SPEC describes and not a default view.
        var figure = new ChartSpecReader(Catalog, RenderLimits.Default).Read(new ChartSpec(
            """
            {"width":400,"height":300,"subPlots":[{
              "yAxis":{"min":-2,"max":7},
              "series":[{"type":"gantt","tasks":["design","build"],"starts":[0,3],"ends":[3,8]}]}]}
            """));

        var series = Assert.Single(new ChartSummarizer().Describe(figure).Series);

        Assert.Equal("gantt", series.Discriminator);
        Assert.Equal(-2, series.YRange!.Value.Min);
        Assert.Equal(7, series.YRange!.Value.Max);
    }

    [Fact]
    public void ASecondarySeries_IsSummarisedBesideThePrimaryOne()
    {
        var figure = new ChartSpecReader(Catalog, RenderLimits.Default).Read(new ChartSpec(
            """
            {"width":400,"height":300,"subPlots":[{
              "series":[{"type":"line","xData":[1,2],"yData":[1,2],"label":"left"}],
              "secondaryYAxis":{"label":"right"},
              "secondarySeries":[{"type":"scatter","xData":[1,2],"yData":[9,8],"label":"right"}]}]}
            """));

        var summary = new ChartSummarizer().Describe(figure);

        Assert.Equal(["left", "right"], summary.Series.Select(s => s.Label));
    }

    // ---- the vocabulary arms a chart type decides ----------------------------------------------------------------

    [Fact]
    public void ABulletGraphOrientation_IsTheLineVocabulary_NotTheBarOne()
    {
        var figure = new ChartSpecReader(Catalog, RenderLimits.Default).Read(new ChartSpec(
            """{"width":400,"height":300,"subPlots":[{"series":[{"type":"bulletgraph","gaugeValue":3,"gaugeMin":0,"gaugeMax":10,"orientation":"Vertical"}]}]}"""));

        Assert.Single(figure.SubPlots[0].Series);
    }

    [Fact]
    public void ASeriesFieldThatIsNotEnumValued_IsLeftAlone()
    {
        // "orientation" means nothing on a line series, so the reader must not invent a rule for it — the
        // serializer will ignore it, and inventing one here would refuse a spec the library accepts.
        var figure = new ChartSpecReader(Catalog, RenderLimits.Default).Read(new ChartSpec(
            """{"width":400,"height":300,"subPlots":[{"series":[{"type":"line","xData":[1,2],"yData":[1,2],"orientation":"whatever"}]}]}"""));

        Assert.Single(figure.SubPlots[0].Series);
    }

    [Fact]
    public void TheSchemaOfAFieldWithoutANote_IsStillListed()
    {
        string schema = new ChartSchemaDescription(Catalog).Describe("line");

        Assert.Contains("xData: number[]  — x values", schema);   // a field with a note
        Assert.Contains("markerSize: number", schema);           // and one without
    }
}
