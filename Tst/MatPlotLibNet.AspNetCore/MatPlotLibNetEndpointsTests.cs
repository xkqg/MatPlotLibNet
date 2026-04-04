// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.AspNetCore.Tests;

public class MatPlotLibNetEndpointsTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public MatPlotLibNetEndpointsTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddMatPlotLibNetSignalR();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapChartEndpoint("/api/chart/test", _ =>
                            Plt.Create()
                                .WithTitle("Test Chart")
                                .WithSize(800, 600)
                                .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
                                .Build());

                        endpoints.MapChartSvgEndpoint("/api/chart/test.svg", _ =>
                            Plt.Create()
                                .WithTitle("SVG Chart")
                                .Plot([1.0, 2.0], [3.0, 4.0])
                                .Build());
                    });
                });
            })
            .Start();

        _client = _host.GetTestClient();
    }

    [Fact]
    public async Task MapChartEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/chart/test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapChartEndpoint_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/api/chart/test");
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MapChartEndpoint_ReturnsValidJson()
    {
        var response = await _client.GetAsync("/api/chart/test");
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        Assert.Equal("Test Chart", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(800, doc.RootElement.GetProperty("width").GetDouble());
    }

    [Fact]
    public async Task MapChartEndpoint_IncludesSeriesData()
    {
        var response = await _client.GetAsync("/api/chart/test");
        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);
        var subPlots = doc.RootElement.GetProperty("subPlots");
        Assert.True(subPlots.GetArrayLength() > 0);

        var series = subPlots[0].GetProperty("series");
        Assert.True(series.GetArrayLength() > 0);
        Assert.Equal("line", series[0].GetProperty("type").GetString());
    }

    [Fact]
    public async Task MapChartSvgEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/chart/test.svg");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MapChartSvgEndpoint_ReturnsSvgContentType()
    {
        var response = await _client.GetAsync("/api/chart/test.svg");
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MapChartSvgEndpoint_ReturnsValidSvg()
    {
        var response = await _client.GetAsync("/api/chart/test.svg");
        var svg = await response.Content.ReadAsStringAsync();

        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
        Assert.Contains("SVG Chart", svg);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}
