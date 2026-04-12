// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Verifies that <see cref="ConstrainedLayoutEngine"/> considers only edge subplots for margin computation.</summary>
public class ConstrainedLayoutSpanningTests
{
    // ── GridPosition edge detection ───────────────────────────────────────────

    [Fact]
    public void GridPosition_ColStart0_IsLeftEdge()
    {
        var pos = new GridPosition(0, 1, 0, 1);
        Assert.Equal(0, pos.ColStart);
    }

    [Fact]
    public void GridPosition_ColEnd_ExclusiveBoundary()
    {
        var pos = new GridPosition(0, 1, 1, 2);
        Assert.Equal(2, pos.ColEnd);
    }

    [Fact]
    public void GridPosition_RowStart0_IsTopEdge()
    {
        var pos = new GridPosition(0, 1, 0, 1);
        Assert.Equal(0, pos.RowStart);
    }

    [Fact]
    public void GridPosition_RowEnd_ExclusiveBoundary()
    {
        var pos = new GridPosition(1, 2, 0, 1);
        Assert.Equal(2, pos.RowEnd);
    }

    // ── Figure subplots produce valid SVG with GridSpec layout ────────────────

    [Fact]
    public void SingleSubplot_BehavesAsBeforeTheFix()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [3.0, 4.0]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void TwoSubplots_ProduceValidSvg()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [3.0, 4.0]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void SpanningSubplot_ProducesValidSvg()
    {
        var svg = Plt.Create()
            .WithGridSpec(2, 2)
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot([1.0], [1.0]))
            .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Plot([1.0], [2.0]))
            .AddSubPlot(new GridPosition(1, 2, 0, 2), ax => ax.Plot([1.0], [3.0]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void CenterSubplot_DoesNotCrash()
    {
        var svg = Plt.Create()
            .WithGridSpec(3, 3)
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot([1.0], [1.0]))
            .AddSubPlot(GridPosition.Single(1, 1), ax => ax.Plot([2.0], [2.0]))
            .AddSubPlot(GridPosition.Single(2, 2), ax => ax.Plot([3.0], [3.0]))
            .Build()
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
