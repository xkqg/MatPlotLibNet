// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A chart that streams from the server is looked at in browsers, and a browser zooms, pans and presses Home like
/// any other. The streaming session used to publish and nothing else: a gesture sent for a streaming chart found no session
/// and vanished, and the scale mode that rewrites the limits at every frame would have overruled it anyway. The session now
/// reads gestures too, applies them to the figure the stream also writes (one at a time, so neither sees half the other's
/// work), and leaves a moved axes alone until Home gives it back.</summary>
public class StreamingGestureTests
{
    private sealed class Publisher : IChartPublisher
    {
        public ConcurrentQueue<string> Calls { get; } = new();
        public bool Throw;

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Calls.Enqueue(chartId);
            return Throw ? Task.FromException(new InvalidOperationException("the publish failed")) : Task.CompletedTask;
        }

        public async Task WaitForAsync(int calls)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (Calls.Count < calls && DateTime.UtcNow < deadline) await Task.Delay(10);
        }
    }

    private static (StreamingFigure Stream, StreamingLineSeries Series, FigureRegistry Registry, Publisher Publisher) Streaming()
    {
        var figure = new Figure();
        var series = figure.AddSubPlot().AddSeries(new StreamingLineSeries(100));
        figure.ChartId = "s1";
        for (int i = 0; i < 10; i++) series.AppendPoint(i, i * 2.0);
        var stream = new StreamingFigure(figure)
        {
            DefaultConfig = new StreamingAxesConfig(new AxisScaleMode.AutoScale(), new AxisScaleMode.AutoScale()),
        };
        stream.ApplyAxisScaling();
        var publisher = new Publisher();
        var registry = new FigureRegistry(publisher);
        registry.RegisterStreaming("s1", stream);
        return (stream, series, registry, publisher);
    }

    [Fact]
    public async Task AZoomFromABrowser_IsAppliedToTheStreamingFigure_AndPublished()
    {
        var (stream, _, registry, publisher) = Streaming();

        Assert.True(registry.Publish("s1", new ZoomEvent("s1", 0, 2, 4, 4, 8)));
        await publisher.WaitForAsync(1);

        Assert.Equal(2.0, stream.Figure.SubPlots[0].XAxis.Min);
        Assert.Equal(4.0, stream.Figure.SubPlots[0].XAxis.Max);
        stream.Dispose();
    }

    [Fact]
    public async Task AZoomedStreamingChart_StaysZoomedWhileTheStreamRenders_UntilHome()
    {
        var (stream, series, registry, publisher) = Streaming();
        registry.Publish("s1", new ZoomEvent("s1", 0, 2, 4, 4, 8));
        await publisher.WaitForAsync(1);

        for (int i = 10; i < 20; i++) series.AppendPoint(i, i * 2.0);
        stream.RequestRender();
        Assert.Equal(4.0, stream.Figure.SubPlots[0].XAxis.Max);   // the render did not overrule the reader

        registry.Publish("s1", new ResetEvent("s1", 0, 2, 4, 4, 8));   // the page sends the zoomed numbers
        await publisher.WaitForAsync(3);
        stream.RequestRender();
        Assert.Equal(19.0, stream.Figure.SubPlots[0].XAxis.Max);   // Home handed the axes back to the scale mode
        stream.Dispose();
    }

    [Fact]
    public async Task APublishThatFails_OnARenderTick_DoesNotStopTheNextRenderFromPublishing()
    {
        var (stream, _, registry, publisher) = Streaming();
        publisher.Throw = true;

        stream.RequestRender();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        publisher.Throw = false;
        stream.RequestRender();
        await publisher.WaitForAsync(2);

        Assert.Equal(2, publisher.Calls.Count);
        stream.Dispose();
        await registry.UnregisterAsync("s1");
    }
}
