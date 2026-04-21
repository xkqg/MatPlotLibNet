// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="EventplotSeries"/> rendering.</summary>
public class EventplotSeriesRenderTests
{
    private static readonly double[][] Positions = [[1.0, 2.0, 3.0], [4.0, 5.0]];

    [Fact]
    public void Eventplot_SvgContainsLines()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Eventplot_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Eventplot(Positions)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── Branch coverage ──────────────────────────────────────────────────────

    [Fact]
    public void Eventplot_EmptyPositions_ReturnsDefaultDataRange()
    {
        // EventplotSeries.ComputeDataRange: Positions.Length == 0 branch
        // EventplotSeriesRenderer.Render: Positions.Length == 0 early-return branch
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot([]))
            .ToSvg();
        Assert.StartsWith("<svg", svg);
    }

    [Fact]
    public void Eventplot_EmptyInnerArrays_AllPosEmptyBranch()
    {
        // EventplotSeries.ComputeDataRange: allPos.Length == 0 ternary false-branches
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot([[]]))
            .ToSvg();
        Assert.StartsWith("<svg", svg);
    }

    [Fact]
    public void Eventplot_WithColors_UsesPerRowColor()
    {
        // EventplotSeriesRenderer: series.Colors is not null true-branch
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions, s =>
            {
                s.Colors = [Colors.Red, Colors.Blue];
            }))
            .ToSvg();
        Assert.Contains("<line", svg);
    }

    [Fact]
    public void Eventplot_MoreRowsThanColors_FallsBackForExtraRows()
    {
        // EventplotSeriesRenderer: i >= series.Colors.Length false-branch
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions, s =>
            {
                s.Colors = [Colors.Red];  // 1 color for 2 rows
            }))
            .ToSvg();
        Assert.Contains("<line", svg);
    }
}
