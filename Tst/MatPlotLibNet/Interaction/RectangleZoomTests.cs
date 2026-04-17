// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class RectangleZoomTests
{
    [Fact]
    public void RectangleZoomEvent_InheritsAxisRangeEvent()
    {
        var evt = new RectangleZoomEvent("c", 0, 1, 5, 2, 8);
        Assert.IsAssignableFrom<AxisRangeEvent>(evt);
    }

    [Fact]
    public void RectangleZoomEvent_ApplyTo_SetsAxisLimits()
    {
        var figure = new MatPlotLibNet.Models.Figure { ChartId = "c" };
        figure.AddSubPlot();
        var evt = new RectangleZoomEvent("c", 0, 1.0, 5.0, 2.0, 8.0);
        evt.ApplyTo(figure);

        Assert.Equal(1.0, figure.SubPlots[0].XAxis.Min);
        Assert.Equal(5.0, figure.SubPlots[0].XAxis.Max);
        Assert.Equal(2.0, figure.SubPlots[0].YAxis.Min);
        Assert.Equal(8.0, figure.SubPlots[0].YAxis.Max);
    }

    [Fact]
    public void RectangleZoomState_StoresPixelCoords()
    {
        var state = new RectangleZoomState(10, 20, 100, 200, 0);
        Assert.Equal(10, state.StartPixelX);
        Assert.Equal(100, state.CurrentPixelX);
    }
}
