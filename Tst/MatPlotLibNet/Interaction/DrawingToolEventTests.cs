// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Verifies <see cref="AddTrendlineEvent"/>, <see cref="AddHorizontalLevelEvent"/>,
/// <see cref="AddFibonacciRetracementEvent"/>, and <see cref="RemoveDrawingToolEvent"/>: each kind of tool enters and leaves the list
/// it names.</summary>
public class DrawingToolEventTests
{
    private static Figure MakeFigure()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        return fig;
    }

    [Fact]
    public void AnAddEvent_PutsItsToolInTheListOfItsKind()
    {
        var fig = MakeFigure();
        var trendline = new Trendline(0.0, 10.0, 5.0, 20.0);
        var level = new HorizontalLevel(150.0);
        var retracement = new FibonacciRetracement(200.0, 100.0);

        new AddTrendlineEvent("chart-1", AxesIndex: 0, trendline).ApplyTo(fig);
        new AddHorizontalLevelEvent("chart-1", AxesIndex: 0, level).ApplyTo(fig);
        new AddFibonacciRetracementEvent("chart-1", AxesIndex: 0, retracement).ApplyTo(fig);

        var axes = fig.SubPlots[0];
        Assert.Same(trendline, Assert.Single(axes.Trendlines));
        Assert.Same(level, Assert.Single(axes.HorizontalLevels));
        Assert.Same(retracement, Assert.Single(axes.FibonacciRetracements));
    }

    [Fact]
    public void ARemoveEvent_TakesItsToolOutOfTheListOfItsKind()
    {
        var fig = MakeFigure();
        var axes = fig.SubPlots[0];
        object[] tools = [axes.AddTrendline(0.0, 10.0, 5.0, 20.0), axes.AddLevel(100.0), axes.AddFibonacci(200.0, 100.0)];

        foreach (var tool in tools) new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, tool).ApplyTo(fig);

        Assert.Empty(axes.Trendlines);
        Assert.Empty(axes.HorizontalLevels);
        Assert.Empty(axes.FibonacciRetracements);
    }

    /// <summary>Only a drawing tool is taken off: an object that is none changes nothing, and neither does a line that is drawn like a
    /// tool but is no tool (a reference line strokes as the tools do, and stays).</summary>
    [Fact]
    public void RemoveDrawingToolEvent_ApplyTo_WhatIsNoTool_RemovesNothing()
    {
        var fig = MakeFigure();
        var line = fig.SubPlots[0].AxHLine(1.5);

        var ex = Record.Exception(() => new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, "not-a-drawing-tool").ApplyTo(fig));
        new RemoveDrawingToolEvent("chart-1", AxesIndex: 0, line).ApplyTo(fig);

        Assert.Null(ex);
        Assert.Same(line, Assert.Single(fig.SubPlots[0].ReferenceLines));
    }
}
