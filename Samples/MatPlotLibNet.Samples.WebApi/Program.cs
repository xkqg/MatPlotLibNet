// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Styling;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMatPlotLibNetSignalR();

var app = builder.Build();

// Static chart endpoint (JSON)
app.MapChartEndpoint("/api/chart/sales", _ =>
    Plt.Create()
        .WithTitle("Monthly Sales")
        .WithTheme(Theme.Seaborn)
        .Plot([1, 2, 3, 4, 5, 6], [120, 340, 250, 410, 380, 520],
            line => { line.Color = Colors.Blue; line.Label = "Revenue ($k)"; })
        .Build());

// Static chart endpoint (SVG)
app.MapChartSvgEndpoint("/api/chart/sales.svg", _ =>
    Plt.Create()
        .WithTitle("Monthly Sales")
        .WithTheme(Theme.Seaborn)
        .Plot([1, 2, 3, 4, 5, 6], [120, 340, 250, 410, 380, 520],
            line => { line.Color = Colors.Blue; line.Label = "Revenue ($k)"; })
        .Build());

// SignalR hub for real-time updates
app.MapChartHub();

// Background service that publishes live updates every 5 seconds
_ = Task.Run(async () =>
{
    await Task.Delay(2000);
    var publisher = app.Services.GetRequiredService<IChartPublisher>();
    var random = new Random();
    while (true)
    {
        var data = Enumerable.Range(1, 10).Select(i => random.NextDouble() * 100).ToArray();
        var figure = Plt.Create()
            .WithTitle($"Live Sensor Data ({DateTime.Now:HH:mm:ss})")
            .Plot(Enumerable.Range(1, 10).Select(i => (double)i).ToArray(), data,
                line => { line.Color = Colors.Orange; })
            .Build();
        await publisher.PublishSvgAsync("sensor-1", figure);
        await Task.Delay(5000);
    }
});

Console.WriteLine("Endpoints: GET /api/chart/sales (JSON), GET /api/chart/sales.svg (SVG)");
Console.WriteLine("SignalR hub: /charts-hub (subscribe to 'sensor-1' for live updates)");
app.Run();
