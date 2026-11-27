// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NSubstitute;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>A viewer that patches gets only what changed. A connection that subscribed with <c>SubscribeToParts</c> is sent the
/// whole document once, with every drawn part in a named group, and afterwards only the parts that changed; a connection that
/// subscribed the old way keeps getting the plain document, as before; and when nothing changed nobody is sent anything.</summary>
public sealed class ChartPublisherPartsTests
{
    private const string Chart = "wall";

    private static Figure TwoPanels() => Plt.Create()
        .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
        .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2, 3], [2.0, 1, 3]))
        .Build();

    [Fact]
    public async Task APublishHandsOnThePlainSvgItDrew_WhetherItWasSent_OrNothingChanged()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        IChartPublisher publisher = hub.Publisher();
        var figure = TwoPanels();

        string? first = await publisher.PublishSvgAndHandOnAsync(Chart, figure, TestContext.Current.CancellationToken);
        string? unchanged = await publisher.PublishSvgAndHandOnAsync(Chart, figure, TestContext.Current.CancellationToken);

        Assert.Equal(figure.ToSvg(), first);
        Assert.Equal(first, unchanged);
    }

    [Fact]
    public async Task WithoutTheSubscriptions_APublishHandsOnThePlainSvgItSentTheGroup()
    {
        var hub = new ChartHubDouble(Chart);
        IChartPublisher publisher = new ChartPublisher(hub.Context, new MatPlotLibNet.Serialization.ChartSerializer(), new SvgTransform());
        var figure = TwoPanels();

        string? handedOn = await publisher.PublishSvgAndHandOnAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Group.Received(1).UpdateChartSvg(Chart, handedOn!);
        Assert.Equal(figure.ToSvg(), handedOn);
    }

    [Fact]
    public async Task WithNobodyPatching_ThePlainDocumentGoesToTheGroup_AsBefore()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.Subscribe(Chart, "old");
        var figure = TwoPanels();

        await hub.Publisher().PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Group.Received(1).UpdateChartSvg(Chart, figure.ToSvg());
    }

    [Fact]
    public async Task APatchingViewer_GetsTheWholeWrappedDocumentFirst_AndTheOthersThePlainOne()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.Subscribe(Chart, "old");
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var figure = TwoPanels();

        await hub.Publisher().PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.AllButParts.Received(1).UpdateChartSvg(Chart, figure.ToSvg());
        await hub.Proxy("new").Received(1).UpdateChartSvg(Chart, Arg.Is<string>(svg =>
            svg.Contains("<g data-mpl-part=\"axes-1\">") && svg.Contains("<g data-mpl-part=\"background\">")));
        await hub.Group.DidNotReceive().UpdateChartSvg(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AChangeToOneAxes_SendsThatAxesAlone()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        // A colour moves nothing in the layout, so only the lower axes is drawn differently (a title would take room and move both).
        ((MatPlotLibNet.Models.Series.LineSeries)figure.SubPlots[1].Series[0]).Color = MatPlotLibNet.Styling.Colors.Red;
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        var expected = new SvgTransform().RenderDocument(figure).WrappedAxes(1);
        await hub.Proxy("new").Received(1).UpdateChartParts(Chart, Arg.Is<ChartSvgParts>(parts =>
            parts.RootTag == null && parts.Background == null && parts.Overlay == null
            && parts.Axes.Count == 1 && parts.Axes[0].Index == 1 && parts.Axes[0].Markup == expected));
        await hub.Proxy("new").Received(1).UpdateChartSvg(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task NothingChanged_SendsNothingToAnyone()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.Subscribe(Chart, "old");
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.AllButParts.Received(1).UpdateChartSvg(Arg.Any<string>(), Arg.Any<string>());
        await hub.Proxy("new").Received(1).UpdateChartSvg(Arg.Any<string>(), Arg.Any<string>());
        await hub.Proxy("new").DidNotReceive().UpdateChartParts(Arg.Any<string>(), Arg.Any<ChartSvgParts>());
    }

    [Fact]
    public async Task ANewBackgroundOrOverlay_IsSentAsThatPartAlone()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        figure.AddText("a note on the figure", 0.5, 0.95);
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);
        figure.BackgroundColor = MatPlotLibNet.Styling.Colors.Black;
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        var document = new SvgTransform().RenderDocument(figure);
        await hub.Proxy("new").Received(1).UpdateChartParts(Chart, Arg.Is<ChartSvgParts>(parts =>
            parts.Overlay != null && parts.Overlay.Contains("a note on the figure") && parts.Background == null));
        await hub.Proxy("new").Received(1).UpdateChartParts(Chart, Arg.Is<ChartSvgParts>(parts =>
            parts.Background == document.WrappedBackground()));
    }

    [Fact]
    public async Task ASubplotAdded_SendsTheWholeWrappedDocumentAgain()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        figure.AddSubPlot().AddSeries(new MatPlotLibNet.Models.Series.LineSeries([1.0, 2], [2.0, 1]));
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Proxy("new").Received(2).UpdateChartSvg(Chart, Arg.Is<string>(svg => svg.Contains("data-mpl-part")));
        await hub.Proxy("new").DidNotReceive().UpdateChartParts(Arg.Any<string>(), Arg.Any<ChartSvgParts>());
    }

    [Fact]
    public async Task AZoomThatOnlyMovesTheRootsView_SendsTheNewRootTag()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        figure.ServerInteraction = true;
        figure.ChartId = Chart;
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        new MatPlotLibNet.Interaction.ZoomEvent(Chart, 0, 1.5, 2.5, 1.5, 2.5).ApplyTo(figure);
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        var document = new SvgTransform().RenderDocument(figure);
        await hub.Proxy("new").Received(1).UpdateChartParts(Chart, Arg.Is<ChartSvgParts>(parts =>
            parts.RootTag == document.RootTag && parts.Axes.Any(a => a.Index == 0)));
    }

    [Fact]
    public async Task AViewerWhoJoinsLater_GetsTheWholeDocument_WhileTheOthersGetThePatch()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "first");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        hub.Subscriptions.SubscribeToParts(Chart, "later");
        figure.SubPlots[0].Title = "changed";
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Proxy("later").Received(1).UpdateChartSvg(Chart, Arg.Is<string>(svg => svg.Contains("data-mpl-part") && svg.Contains("changed")));
        await hub.Proxy("later").DidNotReceive().UpdateChartParts(Arg.Any<string>(), Arg.Any<ChartSvgParts>());
        await hub.Proxy("first").Received(1).UpdateChartParts(Chart, Arg.Any<ChartSvgParts>());
    }

    [Fact]
    public async Task AViewerWhoJoinsWhileNothingChanges_StillGetsThePicture()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.Subscribe(Chart, "old");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        hub.Subscriptions.Subscribe(Chart, "old later");
        hub.Subscriptions.SubscribeToParts(Chart, "new later");
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Proxy("old later").Received(1).UpdateChartSvg(Chart, figure.ToSvg());
        await hub.Proxy("new later").Received(1).UpdateChartSvg(Chart, Arg.Is<string>(svg => svg.Contains("data-mpl-part")));
        await hub.Group.Received(1).UpdateChartSvg(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AViewerWhoLeft_IsSentNothingMore()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "gone");
        var publisher = hub.Publisher();
        var figure = TwoPanels();
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        hub.Subscriptions.Unsubscribe(Chart, "gone");
        figure.SubPlots[0].Title = "changed";
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Proxy("gone").DidNotReceive().UpdateChartParts(Arg.Any<string>(), Arg.Any<ChartSvgParts>());
        await hub.Proxy("gone").Received(1).UpdateChartSvg(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ARendererThatIsNotTheLibrarys_SendsThePlainDocumentToTheGroup_EveryTime()
    {
        var hub = new ChartHubDouble(Chart);
        hub.Subscriptions.SubscribeToParts(Chart, "new");
        var renderer = Substitute.For<ISvgRenderer>();
        renderer.Render(Arg.Any<Figure>()).Returns("<svg/>");
        var publisher = new ChartPublisher(hub.Context, new MatPlotLibNet.Serialization.ChartSerializer(), renderer, hub.Subscriptions);
        var figure = TwoPanels();

        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Group.Received(2).UpdateChartSvg(Chart, "<svg/>");
    }

    [Fact]
    public async Task APublisherMadeWithoutTheSubscriptions_SendsThePlainDocumentToTheGroup_EveryTime()
    {
        var hub = new ChartHubDouble(Chart);
        var publisher = new ChartPublisher(hub.Context, new MatPlotLibNet.Serialization.ChartSerializer(), new SvgTransform());
        var figure = TwoPanels();

        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);
        await publisher.PublishSvgAsync(Chart, figure, TestContext.Current.CancellationToken);

        await hub.Group.Received(2).UpdateChartSvg(Chart, figure.ToSvg());
    }

    [Fact]
    public void ThePartsSubscriptions_AreKeptAndDroppedWithTheConnection()
    {
        var subscriptions = new ChartSubscriptions();
        subscriptions.SubscribeToParts(Chart, "a");
        subscriptions.Subscribe(Chart, "b");
        subscriptions.SubscribeToParts("other", "a");

        Assert.Equal(["a"], subscriptions.PartsConnectionsOf(Chart));
        Assert.Equal(["a", "b"], subscriptions.ConnectionsOf(Chart).Order(StringComparer.Ordinal));

        subscriptions.Disconnected("a");

        Assert.Empty(subscriptions.PartsConnectionsOf(Chart));
        Assert.Empty(subscriptions.PartsConnectionsOf("other"));
        Assert.Equal(["b"], subscriptions.ConnectionsOf(Chart));
        Assert.Empty(subscriptions.ConnectionsOf("none"));
        Assert.Empty(subscriptions.PartsConnectionsOf("none"));
    }

    [Fact]
    public async Task AClientThatImplementsTheHubInterfaceItself_IsNotBrokenByThePartsMethod()
    {
        // A default interface member: a class written against the interface before it existed still compiles and does nothing.
        IChartHubClient bare = new BareHubClient();

        await bare.UpdateChartParts(Chart, new ChartSvgParts(null, null, null, []));
    }
}
