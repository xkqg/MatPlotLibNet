// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

/// <summary>Verifies <see cref="MatPlotLibNetEndpoints"/> behavior.</summary>
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

    /// <summary>Verifies that the chart JSON endpoint returns HTTP 200 OK.</summary>
    [Fact]
    public async Task MapChartEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/chart/test", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that the chart JSON endpoint returns application/json content type.</summary>
    [Fact]
    public async Task MapChartEndpoint_ReturnsJsonContentType()
    {
        var response = await _client.GetAsync("/api/chart/test", TestContext.Current.CancellationToken);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies that the chart JSON endpoint returns valid JSON with the expected title and width.</summary>
    [Fact]
    public async Task MapChartEndpoint_ReturnsValidJson()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/chart/test", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        var doc = JsonDocument.Parse(json);
        Assert.Equal("Test Chart", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal(800, doc.RootElement.GetProperty("width").GetDouble());
    }

    /// <summary>Verifies that the chart JSON endpoint includes subplot and series data in the response.</summary>
    [Fact]
    public async Task MapChartEndpoint_IncludesSeriesData()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/chart/test", ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        var doc = JsonDocument.Parse(json);
        var subPlots = doc.RootElement.GetProperty("subPlots");
        Assert.True(subPlots.GetArrayLength() > 0);

        var series = subPlots[0].GetProperty("series");
        Assert.True(series.GetArrayLength() > 0);
        Assert.Equal("line", series[0].GetProperty("type").GetString());
    }

    /// <summary>Verifies that the chart SVG endpoint returns HTTP 200 OK.</summary>
    [Fact]
    public async Task MapChartSvgEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/chart/test.svg", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Verifies that the chart SVG endpoint returns image/svg+xml content type.</summary>
    [Fact]
    public async Task MapChartSvgEndpoint_ReturnsSvgContentType()
    {
        var response = await _client.GetAsync("/api/chart/test.svg", TestContext.Current.CancellationToken);
        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>Verifies that the chart SVG endpoint returns valid SVG containing the chart title.</summary>
    [Fact]
    public async Task MapChartSvgEndpoint_ReturnsValidSvg()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/api/chart/test.svg", ct);
        var svg = await response.Content.ReadAsStringAsync(ct);

        Assert.StartsWith("<svg", svg.TrimStart());
        Assert.Contains("</svg>", svg);
        Assert.Contains("SVG Chart", svg);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync(CancellationToken.None);
        _host.Dispose();
    }
}
