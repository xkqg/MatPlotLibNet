// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule a mark that stands on a base draws by: a base the axis' scale cannot place (0 or a negative value
/// on a log axis) is clipped to the edge where the axis begins, as matplotlib clips it (<c>nonpositive='clip'</c>). A
/// missing value or an infinity is not clipped: it stays NaN and the mark is skipped, as a line or a marker that cannot be
/// placed is masked. A value the scale can place is exactly where <see cref="DataTransform.DataToPixel"/> puts it.</summary>
public class DataTransformClipTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>A log Y axis from 1 (the bottom edge, 300 px) to 100 (the top edge, 0 px).</summary>
    private static DataTransform LogY() =>
        new(0, 10, 0, 2, Bounds, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);

    /// <summary>The same log Y axis set high to low: it begins (at 1) on the top edge.</summary>
    private static DataTransform LogYReversed() =>
        new(0, 10, 2, 0, Bounds, null, null, 0, 10, 100, 1, AxisScale.Linear, AxisScale.Log, 1, 1);

    /// <summary>A log X axis from 1 (the left edge, 0 px) to 100 (the right edge, 400 px).</summary>
    private static DataTransform LogX() =>
        new(0, 2, 0, 10, Bounds, null, null, 1, 100, 0, 10, AxisScale.Log, AxisScale.Linear, 1, 1);

    /// <summary>The same log X axis set high to low: it begins (at 1) on the right edge.</summary>
    private static DataTransform LogXReversed() =>
        new(2, 0, 0, 10, Bounds, null, null, 100, 1, 0, 10, AxisScale.Log, AxisScale.Linear, 1, 1);

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(double.MinValue)]
    public void ABaseTheLogYScaleCannotPlace_IsClippedToTheBottomEdge(double y)
    {
        var transform = LogY();

        var clipped = transform.DataToPixelClippedY(3, y);

        Assert.Equal(new Point(transform.DataToPixel(3, 10).X, 300), clipped);
    }

    [Fact]
    public void ANegativeZeroBase_IsClippedAsZeroIs() => Assert.Equal(300, LogY().DataToPixelClippedY(3, -0.0).Y);

    [Fact]
    public void OnAReversedLogYAxis_ABaseItCannotPlace_IsClippedToTheTopEdge_WhereTheAxisBegins() =>
        Assert.Equal(0, LogYReversed().DataToPixelClippedY(3, 0).Y);

    [Fact]
    public void ABaseTheLogXScaleCannotPlace_IsClippedToTheLeftEdge()
    {
        var transform = LogX();

        Assert.Equal(new Point(0, transform.DataToPixel(10, 4).Y), transform.DataToPixelClippedX(0, 4));
    }

    [Fact]
    public void OnAReversedLogXAxis_ABaseItCannotPlace_IsClippedToTheRightEdge_WhereTheAxisBegins() =>
        Assert.Equal(400, LogXReversed().DataToPixelClippedX(-1, 4).X);

    /// <summary>Only the base is clipped: the other coordinate is mapped as ever, so an X the axis cannot place stays NaN.</summary>
    [Fact]
    public void OnlyTheBaseCoordinateIsClipped()
    {
        Assert.True(double.IsNaN(LogX().DataToPixelClippedY(0, 4).X));
        Assert.True(double.IsNaN(LogY().DataToPixelClippedX(3, 0).Y));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(10.0)]
    [InlineData(0.5)]     // placeable, below the range: beyond the edge, as DataToPixel puts it
    [InlineData(1000.0)]
    public void ABaseTheScaleCanPlace_IsWhereDataToPixelPutsIt(double value)
    {
        Assert.Equal(LogY().DataToPixel(3, value), LogY().DataToPixelClippedY(3, value));
        Assert.Equal(LogX().DataToPixel(value, 4), LogX().DataToPixelClippedX(value, 4));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AMissingOrInfiniteBase_IsNotClipped_ItCannotBeDrawn(double value)
    {
        Assert.False(double.IsFinite(LogY().DataToPixelClippedY(3, value).Y));
        Assert.False(double.IsFinite(LogX().DataToPixelClippedX(value, 4).X));
    }

    /// <summary>A linear axis places every finite value: a negative base is drawn where it lies.</summary>
    [Fact]
    public void OnALinearAxis_ANegativeBase_IsWhereDataToPixelPutsIt()
    {
        var linear = new DataTransform(0, 10, -10, 10, Bounds);

        Assert.Equal(linear.DataToPixel(3, -5), linear.DataToPixelClippedY(3, -5));
        Assert.Equal(linear.DataToPixel(-5, 3), linear.DataToPixelClippedX(-5, 3));
    }

    [Fact]
    public void TheBatchTransform_ClipsEachBaseTheScaleCannotPlace_AndLeavesTheRest()
    {
        var transform = LogY();
        double[] bases = [10, 0, -1, double.NaN, 100];

        double[] pixels = transform.TransformYClipped(bases);

        Assert.Equal(transform.TransformY(bases)[0], pixels[0]);
        Assert.Equal(300, pixels[1]);
        Assert.Equal(300, pixels[2]);
        Assert.True(double.IsNaN(pixels[3]));
        Assert.Equal(transform.TransformY(bases)[4], pixels[4]);
    }

    /// <summary>On a linear axis the batch is the plain batch, bit for bit (the SIMD path): no figure on a linear axis
    /// changes a byte.</summary>
    [Fact]
    public void OnALinearAxis_TheBatchTransform_IsThePlainOne()
    {
        var linear = new DataTransform(0, 10, -10, 10, Bounds);
        double[] bases = [-7.3, 0, 0.1, 9.99, double.NaN];

        Assert.Equal(linear.TransformY(bases), linear.TransformYClipped(bases));
    }
}
