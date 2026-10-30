// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A gradient is a property of the fill, so the raster backend paints it where the SVG one does: the colour at a
/// pixel is the blend of the two ends in proportion to how far along the line the pixel lies. A figure that shows a gradient
/// on screen and exports it flat to PNG or PDF is the divergence the hatch tests exist for; the same pins hold here, and the
/// PDF keeps the blend as a vector shading rather than a bitmap.</summary>
public class GradientRasterParityTests
{
    private static readonly Color Red = Color.FromHex("#FF0000");
    private static readonly Color Blue = Color.FromHex("#0000FF");

    private static SKBitmap Paint(ShapeStyle shape, Rect rect, int width = 120, int height = 40)
    {
        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        new SkiaRenderContext(canvas).DrawRectangle(rect, shape);
        return bitmap;
    }

    private static ShapeStyle Blend(Color from, Color to) =>
        new(from, null, 0) { Gradient = new LinearGradient(new Point(10, 0), new Point(110, 0), from, to) };

    [Fact]
    public void ARectangleWithAGradient_BlendsFromTheStartColourToTheEndColour()
    {
        using var bitmap = Paint(Blend(Red, Blue), new Rect(0, 0, 120, 40));

        var left = bitmap.GetPixel(11, 20);
        var middle = bitmap.GetPixel(60, 20);
        var right = bitmap.GetPixel(109, 20);

        Assert.True(left.Red > 240 && left.Blue < 15, $"the start of the gradient is not red: {left}");
        Assert.True(right.Blue > 240 && right.Red < 15, $"the end of the gradient is not blue: {right}");
        Assert.InRange(middle.Red, 118, 138);
        Assert.InRange(middle.Blue, 118, 138);
    }

    [Fact]
    public void BeyondEitherEnd_TheNearestColourHolds()
    {
        using var bitmap = Paint(Blend(Red, Blue), new Rect(0, 0, 120, 40));

        Assert.True(bitmap.GetPixel(2, 20).Red > 240);
        Assert.True(bitmap.GetPixel(118, 20).Blue > 240);
    }

    [Fact]
    public void AFlatFill_StaysFlat()
    {
        using var bitmap = Paint(new ShapeStyle(Red, null, 0), new Rect(0, 0, 120, 40));

        Assert.Equal(bitmap.GetPixel(10, 20), bitmap.GetPixel(100, 20));
    }

    [Fact]
    public void AGradientWithAHatch_ShowsTheHatchOverTheBlend()
    {
        var shape = Blend(Red, Blue) with { Hatch = HatchPattern.Cross, HatchColor = Colors.Black };

        using var hatched = Paint(shape, new Rect(0, 0, 120, 40));
        using var plain = Paint(Blend(Red, Blue), new Rect(0, 0, 120, 40));

        int differing = 0;
        for (int x = 0; x < 120; x++)
        {
            for (int y = 0; y < 40; y++)
            {
                if (hatched.GetPixel(x, y) != plain.GetPixel(x, y)) differing++;
            }
        }

        Assert.True(differing > 200, $"the hatch left only {differing} pixels different from the plain gradient");
        // And where the hatch does not stand the blend is still the blend.
        Assert.True(differing < 120 * 40, "the hatch covers every pixel of the blend");
    }

    private static Figure SankeyOf(SankeyLinkColorMode mode) => Plt.Create().WithSize(400, 200)
        .AddSubPlot(1, 1, 1, ax => ax.Sankey(
            [new SankeyNode("A") { Color = Red }, new SankeyNode("B") { Color = Blue }],
            [new SankeyLink(0, 1, 10)],
            s => s.LinkColorMode = mode))
        .Build();

    [Fact]
    public void ASankeyLinkInGradientMode_BlendsFromTheColourOfItsSourceToTheColourOfItsTarget_InPng()
    {
        using var bitmap = SKBitmap.Decode(SankeyOf(SankeyLinkColorMode.Gradient).ToPng());
        int y = bitmap.Height / 2;

        // The nodes are the pure-colour bars at either end of the link; the link lies between them.
        int sourceEdge = Enumerable.Range(0, bitmap.Width).Last(x => IsClose(bitmap.GetPixel(x, y), Red));
        int targetEdge = Enumerable.Range(0, bitmap.Width).First(x => IsClose(bitmap.GetPixel(x, y), Blue));
        Assert.True(targetEdge - sourceEdge > 40, $"no link between the nodes ({sourceEdge}..{targetEdge})");

        var nearSource = bitmap.GetPixel(sourceEdge + (targetEdge - sourceEdge) / 5, y);
        var middle = bitmap.GetPixel((sourceEdge + targetEdge) / 2, y);
        var nearTarget = bitmap.GetPixel(targetEdge - (targetEdge - sourceEdge) / 5, y);

        Assert.True(nearSource.Red > nearSource.Blue + 20, $"near the source the link is not reddish: {nearSource}");
        Assert.True(nearTarget.Blue > nearTarget.Red + 20, $"near the target the link is not bluish: {nearTarget}");
        Assert.True(Math.Abs(middle.Red - middle.Blue) < 40, $"in the middle the link is not a mix: {middle}");
    }

    [Fact]
    public void ASankeyLinkInGradientMode_IsDrawnDifferentlyFromOneInSourceMode_InPng()
    {
        Assert.False(SankeyOf(SankeyLinkColorMode.Gradient).ToPng().AsSpan().SequenceEqual(SankeyOf(SankeyLinkColorMode.Source).ToPng()));
    }

    [Fact]
    public void ASankeyGradient_StaysAVectorShadingInPdf()
    {
        byte[] pdf = SankeyOf(SankeyLinkColorMode.Gradient).ToPdf();

        Assert.Contains("/ShadingType 2", Encoding.Latin1.GetString(pdf), StringComparison.Ordinal);
    }

    private static bool IsClose(SKColor pixel, Color colour) =>
        Math.Abs(pixel.Red - colour.R) < 12 && Math.Abs(pixel.Green - colour.G) < 12 && Math.Abs(pixel.Blue - colour.B) < 12;
}
