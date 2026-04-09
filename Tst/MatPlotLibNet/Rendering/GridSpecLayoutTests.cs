// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that <see cref="ChartRenderer.ComputeSubPlotLayout"/> handles
/// GridSpec ratios and GridPosition spanning correctly.</summary>
public class GridSpecLayoutTests
{
    private static readonly double PlotAreaTop = 70; // MarginTop(40) + TitleHeight(30)

    /// <summary>Verifies that GridSpec with null ratios produces equal-sized cells matching legacy behavior.</summary>
    [Fact]
    public void ComputeLayout_EqualRatios_MatchesLegacy()
    {
        // 800x600 figure, 2x2 grid, default spacing (60L,20R,40T,50B,40Hgap,40Vgap)
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 2 };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, GridPosition.Single(0, 0));
        fig.AddSubPlot(gs, GridPosition.Single(0, 1));
        fig.AddSubPlot(gs, GridPosition.Single(1, 0));
        fig.AddSubPlot(gs, GridPosition.Single(1, 1));

        var legacyFig = new Figure { Width = 800, Height = 600 };
        legacyFig.AddSubPlot(2, 2, 1);
        legacyFig.AddSubPlot(2, 2, 2);
        legacyFig.AddSubPlot(2, 2, 3);
        legacyFig.AddSubPlot(2, 2, 4);

        var renderer = new ChartRenderer();
        var gsAreas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);
        var legacyAreas = renderer.ComputeSubPlotLayout(legacyFig, PlotAreaTop);

        Assert.Equal(4, gsAreas.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(legacyAreas[i].X, gsAreas[i].X, 0.01);
            Assert.Equal(legacyAreas[i].Y, gsAreas[i].Y, 0.01);
            Assert.Equal(legacyAreas[i].Width, gsAreas[i].Width, 0.01);
            Assert.Equal(legacyAreas[i].Height, gsAreas[i].Height, 0.01);
        }
    }

    /// <summary>Verifies that width ratios [1, 2] produce columns with 1:2 width ratio.</summary>
    [Fact]
    public void ComputeLayout_WidthRatios_ProducesUnequalWidths()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 1, Cols = 2, WidthRatios = [1, 2] };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, GridPosition.Single(0, 0));
        fig.AddSubPlot(gs, GridPosition.Single(0, 1));

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        // Total width = 800 - 60 - 20 = 720, gap = 40 -> available = 680
        // Ratio 1:2 -> col0 = 680/3 ≈ 226.67, col1 = 680*2/3 ≈ 453.33
        Assert.Equal(2, areas.Count);
        Assert.Equal(2.0, areas[1].Width / areas[0].Width, 0.01);
    }

    /// <summary>Verifies that height ratios [1, 3] produce rows with 1:3 height ratio.</summary>
    [Fact]
    public void ComputeLayout_HeightRatios_ProducesUnequalHeights()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 1, HeightRatios = [1, 3] };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, GridPosition.Single(0, 0));
        fig.AddSubPlot(gs, GridPosition.Single(1, 0));

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        Assert.Equal(2, areas.Count);
        Assert.Equal(3.0, areas[1].Height / areas[0].Height, 0.01);
    }

    /// <summary>Verifies that both ratios produce correctly sized rectangles.</summary>
    [Fact]
    public void ComputeLayout_BothRatios_ProducesCorrectRects()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 2, WidthRatios = [1, 3], HeightRatios = [1, 2] };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, GridPosition.Single(0, 0));
        fig.AddSubPlot(gs, GridPosition.Single(0, 1));
        fig.AddSubPlot(gs, GridPosition.Single(1, 0));
        fig.AddSubPlot(gs, GridPosition.Single(1, 1));

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        // Width ratio 1:3, height ratio 1:2
        Assert.Equal(3.0, areas[1].Width / areas[0].Width, 0.01);
        Assert.Equal(2.0, areas[2].Height / areas[0].Height, 0.01);
    }

    /// <summary>Verifies that an axes spanning two columns gets a wide rect including the gap.</summary>
    [Fact]
    public void ComputeLayout_SpanningTwoColumns_ProducesWideRect()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 3 };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, new GridPosition(0, 1, 0, 2)); // top row, cols 0-1
        fig.AddSubPlot(gs, GridPosition.Single(0, 2));       // top row, col 2
        fig.AddSubPlot(gs, new GridPosition(1, 2, 0, 3)); // bottom row, all cols

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        // Spanning cols 0-1 should be ~2x single col width + 1 gap
        var singleColWidth = areas[1].Width;
        var spanWidth = areas[0].Width;
        Assert.Equal(spanWidth, singleColWidth * 2 + fig.Spacing.HorizontalGap, 0.01);
    }

    /// <summary>Verifies that an axes spanning two rows gets a tall rect including the gap.</summary>
    [Fact]
    public void ComputeLayout_SpanningTwoRows_ProducesTallRect()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 3, Cols = 1 };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, new GridPosition(0, 2, 0, 1)); // rows 0-1
        fig.AddSubPlot(gs, GridPosition.Single(2, 0));       // row 2

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        var singleRowHeight = areas[1].Height;
        var spanHeight = areas[0].Height;
        Assert.Equal(spanHeight, singleRowHeight * 2 + fig.Spacing.VerticalGap, 0.01);
    }

    /// <summary>Verifies that an axes spanning all columns fills the available width.</summary>
    [Fact]
    public void ComputeLayout_SpanningFullWidth_MatchesFigureWidth()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 3 };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, new GridPosition(0, 1, 0, 3)); // full width
        fig.AddSubPlot(gs, new GridPosition(1, 2, 0, 3)); // full width

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        double expectedWidth = fig.Width - fig.Spacing.MarginLeft - fig.Spacing.MarginRight;
        Assert.Equal(expectedWidth, areas[0].Width, 0.01);
        Assert.Equal(expectedWidth, areas[1].Width, 0.01);
    }

    /// <summary>Verifies that without a GridSpec the legacy path is used unchanged.</summary>
    [Fact]
    public void ComputeLayout_NoGridSpec_UsesLegacyPath()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        fig.AddSubPlot(2, 2, 1);
        fig.AddSubPlot(2, 2, 2);

        Assert.Null(fig.GridSpec);

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        Assert.Equal(2, areas.Count);
        // Equal widths in legacy mode
        Assert.Equal(areas[0].Width, areas[1].Width, 0.01);
    }

    /// <summary>Verifies that width ratios with spanning produce correct results.</summary>
    [Fact]
    public void ComputeLayout_WidthRatios_WithSpanning_CorrectWidth()
    {
        var fig = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 1, Cols = 3, WidthRatios = [1, 2, 1] };
        fig.GridSpec = gs;
        fig.AddSubPlot(gs, new GridPosition(0, 1, 0, 2)); // span cols 0+1 (ratios 1+2=3)
        fig.AddSubPlot(gs, GridPosition.Single(0, 2));       // col 2 (ratio 1)

        var renderer = new ChartRenderer();
        var areas = renderer.ComputeSubPlotLayout(fig, PlotAreaTop);

        // Span covers ratio sum 3, single covers ratio 1 -> span = 3x single width + gap
        // Total available = 800-60-20-2*40 = 640, ratio sum 4 -> unit = 160
        // col0 = 160, col1 = 320, col2 = 160
        // span(0,1) = 160 + 40(gap) + 320 = 520
        // single(2) = 160
        Assert.Equal(520, areas[0].Width, 0.01);
        Assert.Equal(160, areas[1].Width, 0.01);
    }
}
