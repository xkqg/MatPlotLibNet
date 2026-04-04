# MatPlotLibNet.Blazor

Blazor components for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Renders charts as inline SVG with optional real-time updates via SignalR.

## Installation

```
dotnet add package MatPlotLibNet.Blazor
```

## Components

### MplChart -- static chart

```razor
@using MatPlotLibNet
@using MatPlotLibNet.Blazor

<MplChart Figure="@_figure" CssClass="my-chart" />

@code {
    private Figure _figure = Plt.Create()
        .WithTitle("Sales")
        .Bar(["Q1", "Q2", "Q3"], [100, 200, 150])
        .Build();
}
```

### MplLiveChart -- real-time updates via SignalR

```razor
<MplLiveChart ChartId="dashboard-1"
              HubUrl="/charts-hub"
              CssClass="live-chart" />
```

Connects to a `ChartHub` endpoint and updates automatically when the server publishes new data.

### Extension method

```csharp
@((MarkupString)figure.ToMarkupString())
```

`ToMarkupString()` converts a `Figure` to an SVG `MarkupString` for direct rendering.

## Dependencies

- `MatPlotLibNet` (core)
- `Microsoft.AspNetCore.SignalR.Client`

## License

[GPL-3.0](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
