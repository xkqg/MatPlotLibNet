// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Tests.Interaction;

public class SignalREventSinkTests
{
    [Fact]
    public void ZoomEvent_DispatchesToOnZoom()
    {
        string? capturedMethod = null;
        object? capturedPayload = null;
        var sink = SignalREventSink.Create((method, payload) =>
        {
            capturedMethod = method;
            capturedPayload = payload;
            return Task.CompletedTask;
        });

        var zoom = new ZoomEvent("chart-1", 0, 1.0, 9.0, 0.5, 4.5);
        sink(zoom);

        Assert.Equal("OnZoom", capturedMethod);
        Assert.Same(zoom, capturedPayload);
    }

    [Fact]
    public void PanEvent_DispatchesToOnPan()
    {
        string? capturedMethod = null;
        object? capturedPayload = null;
        var sink = SignalREventSink.Create((method, payload) =>
        {
            capturedMethod = method;
            capturedPayload = payload;
            return Task.CompletedTask;
        });

        var pan = new PanEvent("chart-1", 0, 1.5, -0.5);
        sink(pan);

        Assert.Equal("OnPan", capturedMethod);
        Assert.Same(pan, capturedPayload);
    }

    [Fact]
    public void ResetEvent_DispatchesToOnReset()
    {
        string? capturedMethod = null;
        var sink = SignalREventSink.Create((method, _) =>
        {
            capturedMethod = method;
            return Task.CompletedTask;
        });

        sink(new ResetEvent("chart-1", 0, 0, 10, 0, 5));
        Assert.Equal("OnReset", capturedMethod);
    }

    [Fact]
    public void LegendToggleEvent_DispatchesToOnLegendToggle()
    {
        string? capturedMethod = null;
        var sink = SignalREventSink.Create((method, _) =>
        {
            capturedMethod = method;
            return Task.CompletedTask;
        });

        sink(new LegendToggleEvent("chart-1", 0, 0));
        Assert.Equal("OnLegendToggle", capturedMethod);
    }

    [Fact]
    public void BrushSelectEvent_DispatchesToOnBrushSelect()
    {
        string? capturedMethod = null;
        var sink = SignalREventSink.Create((method, _) =>
        {
            capturedMethod = method;
            return Task.CompletedTask;
        });

        sink(new BrushSelectEvent("chart-1", 0, 1, 2, 3, 4));
        Assert.Equal("OnBrushSelect", capturedMethod);
    }

    [Fact]
    public void HoverEvent_DispatchesToOnHover()
    {
        string? capturedMethod = null;
        var sink = SignalREventSink.Create((method, _) =>
        {
            capturedMethod = method;
            return Task.CompletedTask;
        });

        sink(new HoverEvent("chart-1", 0, 5.0, 3.0));
        Assert.Equal("OnHover", capturedMethod);
    }
}
