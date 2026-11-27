// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        var registry = new FigureRegistry(new SilentChartPublisher());
        var recorder = new RecordingChartEventHandler();
        registry.Register("c", Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build(), o => o.Handle(recorder));
        var caller = Substitute.For<HubCallerContext>();
        caller.ConnectionId.Returns("conn-1");
        var hub = new ChartHub(registry, new ChartSubscriptions()) { Context = caller };

        typeof(ChartHub).GetMethod(method)!.Invoke(hub, [evt]);
        await recorder.WaitForAsync(1);

        var seen = Assert.Single(recorder.Events);
        Assert.Equal(evt.GetType(), seen.GetType());
        Assert.Equal("conn-1", seen.ViewerId);
        await registry.UnregisterAsync("c");
    }
}
