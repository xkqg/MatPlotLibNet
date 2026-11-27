// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A tick mark, a grid line and a bar stand on the pixel grid of a raster export under the matplotlib themes, each by the
/// rule matplotlib's Agg backend puts its kind of artist there (matplotlib 3.11.2, <c>src/_backend_agg.h</c> and
/// <c>src/path_converters.h</c>):
/// <list type="bullet">
/// <item>A grid line is a <c>Line2D</c> and a bar a <c>Rectangle</c>, both drawn by <c>RendererAgg::draw_path</c>, whose
/// <c>PathSnapper</c> moves every vertex of a path of horizontal and vertical segments to
/// <c>floor(v + 0.5) + snap_value</c>, where <c>snap_value = (mpl_round_to_int(stroke_width) % 2) ? 0.5 : 0.0</c>, and
/// <c>snapping_linewidth = 0.0</c> when <c>gc.color.a == 0.0</c> (a bar with no edge).</item>
/// <item>A tick mark is a marker (<c>Tick.tick1line</c> is a <c>Line2D</c> with <c>marker=TICKDOWN</c> and so on, whose
/// <c>_snap_threshold = 1.0</c> forces snapping), drawn by <c>RendererAgg::draw_markers</c>: the point it hangs from is
/// translated by <c>(0.5, height + 0.5)</c> and truncated (<c>x = floor(x); y = floor(y);</c>), and the marker's own path is
/// snapped by the same <c>PathSnapper</c> with the tick's width, so both ends of the mark stand at the rounded point plus
/// <c>floor(offset + 0.5) + snap_value</c>.</item>
/// </list>
/// A mark of a width that rounds to one pixel then fills one column (or row) and spills evenly onto the two beside it; a mark left
/// where it stands straddles two columns and paints them unevenly.</summary>
public class MarkPixelGridTests
{
    private static readonly Theme[] MatplotlibThemes = [Theme.MatplotlibV2, Theme.MatplotlibClassic];

    // ── The two rules, read off a context over a canvas at its natural scale ──────────────────────────────────────────────────

    private static IPixelGridRenderContext Snapping(SKCanvas canvas) => canvas.RasterExportUnder(Theme.MatplotlibV2);

    [Fact]
    public void AMarker_HangsFromItsPointRoundedToAPixel_AndItsStrokeIsSnappedByItsOwnWidth()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);

        // A mark 4.86 long down from (10.3, 20.7), 1.11 wide: the point rounds to (10, 21); the stroke's ends at offsets 0 and 4.86
        // snap to floor(0 + 0.5) + 0.5 and floor(4.86 + 0.5) + 0.5 (1.11 rounds to 1, odd: half a pixel on).
        var mark = Snapping(canvas).MarkerOnPixelGrid(new Point(10.3, 20.7),
            new PixelSegment(new Point(10.3, 20.7), new Point(10.3, 25.56)), 1.11);

        Assert.Equal(new PixelSegment(new Point(10.5, 21.5), new Point(10.5, 26.5)), mark);
    }

    [Fact]
    public void AMarker_OfAWidthThatRoundsEven_StandsOnPixelEdges()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);

        // A mark pointing left 3.4 from (40.6, 12.2), 2.2 wide (rounds to 2, even: no half pixel): (41, 12) and offsets 0, -3.4.
        var mark = Snapping(canvas).MarkerOnPixelGrid(new Point(40.6, 12.2),
            new PixelSegment(new Point(40.6, 12.2), new Point(37.2, 12.2)), 2.2);

        Assert.Equal(new PixelSegment(new Point(41, 12), new Point(38, 12)), mark);
    }

    [Fact]
    public void AMarker_OnACanvasScaledForADpi_IsSnappedInDevicePixels()
    {
        using var bitmap = new SKBitmap(200, 200);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(2f);

        // In device pixels the point is (20.6, 41.4) -> (21, 41), the offsets 0 and 9.72 -> 0.5 and 10.5 (2.22 rounds to 2: even, so
        // no half pixel: 0 and 10). Read back in the figure's units: halved.
        var mark = Snapping(canvas).MarkerOnPixelGrid(new Point(10.3, 20.7),
            new PixelSegment(new Point(10.3, 20.7), new Point(10.3, 25.56)), 1.11);

        Assert.Equal(new PixelSegment(new Point(10.5, 20.5), new Point(10.5, 25.5)), mark);
    }

    [Fact]
    public void AMarkerThatIsNeitherHorizontalNorVertical_OrAContextThatDoesNotSnap_IsNotPlaced()
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        var slanted = new PixelSegment(new Point(10.3, 20.7), new Point(13.3, 25.7));
        var straight = new PixelSegment(new Point(10.3, 20.7), new Point(10.3, 25.7));

        Assert.Null(Snapping(canvas).MarkerOnPixelGrid(new Point(10.3, 20.7), slanted, 1.0));
        Assert.Null(((IPixelGridRenderContext)new SkiaRenderContext(canvas)).MarkerOnPixelGrid(new Point(10.3, 20.7), straight, 1.0));
    }

    [Fact]
    public void OnACanvasThatIsTurned_NeitherAMarkerNorARectangleIsPlaced()
    {
        // A turned canvas has no pixel grid along the figure's axes: the matrix does more than scale and move.
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);
        canvas.RotateDegrees(30);
        var area = new Rect(10.3, 20.7, 40.2, 30.9);

        Assert.Null(Snapping(canvas).MarkerOnPixelGrid(new Point(10.3, 20.7),
            new PixelSegment(new Point(10.3, 20.7), new Point(10.3, 25.7)), 1.0));
        Assert.Equal(area, Snapping(canvas).OnPixelGrid(area, 1.0));
    }

    [Theory]
    [InlineData(1.0, 10.5, 21.5, 51.5, 52.5)]  // an edge that rounds to one pixel: every corner half a pixel on
    [InlineData(2.0, 10, 21, 51, 52)]          // an edge that rounds to two: on the pixel edges
    [InlineData(0.0, 10, 21, 51, 52)]          // no edge (a v2 bar, a matrix cell): on the pixel edges
    public void ARectangle_HasEachCornerSnappedByTheWidthOfItsEdge(double edge, double left, double top, double right, double bottom)
    {
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);

        var snapped = Snapping(canvas).OnPixelGrid(new Rect(10.3, 20.7, 40.2, 30.9), edge);

        Assert.Equal(new Rect(left, top, right - left, bottom - top), snapped);
    }

    // ── Each mark in a figure ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How dark a pixel is against white: 0 for white, 765 for black.</summary>
    private static int Ink(SKColor c) => 765 - c.Red - c.Green - c.Blue;

    /// <summary>The ink of each column between <paramref name="fromX"/> and <paramref name="toX"/>: the darkest pixel of the column over
    /// the rows <paramref name="fromY"/> to <paramref name="toY"/> (a dotted line is dark only on its dots).</summary>
    private static int[] ColumnInk(SKBitmap picture, int fromX, int toX, int fromY, int toY)
    {
        var ink = new int[toX - fromX + 1];
        for (int x = fromX; x <= toX; x++)
            for (int y = fromY; y <= toY; y++)
                ink[x - fromX] = Math.Max(ink[x - fromX], Ink(picture.GetPixel(x, y)));
        return ink;
    }

    /// <summary>Whether the darkest column of <paramref name="ink"/> has the same ink on both sides: a vertical mark of a width that
    /// rounds to one pixel stands on the middle of that column.</summary>
    private static bool CentredOnAColumn(int[] ink)
    {
        int darkest = Array.IndexOf(ink, ink.Max());
        return darkest > 0 && darkest < ink.Length - 1 && Math.Abs(ink[darkest - 1] - ink[darkest + 1]) <= 6;
    }

    private static readonly double[] XTicks = [2, 4, 6, 8];

    /// <summary>A plot over 0 to 9.3 across (ticks at 0, 2, ..., 8, each at a different fraction of a pixel) whose one line runs
    /// above the view, so nothing but the marks asked for stands in the plot: the grid, when asked, on the X ticks only.</summary>
    private static Figure Empty(Theme theme, bool grid) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot([0.0, 9.3], [20.0, 20.0]).SetXLim(0, 9.3).SetYLim(0, 10);
            if (grid) ax.WithGrid(g => g with { Visible = true, Axis = GridAxis.X });
        })
        .Build();

    private static double ColumnOf(Rect plot, double tick) => plot.X + tick / 9.3 * plot.Width;

    [Fact]
    public void AGridLine_UnderTheMatplotlibThemes_StandsOnTheMiddleOfAColumn()
    {
        foreach (var theme in MatplotlibThemes)
        {
            var figure = Empty(theme, grid: true);
            var plot = figure.PlotAreaOnSkia();
            using var picture = SKBitmap.Decode(figure.ToPng());
            int fromY = (int)(plot.Y + plot.Height * 0.3), toY = (int)(plot.Y + plot.Height * 0.7);

            foreach (double tick in XTicks)
            {
                int x = (int)ColumnOf(plot, tick);
                var ink = ColumnInk(picture, x - 3, x + 3, fromY, toY);
                Assert.True(CentredOnAColumn(ink), $"{theme.Name}: the grid line at {tick} (x {ColumnOf(plot, tick):F2}): {string.Join(' ', ink)}");
            }
        }
    }

    [Fact]
    public void ATickMark_UnderTheMatplotlibThemes_StandsOnTheMiddleOfAColumn()
    {
        foreach (var theme in MatplotlibThemes)
        {
            var figure = Empty(theme, grid: false);
            var plot = figure.PlotAreaOnSkia();
            using var picture = SKBitmap.Decode(figure.ToPng());
            double bottom = plot.Y + plot.Height;
            // The v2 marks point out (below the spine), the classic ones in (above it): two rows inside the mark, clear of the spine.
            int fromY = theme == Theme.MatplotlibV2 ? (int)bottom + 2 : (int)bottom - 3;

            foreach (double tick in XTicks)
            {
                int x = (int)ColumnOf(plot, tick);
                Assert.True(CentredOnAColumn(ColumnInk(picture, x - 3, x + 3, fromY, fromY + 1)), $"{theme.Name}: the tick mark at {tick}");
            }
        }
    }

    /// <summary>Five bars over -1 to 5 across, so every side of every bar stands inside the plot under both themes.</summary>
    private static Figure Bars(Theme theme, Action<BarSeries>? edge = null) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b", "c", "d", "e"], [3.0, 5.0, 4.0, 6.0, 2.0], edge).SetXLim(-1, 5))
        .Build();

    [Fact]
    public void ABarWithNoEdge_UnderTheMatplotlibThemes_FillsWholePixels_WithNoBlendAtItsSides()
    {
        foreach (var theme in MatplotlibThemes)
        {
            var figure = Bars(theme, bar => { bar.Color = Colors.Red; bar.EdgeColor = null; bar.LineWidth = 0; });
            var plot = figure.PlotAreaOnSkia();
            using var picture = SKBitmap.Decode(figure.ToPng());
            int row = (int)(plot.Y + plot.Height * 0.9);

            for (int x = (int)Math.Ceiling(plot.X) + 2; x < (int)(plot.X + plot.Width) - 2; x++)
            {
                var c = picture.GetPixel(x, row);
                Assert.True(c == new SKColor(255, 0, 0) || c == SKColors.White, $"{theme.Name}: a blend {c} at column {x}");
            }
        }
    }

    [Fact]
    public void TheEdgeOfABar_UnderTheMatplotlibThemes_StandsOnTheMiddleOfAColumn()
    {
        foreach (var theme in MatplotlibThemes)
        {
            // An edge of 1 point: 1.39 pixels at the 96 dpi of a PNG at its natural size, which rounds to one pixel.
            var figure = Bars(theme, bar => { bar.Color = Colors.White; bar.EdgeColor = Colors.Black; bar.LineWidth = 1.0.PointsInPixels; });
            var plot = figure.PlotAreaOnSkia();
            using var picture = SKBitmap.Decode(figure.ToPng());
            int row = (int)(plot.Y + plot.Height * 0.9);
            var ink = ColumnInk(picture, (int)Math.Ceiling(plot.X) + 2, (int)(plot.X + plot.Width) - 2, row, row);

            // Each run of inked columns is one side of a bar: its darkest column has the same ink on both sides.
            int sides = 0;
            for (int i = 1; i < ink.Length - 1; i++)
            {
                if (ink[i] < 100 || ink[i] < ink[i - 1] || ink[i] < ink[i + 1]) continue;
                sides++;
                Assert.True(Math.Abs(ink[i - 1] - ink[i + 1]) <= 6, $"{theme.Name}: a side at column {i} spills {ink[i - 1]} and {ink[i + 1]}");
            }
            Assert.True(sides == 10, $"{theme.Name}: {sides} sides in {string.Join(' ', ink)}");
        }
    }

    [Fact]
    public void TheLibrarysThemes_LeaveTheMarksWhereTheyStand()
    {
        var figure = Empty(Theme.Default, grid: true);
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int fromY = (int)(plot.Y + plot.Height * 0.3), toY = (int)(plot.Y + plot.Height * 0.7);

        Assert.Contains(XTicks, tick =>
        {
            int x = (int)ColumnOf(plot, tick);
            return !CentredOnAColumn(ColumnInk(picture, x - 3, x + 3, fromY, toY));
        });
    }
}
