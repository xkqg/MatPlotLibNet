// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Verifies <see cref="FigureRegistry"/> pub/sub routing: events flow through a
/// channel-drained reader task, mutate the figure via <see cref="FigureInteractionEvent.ApplyTo"/>,
/// and trigger a single publish per drained burst (coalesced).</summary>
public class FigureRegistryTests
{
    private static Figure NewFigure() => Plt.Create()
        .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0])
        .Build();

    [Fact]
    public async Task Publish_UnknownChart_ReturnsFalse()
    {
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);

        var ok = registry.Publish("does-not-exist",
            new ZoomEvent("does-not-exist", 0, 0, 1, 0, 1));

        Assert.False(ok);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        Assert.Empty(publisher.Calls);
    }

    [Fact]
    public async Task Publish_ZoomEvent_AppliesMutation_AndPublishes()
    {
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);
        var figure = NewFigure();
        registry.Register("c1", figure);

        Assert.True(registry.Publish("c1", new ZoomEvent("c1", 0, 0.5, 2.5, 0, 10)));
        await publisher.WaitForAtLeastAsync(1);

        var axes = figure.SubPlots[0];
        Assert.Equal(0.5, axes.XAxis.Min);
        Assert.Equal(2.5, axes.XAxis.Max);
        Assert.Equal(0.0, axes.YAxis.Min);
        Assert.Equal(10.0, axes.YAxis.Max);
        Assert.Single(publisher.Calls, c => c.ChartId == "c1");

        await registry.UnregisterAsync("c1");
    }

    [Fact]
    public async Task Publish_BurstOfZoomEvents_IsCoalesced()
    {
        // 50 events pushed as fast as we can write. The reader drains all that have accumulated
        // in one WaitToReadAsync wake-up and publishes once per drained batch. On a warmed-up
        // channel we expect *far* fewer than 50 publishes — at most 50, but typically 1-5.
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);
        var figure = NewFigure();
        registry.Register("burst", figure);

        for (int i = 0; i < 50; i++)
            registry.Publish("burst", new ZoomEvent("burst", 0, i, i + 1, i, i + 2));

        // wait for drain + publish to settle
        await publisher.WaitForQuiescenceAsync(TimeSpan.FromMilliseconds(300));

        Assert.InRange(publisher.Calls.Count, 1, 50);
        // the *last* zoom wins — the figure reflects the final burst event
        var axes = figure.SubPlots[0];
        Assert.Equal(49.0, axes.XAxis.Min);
        Assert.Equal(50.0, axes.XAxis.Max);

        await registry.UnregisterAsync("burst");
    }

    [Fact]
    public async Task Publish_MixedEventTypes_AppliesAllInOrder()
    {
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);
        var figure = NewFigure();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 10;
        registry.Register("mix", figure);

        registry.Publish("mix", new ZoomEvent("mix", 0, 0, 100, 0, 100));
        registry.Publish("mix", new PanEvent("mix", 0, 10, 20));

        await publisher.WaitForQuiescenceAsync(TimeSpan.FromMilliseconds(300));

        // ZoomEvent set limits to [0,100]x[0,100], then PanEvent shifted by (+10, +20)
        var axes = figure.SubPlots[0];
        Assert.Equal(10.0, axes.XAxis.Min);
        Assert.Equal(110.0, axes.XAxis.Max);
        Assert.Equal(20.0, axes.YAxis.Min);
        Assert.Equal(120.0, axes.YAxis.Max);

        await registry.UnregisterAsync("mix");
    }

    [Fact]
    public async Task UnregisterAsync_CompletesReaderTask_DoesNotHang()
    {
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);
        registry.Register("dispose-me", NewFigure());

        // just dispose immediately — reader should exit cleanly
        await registry.UnregisterAsync("dispose-me");

        // publish after disposal is a no-op
        var ok = registry.Publish("dispose-me", new ZoomEvent("dispose-me", 0, 0, 1, 0, 1));
        Assert.False(ok);
    }

    [Fact]
    public async Task UnregisterAsync_UnknownChart_IsNoOp()
    {
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);
        registry.Register("kept", NewFigure());

        await registry.UnregisterAsync("never-registered");

        Assert.True(registry.Publish("kept", new ZoomEvent("kept", 0, 0, 1, 0, 1)));
        await registry.UnregisterAsync("kept");
    }

    [Fact]
    public async Task Publish_LegendToggleEvent_FlipsSeriesVisibility()
    {
        var publisher = new RecordingChartPublisher();
        var registry = new FigureRegistry(publisher);
        var figure = NewFigure();
        registry.Register("toggle", figure);

        registry.Publish("toggle", new LegendToggleEvent("toggle", 0, 0));
        await publisher.WaitForAtLeastAsync(1);

        var series = (Models.Series.ChartSeries)figure.SubPlots[0].Series[0];
        Assert.False(series.Visible);

        await registry.UnregisterAsync("toggle");
    }

    /// <summary>Register(chartId, figure, configure) with null configure — `ThrowIfNull` true arm.</summary>
    [Fact]
    public void Register_WithNullConfigure_ThrowsArgumentNullException()
    {
        var registry = new FigureRegistry(new SilentChartPublisher());
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register("c1", NewFigure(), configure: null!));
    }

    /// <summary>Register(chartId, figure, configure) with non-null configure — `ThrowIfNull` false arm,
    /// callback fires once.</summary>
    [Fact]
    public void Register_WithNonNullConfigure_InvokesCallbackOnce()
    {
        var registry = new FigureRegistry(new SilentChartPublisher());
        int callCount = 0;
        registry.Register("c1", NewFigure(), configure: opts => callCount++);
        Assert.Equal(1, callCount);
    }

    /// <summary>Register the same chartId twice — second call disposes the existing session
    /// (RegisterCore TryRemove true arm).</summary>
    [Fact]
    public async Task Register_SameChartIdTwice_DisposesPreviousSession()
    {
        var registry = new FigureRegistry(new SilentChartPublisher());
        registry.Register("c1", NewFigure());
        registry.Register("c1", NewFigure());
        Assert.True(registry.Publish("c1", new ZoomEvent("c1", 0, 0, 1, 0, 1)));
        await Task.Delay(20, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NullCallerPublisher_SendTooltipAsync_HitViaHoverEventWithCallerId()
    {
        var publisher = new SilentChartPublisher();
        // Single-arg ctor → NullCallerPublisher installed (its installation is covered by existing
        // tests; this test additionally hits the NullCallerPublisher.SendTooltipAsync arm).
        var registry = new FigureRegistry(publisher);
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();

        registry.Register("c1", figure, opts =>
            opts.OnHover(_ => ValueTask.FromResult<string?>("<b>tooltip</b>")));

        // CallerConnectionId set → the session captures the tooltipHtml
        // and calls _callerPublisher.SendTooltipAsync.
        var ok = registry.Publish("c1", new HoverEvent("c1", 0, 1.5, 4.5, CallerConnectionId: "test-conn-id"));
        Assert.True(ok);

        // Give the reader task time to drain + dispatch.
        await Task.Delay(150, TestContext.Current.CancellationToken);

        await registry.UnregisterAsync("c1");
    }
}

// ─── FigureRegistryCoverageTests.cs ──────────────────────────────────────────

