// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Blazor.Tests.Infrastructure;
using MatPlotLibNet.Models.Series;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.Blazor.Tests;

/// <summary>The client subscribes as a viewer that patches and still hands every component a whole SVG: it keeps the last
/// whole document of a chart and puts each part the server sends in place in it. Against a server from before parts existed
/// it subscribes the old way.</summary>
public sealed class ChartSubscriptionClientPartsTests : IClassFixture<StreamingHostFixture>
{
    private readonly StreamingHostFixture _fixture;

    public ChartSubscriptionClientPartsTests(StreamingHostFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task APartThatChanged_IsHandedOnInsideTheWholeDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var client = new ChartSubscriptionClient();
        var received = new BlockingCollection<string>();
        client.OnSvgUpdated((_, svg) => { received.Add(svg); return Task.CompletedTask; });
        await client.ConnectAsync(_fixture.HubUrl, ct);
        await client.SubscribeAsync("parts-chart", ct);

        var figure = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2, 3], [2.0, 1, 3]))
            .Build();
        await _fixture.Publisher.PublishSvgAsync("parts-chart", figure, ct);
        Assert.True(received.TryTake(out var first, TimeSpan.FromSeconds(10)), "no first document");
        // The component is handed the plain document, as ToSvg writes it: the parts are the wire's business.
        Assert.DoesNotContain("data-mpl-part", first);
        Assert.Equal(figure.ToSvg(), first);

        ((LineSeries)figure.SubPlots[1].Series[0]).Color = MatPlotLibNet.Styling.Colors.Red;
        await _fixture.Publisher.PublishSvgAsync("parts-chart", figure, ct);

        Assert.True(received.TryTake(out var second, TimeSpan.FromSeconds(10)), "no second document");
        Assert.NotEqual(first, second);
        Assert.Contains("#FF0000", second, StringComparison.OrdinalIgnoreCase);
        // The part that came over the wire, put in place in the document the client held, is the figure's picture as it is now.
        Assert.Equal(figure.ToSvg(), second);
    }

    [Fact]
    public async Task APartForAChartHeldWhole_IsNotHandedOn_WhenThereIsNoDocumentToPutItIn()
    {
        await using var client = new ChartSubscriptionClient();
        var received = new List<string>();
        client.OnSvgUpdated((_, svg) => { received.Add(svg); return Task.CompletedTask; });

        await client.HandleSvgUpdatedAsync("plain", "<svg><rect/></svg>");
        await client.HandlePartsAsync("plain", new ChartSubscriptionClient.SvgPartsMessage(null, null, null, []));
        await client.HandlePartsAsync("never seen", new ChartSubscriptionClient.SvgPartsMessage(null, null, null, []));

        Assert.Equal(["<svg><rect/></svg>"], received);
    }

    [Fact]
    public async Task AgainstAServerWithoutParts_ItSubscribesTheOldWay()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services => services.AddSignalR());
                web.Configure(app => app.UseRouting().UseEndpoints(endpoints => endpoints.MapHub<OldHub>("/old-hub")));
            })
            .Start();
        var server = host.GetTestServer();
        await using var client = new ChartSubscriptionClient(new RecordingHubConnector(connect: hubUrl => new HubConnectionBuilder()
            .WithUrl(hubUrl, options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build()));
        await client.ConnectAsync("http://localhost/old-hub", ct);

        await client.SubscribeAsync("old-chart", ct);

        Assert.Equal(["old-chart"], OldHub.Subscribed);
    }


    /// <summary>A hub as a server before parts existed has it: only Subscribe.</summary>
    private sealed class OldHub : Hub
    {
        public static readonly ConcurrentBag<string> Subscribed = [];

        public Task Subscribe(string chartId)
        {
            Subscribed.Add(chartId);
            return Task.CompletedTask;
        }
    }
}
