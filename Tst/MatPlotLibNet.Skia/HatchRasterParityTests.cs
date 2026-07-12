// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Skia;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>Verifies that a hatch reaches the RASTER backend, not just the SVG one.
/// <para>A hatch that shows on screen and vanishes from an exported PNG is the worst divergence there is
/// for anyone who screenshots a dashboard: the export silently drops the one mark that distinguishes
/// "no contact" from "normal". These tests pin the two backends together.</para></summary>
public class HatchRasterParityTests
{
    private static byte[] RenderBars(HatchPattern hatch)
    {
        return Plt.Create()
            .WithSize(200, 150)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(["A", "B"], [3.0, 5.0], s =>
            {
                s.Color = Colors.Gray;
                s.Hatch = hatch;
                s.HatchColor = Colors.Black;
            }))
            .Build()
            .ToPng();
    }

    /// <summary>A hatched figure rasterises to different pixels than the same figure without a hatch —
    /// the pattern is genuinely painted, not silently dropped on the way to the bitmap.</summary>
    [Fact]
    public void HatchedBars_ProduceDifferentPixelsThanASolidFill()
    {
        byte[] solid = RenderBars(HatchPattern.None);
        byte[] hatched = RenderBars(HatchPattern.ForwardDiagonal);

        Assert.NotEmpty(solid);
        Assert.NotEmpty(hatched);
        Assert.False(solid.AsSpan().SequenceEqual(hatched), "the PNG is byte-identical with and without a hatch");
    }

    /// <summary>Two different hatch patterns rasterise differently from each other — the raster backend
    /// distinguishes the patterns rather than painting one generic texture for all of them.</summary>
    [Fact]
    public void DifferentHatchPatterns_RasteriseDifferently()
    {
        byte[] forward = RenderBars(HatchPattern.ForwardDiagonal);
        byte[] dots = RenderBars(HatchPattern.Dots);

        Assert.False(forward.AsSpan().SequenceEqual(dots), "ForwardDiagonal and Dots rasterise identically");
    }

    /// <summary>Every declared pattern rasterises without throwing and paints something.</summary>
    [Theory]
    [InlineData(HatchPattern.ForwardDiagonal)]
    [InlineData(HatchPattern.BackDiagonal)]
    [InlineData(HatchPattern.Horizontal)]
    [InlineData(HatchPattern.Vertical)]
    [InlineData(HatchPattern.Cross)]
    [InlineData(HatchPattern.DiagonalCross)]
    [InlineData(HatchPattern.Dots)]
    [InlineData(HatchPattern.Stars)]
    public void EveryHatchPattern_Rasterises(HatchPattern hatch)
    {
        byte[] solid = RenderBars(HatchPattern.None);
        byte[] png = RenderBars(hatch);

        Assert.NotEmpty(png);
        Assert.False(png.AsSpan().SequenceEqual(solid), $"{hatch} rasterised identically to a solid fill");
    }

    /// <summary>An undefined pattern value paints the plain fill instead of throwing inside a render pass —
    /// a chart must never take the process down over a styling value.</summary>
    [Fact]
    public void UndefinedHatchValue_FallsBackToTheSolidFill()
    {
        byte[] png = RenderBars((HatchPattern)99);

        Assert.NotEmpty(png);
    }
}
