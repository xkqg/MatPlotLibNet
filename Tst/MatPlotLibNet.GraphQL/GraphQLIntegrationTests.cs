// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MatPlotLibNet.GraphQL;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Verifies <see cref="GraphQLExtensions"/> integration behavior.</summary>
public class GraphQLIntegrationTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public GraphQLIntegrationTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddMatPlotLibNetGraphQL(chartId =>
                        Plt.Create()
                            .WithTitle($"Chart {chartId}")
                            .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
                            .Build());
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapMatPlotLibNetGraphQL();
                    });
                });
            })
            .Start();

        _client = _host.GetTestClient();
    }

    /// <summary>Verifies that the chartSvg GraphQL query returns valid SVG containing the chart title.</summary>
    [Fact]
    public async Task ChartSvgQuery_ReturnsValidSvg()
    {
        var query = new { query = "{ chartSvg(chartId: \"test\") }" };
        var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/graphql", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(json);
        var svg = doc.RootElement.GetProperty("data").GetProperty("chartSvg").GetString();
        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("Chart test", svg);
    }

    /// <summary>Verifies that the chartJson GraphQL query returns valid JSON containing the chart title.</summary>
    [Fact]
    public async Task ChartJsonQuery_ReturnsValidJson()
    {
        var query = new { query = "{ chartJson(chartId: \"sensor-1\") }" };
        var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/graphql", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var doc = JsonDocument.Parse(json);
        var chartJson = doc.RootElement.GetProperty("data").GetProperty("chartJson").GetString();
        Assert.NotNull(chartJson);
        Assert.Contains("Chart sensor-1", chartJson);
    }

    /// <summary>Verifies that the GraphQL endpoint returns HTTP 200 OK for a valid query.</summary>
    [Fact]
    public async Task GraphQLEndpoint_ReturnsOk()
    {
        var query = new { query = "{ chartSvg(chartId: \"x\") }" };
        var content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/graphql", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync(TestContext.Current.CancellationToken);
        _host.Dispose();
    }
}
