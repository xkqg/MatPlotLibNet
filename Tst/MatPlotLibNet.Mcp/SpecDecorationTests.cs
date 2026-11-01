// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A chart is more than its series: annotations, reference lines, shaded spans, trend lines, levels, axis
/// breaks and insets all carry enum-valued strings and colours of their own, and the serializer ignores a
/// misspelling in every one of them. The reader walks the whole document, so a typo anywhere is named — and what
/// it normalises still has to survive the serializer, which is what these assert.</summary>
public class SpecDecorationTests
{
    private static readonly ChartSpecReader Reader = new(new ChartTypeCatalog(), RenderLimits.Default);

    private const string Line = """{"type":"line","xData":[1,2,3],"yData":[2,4,3]}""";

    private static Figure Read(string axesExtras) =>
        Reader.Read(new ChartSpec($$"""{"width":400,"height":300,"subPlots":[{"series":[{{Line}}]{{axesExtras}}}]}"""));

    // ---- the decorations the reader walks ---------------------------------------------------------------------

    [Fact]
    public void AnAnnotation_KeepsItsTextAndItsStyles()
    {
        var figure = Read(""","annotations":[{"text":"peak","x":2,"y":4,"connectionStyle":"arc3","boxStyle":"round"}]""");

        var annotation = Assert.Single(figure.SubPlots[0].Annotations);
        Assert.Equal("peak", annotation.Text);
        Assert.Equal(ConnectionStyle.Arc3, annotation.ConnectionStyle);
        Assert.Equal(BoxStyle.Round, annotation.BoxStyle);
    }

    [Fact]
    public void AnAnnotation_KeepsItsVerticalAlignmentAndItsPixelOffset()
    {
        var figure = Read(""","annotations":[{"text":"peak","x":2,"y":4,"verticalAlignment":"center","offsetX":12,"offsetY":-8}]""");

        var annotation = Assert.Single(figure.SubPlots[0].Annotations);
        Assert.Equal(MatPlotLibNet.Rendering.TextVerticalAlignment.Center, annotation.VerticalAlignment);
        Assert.Equal(12, annotation.OffsetX);
        Assert.Equal(-8, annotation.OffsetY);
    }

    [Theory]
    [InlineData("""{"text":"x","x":1,"y":1,"connectionStyle":"arc9"}""", "arc9")]
    [InlineData("""{"text":"x","x":1,"y":1,"boxStyle":"roundish"}""", "roundish")]
    [InlineData("""{"text":"x","x":1,"y":1,"verticalAlignment":"middle"}""", "middle")]
    public void AMisspelledAnnotationStyle_IsRefusedByValue(string annotation, string wrong)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read($""","annotations":[{annotation}]"""));

        Assert.Contains($"'{wrong}'", refusal.Message);
        Assert.Contains("annotations[0]", refusal.Message);
    }

    [Fact]
    public void AReferenceLine_AndASpan_KeepTheirOrientation()
    {
        var figure = Read(""","referenceLines":[{"value":3,"orientation":"vertical","lineStyle":"dashed"}],"spans":[{"min":1,"max":2,"orientation":"vertical","alpha":0.2}]""");

        Assert.Equal(Orientation.Vertical, Assert.Single(figure.SubPlots[0].ReferenceLines).Orientation);
        Assert.Equal(Orientation.Vertical, Assert.Single(figure.SubPlots[0].Spans).Orientation);
    }

    [Theory]
    [InlineData(""","referenceLines":[{"value":1,"orientation":"sideways"}]""", "sideways")]
    [InlineData(""","spans":[{"min":1,"max":2,"orientation":"sideways"}]""", "sideways")]
    [InlineData(""","trendlines":[{"x1":1,"y1":1,"x2":2,"y2":2,"lineStyle":"dotty"}]""", "dotty")]
    [InlineData(""","horizontalLevels":[{"value":2,"lineStyle":"dotty"}]""", "dotty")]
    [InlineData(""","xBreaks":[{"from":1,"to":2,"style":"wiggle"}]""", "wiggle")]
    [InlineData(""","yBreaks":[{"from":1,"to":2,"style":"wiggle"}]""", "wiggle")]
    [InlineData(""","barMode":"piled" """, "piled")]
    public void AMisspelledDecorationValue_IsRefusedByValue(string axesExtras, string wrong)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read(axesExtras));

        Assert.Contains($"'{wrong}'", refusal.Message);
    }

    [Fact]
    public void ATrendline_AndALevel_KeepTheirColours()
    {
        var figure = Read(""","trendlines":[{"x1":1,"y1":1,"x2":3,"y2":3,"color":"crimson"}],"horizontalLevels":[{"value":2,"color":"darkorange"}]""");

        Assert.Equal(Color.FromName("crimson"), Assert.Single(figure.SubPlots[0].Trendlines).Color);
        Assert.Equal(Color.FromName("darkorange"), Assert.Single(figure.SubPlots[0].HorizontalLevels).Color);
    }

    [Fact]
    public void AnAxisBreak_IsKept() =>
        Assert.Single(Read(""","xBreaks":[{"from":1.5,"to":2.5,"style":"straight"}]""").SubPlots[0].XBreaks);

    [Fact]
    public void AnInset_CountsAsSomethingToDraw()
    {
        var figure = Reader.Read(new ChartSpec(
            $$"""{"width":400,"height":300,"subPlots":[{"series":[],"insets":[{"insetBounds":{"x":0.5,"y":0.5,"width":0.3,"height":0.3},"series":[{{Line}}]}]}]}"""));

        Assert.Single(figure.SubPlots[0].Insets);
    }

    [Fact]
    public void AnInsetWithABadSeries_IsRefusedWithItsPath()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(
            """{"width":400,"height":300,"subPlots":[{"series":[],"insets":[{"insetBounds":{"x":0.5,"y":0.5,"width":0.3,"height":0.3},"series":[{"type":"lien"}]}]}]}""")));

        Assert.Contains("insets[0].series[0]", refusal.Message);
    }

    [Fact]
    public void ASeriesWithoutAType_IsRefusedAsSuch()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Read2("""{"xData":[1,2],"yData":[1,2]}"""));

        Assert.Contains("no 'type'", refusal.Message);
    }

    private static Figure Read2(string series) =>
        Reader.Read(new ChartSpec($$"""{"width":400,"height":300,"subPlots":[{"series":[{{series}}]}]}"""));

    // ---- an axes with no series and something of its own ---------------------------------------------------------

    private static readonly Figure EmptyAxes = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, _ => { }).Build();

    private static string SpecOf(string subPlot) => $$"""{"width":400,"height":300,"subPlots":[{{subPlot}}]}""";

    /// <summary>The library draws an axes that holds a span and no series (its spans, patches, reference lines and the like are
    /// drawn whatever the axes holds besides), so a spec that holds only a span is a picture, not "nothing to draw": the reader
    /// takes it and the picture is the one the library draws for the same figure.</summary>
    [Fact]
    public void ASpanOnlySpec_IsDrawn()
    {
        var figure = Reader.Read(new ChartSpec(SpecOf(
            """{"spans":[{"min":1,"max":2,"orientation":"vertical","alpha":0.3,"color":"crimson"}]}""")));
        var library = Plt.Create().WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AxVSpan(1, 2, span => { span.Alpha = 0.3; span.Color = Color.FromName("crimson"); }))
            .Build();

        Assert.Empty(figure.SubPlots[0].Series);
        Assert.Single(figure.SubPlots[0].Spans);
        Assert.Equal(library.ToSvg(), figure.ToSvg());
        Assert.NotEqual(EmptyAxes.ToSvg(), figure.ToSvg());
    }

    /// <summary>Every overlay list of an axes the Cartesian renderer draws counts, not the span alone.</summary>
    [Theory]
    [InlineData("annotations", """{"text":"peak","x":2,"y":4}""")]
    [InlineData("referenceLines", """{"value":3,"orientation":"vertical"}""")]
    [InlineData("spans", """{"min":1,"max":2,"orientation":"horizontal"}""")]
    [InlineData("trendlines", """{"x1":1,"y1":1,"x2":3,"y2":3}""")]
    [InlineData("horizontalLevels", """{"value":2}""")]
    [InlineData("fibonacciRetracements", """{"priceHigh":10,"priceLow":2}""")]
    [InlineData("signals", """{"x":1.5,"y":2,"direction":"Buy"}""")]
    [InlineData("patches", """{"type":"rectangle","anchor":{"dataX":1,"dataY":1},"width":2,"height":1}""")]
    public void AnAxesWithOnlyAnOverlay_IsDrawn(string list, string entry)
    {
        var figure = Reader.Read(new ChartSpec(SpecOf($$"""{"{{list}}":[{{entry}}]}""")));

        Assert.Empty(figure.SubPlots[0].Series);
        Assert.NotEqual(EmptyAxes.ToSvg(), figure.ToSvg());
    }

    /// <summary>The overlay lists of an axes, named: each is either counted as something to draw (the theory above has a row for
    /// it) or left out because it describes what series draw. A list added to the axes turns this red until it is one or the other.</summary>
    [Fact]
    public void EveryOverlayListOfAxes_IsCountedAsSomethingToDrawOrNamedAsNot()
    {
        string[] counted = ["Annotations", "ReferenceLines", "Trendlines", "HorizontalLevels", "FibonacciRetracements", "Spans", "Signals", "Patches"];
        string[] describesWhatSeriesDraw = ["XBreaks", "YBreaks"];   // the markers of a gap in an axis that has data
        string[] holdAxesNotOverlays = ["Insets"];                   // walked in turn: each inset is an axes of its own
        var overlayLists = typeof(Axes).GetProperties()
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                && !typeof(ISeries).IsAssignableFrom(property.PropertyType.GetGenericArguments()[0])
                && property.PropertyType.GetGenericArguments()[0].Name != "SecondaryAxisSlot")
            .Select(property => property.Name);

        Assert.Equal(
            counted.Concat(describesWhatSeriesDraw).Concat(holdAxesNotOverlays).Order(StringComparer.Ordinal),
            overlayLists.Order(StringComparer.Ordinal));
    }

    /// <summary>An inset is an axes of its own: a span on an inset with no series is a picture too.</summary>
    [Fact]
    public void AnInsetWithOnlyAnOverlay_IsDrawn()
    {
        var figure = Reader.Read(new ChartSpec(SpecOf(
            """{"insets":[{"insetBounds":{"x":0.5,"y":0.5,"width":0.3,"height":0.3},"spans":[{"min":1,"max":2}]}]}""")));

        Assert.Single(figure.SubPlots[0].Insets[0].Spans);
    }

    /// <summary>The renderer skips an inset that has no bounds, so a span on one is no picture.</summary>
    [Fact]
    public void AnInsetWithoutBounds_IsNotSomethingToDraw()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(SpecOf(
            """{"insets":[{"spans":[{"min":1,"max":2}]}]}"""))));

        Assert.Contains("Nothing to draw", refusal.Message);
    }

    /// <summary>The polar, ternary and 3-D renderers draw no overlay, so a span on such an axes is no picture: the refusal stands.</summary>
    [Theory]
    [InlineData("Polar")]
    [InlineData("ThreeD")]
    [InlineData("Ternary")]
    public void AnOverlayOnAnAxesThatDrawsNone_IsStillNothingToDraw(string coordinateSystem)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(SpecOf(
            $$"""{"coordinateSystem":"{{coordinateSystem}}","spans":[{"min":1,"max":2}]}"""))));

        Assert.Contains("Nothing to draw", refusal.Message);
    }

    /// <summary>A break marks a gap in an axis that has data, a legend and a colour bar say what series draw: with no series
    /// they are no picture, and the refusal names what would be one.</summary>
    [Theory]
    [InlineData("""{"xBreaks":[{"from":1,"to":2}]}""")]
    [InlineData("""{"legend":{}}""")]
    [InlineData("""{"colorBar":{}}""")]
    [InlineData("""{"spans":[]}""")]
    public void AnAxesWithOnlyWhatDescribesSeries_IsStillNothingToDraw(string subPlot)
    {
        var refusal = Assert.Throws<ToolRefusalException>(() => Reader.Read(new ChartSpec(SpecOf(subPlot))));

        Assert.Contains("Nothing to draw", refusal.Message);
        Assert.Contains("series", refusal.Message);
        Assert.Contains("spans", refusal.Message);
        Assert.Contains("patches", refusal.Message);
    }

    // ---- colours, wherever they sit ---------------------------------------------------------------------------

    [Fact]
    public void ColoursInAList_AreNormalisedToo()
    {
        var figure = Read2("""{"type":"statetimeline","stateSegmentColors":["seagreen","#f00"],"xData":[0,1],"yData":[0,1]}""");

        Assert.NotEmpty(figure.SubPlots[0].Series);
    }

    [Fact]
    public void ABadColourInAList_IsRefusedWithItsIndex()
    {
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Read2("""{"type":"statetimeline","stateSegmentColors":["seagreen","nosuchcolour"],"xData":[0,1],"yData":[0,1]}"""));

        Assert.Contains("stateSegmentColors[1]", refusal.Message);
        Assert.Contains("'nosuchcolour'", refusal.Message);
    }

    [Theory]
    [InlineData("#ABC", "#AABBCC")]
    [InlineData("#abcd", "#AABBCCDD")]
    [InlineData("00FF00", "#00FF00")]
    public void AShortOrUnprefixedHex_IsExpanded(string written, string expected)
    {
        var figure = Read2($$"""{"type":"line","xData":[1,2],"yData":[1,2],"color":"{{written}}"}""");

        Assert.Equal(Color.FromHex(expected), ((Models.Series.LineSeries)figure.SubPlots[0].Series[0]).Color);
    }

    [Theory]
    [InlineData("#12345")]
    [InlineData("#GGHHII")]
    public void AHexThatIsNotOneOfTheAcceptedLengths_IsRefused(string written) =>
        Assert.Throws<ToolRefusalException>(() => Read2($$"""{"type":"line","xData":[1,2],"yData":[1,2],"color":"{{written}}"}"""));

    // ---- what the library itself still refuses ----------------------------------------------------------------

    [Fact]
    public void AFigureTheLibraryRefuses_IsReportedAsARefusal_NotACrash()
    {
        // The reader checks the pairs it knows about; the library has invariants of its own, and what it throws
        // has to reach the model as a message rather than as a stack trace.
        var refusal = Assert.Throws<ToolRefusalException>(() =>
            Read2("""{"type":"candlestick","open":[1,2],"high":[2,3],"low":[0,1],"close":[1]}"""));

        Assert.Contains("refused", refusal.Message);
    }
}
