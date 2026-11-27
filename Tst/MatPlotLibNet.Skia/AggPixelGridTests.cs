// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using SkiaSharp;
using MplColor = MatPlotLibNet.Styling.Color;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>What a raster export does with a mark as matplotlib's Agg backend does: the ends of a horizontal or vertical stroke, the
/// corners of a rectangle and the ends of a marker moved to whole pixels (half a pixel on for an odd width), and a line or the frame
/// of a rectangle filled as the rectangle it covers. On a canvas that is no grid of pixels (turned, skewed) a mark stays where it
/// stands and nothing is filled.</summary>
public sealed class AggPixelGridTests
{
    private static readonly SKColor Black = new(0, 0, 0);

    private static (SKBitmap Bitmap, SKCanvas Canvas) Surface(int size = 100, Action<SKCanvas>? place = null)
    {
        var bitmap = new SKBitmap(size, size);
        var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        place?.Invoke(canvas);
        return (bitmap, canvas);
    }

    private static PixelSegment Horizontal(double x1, double x2, double y) => new(new Point(x1, y), new Point(x2, y));

    private static PixelSegment Vertical(double x, double y1, double y2) => new(new Point(x, y1), new Point(x, y2));

    // ── A stroke ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AHorizontalStrokeOfAnOddWidth_StandsOnTheCentreOfAPixelRow()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var snapped = new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(Horizontal(10.3, 50.3, 20.7), 1.0, projectingEnds: false);

            Assert.Equal(Horizontal(10.5, 50.5, 21.5), snapped);
        }
    }

    [Fact]
    public void AVerticalStrokeOfAnEvenWidth_StandsOnAPixelEdge()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var snapped = new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(Vertical(20.7, 10.3, 50.3), 2.0, projectingEnds: false);

            Assert.Equal(Vertical(21, 10, 50), snapped);
        }
    }

    [Fact]
    public void AProjectingStroke_CarriesOnByHalfItsWidthAtBothEnds_WhicheverWayItRuns()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var grid = new SkiaRenderContext.AggPixelGrid(canvas);

            // A width of 3 rounds to an odd 3: the ends 10.3 and 50.3 snap to 10.5 and 50.5, the row 20.7 to 21.5, and each end carries on by 1.5.
            Assert.Equal(Horizontal(9, 52, 21.5), grid.OnPixelGrid(Horizontal(10.3, 50.3, 20.7), 3.0, projectingEnds: true));
            Assert.Equal(Horizontal(52, 9, 21.5), grid.OnPixelGrid(Horizontal(50.3, 10.3, 20.7), 3.0, projectingEnds: true));
            Assert.Equal(Vertical(21.5, 9, 52), grid.OnPixelGrid(Vertical(20.7, 10.3, 50.3), 3.0, projectingEnds: true));
            Assert.Equal(Vertical(21.5, 52, 9), grid.OnPixelGrid(Vertical(20.7, 50.3, 10.3), 3.0, projectingEnds: true));
        }
    }

    /// <summary>A stroke that is neither horizontal nor vertical, a stroke of no length, and a horizontal one on a canvas that is turned
    /// (no grid of pixels) stay where they stand.</summary>
    [Theory]
    [InlineData(10.3, 20.7, 50.3, 40.1, false)]
    [InlineData(10.3, 20.7, 10.3, 20.7, false)]
    [InlineData(10.3, 20.7, 50.3, 20.7, true)]
    public void AStrokeTheGridDoesNotSnap_StaysWhereItStands(double x1, double y1, double x2, double y2, bool onATurnedCanvas)
    {
        var stroke = new PixelSegment(new Point(x1, y1), new Point(x2, y2));
        var (bitmap, canvas) = Surface(place: onATurnedCanvas ? c => c.RotateDegrees(90, 50, 50) : null);
        using (bitmap) using (canvas)
        {
            Assert.Equal(stroke, new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(stroke, 1.0, projectingEnds: false));
        }
    }

    [Fact]
    public void AStrokeOnAScaledCanvas_IsMovedByThePixelsItLandsOn_AndReadBackInUnits()
    {
        var (bitmap, canvas) = Surface(place: c => c.Scale(2f));
        using (bitmap) using (canvas)
        {
            // 20.7 units are row 41.4 on the device, whole pixel 41, which is 20.5 units; the width of 1 unit is 2 px: even, no half pixel.
            var snapped = new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(Horizontal(10.3, 50.3, 20.7), 1.0, projectingEnds: false);

            Assert.Equal(Horizontal(10.5, 50.5, 20.5), snapped);
        }
    }

    // ── A marker ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AStraightMarker_HangsFromItsPointRoundedToAPixel()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var mark = new SkiaRenderContext.AggPixelGrid(canvas).MarkerOnPixelGrid(new Point(10.3, 20.7), Vertical(10.3, 20.7, 25.56), 1.11);

            Assert.Equal(Vertical(10.5, 21.5, 26.5), mark);
        }
    }

    [Fact]
    public void AHorizontalMarker_IsSnappedByItsWidthAcross()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var mark = new SkiaRenderContext.AggPixelGrid(canvas).MarkerOnPixelGrid(new Point(40.6, 12.2), Horizontal(40.6, 37.2, 12.2), 2.2);

            Assert.Equal(Horizontal(41, 38, 12), mark);
        }
    }

    [Fact]
    public void AMarkerThatIsNotStraight_IsLeftAlone()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var grid = new SkiaRenderContext.AggPixelGrid(canvas);

            Assert.Null(grid.MarkerOnPixelGrid(new Point(10, 10), new PixelSegment(new Point(10, 10), new Point(14, 13)), 1.0));
            Assert.Null(grid.MarkerOnPixelGrid(new Point(10, 10), new PixelSegment(new Point(10, 10), new Point(10, 10)), 1.0));
        }
    }

    [Fact]
    public void AMarkerOnATurnedCanvas_IsLeftAlone()
    {
        var (bitmap, canvas) = Surface(place: c => c.RotateDegrees(45));
        using (bitmap) using (canvas)
        {
            Assert.Null(new SkiaRenderContext.AggPixelGrid(canvas).MarkerOnPixelGrid(new Point(10.3, 20.7), Vertical(10.3, 20.7, 25.56), 1.0));
        }
    }

    // ── A rectangle ──────────────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ARectangle_HasEachCornerOnTheNearestWholePixel_HalfAPixelOnForAnOddEdge()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var grid = new SkiaRenderContext.AggPixelGrid(canvas);

            Assert.Equal(new Rect(10, 21, 41, 31), grid.OnPixelGrid(new Rect(10.3, 20.7, 40.2, 30.9), 0));
            Assert.Equal(new Rect(10.5, 21.5, 41, 31), grid.OnPixelGrid(new Rect(10.3, 20.7, 40.2, 30.9), 1));
        }
    }

    [Fact]
    public void ARectangleOnATurnedCanvas_StaysWhereItStands()
    {
        var (bitmap, canvas) = Surface(place: c => c.RotateDegrees(30));
        using (bitmap) using (canvas)
        {
            var area = new Rect(10.3, 20.7, 40.2, 30.9);

            Assert.Equal(area, new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(area, 1));
        }
    }

    // ── A line filled as the rectangle it covers ─────────────────────────────────────────────────────────────────────────────

    private static ScaledDashPattern Dashes(LineStyle style, double scale) => new(DashPatterns.GetPattern(style), scale);

    private static ScaledDashPattern Solid => Dashes(LineStyle.Solid, 1);

    [Fact]
    public void ASolidHorizontalLine_IsFilledByTheShareOfEachPixelItTakes()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            Assert.True(new SkiaRenderContext.AggPixelGrid(canvas).TryFillLine(new Point(5, 20.5), new Point(55, 20.5), Black, 1.3888889, Solid));

            Assert.InRange(bitmap.GetPixel(30, 19).Red, 204, 207);
            Assert.Equal(0, bitmap.GetPixel(30, 20).Red);
            Assert.Equal(255, bitmap.GetPixel(4, 20).Red);
        }
    }

    [Fact]
    public void ASolidVerticalLineRunningUp_IsFilledAsAColumn()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            Assert.True(new SkiaRenderContext.AggPixelGrid(canvas).TryFillLine(new Point(20.5, 55), new Point(20.5, 5), Black, 1.3888889, Solid));

            Assert.InRange(bitmap.GetPixel(19, 30).Red, 204, 207);
            Assert.Equal(0, bitmap.GetPixel(20, 30).Red);
        }
    }

    [Fact]
    public void ADashedLine_IsPaintedAlongItsRectangleByTheShareEachDashCovers()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            Assert.True(new SkiaRenderContext.AggPixelGrid(canvas).TryFillLine(new Point(5, 20.5), new Point(55, 20.5), Black, 1, Dashes(LineStyle.Dashed, 2)));

            // [10, 4] from x = 5: the first dash covers pixels 5 to 14, its gap 15 to 18, and the second dash starts at 19.
            Assert.Equal(0, bitmap.GetPixel(10, 20).Red);
            Assert.Equal(255, bitmap.GetPixel(16, 20).Red);
            Assert.Equal(0, bitmap.GetPixel(22, 20).Red);
        }
    }

    [Fact]
    public void ADashedVerticalLine_IsPaintedAlongItsColumn()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            Assert.True(new SkiaRenderContext.AggPixelGrid(canvas).TryFillLine(new Point(20.5, 5), new Point(20.5, 55), Black, 1, Dashes(LineStyle.Dashed, 2)));

            Assert.Equal(0, bitmap.GetPixel(20, 10).Red);
            Assert.Equal(255, bitmap.GetPixel(20, 16).Red);
        }
    }

    [Fact]
    public void ADashedLineWhoseGapIsNarrowerThanAPixel_IsNotFilled()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            Assert.False(new SkiaRenderContext.AggPixelGrid(canvas).TryFillLine(new Point(5, 20.5), new Point(55, 20.5), Black, 1, Dashes(LineStyle.Dotted, 0.2)));
            Assert.Equal(255, bitmap.GetPixel(10, 20).Red);
        }
    }

    [Fact]
    public void ADashedLineOnATurnedCanvasOrOfNoFiniteLength_IsNotFilled()
    {
        var (bitmap, canvas) = Surface(place: c => c.RotateDegrees(90, 50, 50));
        using (bitmap) using (canvas)
        {
            Assert.False(new SkiaRenderContext.AggPixelGrid(canvas).TryFillLine(new Point(5, 20.5), new Point(55, 20.5), Black, 1, Dashes(LineStyle.Dashed, 2)));
        }

        var (second, upright) = Surface();
        using (second) using (upright)
        {
            Assert.False(new SkiaRenderContext.AggPixelGrid(upright).TryFillLine(new Point(5, 20.5), new Point(double.PositiveInfinity, 20.5), Black, 1, Dashes(LineStyle.Dashed, 2)));
        }
    }

    [Fact]
    public void TwoDashedLinesOfOnePatternLength_ReuseTheStopsAndOfAnotherLengthMakeNew()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var grid = new SkiaRenderContext.AggPixelGrid(canvas);
            Assert.True(grid.TryFillLine(new Point(5, 20.5), new Point(55, 20.5), Black, 1, Dashes(LineStyle.Dashed, 2)));
            Assert.True(grid.TryFillLine(new Point(5, 30.5), new Point(55, 30.5), Black, 1, Dashes(LineStyle.Dashed, 2)));
            Assert.True(grid.TryFillLine(new Point(5, 40.5), new Point(55, 40.5), Black, 1, Dashes(LineStyle.DashDot, 2)));

            Assert.Equal(0, bitmap.GetPixel(10, 30).Red);
            Assert.Equal(0, bitmap.GetPixel(10, 40).Red);
        }
    }

    // ── The frame of a rectangle ─────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheFrameOfARectangle_IsFourBandsCentredOnItsSides()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            new SkiaRenderContext.AggPixelGrid(canvas).FillFrame(new Rect(10, 10, 30, 30), Black, 2);

            Assert.Equal(0, bitmap.GetPixel(25, 9).Red);    // 9 to 11 across the top side
            Assert.Equal(0, bitmap.GetPixel(25, 10).Red);
            Assert.Equal(255, bitmap.GetPixel(25, 12).Red); // the inside
            Assert.Equal(0, bitmap.GetPixel(9, 25).Red);
            Assert.Equal(0, bitmap.GetPixel(39, 25).Red);
            Assert.Equal(0, bitmap.GetPixel(25, 40).Red);
        }
    }

    [Fact]
    public void AnEdgeAsWideAsTheRectangleIsThick_CoversAllOfIt()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            new SkiaRenderContext.AggPixelGrid(canvas).FillFrame(new Rect(10, 10, 4, 4), Black, 6);

            Assert.Equal(0, bitmap.GetPixel(12, 12).Red);
        }
    }

    // ── A line of horizontal and vertical segments ──────────────────────────────────────────────────────────────────────

    // matplotlib 3.11.2, src/path_converters.h PathSnapper::should_snap in SNAP_AUTO (a Line2D's snap None): a path of at most 1024
    // vertices whose every line_to is horizontal or vertical (fabs(dx) < 1e-4 or fabs(dy) < 1e-4, device pixels) has each vertex moved
    // to floor(v + 0.5) + snap_value, snap_value 0.5 for a width that rounds to an odd number of pixels. Printed: a 1 pt ax.step run
    // at device y 19.7 is the rows 205, 0, 205; a line with one slanted segment is left where it is.

    private static readonly Point[] Steps = [new(10.3, 20.3), new(10.3, 60.7), new(50.6, 60.7), new(50.6, 80.2)];

    [Fact]
    public void ALineOfHorizontalAndVerticalSegments_HasEveryVertexOnAPixelCentre_ForAnOddWidth()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var line = new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(Steps, 100.0 / 72);   // 1.39 rounds to 1: half a pixel on

            Assert.Equal([new Point(10.5, 20.5), new Point(10.5, 61.5), new Point(51.5, 61.5), new Point(51.5, 80.5)], line);
        }
    }

    [Fact]
    public void ALineOfAWidthThatRoundsEven_StandsOnPixelEdges_AndABreakIsKept()
    {
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            // A missing point breaks the line: the segments beside it are not drawn, so they are not asked to be straight.
            Point[] broken = [new(10.3, 20.3), new(10.3, 60.7), new(double.NaN, double.NaN), new(70.2, 33.1), new(90.6, 33.1)];

            var line = new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid(broken, 2.2);

            Assert.Equal(new Point(10, 20), line[0]);
            Assert.Equal(new Point(10, 61), line[1]);
            Assert.True(double.IsNaN(line[2].X) && double.IsNaN(line[2].Y));
            Assert.Equal(new Point(70, 33), line[3]);
            Assert.Equal(new Point(91, 33), line[4]);
        }
    }

    [Fact]
    public void OnACanvasScaledForADpi_TheVerticesAreSnappedInDevicePixels()
    {
        var (bitmap, canvas) = Surface(200, c => c.Scale(2f));
        using (bitmap) using (canvas)
        {
            // 1.39 units are 2.78 device pixels (rounds to 3, odd): (20.6, 40.6) -> (21.5, 41.5), read back halved.
            var line = new SkiaRenderContext.AggPixelGrid(canvas).OnPixelGrid([new Point(10.3, 20.3), new Point(30.3, 20.3)], 100.0 / 72);

            Assert.Equal([new Point(10.75, 20.75), new Point(30.75, 20.75)], line);
        }
    }

    [Fact]
    public void ALineWithASlantedSegment_MoreThan1024Points_OrATurnedCanvas_IsLeftAsItStands()
    {
        Point[] slanted = [new(10.3, 20.3), new(10.3, 60.7), new(50.6, 70.2)];
        var longest = Enumerable.Range(0, 1024).Select(i => new Point(10.3, i % 2 == 0 ? 20.3 : 30.3)).ToArray();
        var tooLong = Enumerable.Range(0, 1025).Select(i => new Point(10.3, i % 2 == 0 ? 20.3 : 30.3)).ToArray();
        var (bitmap, canvas) = Surface();
        using (bitmap) using (canvas)
        {
            var grid = new SkiaRenderContext.AggPixelGrid(canvas);

            Assert.Same(slanted, grid.OnPixelGrid(slanted, 1.0));
            Assert.Same(tooLong, grid.OnPixelGrid(tooLong, 1.0));
            Assert.Equal(new Point(10.5, 20.5), grid.OnPixelGrid(longest, 1.0)[0]);

            canvas.RotateDegrees(30);
            Assert.Same(Steps, grid.OnPixelGrid(Steps, 1.0));
        }
    }

    private const float Width = 1.3888889f;   // 1 pt at 100 dpi

    private static SKBitmap Draw(bool rasterExport, Point from, Point to, LineStyle style = LineStyle.Solid, float scale = 1f)
    {
        var bitmap = new SKBitmap(120, 120);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        canvas.Scale(scale);
        (rasterExport ? canvas.RasterExportUnder(Theme.MatplotlibV2) : new SkiaRenderContext(canvas))
            .DrawLine(from, to, new StrokeStyle(MplColor.FromHex("#000000"), Width, style));
        return bitmap;
    }

    [Fact]
    public void AHorizontalLineInARasterExport_CoversTheRowsBesideItByTheAreaItTakes()
    {
        using var bitmap = Draw(rasterExport: true, new Point(5, 20.5), new Point(55, 20.5));

        Assert.InRange(bitmap.GetPixel(30, 19).Red, 204, 207);   // 255 * (1 - 0.194) = 205.5
        Assert.Equal(0, bitmap.GetPixel(30, 20).Red);
        Assert.InRange(bitmap.GetPixel(30, 21).Red, 204, 207);
        Assert.Equal(255, bitmap.GetPixel(4, 20).Red);           // butt ends: nothing past the line's ends
        Assert.Equal(255, bitmap.GetPixel(55, 20).Red);
    }

    [Fact]
    public void ALineDrawnRightToLeft_CoversTheSamePixels()
    {
        using var bitmap = Draw(rasterExport: true, new Point(55, 20.5), new Point(5, 20.5));

        Assert.InRange(bitmap.GetPixel(30, 19).Red, 204, 207);
        Assert.Equal(0, bitmap.GetPixel(30, 20).Red);
        Assert.Equal(255, bitmap.GetPixel(4, 20).Red);
    }

    [Fact]
    public void AVerticalLineInARasterExport_CoversTheColumnsBesideItByTheAreaItTakes()
    {
        using var bitmap = Draw(rasterExport: true, new Point(20.5, 55), new Point(20.5, 5));

        Assert.InRange(bitmap.GetPixel(19, 30).Red, 204, 207);
        Assert.Equal(0, bitmap.GetPixel(20, 30).Red);
        Assert.InRange(bitmap.GetPixel(21, 30).Red, 204, 207);
        Assert.Equal(255, bitmap.GetPixel(20, 4).Red);
    }

    [Fact]
    public void OnACanvasScaledForAHigherResolution_TheLineIsCoveredByTheAreaItTakesOfEachDevicePixel()
    {
        // Twice the resolution: the line is 2.78 device pixels wide, centred on the device row edge 42 (40.61 to 43.39), so rows 41
        // and 42 are black and the rows 40 and 43 take 0.39 of a pixel each.
        using var bitmap = Draw(rasterExport: true, new Point(5, 21), new Point(55, 21), scale: 2f);

        Assert.Equal(0, bitmap.GetPixel(60, 41).Red);
        Assert.Equal(0, bitmap.GetPixel(60, 42).Red);
        Assert.InRange(bitmap.GetPixel(60, 40).Red, 153, 158);   // 255 * (1 - 0.389) = 155.8
        Assert.InRange(bitmap.GetPixel(60, 43).Red, 153, 158);
    }

    [Fact]
    public void ADashedLineInARasterExport_CoversEachDashByTheAreaItTakes_AndLeavesTheGapsBlank()
    {
        // The default style's dashed is 3.7 on and 1.6 off, times the width (5.14 and 2.22): the first dash runs 5 to 10.14, the gap to
        // 12.36, the next dash to 17.5. matplotlib's Agg covers a dash as it covers a solid line, so the rows beside it are 0.19 covered here too.
        using var dashed = Draw(rasterExport: true, new Point(5, 20.5), new Point(55, 20.5), LineStyle.Dashed);
        using var upwards = Draw(rasterExport: true, new Point(20.5, 55), new Point(20.5, 5), LineStyle.Dashed);

        Assert.InRange(dashed.GetPixel(8, 19).Red, 204, 207);
        Assert.Equal(0, dashed.GetPixel(8, 20).Red);
        Assert.InRange(dashed.GetPixel(10, 20).Red, 218, 222);    // the first dash ends 0.14 into pixel 10: 255 * 0.86 = 220
        Assert.Equal(255, dashed.GetPixel(11, 20).Red);
        Assert.InRange(dashed.GetPixel(12, 20).Red, 90, 94);      // the next starts 0.36 into pixel 12: 255 * 0.36 = 92
        Assert.Equal(0, dashed.GetPixel(14, 20).Red);
        Assert.InRange(dashed.GetPixel(17, 20).Red, 125, 130);    // and ends half way through pixel 17
        Assert.InRange(upwards.GetPixel(19, 52).Red, 204, 207);   // the first dash runs up from 55 to 49.86
        Assert.Equal(255, upwards.GetPixel(20, 48).Red);           // the gap, 49.86 to 47.64
    }

    [Fact]
    public void ADashedLineOnACanvasScaledForAHigherResolution_IsCoveredByEachDevicePixel()
    {
        // Twice the resolution: dashes of 5.14 and gaps of 2.22 units are 10.28 and 4.44 device pixels; the first dash runs from device
        // column 10 to 20.28, the gap to 24.72.
        using var bitmap = Draw(rasterExport: true, new Point(5, 21), new Point(55, 21), LineStyle.Dashed, scale: 2f);

        Assert.Equal(0, bitmap.GetPixel(15, 41).Red);
        Assert.InRange(bitmap.GetPixel(15, 40).Red, 153, 158);    // 0.39 across, as a solid line
        Assert.Equal(0, bitmap.GetPixel(19, 41).Red);
        Assert.InRange(bitmap.GetPixel(20, 41).Red, 182, 186);    // 0.28 of pixel 20 along: 255 * 0.72 = 184
        Assert.Equal(255, bitmap.GetPixel(22, 41).Red);
        Assert.InRange(bitmap.GetPixel(24, 41).Red, 182, 186);    // the next dash starts 0.72 into pixel 24
    }

    [Fact]
    public void ASlantedLine_AndACanvasThatIsNotARasterExport_KeepTheStroke()
    {
        using var control = Draw(rasterExport: false, new Point(5, 20.5), new Point(55, 20.5));
        using var slanted = Draw(rasterExport: true, new Point(5, 5), new Point(55, 55));

        Assert.Equal(191, control.GetPixel(30, 19).Red);          // Skia's quarter of a pixel
        Assert.True(slanted.GetPixel(30, 30).Red < 128);
    }

    [Fact]
    public void ADashedLineOfNoFiniteLength_KeepsTheStroke()
    {
        // A length no pattern can be laid along (an infinity) is left to the stroke, which draws what Skia makes of it.
        using var bitmap = Draw(rasterExport: true, new Point(5, 20.5), new Point(double.PositiveInfinity, 20.5), LineStyle.Dashed);

        Assert.Equal(255, bitmap.GetPixel(2, 20).Red);
    }

    [Fact]
    public void ADottedLineWhoseGapsAreNarrowerThanAPixel_KeepsTheStroke()
    {
        // Dotted is 1 on and 3 off, times the width: at 0.2 px the gaps are 0.6 px, so two dots would share a pixel, and the line is
        // stroked as before. It is drawn: a faint grey row, never nothing.
        var bitmap = new SKBitmap(60, 60);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.RasterExportUnder(Theme.MatplotlibV2)
                .DrawLine(new Point(5, 20.5), new Point(55, 20.5), new StrokeStyle(MplColor.FromHex("#000000"), 0.2, LineStyle.Dotted));
        }

        using (bitmap) Assert.InRange(Enumerable.Range(5, 50).Min(x => bitmap.GetPixel(x, 20).Red), 1, 254);
    }

    [Fact]
    public void ADashedLineOnATurnedCanvas_KeepsTheStroke()
    {
        var bitmap = new SKBitmap(60, 60);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            canvas.RotateDegrees(90, 30, 30);
            canvas.RasterExportUnder(Theme.MatplotlibV2)
                .DrawLine(new Point(5, 20.5), new Point(55, 20.5), new StrokeStyle(MplColor.FromHex("#000000"), Width, LineStyle.Dashed));
        }

        // Turned a quarter about the centre, the line runs down column 39 (60 - 20.5); its first dash covers rows 5 to 11.
        using (bitmap) Assert.True(bitmap.GetPixel(39, 8).Red < 64);
    }

    [Fact]
    public void ALineOfNoLength_DrawsNothing()
    {
        using var bitmap = Draw(rasterExport: true, new Point(20.5, 20.5), new Point(20.5, 20.5));

        Assert.Equal(255, bitmap.GetPixel(20, 20).Red);
    }
}