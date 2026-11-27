// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR.Client;
using MatPlotLibNet.Interaction;

namespace MatPlotLibNet.Interactive.Tests;

/// <summary>The page a figure is opened in reaches the hub of the server that served it: on the same origin, so no browser treats
/// it as a cross-origin call, and the hub answers there. The page named <c>localhost</c> while the server listens on, and the
/// figure is opened at, <c>127.0.0.1</c>: another origin, and on a machine where <c>localhost</c> resolves to <c>::1</c> first,
/// another address.</summary>
public partial class ChartPageHubTests
{
    [GeneratedRegex(@"\.withUrl\(""(?<url>[^""]+)""\)")]
    private static partial Regex HubUrl();

    [Fact]
    public async Task ThePage_ReachesTheHub_OnTheOriginItWasServedFrom()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var server = new ChartServer();
        await server.EnsureStartedAsync(ct);
        string chartId = server.RegisterFigure(Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build());
        var page = new Uri(server.GetFigureUrl(chartId));

        using var http = new HttpClient();
        string html = await http.GetStringAsync(page, ct);
        var hub = new Uri(page, HubUrl().Match(html).Groups["url"].Value);

        Assert.Equal(page.GetLeftPart(UriPartial.Authority), hub.GetLeftPart(UriPartial.Authority));
        var connection = new HubConnectionBuilder().WithUrl(hub).Build();
        await connection.StartAsync(ct);
        await connection.InvokeAsync(HubMethodNames.SubscribeToParts, chartId, ct);
        await connection.DisposeAsync();
    }
}
