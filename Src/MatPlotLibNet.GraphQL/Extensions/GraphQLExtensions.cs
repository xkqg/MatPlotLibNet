// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.GraphQL;

/// <summary>Extension methods for registering MatPlotLibNet GraphQL services and endpoints.</summary>
public static class GraphQLExtensions
{
    /// <summary>Registers HotChocolate GraphQL services with chart query and subscription types.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="figureFactory">Factory function that creates a <see cref="Figure"/> for a given chart identifier.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMatPlotLibNetGraphQL(
        this IServiceCollection services,
        Func<string, Figure> figureFactory)
    {
        services.AddSingleton(new ChartFigureFactory(figureFactory));
        services.AddSingleton<IChartSerializer, ChartSerializer>();
        services.AddSingleton<IChartRenderer, ChartRenderer>();
        services.AddSingleton<ISvgRenderer, SvgTransform>();
        services.AddSingleton<IChartEventSender, ChartEventSender>();
        services.AddSingleton<IChartPublisher, ChartPublisher>();

        services
            .AddGraphQLServer()
            .AddQueryType<ChartQueryType>()
            .AddSubscriptionType<ChartSubscriptionType>()
            .AddInMemorySubscriptions();

        return services;
    }

    /// <summary>Maps the MatPlotLibNet GraphQL endpoint to the specified route pattern.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The URL pattern for the GraphQL endpoint (defaults to "/graphql").</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapMatPlotLibNetGraphQL(
        this IEndpointRouteBuilder endpoints, string pattern = "/graphql")
    {
        endpoints.MapGraphQL(pattern);
        return endpoints;
    }
}
