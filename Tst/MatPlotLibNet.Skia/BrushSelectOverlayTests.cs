// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The WPF, Avalonia and Uno controls each drew the rubber band of a brush-select drag with the same twelve lines
/// of SkiaSharp, copied into each. It is one extension on the canvas now: a translucent fill, and a firmer outline, over
/// the rectangle between where the drag began and where the pointer is, whichever corner it began at.</summary>
public class BrushSelectOverlayTests
{
    private static SKBitmap Draw(BrushSelectState state)
    {
        var bitmap = new SKBitmap(100, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBrushSelect(state);
        return bitmap;
    }

    [Fact]
    public void TheInsideOfTheBand_IsFilledAndTheOutsideIsNot()
    {
        using var bitmap = Draw(new BrushSelectState(20, 10, 60, 50, 0));

        Assert.True(bitmap.GetPixel(40, 30).Alpha > 0);   // inside
        Assert.Equal(0, bitmap.GetPixel(80, 70).Alpha);   // outside
        Assert.Equal(0, bitmap.GetPixel(5, 5).Alpha);
    }

    [Fact]
    public void TheEdgeIsFirmerThanTheFill()
    {
        using var bitmap = Draw(new BrushSelectState(20, 10, 60, 50, 0));

        Assert.True(bitmap.GetPixel(20, 30).Alpha > bitmap.GetPixel(40, 30).Alpha);
    }

    [Fact]
    public void ADragToTheUpperLeft_DrawsTheSameBandAsOneToTheLowerRight()
    {
        using var forward = Draw(new BrushSelectState(20, 10, 60, 50, 0));
        using var backward = Draw(new BrushSelectState(60, 50, 20, 10, 0));

        for (int y = 0; y < forward.Height; y++)
        {
            for (int x = 0; x < forward.Width; x++)
            {
                Assert.Equal(forward.GetPixel(x, y), backward.GetPixel(x, y));
            }
        }
    }
}
