// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using MatPlotLibNet.Models;
using MatPlotLibNet.Tests.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>One GraphQL host for the whole class; the chart id it is asked for names the composed chart to build, as
/// <c>name:missing</c> (<c>lollipop:False</c>).</summary>
public sealed class ComposedChartGraphQLFixture : IAsyncDisposable
{
    public IHost Host { get; }

    public ComposedChartGraphQLFixture()
    {
        Host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddMatPlotLibNetGraphQL(chartId =>
                    {
                        string[] parts = chartId.Split(':');
                        return ComposedChartShapes.Named(parts[0]).Build(bool.Parse(parts[1])).Build();
                    });
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMatPlotLibNetGraphQL());
                });
            })
            .Start();
    }

    public async ValueTask DisposeAsync()
    {
        await Host.StopAsync(CancellationToken.None);
        Host.Dispose();
    }
}

/// <summary>A composed chart is its parts in JSON, and a GraphQL client asking for a chart gets either its picture or its
/// JSON. Every shape asked for through the <c>chartSvg</c> and <c>chartJson</c> queries must come out as the picture the
/// server draws, the JSON drawing it again on the client, with and without a missing value.</summary>
public class ComposedChartQueryTests : IClassFixture<ComposedChartGraphQLFixture>
{
    private readonly ComposedChartGraphQLFixture _fixture;

    public ComposedChartQueryTests(ComposedChartGraphQLFixture fixture) => _fixture = fixture;

    private async Task<string> QueryAsync(string field, string chartId)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _fixture.Host.GetTestClient();
        var body = new StringContent(JsonSerializer.Serialize(new { query = $"{{ {field}(chartId: \"{chartId}\") }}" }), Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/graphql", body, ct);

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.False(document.RootElement.TryGetProperty("errors", out var errors), $"{field}({chartId}) answered with errors: {errors}");
        return document.RootElement.GetProperty("data").GetProperty(field).GetString()!;
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheSvgQuery_ReturnsTheFiguresPicture(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();

        string svg = await QueryAsync("chartSvg", $"{name}:{missing}");

        Assert.Equal(figure.ToSvg(), svg);
    }

    [Theory]
    [MemberData(nameof(ComposedChartShapes.NamesAndVariants), MemberType = typeof(ComposedChartShapes))]
    public async Task TheJsonQuery_ReturnsAFigureThatDrawsTheSamePicture(string name, bool missing)
    {
        var figure = ComposedChartShapes.Named(name).Build(missing).Build();

        string json = await QueryAsync("chartJson", $"{name}:{missing}");

        Assert.Equal(figure.ToSvg(), ChartSerializerDefault.Current.FromJson(json).ToSvg());
    }
}
