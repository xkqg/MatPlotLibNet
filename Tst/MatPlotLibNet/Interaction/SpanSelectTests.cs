// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class SpanSelectTests
{
    [Fact]
    public void SpanSelectEvent_IsNotificationEvent()
    {
        var evt = new SpanSelectEvent("c", 0, 1.0, 5.0);
        Assert.IsAssignableFrom<FigureNotificationEvent>(evt);
    }

    [Fact]
    public void SpanSelectEvent_DoesNotMutateFigure()
    {
        var figure = new MatPlotLibNet.Models.Figure { ChartId = "c" };
        figure.AddSubPlot();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;

        var evt = new SpanSelectEvent("c", 0, 2.0, 8.0);
        evt.ApplyTo(figure); // no-op (notification)

        Assert.Equal(0.0, figure.SubPlots[0].XAxis.Min); // unchanged
        Assert.Equal(10.0, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void SpanSelectState_StoresPixelRange()
    {
        var state = new SpanSelectState(100, 300, 0, new MatPlotLibNet.Rendering.Rect(50, 50, 400, 400));
        Assert.Equal(100, state.StartPixelX);
        Assert.Equal(300, state.CurrentPixelX);
    }
}
