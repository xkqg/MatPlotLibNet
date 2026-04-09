// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that inset axes are rendered within the parent axes.</summary>
public class InsetRenderTests
{
    /// <summary>Verifies that inset axes render without errors.</summary>
    [Fact]
    public void InsetAxes_RendersWithoutErrors()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]);

        var inset = ax.AddInset(0.6, 0.1, 0.35, 0.35);
        inset.Plot([1.5, 2.5], [4.5, 5.5]);

        string svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Verifies that inset renders series content in the SVG.</summary>
    [Fact]
    public void InsetAxes_WithSeries_RendersContent()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        var inset = ax.AddInset(0.6, 0.1, 0.35, 0.35);
        inset.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = fig.ToSvg();
        // The inset should produce additional SVG content (polyline for the line series)
        // Count polyline elements — should be at least 2 (one for parent, one for inset)
        int polylineCount = System.Text.RegularExpressions.Regex.Matches(svg, "<polyline").Count;
        Assert.True(polylineCount >= 2, $"Expected at least 2 polylines, found {polylineCount}");
    }

    /// <summary>Verifies that rendering without insets is unchanged (regression).</summary>
    [Fact]
    public void NoInsets_RenderingUnchanged()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
        Assert.Empty(ax.Insets);
    }

    /// <summary>Verifies that inset renders its own spine frame.</summary>
    [Fact]
    public void InsetAxes_HasOwnSpineFrame()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0, 2.0], [3.0, 4.0]);

        var inset = ax.AddInset(0.6, 0.1, 0.35, 0.35);
        inset.Plot([1.0, 2.0], [3.0, 4.0]);

        string svg = fig.ToSvg();
        // Should have spine groups from both parent and inset (4 each = 8 total)
        int spineCount = System.Text.RegularExpressions.Regex.Matches(svg, @"class=""spine""").Count;
        Assert.Equal(8, spineCount);
    }
}
