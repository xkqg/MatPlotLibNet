// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the matplotlib themes an axes hands out the colours of its cycle as matplotlib's <c>Axes</c> does: it keeps two cycles,
/// one for its lines and one for its patches, and a series takes the next colour of the cycle its kind draws from, or of neither.
/// The library's own themes keep one cycle that every series takes the next colour of, in the order it was added.
/// <para>matplotlib 3.11.2, read from <c>axes/_base.py</c> and <c>axes/_axes.py</c> and printed with python: <c>Axes.__clear</c> makes
/// <c>self._get_lines = _process_plot_var_args()</c> and <c>self._get_patches_for_fill = _process_plot_var_args('Polygon')</c>, each
/// with its own <c>_PropCycle</c>. <c>plot</c>, <c>step</c>, <c>errorbar</c>, <c>ecdf</c>, <c>hist</c>, <c>stairs</c>, <c>stackplot</c>
/// (one colour a layer), <c>violinplot</c> and <c>streamplot</c> ask <c>_get_lines.get_next_color()</c>; <c>scatter</c>, <c>bar</c>,
/// <c>barh</c>, <c>fill_between</c>, <c>fill_betweenx</c>, <c>fill</c> and <c>pie</c> (one colour a wedge) ask
/// <c>_get_patches_for_fill.get_next_color()</c>; <c>stem</c>, <c>eventplot</c>, <c>broken_barh</c>, <c>hlines</c>, <c>boxplot</c>,
/// <c>quiver</c>, <c>barbs</c> and every colour-mapped call (<c>imshow</c>, <c>pcolormesh</c>, <c>contour</c>, <c>contourf</c>,
/// <c>hexbin</c>, <c>hist2d</c>, <c>specgram</c>, <c>tricontour</c>, <c>tripcolor</c>) ask neither. In classic mode
/// (<c>_internal.classic_mode</c>) <c>scatter</c> takes <c>'b'</c>, <c>fill_between</c> takes <c>patch.facecolor</c> and
/// <c>violinplot</c> takes <c>'y'</c>, none of them from a cycle. <c>Axes3D</c>: <c>plot</c>, <c>plot_surface</c> without a map and
/// <c>plot_trisurf</c> ask the lines cycle; <c>scatter</c>, <c>bar</c>, <c>bar3d</c> and <c>voxels</c> the patch cycle; <c>stem</c>,
/// <c>quiver</c>, <c>plot_wireframe</c>, <c>contour</c>, <c>text</c> and a mapped surface neither. mpltern 1.0.5's ternary
/// <c>scatter</c> is <c>Axes.scatter</c>. seaborn 0.13.2 asks the cycle of the call it scouts with: <c>kdeplot</c>, <c>regplot</c>,
/// <c>residplot</c>, <c>rugplot</c>, <c>pointplot</c> the lines cycle; <c>countplot</c>, a filled <c>kdeplot</c>, <c>stripplot</c> and
/// <c>swarmplot</c> the patch cycle, once.</para>
/// <para>The advances below are the ones python printed for each call, followed by a <c>plot</c> and a <c>bar</c> on the same axes:
/// the index of the plot's colour in the cycle is how far the call moved the lines cycle, the bar's how far it moved the patch
/// cycle.</para></summary>
public class MatplotlibColourCyclesTests
{
    private static readonly double[] Xs = [0.0, 1, 2, 3];
    private static readonly double[] Ys = [1.0, 2, 1.5, 2.5];
    private static readonly double[] Samples = [0.1, 0.4, 0.5, 0.9, 1.3, 1.6, 2.2];

    /// <summary>A series of the kind named: the roster's instance of the type (<see cref="DrawnSeriesFixtures.Instance"/>), or one of the
    /// set-ups whose call depends on how the instance is made (two stacked layers, a density given its fill or not, a map, colours
    /// given, values to colour by).</summary>
    private static ChartSeries Kind(string kind) => kind switch
    {
        "stackedarea2" => new StackedAreaSeries(Xs, [Ys, Ys]),
        "kde" => new KdeSeries(Samples) { Fill = false },
        "kdefill" => new KdeSeries(Samples) { Fill = true },
        "trisurfmapped" => new Trisurf3DSeries(Xs, Ys, Ys) { ColorMap = ColorMaps.Viridis },
        "donut3given" => new DonutSeries([1.0, 2, 3]) { Colors = [Colors.Red, Colors.Green, Colors.Blue] },
        "scattervalues" => new ScatterSeries(Xs, Ys) { C = [1.0, 2, 3, 4] },
        "scatter3dmapped" => new Scatter3DSeries(Xs, Ys, Ys) { ColorMap = ColorMaps.Viridis },
        _ => (ChartSeries)DrawnSeriesFixtures.Instance(kind),
    };

    /// <summary>The colour slots of <paramref name="kind"/> followed by a line and a bar, under <paramref name="theme"/>: the line's slot
    /// is how far the kind moved the lines cycle, the bar's how far it moved the patch cycle.</summary>
    private static (int Lines, int Patches) Advances(string kind, Theme theme)
    {
        var axes = new Axes();
        axes.AddSeries(Kind(kind));
        axes.AddSeries(new LineSeries(Xs, Ys));
        axes.AddSeries(new BarSeries(Xs, Ys));
        var slots = axes.ColorSlots(theme);
        return (slots[1], slots[2]);
    }

    [Theory]
    // the lines cycle: plot, step, ecdf, errorbar, hist, stackplot (a colour a layer), violinplot (not in classic mode), seaborn's
    // kdeplot, regplot, residplot, rugplot and pointplot, streamplot, Axes3D.plot, plot_trisurf without a map, a polar plot
    [InlineData(nameof(LineSeries), 1, 0, 1, 0)]
    [InlineData(nameof(StepSeries), 1, 0, 1, 0)]
    [InlineData(nameof(EcdfSeries), 1, 0, 1, 0)]
    [InlineData(nameof(ErrorBarSeries), 1, 0, 1, 0)]
    [InlineData(nameof(HistogramSeries), 1, 0, 1, 0)]
    [InlineData("stackedarea2", 2, 0, 2, 0)]
    [InlineData(nameof(ViolinSeries), 1, 0, 0, 0)]
    [InlineData("kde", 1, 0, 1, 0)]
    [InlineData(nameof(RegressionSeries), 1, 0, 1, 0)]
    [InlineData(nameof(ResidualSeries), 1, 0, 1, 0)]
    [InlineData(nameof(RugplotSeries), 1, 0, 1, 0)]
    [InlineData(nameof(PointplotSeries), 1, 0, 1, 0)]
    [InlineData(nameof(StreamplotSeries), 1, 0, 1, 0)]
    [InlineData(nameof(Trisurf3DSeries), 1, 0, 1, 0)]
    [InlineData(nameof(Line3DSeries), 1, 0, 1, 0)]
    [InlineData(nameof(PolarLineSeries), 1, 0, 1, 0)]
    [InlineData(nameof(StreamingLineSeries), 1, 0, 1, 0)]
    // the patch cycle: scatter (not in classic mode), bar, fill_between (not in classic mode), pie (a colour a wedge), seaborn's
    // countplot, filled kdeplot, stripplot and swarmplot (once, whatever the groups), Axes3D.scatter, bar, bar3d and voxels, a polar
    // scatter and bar, mpltern's scatter
    [InlineData(nameof(ScatterSeries), 0, 1, 0, 0)]
    [InlineData(nameof(BubbleSeries), 0, 1, 0, 0)]
    [InlineData(nameof(PolarScatterSeries), 0, 1, 0, 0)]
    [InlineData(nameof(TernaryScatterSeries), 0, 1, 0, 0)]
    [InlineData(nameof(Scatter3DSeries), 0, 1, 0, 0)]
    [InlineData(nameof(StreamingScatterSeries), 0, 1, 0, 0)]
    [InlineData(nameof(StripplotSeries), 0, 1, 0, 0)]
    [InlineData(nameof(SwarmplotSeries), 0, 1, 0, 0)]
    [InlineData("kdefill", 0, 1, 0, 0)]
    [InlineData(nameof(AreaSeries), 0, 1, 0, 0)]
    [InlineData(nameof(BarSeries), 0, 1, 0, 1)]
    [InlineData(nameof(PolarBarSeries), 0, 1, 0, 1)]
    [InlineData(nameof(PieSeries), 0, 3, 0, 3)]
    [InlineData(nameof(DonutSeries), 0, 3, 0, 3)]
    [InlineData(nameof(CountSeries), 0, 1, 0, 1)]
    [InlineData(nameof(PlanarBar3DSeries), 0, 1, 0, 1)]
    [InlineData(nameof(Bar3DSeries), 0, 1, 0, 1)]
    [InlineData(nameof(VoxelSeries), 0, 1, 0, 1)]
    // neither: stem, eventplot, broken_barh, boxplot, quiver, its key and barbs, a mapped surface, a wireframe, Axes3D.stem,
    // quiver, contour and text, and every colour-mapped call
    [InlineData(nameof(StemSeries), 0, 0, 0, 0)]
    [InlineData(nameof(Stem3DSeries), 0, 0, 0, 0)]
    [InlineData(nameof(EventplotSeries), 0, 0, 0, 0)]
    [InlineData(nameof(BrokenBarSeries), 0, 0, 0, 0)]
    [InlineData(nameof(BoxSeries), 0, 0, 0, 0)]
    [InlineData(nameof(QuiverSeries), 0, 0, 0, 0)]
    [InlineData(nameof(QuiverKeySeries), 0, 0, 0, 0)]
    [InlineData(nameof(BarbsSeries), 0, 0, 0, 0)]
    [InlineData(nameof(Quiver3DSeries), 0, 0, 0, 0)]
    [InlineData(nameof(SurfaceSeries), 0, 0, 0, 0)]
    [InlineData("trisurfmapped", 0, 0, 0, 0)]
    [InlineData(nameof(WireframeSeries), 0, 0, 0, 0)]
    [InlineData(nameof(HeatmapSeries), 0, 0, 0, 0)]
    [InlineData(nameof(ImageSeries), 0, 0, 0, 0)]
    [InlineData(nameof(Histogram2DSeries), 0, 0, 0, 0)]
    [InlineData(nameof(HexbinSeries), 0, 0, 0, 0)]
    [InlineData(nameof(ContourSeries), 0, 0, 0, 0)]
    [InlineData(nameof(ContourfSeries), 0, 0, 0, 0)]
    [InlineData(nameof(PcolormeshSeries), 0, 0, 0, 0)]
    [InlineData(nameof(TricontourSeries), 0, 0, 0, 0)]
    [InlineData(nameof(TripcolorSeries), 0, 0, 0, 0)]
    [InlineData(nameof(SpectrogramSeries), 0, 0, 0, 0)]
    [InlineData(nameof(PolarHeatmapSeries), 0, 0, 0, 0)]
    [InlineData(nameof(Contour3DSeries), 0, 0, 0, 0)]
    [InlineData(nameof(Text3DSeries), 0, 0, 0, 0)]
    // given its colours (pie(colors=...)) or coloured by values or a colour map (scatter(c=values)): neither
    [InlineData("donut3given", 0, 0, 0, 0)]
    [InlineData("scattervalues", 0, 0, 0, 0)]
    [InlineData("scatter3dmapped", 0, 0, 0, 0)]
    public void AKind_MovesTheCyclesMatplotlibsCallMoves(string kind, int v2Lines, int v2Patches, int classicLines, int classicPatches)
    {
        Assert.Equal((v2Lines, v2Patches), Advances(kind, Theme.MatplotlibV2));
        Assert.Equal((classicLines, classicPatches), Advances(kind, Theme.MatplotlibClassic));
    }

    // ── R65's repro, as drawn ─────────────────────────────────────────────────────────────────────────────────────────

    private static string Svg(Theme theme, Action<AxesBuilder> axes) =>
        Plt.Create().WithSize(500, 400).WithTheme(theme).AddSubPlot(1, 1, 1, axes).Build().ToSvg();

    [Fact]
    public void V2_AScatterAfterALine_WearsTheFirstColour_AndALineAfterItTheSecond()
    {
        // ax.plot(x, y); ax.scatter(x, y); ax.plot(x, y): the scatter #1f77b4, the second line #ff7f0e.
        var axes = new Axes();
        axes.Plot(Xs, Ys);
        axes.Scatter(Xs, Ys);
        axes.Plot(Xs, Ys);

        Assert.Equal([0, 0, 1], axes.ColorSlots(Theme.MatplotlibV2));
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Plot(Xs, Ys).Scatter(Xs, Ys));
        Assert.Contains("fill=\"#1F77B4\"", svg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#FF7F0E", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void V2_AFillAfterALineAndTwoScatters_WearsThePatchCyclesThirdColour()
    {
        // ax.plot; ax.scatter; ax.scatter; ax.plot; ax.fill_between: the fill #2ca02c (cycle_probe2: 'fill_betweenx after
        // bar+fill+scatter #d62728' is the same rule one step on).
        var axes = new Axes();
        axes.Plot(Xs, Ys);
        axes.Scatter(Xs, Ys);
        axes.Scatter(Xs, Ys);
        axes.Plot(Xs, Ys);
        axes.FillBetween(Xs, Ys);

        Assert.Equal([0, 0, 1, 1, 2], axes.ColorSlots(Theme.MatplotlibV2));
    }

    [Fact]
    public void Classic_AScatterAndAFillLeaveBothCycles_ABarAfterThemTakesTheFirstColour()
    {
        var axes = new Axes();
        axes.Plot(Xs, Ys);
        axes.Scatter(Xs, Ys);
        axes.FillBetween(Xs, Ys);
        axes.AddSeries(new BarSeries(Xs, Ys));
        axes.Plot(Xs, Ys);

        Assert.Equal([0, 0, 0, 0, 1], axes.ColorSlots(Theme.MatplotlibClassic));
    }

    [Fact]
    public void TheLibrarysOwnThemes_KeepOneCycle_InTheOrderTheSeriesWereAdded()
    {
        var axes = new Axes();
        axes.Plot(Xs, Ys);
        axes.Scatter(Xs, Ys);
        axes.AddSeries(new StemSeries(Xs, Ys));
        axes.AddSeries(new BarSeries(Xs, Ys));
        axes.Plot(Xs, Ys);

        Assert.Equal([0, 1, 2, 3, 4], axes.ColorSlots(Theme.Default));
        Assert.Equal([0, 1, 2, 3, 4], axes.ColorSlots(Theme.Dark));
    }

    // ── a series given its colour asks no cycle ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void V2_ASeriesGivenAColour_MovesNoCycle()
    {
        // _PropCycle.get_next_color and getdefaults advance only for a call given no colour (axes/_base.py). Printed (cycles_probe2):
        // ax.plot(color='k'); ax.plot() -> '#1f77b4'; ax.scatter(color='k'); ax.scatter() -> '#1f77b4'.
        var axes = new Axes();
        axes.AddSeries(new LineSeries(Xs, Ys) { Color = Colors.Black });
        axes.AddSeries(new LineSeries(Xs, Ys));
        axes.AddSeries(new ScatterSeries(Xs, Ys) { Color = Colors.Black });
        axes.AddSeries(new ScatterSeries(Xs, Ys));
        axes.AddSeries(new BarSeries(Xs, Ys) { Color = Colors.Black });
        axes.AddSeries(new BarSeries(Xs, Ys));

        Assert.Equal([0, 0, 0, 0, 0, 1], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal([0, 0, 0, 0, 0, 0], axes.ColorSlots(Theme.MatplotlibClassic));
        Assert.Equal([0, 1, 2, 3, 4, 5], axes.ColorSlots(Theme.Default));
    }

    [Fact]
    public void V2_APieOrAScatterGivenItsColours_MovesNoCycle()
    {
        // pie(colors=[...]) cycles the colours it is given; scatter(c=[...]) takes them: neither asks the axes.
        var axes = new Axes();
        axes.AddSeries(new PieSeries([1.0, 2, 3]) { Colors = [Colors.Red, Colors.Green, Colors.Blue] });
        axes.AddSeries(new ScatterSeries(Xs, Ys) { Colors = [Colors.Red, Colors.Green, Colors.Blue, Colors.Black] });
        axes.AddSeries(new BarSeries(Xs, Ys));

        Assert.Equal([0, 0, 0], axes.ColorSlots(Theme.MatplotlibV2));
    }

    [Fact]
    public void V2_AGroupGivenAColour_MovesNoCycle()
    {
        var axes = new Axes();
        axes.InColorGroup(axes.AddSeries(new AreaSeries(Xs, Ys) { Color = Colors.Red }), axes.AddSeries(new LineSeries(Xs, Ys) { Color = Colors.Red }));
        axes.Plot(Xs, Ys);

        Assert.Equal([0, 0, 0], axes.ColorSlots(Theme.MatplotlibV2));
    }

    // ── a verb's part composed as another call: the flagged points of a control chart are plot(x, y, 'o') ─────────────

    [Fact]
    public void V2_AControlChartsFlaggedPoints_WearTheColourAfterItsLine_AndTheJsonKeepsIt()
    {
        double[] day = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        double[] fill = [50.2, 49.8, 50.1, 50.4, 49.9, 50.0, 53.9, 50.3, 49.7, 50.1];
        var figure = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax.ControlChart(day, fill, new ControlLimits(50, 49, 51))).Build();

        Assert.Equal([0, 1], figure.SubPlots[0].ColorSlots(Theme.MatplotlibV2)[..2]);
        string json = figure.ToJson();
        Assert.Matches("\"asksAs\":\\s*\"line\"", json);
        var restored = ChartSerializerDefault.Current.FromJson(json);
        Assert.Equal(figure.ToSvg(), restored.ToSvg());
    }

    [Fact]
    public void V2_AFilledStaircase_AsksTheLinesCycle_AsMatplotlibsStairsDoes()
    {
        // Axes.stairs: _color = self._get_lines.get_next_color(), filled or not (cycles_probe: stairs_fill after a plot is C1).
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(Xs, Ys).Stairs([1.0, 2.0, 1.5], [0.0, 1, 2, 3], baseline: 0).Scatter(Xs, Ys)).Build();

        Assert.Equal([0, 1, 0], figure.SubPlots[0].ColorSlots(Theme.MatplotlibV2));
    }

    [Fact]
    public void V2_ASpyPlotsMarkers_AskNoCycle_AsMatplotlibsSpyDoes()
    {
        // Axes.spy with a marker: ret = mlines.Line2D(x, y, linestyle='None', marker=marker, ...); self.add_line(ret): no cycle asked.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Spy(new double[,] { { 1, 0 }, { 0, 1 } }).Plot(Xs, Ys).Scatter(Xs, Ys)).Build();

        Assert.Equal([0, 0, 0], figure.SubPlots[0].ColorSlots(Theme.MatplotlibV2));
    }

    // ── a colour group takes its slot from the cycle its line asks, as the reference composes it ────────────────────

    [Fact]
    public void V2_AGroupOfAFillAndALine_TakesTheLinesCycle_AndLeavesThePatchCycle()
    {
        // The band verb as matplotlib draws it (generate.py _band): mid, = ax.plot(middle); ax.fill_between(..., color=mid.get_color()).
        var axes = new Axes();
        axes.Plot(Xs, Ys);
        axes.InColorGroup(axes.AddSeries(new AreaSeries(Xs, Ys)), axes.AddSeries(new LineSeries(Xs, Ys)));
        axes.Scatter(Xs, Ys);

        Assert.Equal([0, 1, 1, 0], axes.ColorSlots(Theme.MatplotlibV2));
    }

    [Fact]
    public void V2_AGroupOfPatchesOnly_TakesThePatchCycle()
    {
        var axes = new Axes();
        axes.Plot(Xs, Ys);
        axes.InColorGroup(axes.AddSeries(new QuiverSeries(Xs, Ys, Xs, Ys)), axes.AddSeries(new ScatterSeries(Xs, Ys)));
        axes.Plot(Xs, Ys);

        Assert.Equal([0, 0, 0, 1], axes.ColorSlots(Theme.MatplotlibV2));
    }

    // ── seaborn's strip and swarm plots: the palette from its first colour, whatever came before ─────────────────────

    [Fact]
    public void V2_AStripPlotAfterAScatter_ColoursItsGroupsFromTheFirstColour_AsSeabornsPaletteDoes()
    {
        // ax.scatter(...) twice; ax.plot(...); sns.stripplot(data=df): groups C0, C1, C2; the next scatter C3 (cycles_probe4).
        double[][] groups = [[1.0, 2.0, 3.0], [2.0, 3.0, 4.0, 5.0], [3.0, 4.0, 5.0, 6.0, 7.0]];
        var svg = Svg(Theme.MatplotlibV2, ax => ax.Scatter([9.0], [9.0]).Scatter([9.0], [9.0]).Stripplot(groups).Scatter([9.0], [9.0])).Xml();
        var cycle = Theme.MatplotlibV2.CycleColors;

        int Dots(Color c) => svg.Each("circle").Count(e => (string?)e.Attribute("fill") == c.ToHex());
        Assert.Equal(3 + 1, Dots(cycle[0]));   // the first group and the first scatter
        Assert.Equal(4 + 1, Dots(cycle[1]));   // the second group and the second scatter
        Assert.Equal(5, Dots(cycle[2]));
        Assert.Equal(1, Dots(cycle[3]));       // the scatter after the strip plot: the patch cycle moved once
    }

    // ── a twin axis is an axes of its own, with cycles of its own ─────────────────────────────────────────────────────

    [Fact]
    public void V2_EveryTwinAxis_StartsItsOwnCycles_AsMatplotlibsTwinxAndTwinyDo()
    {
        // ax.plot(...); ax2 = ax.twinx(); ax2.plot(...) -> '#1f77b4'; ax3 = ax.twiny(); ax3.plot(...) -> '#1f77b4', and
        // ax2._get_lines is not ax._get_lines (twin_probe.py, both styles).
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Xs, Ys).Scatter(Xs, Ys)
            .WithAddRightYAxis(right => right.Plot(Xs, Ys).Scatter(Xs, Ys))
            .WithAddRightYAxis(right => right.Scatter(Xs, Ys).Plot(Xs, Ys))
            .WithAddLeftYAxis(left => left.Plot(Xs, Ys))
            .WithSecondaryXAxis(top => top.PlotXSecondary(Xs, Ys))).Build();
        var axes = figure.SubPlots[0];

        Assert.Equal([0, 0, 0, 0, 0, 0, 0, 0], axes.ColorSlots(Theme.MatplotlibV2));
        Assert.Equal([0, 0, 0, 0, 0, 0, 0, 0], axes.ColorSlots(Theme.MatplotlibClassic));
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7], axes.ColorSlots(Theme.Default));
    }

    /// <summary>Every series holds the call a verb composed it as (<see cref="ChartSeries.AsksAs"/>), so the call is one byte: with its
    /// nullable flag it fits beside the series' other small fields instead of taking 8 bytes of its own in every series.</summary>
    [Fact]
    public void TheCallASeriesAsksAs_IsOneByte()
    {
        Assert.Equal(1, System.Runtime.CompilerServices.Unsafe.SizeOf<CycleCall>());
        Assert.Equal(2, System.Runtime.CompilerServices.Unsafe.SizeOf<CycleCall?>());
    }

    [Fact]
    public void V2_ATwinAxisWithNoSeries_StartsNothing_AndASeriesOfAnotherPackageAsksTheLinesCycle()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(Xs, Ys)
            .WithAddRightYAxis(right => right.SetYLabel("empty"))
            .WithAddLeftYAxis(left => left.Plot(Xs, Ys).Plot(Xs, Ys))).Build();
        Assert.Equal([0, 0, 1], figure.SubPlots[0].ColorSlots(Theme.MatplotlibV2));

        var axes = new Axes();
        axes.AddSeries(new ForeignSeries());
        axes.Plot(Xs, Ys);
        axes.Scatter(Xs, Ys);
        Assert.Equal([0, 1, 0], axes.ColorSlots(Theme.MatplotlibV2));
    }

    [Fact]
    public void V2_ALineOnATwinAxis_IsDrawnInTheFirstColour()
    {
        string svg = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Xs, Ys).WithAddRightYAxis(right => right.Plot(Xs, [10.0, 30, 20, 40]))).Build().ToSvg();

        Assert.DoesNotContain("#FF7F0E", svg, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, svg.AllElements().Count(element => string.Equals(element["stroke"], "#1F77B4", StringComparison.OrdinalIgnoreCase) && element["fill"] == "none"));
    }

    // ── the theme carries the rule, and so does its JSON ──────────────────────────────────────────────────────────────

    [Fact]
    public void TheMatplotlibThemesKeepTwoCycles_ClassicInItsMode_TheLibrarysOwnOne_AndACopyAndTheJsonKeepIt()
    {
        Assert.Equal(CycleRule.Matplotlib, Theme.MatplotlibV2.Looks.Cycle.Rule);
        Assert.Equal(CycleRule.MatplotlibClassic, Theme.MatplotlibClassic.Looks.Cycle.Rule);
        Assert.Equal(CycleRule.OneCycle, Theme.Default.Looks.Cycle.Rule);
        Assert.Equal(CycleRule.MatplotlibClassic, Theme.MatplotlibClassic.Cloned().Looks.Cycle.Rule);
        Assert.Equal(CycleRule.Matplotlib, Theme.MatplotlibV2.ThroughDto().Looks.Cycle.Rule);
        Assert.Equal(CycleRule.MatplotlibClassic, Theme.MatplotlibClassic.ThroughDto().Looks.Cycle.Rule);
        Assert.Null(ThemeDto.From(Theme.Default).Cycles);
        Assert.Equal(CycleRule.OneCycle, Theme.Default.ThroughDto().Looks.Cycle.Rule);
    }
}
