// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>Phase Q Wave 2 (2026-04-19) — exercises FigureBuilder convenience shortcuts that
/// route through <c>EnsureDefaultAxes()</c> (Treemap / Sunburst / Sankey / PolarPlot / PolarScatter /
/// PolarBar / Wireframe / Scatter3D / AxVLine / AxHSpan). These are the top-level
/// FigureBuilder methods that bypass an explicit AddSubPlot call — they were unhit because
/// every existing test uses the AddSubPlot route. Lifts FigureBuilder from 93.4/89.5 toward 90/90.</summary>
public class FigureBuilderConvenienceTests
{
    [Fact] public void Figure_Treemap_AddsTreemapSeries()
    {
        var root = new TreeNode { Label = "Root", Children = [new() { Label = "A", Value = 10 }] };
        var fig = Plt.Create().Treemap(root).Build();
        Assert.Single(fig.SubPlots);
        Assert.IsType<TreemapSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_Sunburst_AddsSunburstSeries()
    {
        var root = new TreeNode { Label = "Root", Children = [new() { Label = "A", Value = 10 }] };
        var fig = Plt.Create().Sunburst(root).Build();
        Assert.IsType<SunburstSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_Sankey_AddsSankeySeries()
    {
        var fig = Plt.Create().Sankey([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 10)]).Build();
        Assert.IsType<SankeySeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_PolarPlot_AddsPolarLineSeries()
    {
        var fig = Plt.Create().PolarPlot([1.0, 2.0], [0.0, 1.57]).Build();
        Assert.IsType<PolarLineSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_PolarScatter_AddsPolarScatterSeries()
    {
        var fig = Plt.Create().PolarScatter([1.0, 2.0], [0.0, 1.57]).Build();
        Assert.IsType<PolarScatterSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_PolarBar_AddsPolarBarSeries()
    {
        var fig = Plt.Create().PolarBar([5.0, 10.0], [0.0, 1.57]).Build();
        Assert.IsType<PolarBarSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_Wireframe_AddsWireframeSeries()
    {
        var fig = Plt.Create().Wireframe([1.0, 2.0], [1.0, 2.0],
            new double[,] { { 1, 2 }, { 3, 4 } }).Build();
        Assert.IsType<WireframeSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_Scatter3D_AddsScatter3DSeries()
    {
        var fig = Plt.Create().Scatter3D(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }).Build();
        Assert.IsType<Scatter3DSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact] public void Figure_AxVLine_AddsReferenceLine()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).AxVLine(1.5).Build();
        Assert.NotEmpty(fig.SubPlots[0].ReferenceLines);
    }

    [Fact] public void Figure_AxHSpan_AddsSpan()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).AxHSpan(1.0, 2.0).Build();
        Assert.NotEmpty(fig.SubPlots[0].Spans);
    }

    [Fact] public void Figure_PlotDateTime_TwiceOnDateAxis_HitsAlreadySetBranch()
    {
        // FigureBuilder line 110: `if (axes.XAxis.Scale == AxisScale.Date) return;` —
        // the second Plot call on the same axes should hit the early-return branch.
        DateTime[] d1 = [new(2026, 1, 1), new(2026, 2, 1)];
        DateTime[] d2 = [new(2026, 3, 1), new(2026, 4, 1)];
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot(d1, [1.0, 2.0]).Plot(d2, [3.0, 4.0]))
            .Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }
}
