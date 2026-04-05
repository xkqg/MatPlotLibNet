// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.AspNetCore;

/// <summary>Extension methods for registering MatPlotLibNet SignalR services and hub endpoints.</summary>
public static class SignalRExtensions
{
    /// <summary>Registers SignalR services, chart rendering services, and the <see cref="IChartPublisher"/> singleton for real-time chart updates.</summary>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMatPlotLibNetSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IChartSerializer, ChartSerializer>();
        services.AddSingleton<IChartRenderer, ChartRenderer>();
        services.AddSingleton<ISvgRenderer, SvgTransform>();
        services.AddSingleton<IChartPublisher, ChartPublisher>();
        return services;
    }

    /// <summary>Maps the <see cref="ChartHub"/> SignalR hub to the specified route pattern.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the hub endpoint (defaults to "/charts-hub").</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapChartHub(
        this IEndpointRouteBuilder endpoints, string pattern = "/charts-hub")
    {
        endpoints.MapHub<ChartHub>(pattern);
        return endpoints;
    }
}
