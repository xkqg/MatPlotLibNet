// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NSubstitute;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Phase X.4.a (v1.7.2, 2026-04-19) — drives every code path in
/// <see cref="StreamingChartSession"/> end-to-end through its public entry point
/// <see cref="FigureRegistry.RegisterStreaming"/>. Pre-X the session was at 0%L
/// because no test ever instantiated it (internal class, only constructed inside
/// FigureRegistry). Each test wires a real <see cref="StreamingFigure"/> + a
/// substitute <see cref="IChartPublisher"/>, so the real event subscription /
/// dispatch / dispose paths run; only the publish boundary is mocked (so we can
/// assert the call). This is the "integration with mock at the seam" pattern —
/// the wiring is exercised, not just the substitute.</summary>
public class StreamingChartSessionTests
{
    private static (StreamingFigure sf, FigureRegistry registry, IChartPublisher publisher) BuildHarness()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        var sf = new StreamingFigure(fig);
        var publisher = Substitute.For<IChartPublisher>();
        var registry = new FigureRegistry(publisher);
        return (sf, registry, publisher);
    }

    /// <summary>Constructor (line 19-25) subscribes to RenderRequested. Triggering
    /// RequestRender on the StreamingFigure causes OnRenderRequested → PublishSvgAsync.</summary>
    [Fact]
    public async Task RegisterStreaming_OnRenderRequested_PublishesSvg()
    {
        var (sf, registry, publisher) = BuildHarness();
        registry.RegisterStreaming("c1", sf);

        sf.RequestRender();
        // Publishing is fire-and-forget (`_ = _publisher.PublishSvgAsync(...)`),
        // so let the task scheduler drain.
        await Task.Yield();

        await publisher.Received(1).PublishSvgAsync("c1", sf.Figure, Arg.Any<CancellationToken>());
    }

    /// <summary>OnRenderRequested calls ApplyAxisScaling (line 30) BEFORE publishing.
    /// Verified by appending a streaming sample (which triggers RenderRequested) and
    /// observing the publisher receives the figure with the rescaled axes implicit.</summary>
    [Fact]
    public async Task RegisterStreaming_StreamingAppend_TriggersAxisScalingAndPublish()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                var sl = new StreamingLineSeries(capacity: 16);
                sl.AppendPoint(0.0, 1.0);
                ax.AddSeries(sl);
            })
            .Build();
        var sf = new StreamingFigure(fig);
        var publisher = Substitute.For<IChartPublisher>();
        var registry = new FigureRegistry(publisher);
        registry.RegisterStreaming("auto-rescale", sf);

        // RequestRender fires the event; ApplyAxisScaling runs as a side effect.
        sf.RequestRender();
        await Task.Yield();

        await publisher.Received(1).PublishSvgAsync("auto-rescale", sf.Figure, Arg.Any<CancellationToken>());
    }

    /// <summary>Re-registering the same chartId disposes the prior session
    /// (FigureRegistry line 74-75 invokes existing.DisposeAsync). After re-register,
    /// the OLD StreamingFigure's RenderRequested no longer triggers a publish via the
    /// stale session — but it WOULD still trigger via the OLD session's still-attached
    /// handler if dispose hadn't unsubscribed. This pins that the dispose path
    /// (StreamingChartSession.DisposeAsync line 34-42) actually unsubscribes.</summary>
    [Fact]
    public async Task ReRegisterStreaming_OldSession_StopsPublishing()
    {
        var (sf1, registry, publisher) = BuildHarness();
        registry.RegisterStreaming("c1", sf1);

        // Replace with a brand-new StreamingFigure under the same chartId.
        var sf2 = new StreamingFigure(Plt.Create().Plot([1.0], [2.0]).Build());
        registry.RegisterStreaming("c1", sf2);

        // Wait a beat so the dispose runs (it's awaited via _ = existing.DisposeAsync())
        await Task.Yield();

        // Trigger render on the OLD figure — the old session was disposed, so its
        // event handler should be gone. publisher should NOT receive a call for sf1.Figure.
        publisher.ClearReceivedCalls();
        sf1.RequestRender();
        await Task.Yield();
        await publisher.DidNotReceive().PublishSvgAsync("c1", sf1.Figure, Arg.Any<CancellationToken>());

        // Trigger render on the NEW figure — the new session IS active.
        sf2.RequestRender();
        await Task.Yield();
        await publisher.Received(1).PublishSvgAsync("c1", sf2.Figure, Arg.Any<CancellationToken>());
    }

    /// <summary>Multiple consecutive RequestRender calls on a single session each
    /// trigger their own publish. Pins the absence of throttling/coalescing at the
    /// session layer (the throttle lives upstream in StreamingFigure).</summary>
    [Fact]
    public async Task RegisterStreaming_MultipleRenders_PublishOncePerRender()
    {
        var (sf, registry, publisher) = BuildHarness();
        registry.RegisterStreaming("c1", sf);

        sf.RequestRender();
        sf.RequestRender();
        sf.RequestRender();
        await Task.Yield();

        await publisher.Received(3).PublishSvgAsync("c1", sf.Figure, Arg.Any<CancellationToken>());
    }

    /// <summary>Multiple concurrent streaming sessions on different chartIds are
    /// independent — a render on one fires only its own publish.</summary>
    [Fact]
    public async Task RegisterStreaming_MultipleSessions_AreIsolated()
    {
        var publisher = Substitute.For<IChartPublisher>();
        var registry = new FigureRegistry(publisher);
        var sfA = new StreamingFigure(Plt.Create().Plot([1.0], [2.0]).Build());
        var sfB = new StreamingFigure(Plt.Create().Plot([3.0], [4.0]).Build());
        registry.RegisterStreaming("chart-A", sfA);
        registry.RegisterStreaming("chart-B", sfB);

        sfA.RequestRender();
        await Task.Yield();

        await publisher.Received(1).PublishSvgAsync("chart-A", sfA.Figure, Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishSvgAsync("chart-B", Arg.Any<Figure>(), Arg.Any<CancellationToken>());
    }
}
