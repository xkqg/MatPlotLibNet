# How to use MatPlotLibNet.Blazor

## Install

```
dotnet add package MatPlotLibNet.Blazor
```

## 1. Static chart with `MplChart`

The `MplChart` component renders a `Figure` as inline SVG. It re-renders automatically when the `Figure` parameter changes.

```razor
@page "/dashboard"
@using MatPlotLibNet
@using MatPlotLibNet.Models
@using MatPlotLibNet.Styling
@using MatPlotLibNet.Blazor

<h3>Sales Report</h3>
<MplChart Figure="@_figure" CssClass="my-chart" />

@code {
    private Figure _figure = Plt.Create()
        .WithTitle("Quarterly Revenue")
        .WithTheme(Theme.Seaborn)
        .Bar(["Q1", "Q2", "Q3", "Q4"], [150, 230, 180, 310])
        .Build();
}
```

### Styling the container

The component wraps SVG in a `<div class="mpl-chart ...">`. Use `CssClass` to add your own classes:

```css
.my-chart {
    max-width: 800px;
    margin: 0 auto;
    border: 1px solid #e0e0e0;
    border-radius: 8px;
    padding: 16px;
}
```

## 2. Dynamic updates

Reassign the `Figure` parameter and Blazor handles the rest:

```razor
<MplChart Figure="@_figure" />
<button @onclick="Refresh">Refresh</button>

@code {
    private Figure _figure = BuildChart(GetData());

    private void Refresh()
    {
        _figure = BuildChart(GetData());
    }

    private static Figure BuildChart(double[] data) =>
        Plt.Create()
            .WithTitle("Sensor Readings")
            .Plot(Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray(), data)
            .Build();
}
```

## 3. Extension method: `ToMarkupString()`

If you need to render a figure outside of the `MplChart` component, use the extension method directly:

```razor
@using MatPlotLibNet.Blazor

<div class="chart-wrapper">
    @figure.ToMarkupString()
</div>

@code {
    private Figure figure = Plt.Create()
        .WithTitle("Inline SVG")
        .Plot(x, y)
        .Build();
}
```

## 4. Real-time chart with `MplLiveChart`

`MplLiveChart` connects to a SignalR hub and updates automatically when the server pushes new data.

### Server setup (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddMatPlotLibNetSignalR();

var app = builder.Build();
app.MapChartHub();   // maps /charts-hub
```

See the [MatPlotLibNet.AspNetCore howTo](../MatPlotLibNet.AspNetCore/howTo.md) for full server configuration.

### Blazor component

```razor
@using MatPlotLibNet.Blazor

<MplLiveChart ChartId="sensor-1"
              HubUrl="/charts-hub"
              CssClass="live-chart" />
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChartId` | `string` | (required) | Identifies which chart to subscribe to |
| `HubUrl` | `string` | `"/charts-hub"` | URL of the SignalR hub endpoint |
| `CssClass` | `string?` | `null` | Additional CSS classes for the wrapper div |
| `InitialFigure` | `Figure?` | `null` | Figure to display before the first server push |

### How it works

1. On first render, connects to the SignalR hub at `HubUrl`
2. Subscribes to the group identified by `ChartId`
3. Listens for `UpdateChartSvg` (pre-rendered SVG) and `UpdateChart` (JSON figure spec) messages
4. Re-renders when a matching update arrives
5. Unsubscribes and disposes the connection on component disposal

### Show an initial chart while waiting

```razor
<MplLiveChart ChartId="sensor-1"
              InitialFigure="@_placeholder" />

@code {
    private Figure _placeholder = Plt.Create()
        .WithTitle("Waiting for data...")
        .Build();
}
```

## 5. Multiple charts on one page

```razor
<div class="dashboard-grid">
    <MplChart Figure="@_sales" CssClass="card" />
    <MplChart Figure="@_traffic" CssClass="card" />
    <MplLiveChart ChartId="cpu" CssClass="card" />
    <MplLiveChart ChartId="memory" CssClass="card" />
</div>
```

Each `MplLiveChart` maintains its own SignalR connection and subscription.
