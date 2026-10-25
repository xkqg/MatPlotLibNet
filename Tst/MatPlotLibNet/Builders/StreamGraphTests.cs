// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The stream graph in one call: one area series for each layer, filled between its lower and its upper edge, the
/// edges worked out by the very code <c>StackPlot</c> stacks with (so a layer's thickness is its value and the floating
/// baselines are matplotlib's), one colour and one legend entry for each layer, a Y axis without ticks (it shows thickness,
/// not a value from zero), and a missing value that leaves a gap. The verb sets no axis limits.
/// <para>A plain <c>StackPlot</c> is one stacked-area series and fails a stream graph four ways, which the tests below hold
/// the stream graph to: its layers' colours (one series, one slot), its layers' names in the legend, the MCP reader (run by
/// the Mcp project over the same shapes), and the JSON round trip.</para></summary>
public class StreamGraphTests
{
    private static readonly double[] X = [0, 1, 2, 3];

    // Three layers of different heights over four positions, the data BaselineHelperTests worked out by hand against
    // matplotlib's stackplot.py: totals 6 10 12 10.
    private static readonly BarGroup[] Layers =
    [
        new("a", [1, 2, 2, 1]),
        new("b", [1, 3, 2, 2]),
        new("c", [4, 5, 8, 7]),
    ];

    /// <summary>The lower edge of every layer on each baseline, from BaselineHelperTests (matplotlib's own numbers).</summary>
    private static double[][] LowerEdges(StackedBaseline baseline) => baseline switch
    {
        StackedBaseline.Zero => [[0, 0, 0, 0], [1, 2, 2, 1], [2, 5, 4, 3]],
        StackedBaseline.Symmetric => [[-3, -5, -6, -5], [-2, -3, -4, -4], [-1, 0, -2, -2]],
        StackedBaseline.Wiggle => [[-2, -4, -4, -3], [-1, -2, -2, -2], [0, 1, 0, 0]],
        _ => [[-3, -4.55, -6.3, -5.6], [-2, -2.55, -4.3, -4.6], [-1, 0.45, -2.3, -2.6]],
    };

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Stream(IReadOnlyList<BarGroup> layers, StackedBaseline baseline = StackedBaseline.Wiggle,
        Action<AreaSeries>? configure = null) =>
        AxesOf(ax => ax.StreamGraph(X, layers, baseline, configure));

    private static AreaSeries[] Areas(Axes axes) => [.. axes.Series.Cast<AreaSeries>()];

    private static void AssertClose(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) < 1e-12, $"point {i}: expected {expected[i]}, got {actual[i]}");
        }
    }

    // ---- reading the picture ---------------------------------------------------------------------------------------

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private readonly record struct FigureText(string Text, string Anchor);

    private readonly record struct Area(double X, double Y, double W, double H);

    private static string Svg(Action<AxesBuilder> configure, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(700, 500);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder.AddSubPlot(1, 1, 1, configure).Build().ToSvg();
    }

    private static IEnumerable<XElement> Named(XContainer root, string name) =>
        root.Descendants().Where(e => e.Name.LocalName == name);

    private static List<XElement> SeriesGroups(string svg) =>
        [.. Named(XDocument.Parse(svg), "g").Where(g => (string?)g.Attribute("class") == "series")];

    private static List<FigureText> FigureTexts(string svg) =>
        [.. XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "text")
            .Select(e => new FigureText(e.Value, (string)e.Attribute("text-anchor")!))];

    private static List<(double X1, double Y1, double X2, double Y2)> FigureLines(string svg) =>
        [.. XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "line")
            .Select(e => (Num((string)e.Attribute("x1")!), Num((string)e.Attribute("y1")!),
                Num((string)e.Attribute("x2")!), Num((string)e.Attribute("y2")!)))];

    private static Area PlotArea(string svg)
    {
        var rect = XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "rect").Skip(1).First();
        return new Area(Num((string)rect.Attribute("x")!), Num((string)rect.Attribute("y")!),
            Num((string)rect.Attribute("width")!), Num((string)rect.Attribute("height")!));
    }

    // ---- one area series per layer ---------------------------------------------------------------------------------

    /// <summary>The whole chart is one area series for each layer in the order given (the first lowest), each named by its
    /// layer: nothing on a second axis, no overlay, no line or scatter series.</summary>
    [Fact]
    public void TheChart_IsOneAreaSeriesPerLayer_InOrder_EachNamedByItsLayer()
    {
        var axes = Stream(Layers);

        var areas = Areas(axes);
        Assert.Equal(3, areas.Length);
        Assert.Equal(["a", "b", "c"], areas.Select(a => a.Label));
        Assert.Empty(axes.SecondarySeries);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Empty(axes.ReferenceLines);
    }

    /// <summary>Every area holds an x array of its own, so editing one layer's data cannot move another's, and none of them is
    /// the array the caller handed over.</summary>
    [Fact]
    public void EveryLayer_HoldsAnXArrayOfItsOwn()
    {
        var areas = Areas(Stream(Layers));

        Assert.All(areas, area => Assert.Equal(X, area.XData));
        Assert.All(areas, area => Assert.NotSame(X, area.XData));
        Assert.NotSame(areas[0].XData, areas[1].XData);
        Assert.NotSame(areas[1].XData, areas[2].XData);
    }

    // ---- the edges: the baselines are matplotlib's, every layer stands on the one below it -------------------------------

    /// <summary>On every baseline the lower edge of the first layer is the baseline, matplotlib's own numbers (worked out by
    /// hand in BaselineHelperTests); each layer rises by its values, so its upper edge is its lower edge plus its thickness;
    /// and each layer stands exactly on the one below, so the layers touch with no gap and no overlap.</summary>
    [Theory]
    [InlineData(StackedBaseline.Zero)]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void TheLayers_StandOnTheBaseline_EachOnTheOneBelow_AsThickAsTheirValues(StackedBaseline baseline)
    {
        var areas = Areas(Stream(Layers, baseline));

        double[][] lower = LowerEdges(baseline);
        for (int k = 0; k < Layers.Length; k++)
        {
            AssertClose(lower[k], areas[k].YData2!);
            AssertClose([.. lower[k].Select((edge, j) => edge + Layers[k].Values[j])], areas[k].YData);
        }

        AssertClose(areas[0].YData, areas[1].YData2!);
        AssertClose(areas[1].YData, areas[2].YData2!);
    }

    /// <summary>The default baseline is the wiggle (matplotlib's): the call without one draws the layers the wiggle draws, and
    /// not the symmetric ones, which differ on layers of unequal height.</summary>
    [Fact]
    public void TheDefaultBaseline_IsTheWiggle()
    {
        var byDefault = Areas(AxesOf(ax => ax.StreamGraph(X, Layers)));
        var wiggle = Areas(Stream(Layers, StackedBaseline.Wiggle));
        var symmetric = Areas(Stream(Layers, StackedBaseline.Symmetric));

        for (int k = 0; k < Layers.Length; k++)
        {
            Assert.Equal(wiggle[k].YData2, byDefault[k].YData2);
            Assert.Equal(wiggle[k].YData, byDefault[k].YData);
        }

        Assert.NotEqual(symmetric[0].YData2, byDefault[0].YData2);
    }

    /// <summary>No second formula: on every baseline the edges are exactly (bit for bit) the baselines and the top edges the
    /// stacked-area series of <c>StackPlot</c> works out for the same data.</summary>
    [Theory]
    [InlineData(StackedBaseline.Zero)]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void TheEdges_AreExactlyTheOnesStackPlotStacksBy(StackedBaseline baseline)
    {
        double[][] ySets = [.. Layers.Select(layer => layer.Values)];
        var stack = AxesOf(ax => ax.StackPlot(X, ySets, s => s.Baseline = baseline)).Series.OfType<StackedAreaSeries>().Single();
        double[][] baselines = stack.Baseline.ComputeFor(stack.YSets, stack.X.Length);
        double[][] tops = stack.LayerTops(baselines);

        var areas = Areas(Stream(Layers, baseline));

        for (int k = 0; k < Layers.Length; k++)
        {
            Assert.Equal(baselines[k], areas[k].YData2);
            Assert.Equal(tops[k], areas[k].YData);
        }
    }

    /// <summary>The thickness of the whole stream at a position is the total of the values there, whatever the baseline:
    /// the top of the last layer less the bottom of the first.</summary>
    [Theory]
    [InlineData(StackedBaseline.Zero)]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void TheStream_IsAsThickAsTheTotal_WhateverTheBaseline(StackedBaseline baseline)
    {
        var areas = Areas(Stream(Layers, baseline));

        AssertClose([6, 10, 12, 10], [.. areas[^1].YData.Select((top, j) => top - areas[0].YData2![j])]);
    }

    // ---- missing values ----------------------------------------------------------------------------------------------

    private static readonly BarGroup[] LongLayers =
    [
        new("a", [1, 2, 3, 2, 1, 2, 3]),
        new("b", [2, 2, double.NaN, 3, 3, 2, 2]),
        new("c", [1, 3, 2, 2, 1, 1, 2]),
    ];

    private static readonly double[] LongX = [0, 1, 2, 3, 4, 5, 6];

    /// <summary>On a floating baseline the middle of the stream cannot be placed without a value, so a missing value leaves a
    /// gap at its own position in EVERY layer, both edges, and only there: the other positions are the ones of the same
    /// stream with that value left out of nothing (finite everywhere else).</summary>
    [Theory]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void AMissingValue_OnAFloatingBaseline_LeavesAGapInEveryLayerAtItsPosition(StackedBaseline baseline)
    {
        var areas = Areas(AxesOf(ax => ax.StreamGraph(LongX, LongLayers, baseline)));

        Assert.All(areas, area =>
        {
            for (int j = 0; j < LongX.Length; j++)
            {
                bool gap = j == 2;
                Assert.Equal(gap, double.IsNaN(area.YData[j]));
                Assert.Equal(gap, double.IsNaN(area.YData2![j]));
            }
        });
    }

    /// <summary>The picture of that: each layer is drawn in two runs with nothing across the gap, and no coordinate is NaN.
    /// The point alone between two gaps (a run of one) adds no area of its own to count here: the gap is at position 2 of 7.</summary>
    [Theory]
    [InlineData(StackedBaseline.Symmetric)]
    [InlineData(StackedBaseline.Wiggle)]
    [InlineData(StackedBaseline.WeightedWiggle)]
    public void AMissingValue_OnAFloatingBaseline_BreaksEveryLayerInTwoRuns(StackedBaseline baseline)
    {
        string svg = Svg(ax => ax.StreamGraph(LongX, LongLayers, baseline));

        var groups = SeriesGroups(svg);
        Assert.Equal(3, groups.Count);
        Assert.All(groups, group => Assert.Equal(2, Named(group, "polygon").Count()));
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>On the zero baseline the layers below the missing value stand where they stand: the gap is in that layer and
    /// in the layers stacked on it, and the one under it is drawn whole.</summary>
    [Fact]
    public void AMissingValue_OnTheZeroBaseline_LeavesAGapInThatLayerAndTheOnesAboveOnly()
    {
        string svg = Svg(ax => ax.StreamGraph(LongX, LongLayers, StackedBaseline.Zero));
        var areas = Areas(AxesOf(ax => ax.StreamGraph(LongX, LongLayers, StackedBaseline.Zero)));

        var groups = SeriesGroups(svg);
        Assert.Single(Named(groups[0], "polygon"));
        Assert.Equal(2, Named(groups[1], "polygon").Count());
        Assert.Equal(2, Named(groups[2], "polygon").Count());
        Assert.DoesNotContain(areas[0].YData, double.IsNaN);
        Assert.True(double.IsNaN(areas[1].YData[2]));
        Assert.False(double.IsNaN(areas[1].YData2![2]));
        Assert.True(double.IsNaN(areas[2].YData2![2]));
        Assert.DoesNotContain("NaN", svg);
    }

    // ---- the Y axis shows thickness, not a value from zero ---------------------------------------------------------------

    /// <summary>The Y axis carries no ticks: an empty fixed locator (a built-in one, so it survives JSON) draws no tick mark,
    /// no label and no grid line, because where the stream lies up and down means nothing.</summary>
    [Fact]
    public void TheYAxis_CarriesNoTicks()
    {
        var axes = Stream(Layers);
        string svg = Svg(ax => ax.StreamGraph(X, Layers));

        Assert.IsType<FixedLocator>(axes.YAxis.TickLocator);
        Assert.Empty(axes.YAxis.TickLocator!.Locate(-10, 10));
        Assert.DoesNotContain(FigureTexts(svg), t => t.Anchor == "end");
        var plot = PlotArea(svg);
        Assert.DoesNotContain(FigureLines(svg), l => l.Y1 == l.Y2 && l.X1 < plot.X);
    }

    /// <summary>Under a theme that draws a grid (ggplot) no horizontal grid line stands on a scale that reads nothing.</summary>
    [Fact]
    public void UnderAGridTheme_NoHorizontalGridLine_StandsOnTheYAxis()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers), Theme.Ggplot);

        var plot = PlotArea(svg);
        Assert.DoesNotContain(FigureLines(svg), l => l.Y1 == l.Y2 && l.X1 <= plot.X + 1 && l.X2 >= plot.X + plot.W - 1);
    }

    /// <summary>Asked for, the ticks come back: a locator set after the call replaces the empty one (what the documentation
    /// says to do to read thicknesses against a scale).</summary>
    [Fact]
    public void TheTicks_CanBePutBack_ByALocatorSetAfterTheCall()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers).SetYTickLocator(new AutoLocator()));

        Assert.Contains(FigureTexts(svg), t => t.Anchor == "end");
    }

    /// <summary>The empty locator replaces a Y tick locator set before the call (the documentation says so): the ticks a caller
    /// wants come from a locator set after the call.</summary>
    [Fact]
    public void ATickLocatorSetBeforeTheCall_IsReplacedByTheEmptyOne()
    {
        var axes = AxesOf(ax => ax.SetYTickLocator(new AutoLocator()).StreamGraph(X, Layers));

        Assert.Empty(axes.YAxis.TickLocator!.Locate(-10, 10));
    }

    /// <summary>The verb sets no axis limit and keeps one the caller set before the call (W9): a limit clips every series on
    /// the axes and overwrites the caller's own.</summary>
    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Stream(Layers);
        var kept = AxesOf(ax => ax.SetXLim(-1, 4).SetYLim(-20, 20).StreamGraph(X, Layers));

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Equal(-1.0, kept.XAxis.Min);
        Assert.Equal(4.0, kept.XAxis.Max);
        Assert.Equal(-20.0, kept.YAxis.Min);
        Assert.Equal(20.0, kept.YAxis.Max);
    }

    // ---- colours and the legend (the two things StackPlot's one series cannot do) ------------------------------------------

    private static List<string> Outlines(string svg) =>
        [.. SeriesGroups(svg).Select(group => (string)Named(group, "polyline").First().Attribute("stroke")!)];

    /// <summary>One drawn unit is one colour: layer i wears the theme's colour at slot i, in order, the area and its outline
    /// alike; no two layers share a colour. (StackPlot is ONE series: the layers there are parts of one slot.)</summary>
    [Fact]
    public void EveryLayer_WearsItsOwnColourOfTheCycle_InOrder()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers));

        Assert.Equal([Colors.Tab10Blue.ToHex(), Colors.Tab10Orange.ToHex(), Colors.Tab10Green.ToHex()], Outlines(svg));
        Assert.All(SeriesGroups(svg), group => Assert.StartsWith(
            (string)Named(group, "polyline").First().Attribute("stroke")!,
            (string)Named(group, "polygon").First().Attribute("fill")!));
    }

    /// <summary>Each layer takes a colour slot of its own, so a series added after the chart wears the colour after the
    /// layers': the fourth of the cycle after three layers. (A plain stacked-area series is one series: a line added after it
    /// takes its second colour, which one of its own layers already wears.)</summary>
    [Fact]
    public void ASeriesAddedAfterTheChart_TakesTheNextFreeColour()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers).Plot([0, 3], [0, 1]));

        // The three layers' outlines and then the line's: a series with no name is drawn outside any series group.
        var strokes = Named(XDocument.Parse(svg), "polyline").Select(p => (string)p.Attribute("stroke")!).ToList();
        Assert.Equal(4, strokes.Count);
        Assert.Equal(Theme.Default.CycleColors[3].ToHex(), strokes[^1]);
    }

    /// <summary>The colours are the figure's theme's, not the default theme's: under ggplot layer i wears ggplot's colour i.</summary>
    [Fact]
    public void TheColours_FollowTheFiguresTheme()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers), Theme.Ggplot);

        Assert.Equal([.. Theme.Ggplot.CycleColors.Take(3).Select(c => c.ToHex())], Outlines(svg));
    }

    /// <summary>The legend lists one entry for each layer, in the order of the layers, named by them; a click on an entry
    /// hides that layer alone. (StackPlot's layer names never reach the legend.)</summary>
    [Fact]
    public void TheLegend_ListsOneEntryPerLayer_InOrder()
    {
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.StreamGraph(X, Layers)).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        var entries = Regex.Matches(svg, """<g data-legend-index="([0-9]+)" [^>]*aria-label="([^"]*)">""");
        Assert.Equal(["0:a", "1:b", "2:c"], entries.Select(e => $"{e.Groups[1].Value}:{e.Groups[2].Value}"));
        var legend = Named(XDocument.Parse(svg), "g").First(g => (string?)g.Attribute("class") == "legend");
        Assert.Equal(["a", "b", "c"], Named(legend, "text").Select(t => t.Value));
    }

    /// <summary>The stream fills the plot, so the legend in the default corner covers part of it: the legend is the caller's to
    /// move, and a legend set outside the plot under a tight layout stands right of the plot area, covering none of the stream
    /// (what the documentation says to do).</summary>
    [Fact]
    public void TheLegend_CanBeMovedBesideThePlot()
    {
        string svg = Plt.Create().WithSize(700, 500).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.StreamGraph(X, Layers).WithLegend(LegendPosition.OutsideRight)).Build().ToSvg();

        var plot = PlotArea(svg);
        var legend = Named(XDocument.Parse(svg), "g").First(g => (string?)g.Attribute("class") == "legend");
        double legendLeft = Num((string)Named(legend, "rect").First().Attribute("x")!);
        Assert.True(legendLeft > plot.X + plot.W, $"the legend starts at {legendLeft}, not right of the plot area ending at {plot.X + plot.W}");
    }

    /// <summary>A layer with no name (null or empty) is drawn and has no legend entry: an entry with no text would name
    /// nothing.</summary>
    [Fact]
    public void ALayerWithoutAName_IsDrawn_WithNoLegendEntry()
    {
        BarGroup[] layers = [new(null!, [1, 2, 3, 4]), new("", [2, 2, 2, 2]), new("named", [3, 2, 1, 2])];
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.StreamGraph(X, layers)).Build();
        figure.EnableLegendToggle = true;

        var areas = Areas(figure.SubPlots[0]);
        var entries = Regex.Matches(figure.ToSvg(), "data-legend-index=");

        Assert.Equal(3, areas.Length);
        Assert.Null(areas[0].Label);
        Assert.Null(areas[1].Label);
        Assert.Equal("named", areas[2].Label);
        Assert.Single(entries);
    }

    // ---- the streams ---------------------------------------------------------------------------------------------------------

    /// <summary>The streams are opaque, as matplotlib's stackplot draws its layers, so a layer wears exactly the colour of its
    /// legend entry (the swatch ignores transparency) and the grid does not show through it. An area is faint (0.3) by
    /// default.</summary>
    [Fact]
    public void TheStreams_AreOpaque()
    {
        Assert.All(Areas(Stream(Layers)), area => Assert.Equal(1.0, area.Alpha));
    }

    // ---- the callback ----------------------------------------------------------------------------------------------------

    /// <summary>The callback runs once for each layer, in the order of the layers, after the verb has built and styled the
    /// area, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOncePerLayer_InOrder_AfterTheVerbStyledIt()
    {
        var seen = new List<string?>();

        var axes = Stream(Layers, configure: area =>
        {
            Assert.Equal(1.0, area.Alpha);
            seen.Add(area.Label);
            area.Alpha = 0.4;
        });

        Assert.Equal(["a", "b", "c"], seen);
        Assert.All(Areas(axes), area => Assert.Equal(0.4, area.Alpha));
    }

    /// <summary>What the callback sets wins over the cycle: a colour set there is the colour drawn.</summary>
    [Fact]
    public void TheCallback_CanSetAColour_AndItIsTheColourDrawn()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers, configure: area => area.Color = Colors.Red));

        Assert.Equal([Colors.Red.ToHex(), Colors.Red.ToHex(), Colors.Red.ToHex()], Outlines(svg));
    }

    // ---- the verb draws what the hand chain draws -------------------------------------------------------------------------

    /// <summary>The verb adds nothing to the picture that the hand-written chain of public building blocks did not draw, and
    /// leaves nothing out: same SVG, byte for byte, with and without a missing value. (The hand recipe stacks on the
    /// symmetric baseline, which is plain arithmetic; the wiggles are held to matplotlib's numbers above.)</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.StreamGraphWith(StackedBaseline.Symmetric, missing).ToSvg();
        string byHand = ComposedChartShapes.StreamGraphByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    // ---- JSON ----------------------------------------------------------------------------------------------------------------

    /// <summary>Written to JSON and read back, the chart draws the same picture on every baseline, with and without a missing
    /// value: the areas with their edges, the colours, the names and the Y axis without ticks all come back. (StackPlot's
    /// stacked-area series loses the stream graph's layer colours and names on the way; N areas keep them.)</summary>
    [Theory]
    [InlineData(StackedBaseline.Zero, false)]
    [InlineData(StackedBaseline.Symmetric, false)]
    [InlineData(StackedBaseline.Wiggle, false)]
    [InlineData(StackedBaseline.WeightedWiggle, false)]
    [InlineData(StackedBaseline.Zero, true)]
    [InlineData(StackedBaseline.Symmetric, true)]
    [InlineData(StackedBaseline.Wiggle, true)]
    [InlineData(StackedBaseline.WeightedWiggle, true)]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture(StackedBaseline baseline, bool missing)
    {
        var figure = ComposedChartShapes.StreamGraphWith(baseline, missing).Build();
        string drawn = figure.ToSvg();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.DoesNotContain(FigureTexts(readBack), t => t.Anchor == "end");
    }

    /// <summary>The same under a dark theme: the colours the layers wear come back as the theme's.</summary>
    [Fact]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture_UnderADarkTheme()
    {
        var figure = Plt.Create().WithSize(700, 500).WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax.StreamGraph(LongX, LongLayers)).Build();
        string drawn = figure.ToSvg();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- refused arguments -----------------------------------------------------------------------------------------------

    /// <summary>An axes, an x array or a list of layers that is not there is refused by name.</summary>
    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).StreamGraph(X, Layers)).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.StreamGraph(null!, Layers))).ParamName);
        Assert.Equal("layers", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.StreamGraph(X, null!))).ParamName);
    }

    /// <summary>No layers draw nothing: refused by name, in words that say what a stream graph needs.</summary>
    [Fact]
    public void NoLayers_AreRefusedByName()
    {
        var error = Assert.Throws<ArgumentException>(() => Stream([]));

        Assert.Equal("layers", error.ParamName);
        Assert.StartsWith("No layers were given", error.Message);
    }

    /// <summary>A layer that does not carry one value per x position is refused, naming the layer, both counts and the
    /// parameter, in the words every verb that takes groups uses; a layer with no values at all is refused by name, not with a
    /// null reference.</summary>
    [Fact]
    public void ALayerThatDoesNotFitTheXPositions_IsRefusedByName()
    {
        var error = Assert.Throws<ArgumentException>(() => Stream([new("a", [1, 2, 3, 4]), new("b", [1, 2])]));

        Assert.Equal("layers", error.ParamName);
        Assert.Equal("Group 'b' carries 2 values; there are 4 x values. (Parameter 'layers')", error.Message);
        Assert.Equal("layers", Assert.Throws<ArgumentException>(() => Stream([new BarGroup("a", null!)])).ParamName);
        Assert.Equal("layers", Assert.Throws<ArgumentException>(() => Stream([default])).ParamName);
    }

    /// <summary>A thickness is finite and 0 or more: a negative value and an infinity are refused, naming the layer, the x
    /// position and the value (a stream of negative thickness is undefined; NaN marks a missing value instead).</summary>
    [Theory]
    [InlineData(-1.0, "-1")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    public void AValueThatIsNotAThickness_IsRefusedByName(double value, string written)
    {
        var error = Assert.Throws<ArgumentException>(() => Stream([new("a", [1, 2, 3, 4]), new("rock", [1, value, 1, 1])]));

        Assert.Equal("layers", error.ParamName);
        Assert.StartsWith(
            $"The value of 'rock' at x = 1 is {written}; a stream's thickness is finite and 0 or more (NaN marks a missing value).", error.Message);
    }

    /// <summary>Zero is a thickness (a layer that is not there yet), and so is a missing value: neither is refused.</summary>
    [Fact]
    public void AZeroAndAMissingValue_AreNotRefused()
    {
        var areas = Areas(Stream([new("a", [0, 0, 0, 0]), new("b", [1, double.NaN, 1, 1])]));

        Assert.Equal(2, areas.Length);
    }

    /// <summary>A baseline that is not one of the four is refused by name.</summary>
    [Fact]
    public void AnUndefinedBaseline_IsRefusedByName()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Stream(Layers, (StackedBaseline)99));

        Assert.Equal("baseline", error.ParamName);
    }

    /// <summary>A refused call has added nothing: the checks come before the first area, so no series and no tick locator is
    /// left behind.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.StreamGraph(X, [new("a", [1, 2, 3, 4]), new("b", [1, -2, 3, 4])]));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Null(axes.YAxis.TickLocator);
    }

    /// <summary>No positions and layers without values draw nothing and do not throw.</summary>
    [Fact]
    public void NoPositions_DrawNothing()
    {
        string svg = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StreamGraph([], [new("a", [])])).ToSvg();

        Assert.DoesNotContain("<polygon", svg);
        Assert.DoesNotContain("NaN", svg);
    }

    // ---- the example in the documentation ---------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        double[] year = [2019, 2020, 2021, 2022, 2023];

        string svg = Plt.Create()
            .WithTitle("Listening by genre")
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .StreamGraph(year,
                [
                    new BarGroup("Rock", [8, 7, 6, 5, 4]),
                    new BarGroup("Pop", [3, 5, 7, 8, 9]),
                    new BarGroup("Jazz", [2, 2, 3, 3, 4]),
                ])
                .SetXTickLocator(new MultipleLocator(1))
                .SetXLabel("Year")
                .WithLegend(LegendPosition.OutsideRight))
            .ToSvg();

        Assert.Contains(">Listening by genre<", svg);
        Assert.Contains(">Jazz<", svg);
        Assert.Contains(">Year<", svg);
    }
}
