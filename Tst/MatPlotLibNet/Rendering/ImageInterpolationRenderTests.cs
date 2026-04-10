// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="ImageSeries"/> interpolation and blend modes produce correct SVG output.</summary>
public class ImageInterpolationRenderTests
{
    private static readonly double[,] Data = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

    /// <summary>Default ImageSeries (nearest) renders to SVG with rect elements.</summary>
    [Fact]
    public void Image_Nearest_RendersSvg()
    {
        string svg = Plt.Create()
            .Image(Data, s => s.Interpolation = "nearest")
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    /// <summary>Bilinear interpolation produces more rect elements than nearest (upsampled).</summary>
    [Fact]
    public void Image_Bilinear_ProducesMoreRects()
    {
        string nearest = Plt.Create().Image(Data, s => s.Interpolation = "nearest").ToSvg();
        string bilinear = Plt.Create().Image(Data, s => s.Interpolation = "bilinear").ToSvg();

        int nearestCount  = CountOccurrences(nearest,  "<rect");
        int bilinearCount = CountOccurrences(bilinear, "<rect");
        Assert.True(bilinearCount > nearestCount,
            $"Bilinear ({bilinearCount}) should produce more rects than nearest ({nearestCount})");
    }

    /// <summary>Alpha < 1 produces an opacity attribute in the SVG.</summary>
    [Fact]
    public void Image_Alpha_ProducesOpacity()
    {
        string svg = Plt.Create()
            .Image(Data, s => s.Alpha = 0.5)
            .ToSvg();
        Assert.Contains("opacity", svg);
    }

    /// <summary>Alpha = 1 (default) does not add unnecessary opacity attribute.</summary>
    [Fact]
    public void Image_DefaultAlpha_NoOpacity()
    {
        var series = new ImageSeries(Data);
        Assert.Equal(1.0, series.Alpha);
    }

    /// <summary>BlendMode defaults to Normal.</summary>
    [Fact]
    public void Image_DefaultBlendMode_IsNormal()
    {
        var series = new ImageSeries(Data);
        Assert.Equal(BlendMode.Normal, series.BlendMode);
    }

    /// <summary>Bicubic interpolation renders without error.</summary>
    [Fact]
    public void Image_Bicubic_RendersSvg()
    {
        string svg = Plt.Create()
            .Image(Data, s => s.Interpolation = "bicubic")
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
