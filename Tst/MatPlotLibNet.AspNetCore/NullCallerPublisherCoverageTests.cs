// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>Phase X.10.c (v1.7.2, 2026-04-19) — drives <see cref="FigureRegistry"/>'s
/// private-nested <c>NullCallerPublisher.SendTooltipAsync</c> (line 47-48) which the
/// existing harness never reached. Pre-X.10.c that line was dead code in tests because:
///   - The single-arg <see cref="FigureRegistry"/> ctor (used by every existing test)
///     installs <c>NullCallerPublisher.Instance</c> as the caller publisher (line 39-42)
///   - But no test publishes a <see cref="HoverEvent"/> with a non-null
///     <see cref="HoverEvent.CallerConnectionId"/> AND an OnHover handler returning
///     non-null html — those two conditions are required for ChartSession line 89-90 to
///     reach the SendTooltipAsync call.
///
/// This fact registers a chart with an OnHover handler that returns a tooltip string,
/// publishes a HoverEvent with a non-null CallerConnectionId, and waits for the reader
/// task to drain → exercises NullCallerPublisher.SendTooltipAsync exactly once.</summary>
public class NullCallerPublisherCoverageTests
{
    private sealed class NoopPublisher : IChartPublisher
    {
        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) =>
            Task.CompletedTask;
        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    [Fact]
    public async Task NullCallerPublisher_SendTooltipAsync_HitViaHoverEventWithCallerId()
    {
        var publisher = new NoopPublisher();
        // Single-arg ctor → NullCallerPublisher installed (line 39-42 covered by existing
        // tests; this test additionally hits the NullCallerPublisher.SendTooltipAsync arm).
        var registry = new FigureRegistry(publisher);
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();

        registry.Register("c1", figure, opts =>
            opts.OnHover(_ => ValueTask.FromResult<string?>("<b>tooltip</b>")));

        // CallerConnectionId set → ChartSession line 66 condition is true → tooltipHtml
        // is captured → ChartSession line 89-90 calls _callerPublisher.SendTooltipAsync.
        var ok = registry.Publish("c1", new HoverEvent("c1", 0, 1.5, 4.5, CallerConnectionId: "test-conn-id"));
        Assert.True(ok);

        // Give the reader task time to drain + dispatch.
        await Task.Delay(150, TestContext.Current.CancellationToken);

        await registry.UnregisterAsync("c1");
    }
}
