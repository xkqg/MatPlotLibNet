// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="DataTransform"/> behavior.</summary>
public class DataTransformTests
{
    /// <summary>Verifies that the data origin maps to the bottom-left pixel of the plot bounds.</summary>
    [Fact]
    public void DataToPixel_MapsOriginCorrectly()
    {
        var plotBounds = new Rect(100, 50, 600, 400);
        var transform = new DataTransform(0, 10, 0, 100, plotBounds);

        var pixel = transform.DataToPixel(0, 0);
        Assert.Equal(100, pixel.X);
        Assert.Equal(450, pixel.Y); // bottom edge (Y inverted)
    }

    /// <summary>Verifies that the data maximum maps to the top-right pixel of the plot bounds.</summary>
    [Fact]
    public void DataToPixel_MapsMaxCorrectly()
    {
        var plotBounds = new Rect(100, 50, 600, 400);
        var transform = new DataTransform(0, 10, 0, 100, plotBounds);

        var pixel = transform.DataToPixel(10, 100);
        Assert.Equal(700, pixel.X);
        Assert.Equal(50, pixel.Y);
    }

    /// <summary>Verifies that the data midpoint maps to the center pixel of the plot bounds.</summary>
    [Fact]
    public void DataToPixel_MapsMidpointCorrectly()
    {
        var plotBounds = new Rect(0, 0, 200, 100);
        var transform = new DataTransform(0, 10, 0, 10, plotBounds);

        var pixel = transform.DataToPixel(5, 5);
        Assert.Equal(100, pixel.X);
        Assert.Equal(50, pixel.Y);
    }

    /// <summary>Verifies that PixelToData is the inverse of DataToPixel for a round-trip conversion.</summary>
    [Fact]
    public void PixelToData_InvertsDataToPixel()
    {
        var plotBounds = new Rect(100, 50, 600, 400);
        var transform = new DataTransform(0, 10, 0, 100, plotBounds);

        var pixel = transform.DataToPixel(5, 50);
        var (dx, dy) = transform.PixelToData(pixel);
        Assert.Equal(5, dx, 6);
        Assert.Equal(50, dy, 6);
    }

    /// <summary>Verifies that a negative data range correctly maps the origin to the center pixel.</summary>
    [Fact]
    public void NegativeDataRange_WorksCorrectly()
    {
        var plotBounds = new Rect(0, 0, 100, 100);
        var transform = new DataTransform(-5, 5, -10, 10, plotBounds);

        var origin = transform.DataToPixel(0, 0);
        Assert.Equal(50, origin.X);
        Assert.Equal(50, origin.Y);
    }
}
