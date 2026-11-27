// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="DataTransform"/> behavior.</summary>
public class DataTransformTests
{
    [Theory]
    [InlineData(100, 50, 600, 400, 100, 0, 0, 100, 450)] // DataToPixel_MapsOriginCorrectly
    [InlineData(100, 50, 600, 400, 100, 10, 100, 700, 50)] // DataToPixel_MapsMaxCorrectly
    [InlineData(0, 0, 200, 100, 10, 5, 5, 100, 50)] // DataToPixel_MapsMidpointCorrectly
    public void DataToPixel_MapsADataPointToThePixelItStandsAt(int left, int top, int width, int height, int yMax, int dataX, int dataY, int pixelX, int pixelY)
    {
        var plotBounds = new Rect(left, top, width, height);
        var transform = new DataTransform(0, 10, 0, yMax, plotBounds);

        var pixel = transform.DataToPixel(dataX, dataY);
        Assert.Equal(pixelX, pixel.X);
        Assert.Equal(pixelY, pixel.Y); // bottom edge (Y inverted)
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

    /// <summary>On a log axis the pixel mapping works in log10 space; PixelToData inverts the scale, so it returns the
    /// data value, not its logarithm.</summary>
    [Fact]
    public void PixelToData_OnLogAxes_ReturnsTheDataValue()
    {
        var transform = new Rect(100, 50, 600, 400).LogLog(1000, 10000);

        var (x, y) = transform.PixelToData(transform.DataToPixel(50, 300));

        Assert.Equal(50, x, 9);
        Assert.Equal(300, y, 9);
    }

    /// <summary>On a symlog axis PixelToData inverts the symlog transform on both sides of zero and inside the linear
    /// band.</summary>
    [Theory]
    [InlineData(-400.0)]
    [InlineData(-0.5)]
    [InlineData(0.25)]
    [InlineData(60.0)]
    public void PixelToData_OnASymlogAxis_ReturnsTheDataValue(double value)
    {
        const double LinThresh = 2.0;
        var transform = new DataTransform(
            SymlogTransform.Forward(-1000, LinThresh), SymlogTransform.Forward(1000, LinThresh),
            SymlogTransform.Forward(-1000, LinThresh), SymlogTransform.Forward(1000, LinThresh),
            new Rect(0, 0, 500, 500), null, null, -1000, 1000, -1000, 1000,
            AxisScale.SymLog, AxisScale.SymLog, LinThresh, LinThresh);

        var (x, y) = transform.PixelToData(transform.DataToPixel(value, value));

        Assert.Equal(value, x, 9);
        Assert.Equal(value, y, 9);
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

    /// <summary>A transform is made for every axes on every draw, so it carries what each direction places by and no more: the two
    /// directions, where each range ends, the range each shows and the plot (208 bytes of fields, 224 with the object's header on a
    /// 64-bit runtime, the size it had before its directions became a pair). The edge an axis begins at is worked out when a cut asks
    /// for it, not held: almost no axis is ever cut there.</summary>
    [Fact]
    public void ATransform_HoldsNoMoreThanItsDirectionsNeed()
    {
        var plot = new Rect(10, 20, 300, 200);
        _ = new DataTransform(0, 10, 0, 10, plot);

        long before = GC.GetAllocatedBytesForCurrentThread();
        _ = new DataTransform(0, 10, 0, 10, plot);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated <= 224, $"a transform took {allocated} bytes");
    }
}
