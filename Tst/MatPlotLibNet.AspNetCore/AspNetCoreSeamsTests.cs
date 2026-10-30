// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Streaming;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The seams of the server package that a caller reaches by leaving something out or handing in something odd: the
/// interface members a publisher or a client does not have to write, a handler that is not there, a stream registered with
/// options, a publish that fails, a publish that is cancelled.</summary>
public class AspNetCoreSeamsTests
{
    private sealed class BareClient : IChartHubClient
    {
        public Task UpdateChart(string chartId, string figureJson) => Task.CompletedTask;

        public Task UpdateChartSvg(string chartId, string svg) => Task.CompletedTask;

        public Task ReceiveTooltipContent(string chartId, string html) => Task.CompletedTask;
    }

    private sealed class BarePublisher : IChartPublisher
    {
        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FailingPublisher : IChartPublisher
    {
        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default) =>
            Task.FromException(new InvalidOperationException("the client is gone"));
    }

    private sealed class CancellingPublisher : IChartPublisher
    {
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PublishAsync(string chartId, Figure figure, CancellationToken ct = default) => Task.CompletedTask;

        public Task PublishSvgAsync(string chartId, Figure figure, CancellationToken ct = default)
        {
            Called.TrySetResult();
            throw new OperationCanceledException();
        }
    }

    [Fact]
    public async Task AClientThatSaysNothingAboutViewers_AcceptsTheirMessages()
    {
        IChartHubClient client = new BareClient();

        await client.ReceiveCursor("c", "v1", "Viewer 1", 0, 1.0, 2.0);
        await client.ViewChanged("c", "v1", "Viewer 1");
        await client.ViewerLeft("c", "v1");
    }

    [Fact]
    public async Task APublisherThatOnlyDraws_HasNothingToSayAboutWhoMovedTheView()
    {
        IChartPublisher publisher = new BarePublisher();

        await publisher.PublishViewChangeAsync("c", "v1", "Viewer 1", TestContext.Current.CancellationToken);
    }

    [Fact]
    public void ASessionWithoutAHandler_IsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => new ChartSessionOptions().Handle(null!));
    }

    [Fact]
    public async Task AStreamRegisteredWithOptions_HandsTheOptionsToTheCallerToConfigure()
    {
        var registry = new FigureRegistry(new BarePublisher());
        using var stream = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        ChartSessionOptions? configured = null;

        registry.RegisterStreaming("s", stream, options => configured = options);

        Assert.NotNull(configured);
        await registry.UnregisterAsync("s");
    }

    [Fact]
    public async Task AStreamRegisteredWithoutAConfigureCallback_IsRefused()
    {
        var registry = new FigureRegistry(new BarePublisher());
        using var stream = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());

        Assert.Throws<ArgumentNullException>(() => registry.RegisterStreaming("s", stream, null!));
        await registry.UnregisterAsync("s");
    }

    [Fact]
    public async Task APublishThatFails_IsCounted_AndTheStreamGoesOn()
    {
        using var stream = new StreamingFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        await using var session = new StreamingChartSession("s", stream, new FailingPublisher());

        stream.RequestRender();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (session.FaultedPublishCount == 0 && DateTime.UtcNow < deadline) await Task.Delay(10, TestContext.Current.CancellationToken);

        Assert.Equal(1, session.FaultedPublishCount);
    }

    [Fact]
    public async Task APublishThatIsCancelled_IsTheShutdown_NotAFaultOfTheEvent()
    {
        var publisher = new CancellingPublisher();
        var figure = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        var session = new ChartSession("c", figure, publisher, new NullCallerPublisher(), new ChartSessionOptions());

        Assert.True(session.Events.Writer.TryWrite(new ZoomEvent("c", 0, 0.5, 2.5, 1, 9)));
        await publisher.Called.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await session.DisposeAsync();

        Assert.Equal(0, session.FaultedEventCount);
    }
}
