// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Extension methods for mapping chart rendering endpoints in ASP.NET Core minimal APIs.</summary>
public static class MatPlotLibNetEndpoints
{
    /// <summary>Maps a GET endpoint that returns the figure as JSON at the specified route pattern.</summary>
    public static IEndpointRouteBuilder MapChartEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, Figure> figureFactory)
        => MapChartEndpointCore(endpoints, pattern, figureFactory,
            (f, sp) => sp.GetRequiredService<IChartSerializer>().ToJson(f), "application/json");

    /// <summary>Maps a GET endpoint that returns the figure as SVG at the specified route pattern.</summary>
    public static IEndpointRouteBuilder MapChartSvgEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, Figure> figureFactory)
        => MapChartEndpointCore(endpoints, pattern, figureFactory,
            (f, sp) => sp.GetRequiredService<ISvgRenderer>().Render(f), "image/svg+xml");

    /// <summary>Maps a GET endpoint that returns the figure's DATA as an HTML table at the specified route -
    /// the accessible alternative that carries what the picture carries, for a reader the SVG cannot reach.
    /// The body is a fragment (one <c>&lt;table&gt;</c> per group of series), so the page that embeds it supplies
    /// the CSS; the charset is declared on the header because a fragment has nowhere to declare its own.</summary>
    public static IEndpointRouteBuilder MapChartTableEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, Figure> figureFactory)
        => MapChartEndpointCore(endpoints, pattern, figureFactory,
            (f, _) => string.Concat(f.ToDataTables().Select(table => table.ToHtml())), "text/html; charset=utf-8");

    private static IEndpointRouteBuilder MapChartEndpointCore(
        IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, Figure> figureFactory,
        Func<Figure, IServiceProvider, string> serialize,
        string contentType)
    {
        endpoints.MapGet(pattern, (HttpContext ctx) =>
        {
            var figure = figureFactory(ctx);
            var output = serialize(figure, ctx.RequestServices);
            return Results.Content(output, contentType);
        });
        return endpoints;
    }
}
