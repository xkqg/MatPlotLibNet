# MatPlotLibNet.AspNetCore

ASP.NET Core integration for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Serves chart JSON specs and SVG output via minimal API endpoints, with optional real-time push via SignalR.

## Installation

```
dotnet add package MatPlotLibNet.AspNetCore
```

## Quick Start

### Register services and map endpoints

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMatPlotLibNetSignalR();

var app = builder.Build();

app.MapChartEndpoint();    // GET /chart?id=... -> JSON
app.MapChartSvgEndpoint(); // GET /chart/svg?id=... -> SVG
app.MapChartHub();         // SignalR hub at /charts-hub

app.Run();
```

### Publish real-time updates

```csharp
app.MapPost("/update", async (IChartPublisher publisher) =>
{
    var figure = Plt.Create()
        .WithTitle("Live Data")
        .Plot(x, y)
        .Build();

    await publisher.PublishAsync("dashboard-1", figure);
});
```

## API

| Type | Description |
|------|-------------|
| `SignalRExtensions.AddMatPlotLibNetSignalR()` | Registers SignalR services and `IChartPublisher` |
| `SignalRExtensions.MapChartHub()` | Maps the `ChartHub` SignalR endpoint |
| `MatPlotLibNetEndpoints.MapChartEndpoint()` | Maps a JSON chart endpoint |
| `MatPlotLibNetEndpoints.MapChartSvgEndpoint()` | Maps an SVG chart endpoint |
| `IChartPublisher` | Service for broadcasting chart updates |
| `ChartHub` | SignalR hub with `Subscribe`/`Unsubscribe` |

## License

[GPL-3.0](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
