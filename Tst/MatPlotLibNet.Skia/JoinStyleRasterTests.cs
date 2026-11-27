// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;
using SkiaSharp;
using MplColor = MatPlotLibNet.Styling.Color;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The join of a stroke is painted by the one stroke paint of the context: a mitred corner fills the corner square beyond the
/// two edges, a rounded one is the quarter circle of half the width, a bevelled one the cut across it. A mark the context paints as a
/// fill instead (the frame of a table cell on the pixel grid) is painted as the stroke when its join is not the mitre it is.</summary>
public sealed class JoinStyleRasterTests
{
    private static readonly MplColor Black = MplColor.FromHex("#000000");

    // A corner at (40, 40) of a stroke 10 wide: the outer corner square is x and y from 40 to 45; the pixel centred (44.5, 44.5) is 6.4 from the corner.
    private static readonly Point[] Corner = [new(10, 40), new(40, 40), new(40, 10)];

    private static SKBitmap Paint(Action<SkiaRenderContext> draw)
    {
        var bitmap = new SKBitmap(60, 60);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        draw(new SkiaRenderContext(canvas));
        return bitmap;
    }

    [Theory]
    [InlineData(JoinStyle.Miter, 0)]
    [InlineData(JoinStyle.Round, 255)]
    [InlineData(JoinStyle.Bevel, 255)]
    public void ALineCorner_IsFilledAsItsJoinSays(JoinStyle join, int expectedRed)
    {
        using var bitmap = Paint(ctx => ctx.DrawLines(Corner, new StrokeStyle(Black, 10, LineStyle.Solid) { Join = join }));

        Assert.Equal(expectedRed, bitmap.GetPixel(44, 44).Red);
        Assert.Equal(0, bitmap.GetPixel(41, 41).Red);   // inside the corner square every join covers
    }

    /// <summary>A stroke that names no join is joined as the look the context was told says: a line is round under the matplotlib themes
    /// (matplotlib's <c>Line2D</c> and <c>LineCollection</c>) and mitred under a library theme, and a join the stroke names wins.</summary>
    [Theory]
    [InlineData("MatplotlibV2", null, 255)]
    [InlineData("MatplotlibClassic", null, 255)]
    [InlineData("Default", null, 0)]
    [InlineData("MatplotlibV2", JoinStyle.Miter, 0)]
    [InlineData("Default", JoinStyle.Round, 255)]
    public void ALineThatNamesNoJoin_IsJoinedAsTheLookOfTheContextSays(string themeName, JoinStyle? given, int expectedRed)
    {
        var theme = themeName.AsTheme();
        using var bitmap = Paint(ctx =>
        {
            ctx.UseLooksOf(theme);
            ctx.DrawLines(Corner, new StrokeStyle(Black, 10, LineStyle.Solid) { Join = given });
        });

        Assert.Equal(expectedRed, bitmap.GetPixel(44, 44).Red);
        Assert.Equal(0, bitmap.GetPixel(41, 41).Red);
    }

    /// <summary>A context never told a theme draws as a library theme does: the mitre.</summary>
    [Fact]
    public void ALineDrawnOnAContextThatWasNeverTold_IsMitred()
    {
        using var bitmap = Paint(ctx => ctx.DrawLines(Corner, new StrokeStyle(Black, 10, LineStyle.Solid)));

        Assert.Equal(0, bitmap.GetPixel(44, 44).Red);
    }

    /// <summary>matplotlib 3.11.2 (python, Agg, dpi 100): a square scatter marker 35 px across with an edge 16.7 px wide, drawn by a
    /// <c>PathCollection</c> (<c>get_joinstyle()</c> is None, so the round of the graphics context), has its outer corner on the diagonal
    /// 26.4 px from the centre, where a mitre reaches 24.0. The corner pixel of the box round the edge is bare under the matplotlib themes
    /// and filled under a library theme.</summary>
    [Theory]
    [InlineData("MatplotlibV2", false)]
    [InlineData("MatplotlibClassic", false)]
    [InlineData("Default", true)]
    public void TheCornerOfASquareScatterMarkersEdge_IsRoundedUnderTheMatplotlibThemes(string themeName, bool cornerIsInk)
    {
        var theme = themeName.AsTheme();
        var edge = new MplColor(0, 0, 255);
        var figure = Plt.Create().WithSize(400, 400).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax
            .Scatter([5.0], [5.0], s => { s.Marker = MarkerStyle.Square; s.MarkerSize = 2500; s.EdgeColors = [edge]; s.LineWidths = [16.0]; })
            .SetXLim(0, 10).SetYLim(0, 10)).Build();
        using var bitmap = SKBitmap.Decode(figure.ToPng());

        int left = int.MaxValue, top = int.MaxValue;
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Blue > 200 && pixel.Red < 60 && pixel.Green < 60) { left = Math.Min(left, x); top = Math.Min(top, y); }
            }

        Assert.NotEqual(int.MaxValue, left);
        var corner = bitmap.GetPixel(left, top);
        Assert.Equal(cornerIsInk, corner.Blue > 200 && corner.Red < 60 && corner.Green < 60);
    }

    [Theory]
    [InlineData(JoinStyle.Miter, 0)]
    [InlineData(JoinStyle.Round, 255)]
    public void AShapeCorner_IsFilledAsItsStrokeJoinSays(JoinStyle join, int expectedRed)
    {
        using var bitmap = Paint(ctx => ctx.DrawRectangle(new Rect(10, 10, 30, 30), new ShapeStyle(null, Black, 10) { StrokeJoin = join }));

        // The rectangle's bottom right corner is (40, 40).
        Assert.Equal(expectedRed, bitmap.GetPixel(44, 44).Red);
    }

    [Fact]
    public void ACellEdgeOnThePixelGridWithARoundJoin_IsStrokedNotFilledAsFourBars()
    {
        using var bitmap = new SKBitmap(60, 60);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        IPixelGridRenderContext context = canvas.RasterExportUnder(Theme.MatplotlibV2);

        context.DrawRectangleOnPixelGrid(new Rect(10, 10, 30, 30), new ShapeStyle(null, Black, 10) { StrokeJoin = JoinStyle.Round });

        Assert.Equal(255, bitmap.GetPixel(44, 44).Red);   // the four bars of the frame would have filled the square corner
        Assert.Equal(0, bitmap.GetPixel(25, 10).Red);     // the edge itself is there
    }

    [Fact]
    public void ACellEdgeOnThePixelGridWithTheDefaultJoin_IsStillFilledAsFourBars()
    {
        using var bitmap = new SKBitmap(60, 60);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        IPixelGridRenderContext context = canvas.RasterExportUnder(Theme.MatplotlibV2);

        context.DrawRectangleOnPixelGrid(new Rect(10, 10, 30, 30), new ShapeStyle(null, Black, 10));

        Assert.Equal(0, bitmap.GetPixel(44, 44).Red);
    }
}
