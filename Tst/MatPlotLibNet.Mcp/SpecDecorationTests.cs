// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
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

    [Theory]
    [InlineData("""{"text":"x","x":1,"y":1,"connectionStyle":"arc9"}""", "arc9")]
    [InlineData("""{"text":"x","x":1,"y":1,"boxStyle":"roundish"}""", "roundish")]
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
