// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

// Example GraphQL queries (paste into BananaCakePop at /graphql):
//   { chartSvg(chartId: "demo") }
//   { chartJson(chartId: "demo") }
//
// Example subscription:
//   subscription { onChartSvgUpdated(chartId: "live-sensor") }

using MatPlotLibNet;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.GraphQL;
using MatPlotLibNet.Styling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMatPlotLibNetGraphQL(chartId =>
    Plt.Create()
        .WithTitle($"Chart: {chartId}")
        .WithTheme(Theme.FiveThirtyEight)
        .Plot([1, 2, 3, 4, 5], [10, 25, 15, 30, 20],
            line => { line.Color = Color.Orange; line.Label = chartId; })
        .Build());

var app = builder.Build();

app.MapMatPlotLibNetGraphQL();

// Background publisher for subscription demo
_ = Task.Run(async () =>
{
    await Task.Delay(3000);
    var publisher = app.Services.GetRequiredService<IChartPublisher>();
    var random = new Random();
    while (true)
    {
        var data = Enumerable.Range(1, 5).Select(_ => random.NextDouble() * 50).ToArray();
        var figure = Plt.Create()
            .WithTitle($"Live ({DateTime.Now:HH:mm:ss})")
            .Plot([1, 2, 3, 4, 5], data, line => { line.Color = Color.Blue; })
            .Build();
        await publisher.PublishSvgAsync("live-sensor", figure);
        await Task.Delay(5000);
    }
});

Console.WriteLine("GraphQL playground: http://localhost:5000/graphql");
app.Run();
