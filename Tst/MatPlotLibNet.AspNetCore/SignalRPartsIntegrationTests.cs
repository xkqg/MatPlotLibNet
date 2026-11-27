// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.AspNetCore.Tests;

/// <summary>The parts protocol end to end, through a real hub and the publisher the container makes: a client that subscribes
/// with <c>SubscribeToParts</c> receives the whole wrapped document, then only the part that changed, carried over the wire
/// with its index and its markup; a client that subscribes the old way receives the plain document each time something
/// changed.</summary>
public sealed class SignalRPartsIntegrationTests : IAsyncDisposable
{
    private readonly IHost _host = SignalRTestHost.Start();

    private HubConnection Connect() => _host.ConnectToTheHub();

    [Fact]
    public async Task AViewerThatPatches_GetsTheWholeDocument_ThenOnlyThePartThatChanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var documents = new BlockingCollection<string>();
        var patches = new BlockingCollection<ChartSvgParts>();
        var plain = new BlockingCollection<string>();
        await using var patching = Connect();
        patching.On<string, string>("UpdateChartSvg", (_, svg) => documents.Add(svg));
        patching.On<string, ChartSvgParts>("UpdateChartParts", (_, parts) => patches.Add(parts));
        await using var old = Connect();
        old.On<string, string>("UpdateChartSvg", (_, svg) => plain.Add(svg));
        await patching.StartAsync(ct);
        await old.StartAsync(ct);
        await patching.InvokeAsync("SubscribeToParts", "wall", ct);
        await old.InvokeAsync("Subscribe", "wall", ct);

        var publisher = _host.Services.GetRequiredService<IChartPublisher>();
        var figure = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2, 3], [1.0, 3, 2]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2, 3], [2.0, 1, 3]))
            .Build();
        await publisher.PublishSvgAsync("wall", figure, ct);

        Assert.True(documents.TryTake(out var whole, TimeSpan.FromSeconds(10)), "no whole document");
        Assert.Contains("<g data-mpl-part=\"axes-1\">", whole);
        Assert.True(plain.TryTake(out var first, TimeSpan.FromSeconds(10)), "no plain document");
        Assert.DoesNotContain("data-mpl-part", first);

        ((LineSeries)figure.SubPlots[1].Series[0]).Color = MatPlotLibNet.Styling.Colors.Red;
        await publisher.PublishSvgAsync("wall", figure, ct);

        Assert.True(patches.TryTake(out var patch, TimeSpan.FromSeconds(10)), "no patch");
        var changed = Assert.Single(patch!.Axes);
        Assert.Equal(1, changed.Index);
        Assert.StartsWith("<g data-mpl-part=\"axes-1\">", changed.Markup);
        Assert.Null(patch.Background);
        Assert.True(plain.TryTake(out var second, TimeSpan.FromSeconds(10)), "no second plain document");
        Assert.Equal(figure.ToSvg(), second);
        Assert.False(documents.TryTake(out _, TimeSpan.FromMilliseconds(200)), "the patching viewer was sent the whole document again");
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
