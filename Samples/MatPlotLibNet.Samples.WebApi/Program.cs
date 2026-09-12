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

// The same chart as its DATA - an HTML table fragment. A client that cannot render an SVG, a reader using a
// screen reader, and a spreadsheet all want this endpoint, not the picture.
app.MapChartTableEndpoint("/api/chart/sales.table", _ =>
    Plt.Create()
        .WithTitle("Monthly Sales")
        .WithTheme(Theme.Seaborn)
        .Plot([1, 2, 3, 4, 5, 6], [120, 340, 250, 410, 380, 520],
            line => { line.Color = Colors.Blue; line.Label = "Revenue ($k)"; })
        .Build());

// International text without the Skia package: this project renders SVG with <text> elements, and the library marks
// a right-to-left title with direction="rtl" so the browser shapes and orders it. The Hebrew legend entry and the
// Latin axis label sit on one chart. The pre-push gate probes this endpoint for that attribute.
app.MapChartSvgEndpoint("/api/chart/international.svg", _ =>
    Plt.Create()
        .WithTitle("درجة الحرارة")   // Arabic: "temperature"
        .WithTheme(Theme.Seaborn)
        .AddSubPlot(1, 1, 1, ax =>
        {
            ax.SetXLabel("Quarter");
            ax.SetYLabel("°C");
            ax.Plot([1, 2, 3, 4], [12.5, 14.0, 13.2, 15.1], s => s.Label = "תל אביב");   // Hebrew: "Tel Aviv"
            ax.Plot([1, 2, 3, 4], [9.8, 11.2, 10.5, 12.0], s => s.Label = "Amsterdam");
            ax.WithLegend();
        })
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

Console.WriteLine("Endpoints: GET /api/chart/sales (JSON), GET /api/chart/sales.svg (SVG), GET /api/chart/sales.table (HTML data table)");
Console.WriteLine("SignalR hub: /charts-hub (subscribe to 'sensor-1' for live updates)");
app.Run();
