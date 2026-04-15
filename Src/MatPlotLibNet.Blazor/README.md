# MatPlotLibNet.Blazor

Blazor components for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Renders charts as inline SVG with optional real-time server push **and** — as of **v1.2.0** — bidirectional interaction: zoom / pan / reset / legend-toggle events flow from the browser back to the .NET server, which mutates the authoritative `Figure` and streams the updated SVG back.

## Installation

```
dotnet add package MatPlotLibNet.Blazor
```

## Components

### MplChart — static chart

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

### MplLiveChart — one-way server push via SignalR

```razor
<MplLiveChart ChartId="dashboard-1"
              HubUrl="/charts-hub"
              CssClass="live-chart" />
```

Connects to a `ChartHub` endpoint and updates automatically when the server calls `IChartPublisher.PublishSvgAsync` for this chart id.

### Bidirectional interaction (v1.2.0)

Add `.WithServerInteraction(...)` on the figure builder and let the browser drive the server. The embedded dispatcher script inside the SVG invokes the hub's `OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` methods; the server mutates the registered figure and publishes the new SVG through the same fan-out path `MplLiveChart` already listens on.

```razor
@page "/interactive"
@using MatPlotLibNet.AspNetCore
@using MatPlotLibNet.Rendering.Svg
@using MatPlotLibNet.Transforms
@inject FigureRegistry Registry
@implements IDisposable

<div id="host">@((MarkupString)_svg)</div>

<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"></script>
<script>
    (function () {
        var conn = new signalR.HubConnectionBuilder()
            .withUrl('/charts-hub').withAutomaticReconnect().build();
        window.__mpl_signalr_connection = conn;
        conn.on('UpdateChartSvg', function (id, svg) {
            if (id === 'blazor-interactive') document.getElementById('host').innerHTML = svg;
        });
        conn.start().then(function () { return conn.invoke('Subscribe', 'blazor-interactive'); });
    })();
</script>

@code {
    private string _svg = string.Empty;

    protected override void OnInitialized()
    {
        var figure = Plt.Create()
            .WithTitle("Damped sine — server-authoritative")
            .Plot(xs, ys)
            .WithServerInteraction("blazor-interactive", i => i.All())
            .Build();

        figure.SubPlots[0].XAxis.Min = xs[0];
        figure.SubPlots[0].XAxis.Max = xs[^1];

        Registry.Register("blazor-interactive", figure);
        _svg = new SvgTransform().Render(figure);
    }

    public void Dispose() => _ = Registry.UnregisterAsync("blazor-interactive");
}
```

The dispatcher script looks up `window.__mpl_signalr_connection`, so the `<script>` block above only needs to run once per page — no per-chart wiring. Because `FigureRegistry.Register` installs a per-chart background reader task, the hub method returns in microseconds and rendering happens asynchronously off the request thread.

A runnable version ships in `Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor` — route `/interactive`.

### Extension method

```csharp
@((MarkupString)figure.ToMarkupString())
```

`ToMarkupString()` converts a `Figure` to an SVG `MarkupString` for direct rendering.

## Dependencies

- `MatPlotLibNet` (core)
- `MatPlotLibNet.AspNetCore` (for the bidirectional path — needs `FigureRegistry` + `ChartHub`)
- `Microsoft.AspNetCore.SignalR.Client`

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) — Copyright (c) 2026 H.P. Gansevoort
