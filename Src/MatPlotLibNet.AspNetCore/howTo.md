# How to use MatPlotLibNet.AspNetCore

## Install

```
dotnet add package MatPlotLibNet.AspNetCore
```

## 1. Serve charts as JSON or SVG endpoints

Use the `MapChartEndpoint` and `MapChartSvgEndpoint` extension methods to expose charts via minimal API routes. Each takes a route pattern and a factory delegate that builds a `Figure` from the `HttpContext`.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Styling;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// GET /api/chart -> JSON figure spec
app.MapChartEndpoint("/api/chart", ctx =>
{
    var data = GetSalesData();   // your data source
    return Plt.Create()
        .WithTitle("Sales")
        .WithTheme(Theme.Default)
        .Bar(data.Labels, data.Values)
        .Build();
});

// GET /api/chart.svg -> SVG image
app.MapChartSvgEndpoint("/api/chart.svg", ctx =>
{
    return Plt.Create()
        .WithTitle("Sales")
        .Bar(["Q1", "Q2", "Q3"], [100, 200, 150])
        .Build();
});

app.Run();
```

### Use query parameters

The factory receives the full `HttpContext`, so you can read query strings, route values, or headers:

```csharp
app.MapChartSvgEndpoint("/api/chart/{type}.svg", ctx =>
{
    var chartType = ctx.GetRouteValue("type")?.ToString();
    var theme = ctx.Request.Query["theme"].FirstOrDefault() == "dark"
        ? Theme.Dark : Theme.Default;

    return chartType switch
    {
        "sales" => BuildSalesChart(theme),
        "traffic" => BuildTrafficChart(theme),
        _ => Plt.Create().WithTitle("Unknown").Build()
    };
});
```

### Embed SVG in HTML

```html
<!-- Direct embed -->
<img src="/api/chart.svg" alt="Sales chart" />

<!-- Or fetch from Angular / React -->
<object type="image/svg+xml" data="/api/chart.svg"></object>
```

### Consume JSON from Angular / React

```typescript
const response = await fetch('/api/chart');
const spec = await response.json();
// Use the spec to render client-side
```

## 2. Real-time charts with SignalR

### Register services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adds SignalR + IChartPublisher singleton
builder.Services.AddMatPlotLibNetSignalR();

var app = builder.Build();

// Maps the ChartHub at /charts-hub (customizable)
app.MapChartHub();
// Or with a custom path:
app.MapChartHub("/my-hub");

app.Run();
```

### Publish chart updates

Inject `IChartPublisher` anywhere (controllers, minimal API handlers, background services) to push updates to connected clients:

```csharp
app.MapPost("/api/update-chart", async (IChartPublisher publisher) =>
{
    var figure = Plt.Create()
        .WithTitle("Live Sensor Data")
        .Plot(timestamps, values, l => l.Color = Color.Red)
        .Build();

    // Sends the figure as JSON -- client deserializes and renders
    await publisher.PublishAsync("sensor-1", figure);

    return Results.Ok();
});
```

Or send pre-rendered SVG (saves client-side rendering):

```csharp
await publisher.PublishSvgAsync("sensor-1", figure);
```

### Background service example

```csharp
public class SensorMonitor(IChartPublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var readings = await ReadSensorsAsync(ct);

            var figure = Plt.Create()
                .WithTitle($"Sensors -- {DateTime.Now:HH:mm:ss}")
                .Plot(readings.Time, readings.Values)
                .Build();

            await publisher.PublishAsync("sensor-dashboard", figure, ct);

            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
```

Register it:

```csharp
builder.Services.AddHostedService<SensorMonitor>();
```

## 3. Client-side subscription

### From Blazor

Use the `MplLiveChart` component from `MatPlotLibNet.Blazor`:

```razor
<MplLiveChart ChartId="sensor-1" HubUrl="/charts-hub" />
```

### From JavaScript / TypeScript

```typescript
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
    .withUrl('/charts-hub')
    .withAutomaticReconnect()
    .build();

connection.on('UpdateChartSvg', (chartId: string, svg: string) => {
    document.getElementById(chartId)!.innerHTML = svg;
});

connection.on('UpdateChart', (chartId: string, json: string) => {
    // Parse and render client-side
});

await connection.start();
await connection.invoke('Subscribe', 'sensor-1');
```

## 4. API reference

| Type | Description |
|------|-------------|
| `AddMatPlotLibNetSignalR()` | Registers SignalR services and `IChartPublisher` in DI |
| `MapChartHub(pattern)` | Maps the `ChartHub` endpoint (default `"/charts-hub"`) |
| `MapChartEndpoint(pattern, factory)` | Maps a GET endpoint returning figure JSON |
| `MapChartSvgEndpoint(pattern, factory)` | Maps a GET endpoint returning figure SVG |
| `IChartPublisher` | Service interface for pushing updates |
| `PublishAsync(chartId, figure)` | Broadcasts figure as JSON to subscribers |
| `PublishSvgAsync(chartId, figure)` | Broadcasts figure as pre-rendered SVG |
| `ChartHub` | SignalR hub; clients call `Subscribe(chartId)` / `Unsubscribe(chartId)` |
