# How to use MatPlotLibNet.GraphQL

## Install

```
dotnet add package MatPlotLibNet.GraphQL
```

## 1. Register services

```csharp
builder.Services.AddMatPlotLibNetGraphQL(chartId =>
    Plt.Create()
        .WithTitle($"Chart {chartId}")
        .Plot([1, 2, 3, 4, 5], [10, 25, 15, 30, 20])
        .Build());
```

The factory function receives the `chartId` from the GraphQL query and returns a `Figure`.

## 2. Map the endpoint

```csharp
app.MapMatPlotLibNetGraphQL();           // maps to /graphql
app.MapMatPlotLibNetGraphQL("/gql");     // or custom path
```

Open `/graphql` in a browser for the BananaCakePop playground.

## 3. Queries

Fetch a chart as SVG:

```graphql
{
  chartSvg(chartId: "sales")
}
```

Fetch a chart as JSON specification:

```graphql
{
  chartJson(chartId: "sales")
}
```

## 4. Subscriptions

Subscribe to real-time SVG updates:

```graphql
subscription {
  onChartSvgUpdated(chartId: "sensor-1")
}
```

Subscribe to real-time JSON updates:

```graphql
subscription {
  onChartUpdated(chartId: "sensor-1")
}
```

Subscriptions receive events when `IChartPublisher.PublishSvgAsync` or `PublishAsync` is called on the server.

## 5. Publishing updates (server side)

Use `IChartPublisher` to push updates to GraphQL subscribers:

```csharp
app.MapPost("/api/update", async (IChartPublisher publisher) =>
{
    var figure = Plt.Create()
        .WithTitle($"Updated at {DateTime.Now:HH:mm:ss}")
        .Plot([1, 2, 3], [10, 20, 15])
        .Build();

    await publisher.PublishSvgAsync("sensor-1", figure);
    return Results.Ok();
});
```

## 6. Background service

Push periodic updates via a hosted service:

```csharp
builder.Services.AddHostedService<ChartUpdateService>();

public class ChartUpdateService(IChartPublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            var data = Enumerable.Range(1, 5).Select(_ => random.NextDouble() * 100).ToArray();
            var figure = Plt.Create()
                .WithTitle($"Live ({DateTime.Now:HH:mm:ss})")
                .Plot([1, 2, 3, 4, 5], data)
                .Build();
            await publisher.PublishSvgAsync("live-sensor", figure, ct);
            await Task.Delay(5000, ct);
        }
    }
}
```

## 7. Combining with SignalR

GraphQL and SignalR can coexist. Register both:

```csharp
builder.Services.AddMatPlotLibNetSignalR();
builder.Services.AddMatPlotLibNetGraphQL(chartId => BuildChart(chartId));

app.MapChartHub();                    // SignalR at /charts-hub
app.MapMatPlotLibNetGraphQL();        // GraphQL at /graphql
```

Use `GraphQLChartPublisher` to push events to both systems simultaneously by decorating the default publisher.

## 8. Custom figure factory

The factory can use any logic -- database lookups, external APIs, etc.:

```csharp
builder.Services.AddMatPlotLibNetGraphQL(chartId =>
{
    var data = GetSensorData(chartId);  // your data source
    return Plt.Create()
        .WithTitle(data.Name)
        .WithTheme(Theme.Dark)
        .Plot(data.Timestamps, data.Values)
        .Build();
});
```
