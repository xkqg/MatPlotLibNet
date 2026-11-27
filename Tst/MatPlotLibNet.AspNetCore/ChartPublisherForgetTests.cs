// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>What <see cref="ChartPublisher"/> keeps per chart (the last document sent, the viewers that hold it, the gate that
/// keeps two sends of one chart apart) is let go of when it is told the chart ended.</summary>
public sealed class ChartPublisherForgetTests
{
    private const string Chart = "c";

    private static Figure NewFigure() => Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();

    [Fact]
    public async Task Forget_LetsGoOfWhatWasKeptForTheChart()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.Subscribe(Chart, "viewer");
        var publisher = hub.Publisher();
        await publisher.PublishSvgAsync(Chart, NewFigure(), TestContext.Current.CancellationToken);
        Assert.True(publisher.Holds(Chart));

        await ((IChartPublisher)publisher).ForgetAsync(Chart);

        Assert.False(publisher.Holds(Chart));
    }

    [Fact]
    public async Task Forget_OfAChartNeverPublished_DoesNothing()
    {
        var publisher = new ChartHubDouble(Chart).Publisher();

        await ((IChartPublisher)publisher).ForgetAsync("never");

        Assert.False(publisher.Holds("never"));
    }

    [Fact]
    public async Task APublishWaitingWhileTheChartIsForgotten_IsSentAlone_AndKeptAgain()
    {
        // A publish that waited on the chart's gate while the chart was forgotten must not go on through a gate nobody else
        // can find any more: it takes the chart's gate as it is now, so two sends of the chart still never overlap.
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.Subscribe(Chart, "viewer");
        var publisher = hub.Publisher();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int sending = 0, most = 0, calls = 0;
        hub.Group.UpdateChartSvg(Chart, Arg.Any<string>()).Returns(async _ =>
        {
            int now = Interlocked.Increment(ref sending);
            if (now > most) most = now;
            if (Interlocked.Increment(ref calls) == 1) await release.Task;
            Interlocked.Decrement(ref sending);
        });

        var first = publisher.PublishSvgAsync(Chart, NewFigure(), TestContext.Current.CancellationToken);
        var forget = ((IChartPublisher)publisher).ForgetAsync(Chart);
        var second = publisher.PublishSvgAsync(Chart, Plt.Create().Plot([1.0, 2.0], [9.0, 9.0]).Build(), TestContext.Current.CancellationToken);
        var third = publisher.PublishSvgAsync(Chart, Plt.Create().Plot([1.0, 2.0], [8.0, 7.0]).Build(), TestContext.Current.CancellationToken);
        release.TrySetResult();
        await Task.WhenAll(first, forget, second, third).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.Equal(1, most);
        Assert.Equal(3, Volatile.Read(ref calls));
        Assert.True(publisher.Holds(Chart));
    }
}
