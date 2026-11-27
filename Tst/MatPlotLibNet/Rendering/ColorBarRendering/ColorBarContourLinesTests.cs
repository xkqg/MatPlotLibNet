// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>The colour bar of a contour of lines, as matplotlib's. Printed with matplotlib 3.11.2 under the default style for
/// <c>cs = ax.contour(X, Y, Z); cb = fig.colorbar(cs, ax=ax)</c> over <see cref="Surface"/>: <c>cs.levels</c> run from -0.3 to 1.05 in
/// steps of 0.15 (ten levels), <c>cb.get_ticks()</c> are those ten levels, <c>cb.solids</c> is <c>None</c> (a bar of lines has no fill:
/// <c>_draw_all</c> adds solids only <c>if self._filled</c>), and <c>len(cb.lines) == 1</c>: one <c>LineCollection</c> of ten segments
/// <c>[[0, level], [1, level]]</c> across the bar, as wide as the contour's lines (1.5, <c>lines.linewidth</c>) and in their colours
/// (<c>add_lines(cs.levels, cs.to_rgba(cs.cvalues), cs.get_linewidths())</c>: viridis over the levels). The bar's boundaries are the
/// levels with uniform spacing, so the lines stand evenly along it.</summary>
public sealed class ColorBarContourLinesTests
{
    /// <summary>A difference of two bumps over a 41 x 41 grid from -3 to 3, the surface the matplotlib rows were printed for:
    /// <c>Z = exp(-(X² + Y²) / 2) - 0.5 exp(-((X - 1)² + (Y - 1)²))</c>, from -0.2297 to 0.9422.</summary>
    private static (double[] Axis, double[,] Z) Surface()
    {
        const int n = 41;
        var axis = new double[n];
        for (int i = 0; i < n; i++) axis[i] = -3 + i * 6.0 / (n - 1);
        var z = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
            {
                double x = axis[ix], y = axis[iy];
                z[iy, ix] = Math.Exp(-(x * x + y * y) / 2) - 0.5 * Math.Exp(-((x - 1) * (x - 1) + (y - 1) * (y - 1)));
            }
        return (axis, z);
    }

    private static readonly double[] PrintedLevels = [-0.3, -0.15, 0.0, 0.15, 0.3, 0.45, 0.6, 0.75, 0.9, 1.05];

    private static ContourSeries Lines(bool filled = false)
    {
        var (axis, z) = Surface();
        return new ContourSeries(axis, axis, z) { Filled = filled };
    }

    /// <summary>Points on a 9 x 9 lattice over the unit square with <c>z = x + y</c>: the levels run from 0 to 2 in steps of 0.2.</summary>
    private static TricontourSeries Triangles()
    {
        var x = new List<double>();
        var y = new List<double>();
        var z = new List<double>();
        for (int i = 0; i <= 8; i++)
            for (int j = 0; j <= 8; j++)
            {
                x.Add(i / 8.0);
                y.Add(j / 8.0);
                z.Add(i / 8.0 + j / 8.0);
            }
        return new TricontourSeries(new Vec([.. x]), new Vec([.. y]), new Vec([.. z]));
    }

    private static List<CallRecord> BarCalls(Theme theme, ISeries series, ColorBarOrientation orientation = ColorBarOrientation.Vertical)
    {
        var figure = Plt.Create().WithTheme(theme).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(series).WithColorBar(cb => cb with { Orientation = orientation })).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        int start = ctx.Calls.FindIndex(c => c is BeginGroupCall { CssClass: "colorbar" });
        int end = ctx.Calls.FindIndex(start, c => c is EndGroupCall);
        return ctx.Calls.GetRange(start + 1, end - start - 1);
    }

    /// <summary>The lines of the bar that are as wide as the contour's lines: the level lines, not a tick mark.</summary>
    private static List<(Point From, Point To, Color Color)> LevelLines(List<CallRecord> bar, double width) =>
        [.. bar.OfType<DrawLineCall>().Where(k => Math.Abs(k.Thickness - width) < 1e-9)
            .Select(c => (c.P1, c.P2, c.Color))];

    private static int FilledRectangles(List<CallRecord> bar) =>
        bar.Count(c => c is DrawRectangleCall k && k.Fill is not null);

    private static double[] TickValues(List<CallRecord> bar) =>
        [.. bar.OfType<DrawTextCall>().Select(c => c.Text)
            .Where(text => double.TryParse(text.Replace('−', '-'), System.Globalization.CultureInfo.InvariantCulture, out _))
            .Select(text => double.Parse(text.Replace('−', '-'), System.Globalization.CultureInfo.InvariantCulture)).Order()];

    [Fact]
    public void TheLevelsOfTheSurface_AreTheOnesMatplotlibPrinted()
    {
        double[] levels = Lines().LevelsUnder(Theme.MatplotlibV2)!;

        Assert.Equal(PrintedLevels.Length, levels.Length);
        for (int i = 0; i < levels.Length; i++) Assert.Equal(PrintedLevels[i], levels[i], 12);
    }

    /// <summary>A line contour backs a colour bar: the bar covers its first level to its last, as <c>cb.ax.get_ylim()</c> printed
    /// (-0.3, 1.05).</summary>
    [Fact]
    public void ALineContour_BacksAColourBarFromItsFirstLevelToItsLast()
    {
        object series = Lines();

        var provider = Assert.IsAssignableFrom<IColorBarDataProvider>(series);
        var (min, max) = provider.GetColorBarRange();
        Assert.Equal(-0.3, min, 12);
        Assert.Equal(1.05, max, 12);
    }

    /// <summary>Under the default style the bar of a line contour is one line across the bar at every level, in the colour and the
    /// width of that level's line on the plot, evenly spaced from the foot of the bar to its top, and no fill.</summary>
    [Fact]
    public void UnderV2_TheBarOfALineContour_DrawsALineAtEveryLevel_AndNoFill()
    {
        double width = Theme.MatplotlibV2.Looks.Lines.Width!.Value;
        var bar = BarCalls(Theme.MatplotlibV2, Lines());

        var lines = LevelLines(bar, width);
        var frame = bar.Frame().Rect;
        Assert.Equal(10, lines.Count);
        Assert.Equal(0, FilledRectangles(bar));
        var ordered = lines.OrderByDescending(l => l.From.Y).ToList();   // from the foot of the bar up: the lowest level first
        for (int k = 0; k < 10; k++)
        {
            var (from, to, color) = ordered[k];
            Assert.Equal(from.Y, to.Y, 9);
            Assert.Equal(frame.X, from.X, 9);
            Assert.Equal(frame.X + frame.Width, to.X, 9);
            Assert.Equal(frame.Y + frame.Height * (1 - k / 9.0), from.Y, 6);
            Assert.Equal(ColorMaps.Viridis.GetColor(k / 9.0), color);
        }
    }

    [Fact]
    public void UnderV2_TheBarOfALineContour_NamesEveryLevel()
    {
        var ticks = TickValues(BarCalls(Theme.MatplotlibV2, Lines()));

        Assert.Equal(PrintedLevels.Length, ticks.Length);
        for (int i = 0; i < ticks.Length; i++) Assert.Equal(PrintedLevels[i], ticks[i], 9);
    }

    /// <summary>A horizontal bar stands its lines upright, the lowest level at its left end.</summary>
    [Fact]
    public void AHorizontalBarOfALineContour_StandsItsLinesUpright()
    {
        double width = Theme.MatplotlibV2.Looks.Lines.Width!.Value;
        var bar = BarCalls(Theme.MatplotlibV2, Lines(), ColorBarOrientation.Horizontal);

        var lines = LevelLines(bar, width).OrderBy(l => l.From.X).ToList();
        var frame = bar.Frame().Rect;
        Assert.Equal(10, lines.Count);
        Assert.All(lines, l => Assert.Equal(l.From.X, l.To.X, 9));
        Assert.Equal(frame.X, lines[0].From.X, 6);
        Assert.Equal(frame.X + frame.Width, lines[^1].From.X, 6);
        Assert.Equal(ColorMaps.Viridis.GetColor(0.0), lines[0].Color);
    }

    /// <summary>Under the library's own themes a line contour that names no colour map draws its lines in the theme's neutral grey,
    /// and so does its bar: the same lines over the levels, and no gradient of colours the plot does not draw.</summary>
    [Fact]
    public void UnderTheLibrarysOwnTheme_TheBarOfALineContour_DrawsItsGreyLines()
    {
        var bar = BarCalls(Theme.Default, Lines());

        var lines = LevelLines(bar, SeriesSlotRestingWidth);
        Assert.Equal(10, lines.Count);
        Assert.All(lines, l => Assert.Equal(Theme.Default.Neutral, l.Color));
        Assert.Equal(0, FilledRectangles(bar));
        Assert.All(TickValues(bar), tick => Assert.InRange(tick, -0.3 - 1e-9, 1.05 + 1e-9));
    }

    private const double SeriesSlotRestingWidth = 1.5;

    /// <summary>A contour that fills its bands draws the bar of a filled contour: one block per band under the matplotlib themes
    /// (<c>contourf</c>'s bar), and no level line.</summary>
    [Fact]
    public void UnderV2_TheBarOfAFilledContourSeries_IsOneBlockPerBand()
    {
        var bar = BarCalls(Theme.MatplotlibV2, Lines(filled: true));

        var fills = bar.OfType<DrawRectangleCall>().Where(k => k.Fill is not null)
            .Select(c => c.Fill!.Value).Distinct().Count();
        Assert.Equal(9, fills);
        Assert.Empty(LevelLines(bar, Theme.MatplotlibV2.Looks.Lines.Width!.Value));
    }

    /// <summary>The range of a contour's bar: its first level to its last, else the values, else nothing (the bar's own limits).</summary>
    [Fact]
    public void TheRangeOfAContoursBar_IsItsLevels_ElseItsValues_ElseEmpty()
    {
        Assert.Equal(new MinMaxRange(-1, 4), ((double[]?)[-1.0, 0, 4]).BarRangeOver(new Range1D(-9, 9)));
        Assert.Equal(new MinMaxRange(-9, 9), ((double[]?)[2.0]).BarRangeOver(new Range1D(-9, 9)));
        Assert.Equal(new MinMaxRange(-9, 9), ((double[]?)null).BarRangeOver(new Range1D(-9, 9)));
        Assert.Equal(default, ((double[]?)null).BarRangeOver(null));
    }

    /// <summary>A contour's bar is drawn in bands on its levels under the matplotlib themes only, and only with two levels to bound
    /// a band.</summary>
    [Fact]
    public void AContoursBar_IsBandedUnderTheMatplotlibThemes_WithTwoLevelsOrMore()
    {
        double[] levels = [0.0, 1, 2];

        Assert.IsType<ContourBandNormalizer>(((double[]?)levels).BarNormalizerUnder(Theme.MatplotlibClassic, LinearNormalizer.Instance));
        Assert.Null(((double[]?)levels).BarNormalizerUnder(Theme.Default, LinearNormalizer.Instance));
        Assert.Null(((double[]?)[1.0]).BarNormalizerUnder(Theme.MatplotlibV2, LinearNormalizer.Instance));
    }

    /// <summary>A grid with no two values to lay levels over gives its bar no line and no fill.</summary>
    [Fact]
    public void ALineContourWithNoLevels_DrawsAnEmptyBar()
    {
        var flat = new double[3, 3];
        var bar = BarCalls(Theme.MatplotlibV2, new ContourSeries([0.0, 1, 2], [0.0, 1, 2], flat));
        var flatTriangles = BarCalls(Theme.MatplotlibV2, new TricontourSeries(new Vec([0.0, 1, 0]), new Vec([0.0, 0, 1]), new Vec([5.0, 5, 5])));

        Assert.Empty(LevelLines(bar, Theme.MatplotlibV2.Looks.Lines.Width!.Value));
        Assert.Equal(0, FilledRectangles(bar));
        Assert.Empty(LevelLines(flatTriangles, Theme.MatplotlibV2.Looks.Lines.Width!.Value));
        Assert.Equal(0, FilledRectangles(flatTriangles));
    }

    /// <summary>A level outside the bar's range is not drawn (matplotlib's <c>add_lines</c> keeps those within a ten-billionth of
    /// the range), and a bar that runs from high to low stands a level where its value lies from the start.</summary>
    [Fact]
    public void TheStrip_LeavesOutALevelOutsideItsRange_AndPlacesOnAReversedRange()
    {
        var lines = new ColorBarLines([0.0, 0.5, 2.0], [Colors.Red, Colors.Green, Colors.Blue], 1.5);
        var strip = new ColorBarStrip(new Rect(0, 0, 100, 10), Vertical: false, ColorBarExtend.Neither, DrawEdges: false, Steps: 50) { Lines = lines };
        var ctx = new RecordingRenderContext();
        strip.Draw(ctx, ColorMaps.Viridis, new ColorBarScale(1, 0, [], [], [], 0), Colors.Black);

        var drawn = ctx.Of<DrawLineCall>().Select(c => (c.P1, c.Color)).ToList();
        Assert.Equal([(new Point(100, 0), Colors.Red), (new Point(50, 0), Colors.Green)], drawn);
        Assert.Equal(1, ctx.Count<DrawRectangleCall>());   // the frame alone
    }

    /// <summary>A level that is not a number lies on no range, so the bar draws no line for it: the rule of every range of the library
    /// (<c>Range1D.HoldsClose</c>, matplotlib's <c>_interval_contains_close</c>, which <c>add_lines</c> keeps its levels by and which is
    /// false for NaN).</summary>
    [Fact]
    public void TheStrip_DrawsNoLineForALevelThatIsNotANumber()
    {
        var lines = new ColorBarLines([0.0, double.NaN, 0.5], [Colors.Red, Colors.Green, Colors.Blue], 1.5);
        var strip = new ColorBarStrip(new Rect(0, 0, 100, 10), Vertical: false, ColorBarExtend.Neither, DrawEdges: false, Steps: 50) { Lines = lines };
        var ctx = new RecordingRenderContext();
        strip.Draw(ctx, ColorMaps.Viridis, new ColorBarScale(0, 1, [], [], [], 0), Colors.Black);

        var drawn = ctx.Of<DrawLineCall>().Select(c => c.Color).ToList();
        Assert.Equal([Colors.Red, Colors.Blue], drawn);
    }

    /// <summary>A contour on a triangulation backs a bar too (<c>tricontour</c> returns a <c>TriContourSet</c>, a <c>ContourSet</c>):
    /// a line at each of its eleven levels, coloured through its normaliser over the levels, as wide as its lines.</summary>
    [Fact]
    public void UnderV2_TheBarOfATricontour_DrawsALineAtEveryLevel()
    {
        var series = Triangles();
        double[] levels = series.LevelsUnder(Theme.MatplotlibV2)!;
        var bar = BarCalls(Theme.MatplotlibV2, series);

        var lines = LevelLines(bar, Theme.MatplotlibV2.Looks.Lines.Width!.Value).OrderByDescending(l => l.From.Y).ToList();
        Assert.Equal(11, levels.Length);
        Assert.Equal(11, lines.Count);
        Assert.Equal(0, FilledRectangles(bar));
        for (int k = 0; k < 11; k++)
            Assert.Equal(ColorMaps.Viridis.GetColor(LinearNormalizer.Instance.Normalize(levels[k], levels[0], levels[^1])), lines[k].Color);
        var (min, max) = ((IColorBarDataProvider)(object)series).GetColorBarRange();
        Assert.Equal((levels[0], levels[^1]), (min, max));
    }
}
