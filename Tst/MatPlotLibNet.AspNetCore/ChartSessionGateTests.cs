// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The session renders the figure under the gate it shares with whoever else writes that figure (a stream's scale
/// mode): the library's publisher draws before its first await, so taking the gate for the call is taking it for the
/// render. This pins that, so a change to either side that moves the render out from under the gate goes red.</summary>
public sealed class ChartSessionGateTests
{
    [Fact]
    public async Task TheSessionPublishes_WithTheFigureGateHeld()
    {
        var gate = new object();
        var heldDuringRender = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var publisher = new RecordingChartPublisher { WhilePublishing = () => heldDuringRender.TrySetResult(Monitor.IsEntered(gate)) };
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        await using var session = new ChartSession("c", figure, publisher, NullCallerPublisher.Instance, new ChartSessionOptions(), gate);

        Assert.True(session.Events.Writer.TryWrite(new ZoomEvent("c", 0, 0, 1, 0, 1)));

        Assert.True(await heldDuringRender.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }
}
