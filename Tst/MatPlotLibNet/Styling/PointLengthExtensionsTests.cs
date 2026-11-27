// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>The one rule that turns a length in points, the unit matplotlib states its widths and sizes in, into the pixels
/// this library draws in: 100 / 72 pixels to the point, the 100 dots per inch matplotlib draws its pictures at.</summary>
public sealed class PointLengthExtensionsTests
{
    [Theory]
    [InlineData(1.0, 1.3888888888888888)]
    [InlineData(0.8, 1.1111111111111112)]
    [InlineData(0.5, 0.6944444444444444)]
    [InlineData(0.0, 0.0)]
    public void ALengthInPoints_IsAHundredSeventySecondsOfItInPixels(double points, double pixels) =>
        Assert.Equal(pixels, points.PointsInPixels);

    [Theory]
    [InlineData(1.3888888888888888, 1.0)]
    [InlineData(1.1111111111111112, 0.8)]
    [InlineData(0.8, 0.5760000000000001)]
    [InlineData(0.0, 0.0)]
    public void ALengthInPixels_IsSeventyTwoHundredthsOfItInPoints(double pixels, double points) =>
        Assert.Equal(points, pixels.PixelsInPoints);

    [Fact]
    public void APixelPerPoint_IsAHundredOverSeventyTwo() =>
        Assert.Equal(100.0 / 72.0, PointLengthExtensions.PixelsPerPoint);
}
