// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The lines of a contour and of a triangle contour are drawn as wide as the theme draws a line: matplotlib 3.11.2,
/// <c>ContourSet._process_linewidths</c> gives a contour that names no <c>linewidths</c> <c>rcParams['contour.linewidth']</c> and,
/// that being <c>None</c> in both styles, <c>rcParams['lines.linewidth']</c>: 1.5 pt (2.08 px) in the default style and 1 pt
/// (1.39 px) in the classic one, at the 100 dpi the references are drawn at. <c>tricontour</c> is the same <c>ContourSet</c>. The
/// library's own themes name no line width and draw what they drew: 1.5 px for a contour, 1 px for a triangle contour.</summary>
public sealed class ContourLineWidthTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(Theme theme, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(new DataTransform(0, 5, 0, 3, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Theme = theme });
        return ctx;
    }

    private static double[] Thicknesses<TCall>(RecordingRenderContext ctx) where TCall : StrokeCall => [.. ctx.Of<TCall>().Select(call => call.Thickness)];

    private static ContourSeries Ramp()
    {
        var z = new double[4, 6];
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 6; c++)
                z[r, c] = 2.0 * c + 0.5;
        return new ContourSeries([0, 1, 2, 3, 4, 5], [0, 1, 2, 3], z) { Levels = 4 };
    }

    private static TricontourSeries Bump() => new(
        new Vec([0.0, 1.0, 0.5, 1.0, 0.0, 0.5]), new Vec([0.0, 0.0, 0.5, 1.0, 1.0, 0.25]), new Vec([1.0, 2.0, 3.0, 2.0, 1.0, 2.5]));

    /// <summary>A contour is one and a half points under the default matplotlib style and one under the classic one; the library's own theme
    /// draws it one and a half pixels wide.</summary>
    [Theory]
    [MemberData(nameof(ContourWidths))]
    public void AContour_IsTheWidthOfItsTheme(string theme, double width) =>
        Assert.All(Thicknesses<DrawLinesCall>(Render(theme.AsTheme(), c => new ContourSeriesRenderer(c).Render(Ramp()))),
            line => Assert.Equal(width, line, 9));

    /// <summary>A triangle contour is the same set: one and a half points, one point, and one pixel under the library's own theme.</summary>
    [Theory]
    [MemberData(nameof(TricontourWidths))]
    public void ATricontour_IsTheWidthOfItsTheme(string theme, double width) =>
        Assert.All(Thicknesses<DrawLineCall>(Render(theme.AsTheme(), c => new TricontourSeriesRenderer(c).Render(Bump()))),
            line => Assert.Equal(width, line, 9));

    public static IEnumerable<object[]> ContourWidths() => [["v2", 1.5.PointsInPixels], ["classic", 1.0.PointsInPixels], ["default", 1.5]];

    public static IEnumerable<object[]> TricontourWidths() => [["v2", 1.5.PointsInPixels], ["classic", 1.0.PointsInPixels], ["default", 1.0]];

    /// <summary>The control: each kind did draw lines, so the widths read above are the lines'.</summary>
    [Fact]
    public void EachKind_DrawsLines()
    {
        Assert.NotEmpty(Thicknesses<DrawLinesCall>(Render(Theme.MatplotlibV2, c => new ContourSeriesRenderer(c).Render(Ramp()))));
        Assert.NotEmpty(Thicknesses<DrawLineCall>(Render(Theme.MatplotlibV2, c => new TricontourSeriesRenderer(c).Render(Bump()))));
    }
}
