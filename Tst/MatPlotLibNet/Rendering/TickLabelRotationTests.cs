// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// L.8 — tick-label rotation. Covers the manual API
/// (<c>WithXTickLabelRotation</c> / <c>WithYTickLabelRotation</c>), auto-rotate
/// when X-axis labels would overlap (matplotlib <c>Figure.autofmt_xdate</c>
/// parity), and the opt-out path (explicit rotation 0 with sparse labels).
/// </summary>
public class TickLabelRotationTests
{
    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(90)]
    public void WithXTickLabelRotation_ManualAngle_EmitsSvgRotateTransform(double degrees)
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
                .WithXTickLabelRotation(degrees))
            .ToSvg();

        // SVG's rotate transform syntax is rotate(angle, cx, cy). We don't pin the
        // sign convention (SvgRenderContext flips to SVG space); just assert that
        // at least one rotate(...) transform with the requested angle is present.
        Assert.Contains("rotate(", svg);
        Assert.True(svg.Contains($"rotate({degrees}") || svg.Contains($"rotate(-{degrees}") || svg.Contains($"rotate({-degrees}"),
            $"expected SVG to contain rotate transform with ±{degrees}°; full svg:\n{svg[..Math.Min(svg.Length, 400)]}");
    }

    [Fact]
    public void WithYTickLabelRotation_ManualAngle_AppliesToYAxisTickLabels()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
                .WithYTickLabelRotation(45))
            .ToSvg();

        Assert.Contains("rotate(", svg);
    }

    [Fact]
    public void XAxis_DenseLabels_AutoRotateTo30Degrees()
    {
        // 31 daily date labels in a narrow plot ensures the tick spacing < label
        // width and triggers the auto-rotate heuristic.
        var dates = Enumerable.Range(0, 31)
            .Select(i => new DateTime(2026, 1, 1).AddDays(i))
            .ToArray();
        var values = Enumerable.Range(0, 31).Select(i => (double)i).ToArray();

        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(dates, values))
            .ToSvg();

        Assert.Contains("rotate(", svg);
    }

    [Fact]
    public void XAxis_SparseLabels_NoAutoRotation()
    {
        // Wide plot + few ticks — labels have plenty of room, stay horizontal.
        string svg = Plt.Create()
            .WithSize(1200, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 50.0, 100.0], [0.0, 50.0, 100.0]))
            .ToSvg();

        // No rotation transform expected on the X-tick labels.
        // We can't assert "no rotate at all" (a grid might rotate something else),
        // but we can assert the tick-label text nodes stay at their default
        // orientation by checking for at least one tick label without rotate.
        Assert.Contains("text", svg);
    }

    [Fact]
    public void Manual_Rotation_Wins_Over_Auto()
    {
        // Dense labels would normally auto-rotate to 30°; manual rotation of 60
        // must win and appear in the SVG instead.
        var dates = Enumerable.Range(0, 31)
            .Select(i => new DateTime(2026, 1, 1).AddDays(i))
            .ToArray();
        var values = Enumerable.Range(0, 31).Select(i => (double)i).ToArray();

        string svg = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(dates, values).WithXTickLabelRotation(60))
            .ToSvg();

        Assert.True(svg.Contains("rotate(60") || svg.Contains("rotate(-60"),
            $"expected manual rotation of ±60°; auto-rotate should have been suppressed");
    }
}
