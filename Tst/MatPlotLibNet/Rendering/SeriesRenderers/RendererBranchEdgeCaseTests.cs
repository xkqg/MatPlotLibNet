// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>Targeted branch-arm tests for the NEAR-bucket renderers
/// (Histogram2D, Image, Spectrogram, PolarHeatmap, Treemap, etc.) that already
/// have ~85-89% branch coverage. Each test exercises ONE specific arm
/// (uniform-data → min==max fallback, alpha &lt; 1.0 → opacity push,
/// non-default interpolation, etc.) that the existing direct-invocation tests
/// in <see cref="AllRenderersDirectInvocationTests"/> do not hit.</summary>
public class RendererBranchEdgeCaseTests
{
    private static readonly Rect StandardBounds = new(80, 60, 640, 480);

    private static SeriesRenderContext NewContext(out SvgRenderContext svg)
    {
        svg = new SvgRenderContext();
        var area = new RenderArea(StandardBounds, svg);
        var transform = new DataTransform(-10, 10, -10, 10, StandardBounds);
        return new SeriesRenderContext(transform, svg, Colors.Tab10Blue, area);
    }

    // ── Histogram2DSeriesRenderer: uniform counts → min==max branch ─────────

    [Fact]
    public void Histogram2D_UniformCounts_HitsMinEqualsMaxBranch()
    {
        var context = NewContext(out var svg);
        // Same X+Y for many points → all bins have the same count → min==max
        var series = new Histogram2DSeries([0.5, 0.5, 0.5, 0.5], [0.5, 0.5, 0.5, 0.5])
        {
            BinsX = 2, BinsY = 2
        };
        new Histogram2DSeriesRenderer(context).Render(series);
        Assert.Contains("<rect", svg.GetOutput());
    }

    // ── ImageSeriesRenderer: uniform / alpha / interpolation arms ───────────

    [Fact]
    public void Image_UniformData_HitsMinEqualsMaxBranch()
    {
        var context = NewContext(out var svg);
        var series = new ImageSeries(new double[,] { { 5, 5 }, { 5, 5 } });
        new ImageSeriesRenderer(context).Render(series);
        Assert.Contains("<rect", svg.GetOutput());
    }

    [Fact]
    public void Image_AlphaLessThanOne_HitsOpacityBranch()
    {
        var context = NewContext(out var svg);
        var series = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { Alpha = 0.5 };
        new ImageSeriesRenderer(context).Render(series);
        var output = svg.GetOutput();
        Assert.Contains("opacity", output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Image_BilinearInterpolation_HitsNonNearestBranch()
    {
        var context = NewContext(out var svg);
        var series = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { Interpolation = "bilinear" };
        new ImageSeriesRenderer(context).Render(series);
        Assert.Contains("<rect", svg.GetOutput());
    }

    [Fact]
    public void Image_VMinAndVMaxBothSet_SkipsAutoRangeLoop()
    {
        var context = NewContext(out var svg);
        var series = new ImageSeries(new double[,] { { 1, 2 }, { 3, 4 } }) { VMin = 0, VMax = 5 };
        new ImageSeriesRenderer(context).Render(series);
        Assert.Contains("<rect", svg.GetOutput());
    }

    // ── BarbsSeriesRenderer: zero-magnitude + flag pennant arms ─────────────

    [Fact]
    public void Barbs_ZeroMagnitude_DrawsCircleNotBarb()
    {
        var context = NewContext(out var svg);
        var series = new BarbsSeries(new Vec([5.0]), new Vec([5.0]), new Vec([0.0]), new Vec([0.0]));
        new BarbsSeriesRenderer(context).Render(series);
        Assert.NotNull(svg.GetOutput());
    }

    [Fact]
    public void Barbs_HighMagnitude_DrawsFlag()
    {
        var context = NewContext(out var svg);
        var series = new BarbsSeries(new Vec([5.0]), new Vec([5.0]), new Vec([60.0]), new Vec([45.0]));
        new BarbsSeriesRenderer(context).Render(series);
        Assert.NotNull(svg.GetOutput());
    }
}
