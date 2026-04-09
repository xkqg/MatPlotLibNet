// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using MatPlotLibNet.GraphQL;

namespace MatPlotLibNet.GraphQL.Tests;

/// <summary>Verifies <see cref="GraphQLExtensions"/> behavior.</summary>
public class GraphQLExtensionsTests
{
    /// <summary>Verifies that AddMatPlotLibNetGraphQL registers the IChartEventSender service.</summary>
    [Fact]
    public void AddMatPlotLibNetGraphQL_RegistersChartEventSender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMatPlotLibNetGraphQL(_ => Plt.Create().Build());

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<IChartEventSender>();

        Assert.NotNull(sender);
    }

    /// <summary>Verifies that AddMatPlotLibNetGraphQL registers the HotChocolate request executor resolver.</summary>
    [Fact]
    public void AddMatPlotLibNetGraphQL_RegistersGraphQLServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMatPlotLibNetGraphQL(_ => Plt.Create().Build());

        var provider = services.BuildServiceProvider();
        var executor = provider.GetService<HotChocolate.Execution.IRequestExecutorResolver>();

        Assert.NotNull(executor);
    }

    /// <summary>Verifies that AddMatPlotLibNetGraphQL stores and invokes the figure factory delegate.</summary>
    [Fact]
    public void AddMatPlotLibNetGraphQL_StoresFigureFactory()
    {
        var called = false;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMatPlotLibNetGraphQL(_ =>
        {
            called = true;
            return Plt.Create().Build();
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<ChartFigureFactory>();

        Assert.NotNull(factory);
        factory!.Create("test");
        Assert.True(called);
    }
}
