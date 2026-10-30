// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Tools;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A native control sends a rectangle zoom, a span selection, a rotation and the three drawing tools to the hub by the
/// name <see cref="HubMethodNames"/> gives them, and the hub used to have no method of that name, so the event was dropped
/// where it arrived. Each of the six routes now reaches the chart's session, stamped with the connection it came in on.</summary>
public class ChartHubRoutesTests
{
    private sealed class Recorder : IChartEventHandler
    {
        public ConcurrentQueue<FigureInteractionEvent> Seen { get; } = new();

        public ValueTask OnEventAsync(FigureInteractionEvent evt, ChartEventContext context)
        {
            Seen.Enqueue(evt);
            return default;
        }

        public async Task WaitForAsync(int count)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (Seen.Count < count && DateTime.UtcNow < deadline) await Task.Delay(10);
        }
    }

    private sealed class SilentPublisher : IChartPublisher
    {
        public Task PublishAsync(string chartId, Models.Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Models.Figure figure, CancellationToken ct = default) => Task.CompletedTask;
    }

    public static TheoryData<string, FigureInteractionEvent> Routes() => new()
    {
        { nameof(ChartHub.OnRectangleZoom), new RectangleZoomEvent("c", 0, 0, 1, 0, 1) },
        { nameof(ChartHub.OnSpanSelect), new SpanSelectEvent("c", 0, 0, 1) },
        { nameof(ChartHub.OnRotate3D), new Rotate3DEvent("c", 0, 5, 5) },
        { nameof(ChartHub.OnAddTrendline), new AddTrendlineEvent("c", 0, new Trendline(0, 0, 1, 1)) },
        { nameof(ChartHub.OnAddHorizontalLevel), new AddHorizontalLevelEvent("c", 0, new HorizontalLevel(1.5)) },
        { nameof(ChartHub.OnAddFibonacciRetracement), new AddFibonacciRetracementEvent("c", 0, new FibonacciRetracement(2, 1)) },
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task EachRoute_ReachesTheSessionOfTheChart_StampedWithTheConnection(string method, FigureInteractionEvent evt)
    {
        var registry = new FigureRegistry(new SilentPublisher());
        var recorder = new Recorder();
        registry.Register("c", Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build(), o => o.Handle(recorder));
        var caller = Substitute.For<HubCallerContext>();
        caller.ConnectionId.Returns("conn-1");
        var hub = new ChartHub(registry, new ChartSubscriptions()) { Context = caller };

        typeof(ChartHub).GetMethod(method)!.Invoke(hub, [evt]);
        await recorder.WaitForAsync(1);

        var seen = Assert.Single(recorder.Seen);
        Assert.Equal(evt.GetType(), seen.GetType());
        Assert.Equal("conn-1", seen.ViewerId);
        await registry.UnregisterAsync("c");
    }
}
