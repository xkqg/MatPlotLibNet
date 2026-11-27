// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
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

    // Three layers of different heights over four positions, the data StackedBaselineExtensionsTests worked out by hand against
    // matplotlib's stackplot.py: totals 6 10 12 10.
    private static readonly BarGroup[] Layers =
    [
        new("a", [1, 2, 2, 1]),
        new("b", [1, 3, 2, 2]),
        new("c", [4, 5, 8, 7]),
    ];

    /// <summary>The lower edge of every layer on each baseline, from StackedBaselineExtensionsTests (matplotlib's own numbers).</summary>
    private static double[][] LowerEdges(StackedBaseline baseline) => baseline switch
    {
        StackedBaseline.Zero => [[0, 0, 0, 0], [1, 2, 2, 1], [2, 5, 4, 3]],
        StackedBaseline.Symmetric => [[-3, -5, -6, -5], [-2, -3, -4, -4], [-1, 0, -2, -2]],
        StackedBaseline.Wiggle => [[-2, -4, -4, -3], [-1, -2, -2, -2], [0, 1, 0, 0]],
        _ => [[-3, -4.55, -6.3, -5.6], [-2, -2.55, -4.3, -4.6], [-1, 0.45, -2.3, -2.6]],
    };

    private static Axes Stream(IReadOnlyList<BarGroup> layers, StackedBaseline baseline = StackedBaseline.Wiggle,
        Action<AreaSeries>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.StreamGraph(X, layers, baseline, configure));

    private static AreaSeries[] Areas(Axes axes) => [.. axes.Series.Cast<AreaSeries>()];

    // ---- reading the picture ---------------------------------------------------------------------------------------
    private readonly record struct FigureText(string Text, string Anchor);

    private static string Svg(Action<AxesBuilder> configure, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(700, 500);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder.AddSubPlot(1, 1, 1, configure).Build().ToSvg();
    }

    private static List<FigureText> FigureTexts(string svg) =>
        [.. svg.Direct("text")
            .Select(e => new FigureText(e.Value, (string)e.Attribute("text-anchor")!))];

    private static List<(double X1, double Y1, double X2, double Y2)> FigureLines(string svg) =>
        [.. svg.Direct("line")
            .Select(e => (e.Number("x1"), e.Number("y1"),
                e.Number("x2"), e.Number("y2")))];

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
        Assert.Empty(axes.YAxes.Right);
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
    /// hand in StackedBaselineExtensionsTests); each layer rises by its values, so its upper edge is its lower edge plus its thickness;
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
            lower[k].AssertCloseTo(areas[k].YData2!, 1e-12);
            lower[k].Select((edge, j) => edge + Layers[k].Values[j]).ToList().AssertCloseTo(areas[k].YData, 1e-12);
        }

        areas[0].YData.AssertCloseTo(areas[1].YData2!, 1e-12);
        areas[1].YData.AssertCloseTo(areas[2].YData2!, 1e-12);
    }

    /// <summary>The default baseline is the wiggle (matplotlib's): the call without one draws the layers the wiggle draws, and
    /// not the symmetric ones, which differ on layers of unequal height.</summary>
    [Fact]
    public void TheDefaultBaseline_IsTheWiggle()
    {
        var byDefault = Areas(Plt.Create().OneAxes(ax => ax.StreamGraph(X, Layers)));
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
        var stack = Plt.Create().OneAxes(ax => ax.StackPlot(X, ySets, s => s.Baseline = baseline)).Series.OfType<StackedAreaSeries>().Single();
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

        (new double[] { 6, 10, 12, 10 }).AssertCloseTo([.. areas[^1].YData.Select((top, j) => top - areas[0].YData2![j])], 1e-12);
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
        var areas = Areas(Plt.Create().OneAxes(ax => ax.StreamGraph(LongX, LongLayers, baseline)));

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

        var groups = svg.SeriesGroups();
        Assert.Equal(3, groups.Count);
        Assert.All(groups, group => Assert.Equal(2, group.Each("polygon").Count()));
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>On the zero baseline the layers below the missing value stand where they stand: the gap is in that layer and
    /// in the layers stacked on it, and the one under it is drawn whole.</summary>
    [Fact]
    public void AMissingValue_OnTheZeroBaseline_LeavesAGapInThatLayerAndTheOnesAboveOnly()
    {
        string svg = Svg(ax => ax.StreamGraph(LongX, LongLayers, StackedBaseline.Zero));
        var areas = Areas(Plt.Create().OneAxes(ax => ax.StreamGraph(LongX, LongLayers, StackedBaseline.Zero)));

        var groups = svg.SeriesGroups();
        Assert.Single(groups[0].Each("polygon"));
        Assert.Equal(2, groups[1].Each("polygon").Count());
        Assert.Equal(2, groups[2].Each("polygon").Count());
        areas[0].YData.AssertAllFinite();
        Assert.True(double.IsNaN(areas[1].YData[2]));
        areas[1].YData2![2].AssertFinite();
        Assert.True(double.IsNaN(areas[2].YData2![2]));
        svg.AssertNoNonFiniteNumber();
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
        var plot = svg.PlotArea();
        Assert.DoesNotContain(FigureLines(svg), l => l.Y1 == l.Y2 && l.X1 < plot.X);
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
        var axes = Plt.Create().OneAxes(ax => ax.SetYTickLocator(new AutoLocator()).StreamGraph(X, Layers));

        Assert.Empty(axes.YAxis.TickLocator!.Locate(-10, 10));
    }

    // ---- colours and the legend (the two things StackPlot's one series cannot do) ------------------------------------------

    private static List<string> Outlines(string svg) =>
        [.. svg.SeriesGroups().Select(group => (string)group.Each("polyline").First().Attribute("stroke")!)];

    /// <summary>One drawn unit is one colour: layer i wears the theme's colour at slot i, in order, the area and its outline
    /// alike; no two layers share a colour. (StackPlot is one series: its layers take one slot each, from the first slot of the series on.)</summary>
    [Fact]
    public void EveryLayer_WearsItsOwnColourOfTheCycle_InOrder()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers));

        Assert.Equal([Colors.Tab10Blue.ToHex(), Colors.Tab10Orange.ToHex(), Colors.Tab10Green.ToHex()], Outlines(svg));
        Assert.All(svg.SeriesGroups(), group => Assert.StartsWith(
            (string)group.Each("polyline").First().Attribute("stroke")!,
            (string)group.Each("polygon").First().Attribute("fill")!));
    }

    /// <summary>Each layer takes a colour slot of its own, so a series added after the chart wears the colour after the
    /// layers': the fourth of the cycle after three layers. (A StackPlot holds one slot for each of its layers too, so a line
    /// added after it wears the colour after them as well.)</summary>
    [Fact]
    public void ASeriesAddedAfterTheChart_TakesTheNextFreeColour()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers).Plot([0, 3], [0, 1]));

        // The three layers' outlines and then the line's: a series with no name is drawn outside any series group.
        var strokes = svg.Xml().Each("polyline").Select(p => (string)p.Attribute("stroke")!).ToList();
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
    /// hides that layer alone. (A StackPlot lists its named layers too, but a click on one of its entries hides the whole stack.)</summary>
    [Fact]
    public void TheLegend_ListsOneEntryPerLayer_InOrder()
    {
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.StreamGraph(X, Layers)).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Equal(["0:a", "1:b", "2:c"], svg.LegendEntries().Select(entry => $"{entry.Index}:{entry.Label}"));
        var legend = svg.Xml().Each("g").First(g => (string?)g.Attribute("class") == "legend");
        Assert.Equal(["a", "b", "c"], legend.Each("text").Select(t => t.Value));
    }

    /// <summary>The stream fills the plot, so the legend, which stands where it covers the fewest marks by default, covers part of it: the legend is the caller's to
    /// move, and a legend set outside the plot under a tight layout stands right of the plot area, covering none of the stream
    /// (what the documentation says to do).</summary>
    [Fact]
    public void TheLegend_CanBeMovedBesideThePlot()
    {
        string svg = Plt.Create().WithSize(700, 500).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.StreamGraph(X, Layers).WithLegend(LegendPosition.OutsideRight)).Build().ToSvg();

        var plot = svg.PlotArea();
        var legend = svg.Xml().Each("g").First(g => (string?)g.Attribute("class") == "legend");
        double legendLeft = legend.Each("rect").First().Number("x");
        Assert.True(legendLeft > plot.X + plot.Width, $"the legend starts at {legendLeft}, not right of the plot area ending at {plot.X + plot.Width}");
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
        int entries = figure.ToSvg().CountOf("data-legend-index=");

        Assert.Equal(3, areas.Length);
        Assert.Null(areas[0].Label);
        Assert.Null(areas[1].Label);
        Assert.Equal("named", areas[2].Label);
        Assert.Equal(1, entries);
    }

    // ---- the callback ----------------------------------------------------------------------------------------------------

    /// <summary>What the callback sets wins over the cycle: a colour set there is the colour drawn.</summary>
    [Fact]
    public void TheCallback_CanSetAColour_AndItIsTheColourDrawn()
    {
        string svg = Svg(ax => ax.StreamGraph(X, Layers, configure: area => area.Color = Colors.Red));

        Assert.Equal([Colors.Red.ToHex(), Colors.Red.ToHex(), Colors.Red.ToHex()], Outlines(svg));
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

        string readBack = figure.RoundTripped().ToSvg();

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

        string readBack = figure.RoundTripped().ToSvg();

        Assert.Equal(drawn, readBack);
    }

    // ---- refused arguments -----------------------------------------------------------------------------------------------

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
        Assert.Equal(2.RefusalAgainst(4, "layers", "group 'b'"), error.Message);
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
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Stream([new("a", [1, 2, 3, 4]), new("rock", [1, value, 1, 1])]));

        Assert.Equal("layers", error.ParamName);
        Assert.StartsWith("The value of 'rock' at x = 1 must be ", error.Message, StringComparison.Ordinal);
        Assert.Contains($"(got {written}).", error.Message, StringComparison.Ordinal);
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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  No positions and layers without values draw nothing and do not throw.
        new DrawnToken("NoPositions_DrawNothing", () => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StreamGraph([], [new("a", [])])).ToSvg()) { Absent = ["<polygon"], Finite = true },
        //  The example of the verb's documentation, as written there, compiles and draws.
        new DrawnToken("TheDocumentationExample_Draws", () =>
        {
            double[] year = [2019, 2020, 2021, 2022, 2023];
            return Plt.Create()
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
        }, ">Listening by genre<", ">Jazz<", ">Year<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
