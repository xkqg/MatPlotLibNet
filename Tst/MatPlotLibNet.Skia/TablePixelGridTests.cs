// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The cells of a table in a raster export stand on the pixel grid, as matplotlib's Agg backend draws a <c>Rectangle</c> patch
/// (<c>ax.table</c> adds each cell as one): every edge of a rectilinear path is snapped to the pixel centre nearest to it when the
/// line it strokes rounds to an odd number of pixels (matplotlib 3.11.2, <c>PathSnapper</c>: <c>(mpl_round_to_int(stroke_width) % 2) ?
/// 0.5 : 0.0</c>), so a rule of one point (1.39 pixels at 100 dpi) is one whole black pixel with a fifth of the line on each side of it (two cells that meet
/// paint their two fifths over each other). The edge of the rectangle is covered by the share of each pixel it spans, where a stroked
/// path covers a pixel in quarters.
/// The matplotlib themes ask for it (<see cref="RasterLook.StrokesOnPixelGrid"/>); the library's own themes leave the rules where they stand.</summary>
public class TablePixelGridTests
{
    private const int Rows = 3;
    private const int Columns = 3;

    // Cells of one point rules, no text (a glyph would cross the rows scanned), as the Fidelity check sets them, in the plot from its top
    // under every theme, where the scan reads them (a matplotlib theme stands a table below the axes unasked).
    private static Figure Table(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax
            .Table(new string[Rows][] { ["", "", ""], ["", "", ""], ["", "", ""] }, s =>
            {
                s.Location = TableLocation.InPlot;
                s.CellHeight = 10 * 1.2 * 100 / 72;
                s.CellColor = Colors.White;
                s.BorderColor = Colors.Black;
            })
            .HideAllAxes())
        .Build();

    /// <summary>The pixels along a column (<paramref name="column"/>) or a row, from the plot's top left, for as long as the table runs.</summary>
    private static SKColor[] Scan(Theme theme, bool alongRows)
    {
        var figure = Table(theme);
        var plot = figure.PlotAreaOnSkia();
        using var picture = SKBitmap.Decode(figure.ToPng());
        int length = alongRows ? (int)plot.Width + 5 : (int)Math.Ceiling(Rows * 10 * 1.2 * 100 / 72) + 4;
        var pixels = new SKColor[length];
        for (int i = 0; i < length; i++)
        {
            pixels[i] = alongRows
                ? picture.GetPixel((int)plot.X - 2 + i, (int)(plot.Y + 8))
                : picture.GetPixel((int)(plot.X + 6), (int)plot.Y - 2 + i);
        }
        return pixels;
    }

    private static int[] BlackPixels(SKColor[] pixels) => [.. Enumerable.Range(0, pixels.Length).Where(i => pixels[i].Red <= 8)];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMatplotlibTheme_DrawsEveryRuleOfATableAsOneBlackPixelBesideTwoPixelsOfAFifth(bool alongRows)
    {
        // A rule of 1.39 pixels centred on a pixel covers it whole and 0.194 of each neighbour: 255 * (1 - 0.194) = 205. The figure
        // holds other marks outside the table (the pixels beyond its outer rules), so the pixels beside a rule are read inside it.
        foreach (var theme in new[] { Theme.MatplotlibClassic, Theme.MatplotlibV2 })
        {
            var pixels = Scan(theme, alongRows);
            var rules = BlackPixels(pixels);
            Assert.Equal(alongRows ? Columns + 1 : Rows + 1, rules.Length);
            for (int r = 0; r < rules.Length; r++)
            {
                int rule = rules[r];
                var inside = r == 0 ? new[] { rule + 1 } : r == rules.Length - 1 ? new[] { rule - 1 } : new[] { rule - 1, rule + 1 };
                foreach (int side in inside)
                {
                    // The fifth of one stroke (205), or of two that meet there (a cell's rule over its neighbour's: 205 x 0.806 = 165).
                    Assert.True(Math.Abs(pixels[side].Red - 205) <= 2 || Math.Abs(pixels[side].Red - 165) <= 2, $"{pixels[side].Red} beside rule {r}");
                    Assert.Equal(pixels[side].Red, pixels[side].Green);
                }
            }
        }
    }

    [Fact]
    public void TheLibrarysThemes_LeaveTheRulesOfATableWhereTheyStand()
    {
        // Rules at 16.67 pixels apart cannot all stand on whole pixels: some of them are a blend and none is the matplotlib five-tenths grey.
        var pixels = Scan(Theme.Default, alongRows: false);
        Assert.NotEqual(Rows + 1, BlackPixels(pixels).Length);
    }

    // The rectangle of the surface-level tests: 10 to 30 across, 10 to 30 down, at the scale of 100 dpi, so a one point edge is 1.39 pixels.
    private static readonly Rect Cell = new(10, 10, 20, 20);

    private static SKBitmap Paint(bool snaps, ShapeStyle style, Rect area, double scale = 100.0 / 72)
    {
        var bitmap = new SKBitmap(80, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        canvas.Scale((float)scale);
        IPixelGridRenderContext context = snaps ? canvas.RasterExportUnder(Theme.MatplotlibV2) : Told(new SkiaRenderContext(canvas));
        context.DrawRectangleOnPixelGrid(area, style);
        return bitmap;
    }

    /// <summary>A context that does not snap, told the theme the cells are drawn under, so a dashed edge is dashed as the snapping one's is.</summary>
    private static SkiaRenderContext Told(SkiaRenderContext context)
    {
        context.UseLooksOf(Theme.MatplotlibV2);
        return context;
    }

    private static SKBitmap PaintAsDrawn(ShapeStyle style, Rect area)
    {
        var bitmap = new SKBitmap(80, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        canvas.Scale(100f / 72);
        Told(new SkiaRenderContext(canvas)).DrawRectangle(area, style);
        return bitmap;
    }

    private static void AssertSame(SKBitmap expected, SKBitmap actual)
    {
        for (int y = 0; y < expected.Height; y++)
            for (int x = 0; x < expected.Width; x++)
                Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
    }

    [Fact]
    public void ASurfaceThatDoesNotSnap_DrawsTheRectangleAsItStands()
    {
        var style = new ShapeStyle(Colors.White, Colors.Black, 1);
        using var expected = PaintAsDrawn(style, Cell);
        using var actual = Paint(snaps: false, style, Cell);
        AssertSame(expected, actual);
    }

    [Fact]
    public void ADashedEdge_IsStrokedAsAnyOtherFromTheRectangleOnTheGrid()
    {
        var style = new ShapeStyle(Colors.White, Colors.Black, 1) { StrokeLineStyle = LineStyle.Dashed };
        using var scratch = new SKBitmap(80, 80);
        using var canvas = new SKCanvas(scratch);
        canvas.Scale(100f / 72);
        var onGrid = ((IPixelGridRenderContext)canvas.RasterExportUnder(Theme.MatplotlibV2)).OnPixelGrid(Cell, 1);
        Assert.NotEqual(Cell, onGrid);

        using var expected = PaintAsDrawn(style, onGrid);
        using var actual = Paint(snaps: true, style, Cell);
        AssertSame(expected, actual);
    }

    [Fact]
    public void ARectangleWithNoEdge_StandsOnWholePixelsInItsFill()
    {
        using var picture = Paint(snaps: true, new ShapeStyle(Colors.Black, null, 0), Cell);
        // 10 * 100 / 72 = 13.9 rounds to 14, 30 * 100 / 72 = 41.7 rounds to 42: the rectangle spans pixels 14 to 41, and no pixel is a blend.
        Assert.Equal(SKColors.White, picture.GetPixel(13, 20));
        Assert.Equal(new SKColor(0, 0, 0), picture.GetPixel(14, 20));
        Assert.Equal(new SKColor(0, 0, 0), picture.GetPixel(41, 20));
        Assert.Equal(SKColors.White, picture.GetPixel(42, 20));
    }

    [Fact]
    public void AnEdgeWiderThanTheRectangle_CoversAllOfIt()
    {
        // 2 x 2 units at 100 dpi is 2.8 pixels across, inside an edge of 10 units.
        using var picture = Paint(snaps: true, new ShapeStyle(Colors.White, Colors.Black, 10), new Rect(20, 20, 2, 2));
        Assert.Equal(new SKColor(0, 0, 0), picture.GetPixel(29, 29));
    }

    [Fact]
    public void ATranslucentEdge_IsPaintedOnceOverTheFill_NotTwiceWhereTheFourSidesMeet()
    {
        var style = new ShapeStyle(Colors.White, Colors.Black.WithAlpha(128), 4);
        using var picture = Paint(snaps: true, style, Cell);
        // Every pixel of the frame is covered once, by one of its sides: a half-black edge over white is 127 or 128 (whole pixels), not 64.
        var corner = picture.GetPixel(14, 14);
        Assert.InRange(corner.Red, 126, 129);
    }
}
