// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Verifies <see cref="AddTrendlineEvent"/>, <see cref="AddHorizontalLevelEvent"/>,
/// <see cref="AddFibonacciRetracementEvent"/>, and <see cref="RemoveDrawingToolEvent"/>.</summary>
public class DrawingToolEventTests
{
    private static Figure MakeFigure()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        return fig;
    }

    [Fact]
    public void AddTrendlineEvent_ApplyTo_AppendsTrendline()
    {
        var fig = MakeFigure();
        var tool = new Trendline(0.0, 10.0, 5.0, 20.0);
        var evt = new AddTrendlineEvent("chart-1", AxesIndex: 0, tool);

        evt.ApplyTo(fig);

        Assert.Single(fig.SubPlots[0].Trendlines);
        Assert.Same(tool, fig.SubPlots[0].Trendlines[0]);
    }

    [Fact]
    public void AddHorizontalLevelEvent_ApplyTo_AppendsLevel()
    {
        var fig = MakeFigure();
        var tool = new HorizontalLevel(150.0);
        var evt = new AddHorizontalLevelEvent("chart-1", AxesIndex: 0, tool);

        evt.ApplyTo(fig);

        Assert.Single(fig.SubPlots[0].HorizontalLevels);
        Assert.Same(tool, fig.SubPlots[0].HorizontalLevels[0]);
    }

    [Fact]
    public void AddFibonacciRetracementEvent_ApplyTo_AppendsFib()
    {
        var fig = MakeFigure();
        var tool = new FibonacciRetracement(200.0, 100.0);
        var evt = new AddFibonacciRetracementEvent("chart-1", AxesIndex: 0, tool);

        evt.ApplyTo(fig);

        Assert.Single(fig.SubPlots[0].FibonacciRetracements);
        Assert.Same(tool, fig.SubPlots[0].FibonacciRetracements[0]);
    }

    [Fact]
    public void RemoveDrawingToolEvent_ApplyTo_RemovesTrendline()
    {
        var fig = MakeFigure();
        var tool = new Trendline(0.0, 10.0, 5.0, 20.0);
        fig.SubPlots[0].AddTrendline(tool.X1, tool.Y1, tool.X2, tool.Y2);
        var actual = fig.SubPlots[0].Trendlines[0];
        var evt = new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, actual);

        evt.ApplyTo(fig);

        Assert.Empty(fig.SubPlots[0].Trendlines);
    }

    [Fact]
    public void RemoveDrawingToolEvent_ApplyTo_RemovesHorizontalLevel()
    {
        var fig = MakeFigure();
        var tool = fig.SubPlots[0].AddLevel(100.0);
        var evt = new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, tool);

        evt.ApplyTo(fig);

        Assert.Empty(fig.SubPlots[0].HorizontalLevels);
    }

    [Fact]
    public void RemoveDrawingToolEvent_ApplyTo_RemovesFibonacci()
    {
        var fig = MakeFigure();
        var tool = fig.SubPlots[0].AddFibonacci(200.0, 100.0);
        var evt = new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, tool);

        evt.ApplyTo(fig);

        Assert.Empty(fig.SubPlots[0].FibonacciRetracements);
    }

    [Fact]
    public void RemoveDrawingToolEvent_ApplyTo_UnknownTool_NoThrow()
    {
        var fig = MakeFigure();
        // Pass an object that is NOT Trendline, HorizontalLevel, or FibonacciRetracement
        // to hit the switch's implicit-default arm.
        object unknown = "not-a-drawing-tool";
        var evt = new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, unknown);

        var ex = Record.Exception(() => evt.ApplyTo(fig));
        Assert.Null(ex);
    }
}
