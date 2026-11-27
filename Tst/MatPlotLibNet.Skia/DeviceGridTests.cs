// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>A canvas whose matrix only scales (up the page and across, by a positive amount) and moves is a grid of device pixels a
/// unit can be read off and put back on, one <see cref="Skia.SkiaRenderContext.DeviceAxis"/> per direction; a canvas turned, skewed, mirrored or put in
/// perspective is not, and has none.</summary>
public sealed class DeviceGridTests
{
    [Fact]
    public void AScalingAndMovingMatrix_IsAGridHoldingThatScaleAndMove()
    {
        var grid = SkiaRenderContext.AggPixelGrid.DeviceGrid.Of(SKMatrix.CreateScaleTranslation(2f, 3f, 5f, 7f))!.Value;

        Assert.Equal(new SkiaRenderContext.DeviceAxis(2, 5), grid.Along(true));
        Assert.Equal(new SkiaRenderContext.DeviceAxis(3, 7), grid.Along(false));
    }

    [Fact]
    public void TheIdentity_IsAGridAtTheNaturalScale()
    {
        var grid = SkiaRenderContext.AggPixelGrid.DeviceGrid.Of(SKMatrix.Identity)!.Value;

        Assert.Equal(new SkiaRenderContext.DeviceAxis(1, 0), grid.Along(true));
        Assert.Equal(new SkiaRenderContext.DeviceAxis(1, 0), grid.Along(false));
    }

    [Fact]
    public void AUnitIsReadOffTheGrid_AndPutBackOnIt()
    {
        var grid = SkiaRenderContext.AggPixelGrid.DeviceGrid.Of(SKMatrix.CreateScaleTranslation(2f, 4f, 10f, 20f))!.Value;

        Assert.Equal(new Point(16, 36), grid.Device(new Point(3, 4)));
        Assert.Equal(new Point(3, 4), grid.Unit(new Point(16, 36)));
        Assert.Equal(new Point(6, 16), grid.Stretched(new Point(3, 4)));
        Assert.Equal(2, grid.PageScale);
    }

    /// <summary>A unit put on the whole device pixel nearest to it (half up, matplotlib's <c>mpl_round</c>), plus the offset onto the
    /// grid, in device pixels; then read back.</summary>
    [Fact]
    public void AUnitIsRoundedOntoTheGrid_AndReadBack()
    {
        var grid = SkiaRenderContext.AggPixelGrid.DeviceGrid.Of(SKMatrix.CreateScaleTranslation(2f, 4f, 10f, 20f))!.Value;

        Assert.Equal(new Point(17.5, 36.5), grid.OnGrid(new Point(3.3, 4.1), 0.5));
        Assert.Equal(new Point(3.75, 4.125), grid.Snapped(new Point(3.3, 4.1), 0.5));
        Assert.Equal(new Point(17, 36), grid.Rounded(new Point(3.3, 4.1)));
    }

    [Fact]
    public void TheScaleAlongADirection_IsTheScaleAcrossAlongX_AndTheScaleDownAlongY_AndAcrossIsTheOtherOne()
    {
        var grid = SkiaRenderContext.AggPixelGrid.DeviceGrid.Of(SKMatrix.CreateScale(2f, 3f))!.Value;

        Assert.Equal(2, grid.Along(true).Scale);
        Assert.Equal(3, grid.Along(false).Scale);
        Assert.Equal(3, grid.Across(true).Scale);
        Assert.Equal(2, grid.Across(false).Scale);
    }

    public static TheoryData<string, SKMatrix> MatricesThatAreNoGrid()
    {
        var skewX = SKMatrix.Identity; skewX.SkewX = 0.1f;
        var skewY = SKMatrix.Identity; skewY.SkewY = 0.1f;
        var persp0 = SKMatrix.Identity; persp0.Persp0 = 0.001f;
        var persp1 = SKMatrix.Identity; persp1.Persp1 = 0.001f;
        return new TheoryData<string, SKMatrix>
        {
            { "turned", SKMatrix.CreateRotationDegrees(90) },
            { "skewed across", skewX },
            { "skewed down", skewY },
            { "perspective across", persp0 },
            { "perspective down", persp1 },
            { "mirrored across", SKMatrix.CreateScale(-1f, 1f) },
            { "mirrored down", SKMatrix.CreateScale(1f, -1f) },
            { "no width", SKMatrix.CreateScale(0f, 1f) },
            { "no height", SKMatrix.CreateScale(1f, 0f) },
        };
    }

    [Theory]
    [MemberData(nameof(MatricesThatAreNoGrid))]
    public void AMatrixThatDoesMoreThanScaleAndMove_IsNoGrid(string why, SKMatrix matrix)
    {
        Assert.True(SkiaRenderContext.AggPixelGrid.DeviceGrid.Of(matrix) is null, why);
    }
}
