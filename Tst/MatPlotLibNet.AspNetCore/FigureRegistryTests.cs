// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
        var publisher = new RecordingPublisher();
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
        var publisher = new RecordingPublisher();
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
        var publisher = new RecordingPublisher();
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
        var publisher = new RecordingPublisher();
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
        var publisher = new RecordingPublisher();
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
        var publisher = new RecordingPublisher();
        var registry = new FigureRegistry(publisher);

        await registry.UnregisterAsync("never-registered"); // must not throw
    }

    [Fact]
    public async Task Publish_LegendToggleEvent_FlipsSeriesVisibility()
    {
        var publisher = new RecordingPublisher();
        var registry = new FigureRegistry(publisher);
        var figure = NewFigure();
        registry.Register("toggle", figure);

        registry.Publish("toggle", new LegendToggleEvent("toggle", 0, 0));
        await publisher.WaitForAtLeastAsync(1);

        var series = (Models.Series.ChartSeries)figure.SubPlots[0].Series[0];
        Assert.False(series.Visible);

        await registry.UnregisterAsync("toggle");
    }

    /// <summary>Test double for <see cref="IChartPublisher"/> that records every publish and
    /// exposes async helpers to wait for a given call count / quiescence.</summary>
    private sealed class RecordingPublisher : IChartPublisher
    {
        public ConcurrentQueue<(string ChartId, Figure Figure)> Calls { get; } = new();
        private readonly SemaphoreSlim _signal = new(0);

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Calls.Enqueue((chartId, figure));
            _signal.Release();
            return Task.CompletedTask;
        }

        public async Task WaitForAtLeastAsync(int n)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Calls.Count < n && DateTime.UtcNow < deadline)
                await Task.Delay(10);
        }

        public async Task WaitForQuiescenceAsync(TimeSpan quiet)
        {
            var last = Calls.Count;
            var stable = DateTime.UtcNow;
            while (DateTime.UtcNow - stable < quiet)
            {
                await Task.Delay(20);
                if (Calls.Count != last)
                {
                    last = Calls.Count;
                    stable = DateTime.UtcNow;
                }
            }
        }
    }
}
