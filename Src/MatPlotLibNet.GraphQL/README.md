# MatPlotLibNet.GraphQL

GraphQL integration for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library using [HotChocolate](https://chillicream.com/docs/hotchocolate). Provides queries for fetching chart SVG and JSON, plus subscriptions for real-time updates.

## Installation

```
dotnet add package MatPlotLibNet.GraphQL
```

## Server Setup

```csharp
builder.Services.AddMatPlotLibNetGraphQL(chartId =>
    Plt.Create()
        .WithTitle($"Chart {chartId}")
        .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 15.0])
        .Build());

app.MapMatPlotLibNetGraphQL();
```

## Queries

```graphql
# Fetch chart as SVG
{ chartSvg(chartId: "sensor-1") }

# Fetch chart as JSON spec
{ chartJson(chartId: "sensor-1") }
```

## Subscriptions

```graphql
# Subscribe to SVG updates
subscription { onChartSvgUpdated(chartId: "sensor-1") }

# Subscribe to JSON updates
subscription { onChartUpdated(chartId: "sensor-1") }
```

## License

[GPL-3.0](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
