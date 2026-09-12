# MatPlotLibNet.Blazor

Blazor components for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. They render charts as inline SVG, with optional real-time server push. Since **v1.2.0** the interaction also runs in both directions: zoom, pan, reset and legend-toggle events travel from the browser back to the .NET server. The server updates the authoritative `Figure` and streams the new SVG back.

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

The component connects to a `ChartHub` endpoint. It updates automatically when the server calls `IChartPublisher.PublishSvgAsync` for this chart id.

### Bidirectional interaction (v1.2.0)

Add `.WithServerInteraction(...)` on the figure builder to send browser interaction to the server. The dispatcher script embedded in the SVG invokes the hub's `OnZoom`, `OnPan`, `OnReset` and `OnLegendToggle` methods. The server then updates the registered figure and publishes the new SVG through the same fan-out path that `MplLiveChart` already listens on.

For client-side-only interaction (no server round-trip), use `.WithBrowserInteraction()` instead. It switches on pan and zoom, **legend toggle (click) and legend drag (press-and-hold to reposition; v1.7.2)**, treemap drilldown, sankey hover, 3D rotation, rich tooltips, highlight and brush selection, all automatically. The library detects which scripts are relevant per chart and emits only those.

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

The dispatcher script looks up `window.__mpl_signalr_connection`, so the `<script>` block above only needs to run once per page. There is no per-chart wiring. `FigureRegistry.Register` installs a background reader task for each chart, so the hub method returns in microseconds and the rendering happens asynchronously, off the request thread.

A runnable version ships in `Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor`, on route `/interactive`.

### Extension method

```csharp
@((MarkupString)figure.ToMarkupString())
```

`ToMarkupString()` converts a `Figure` to an SVG `MarkupString` for direct rendering.

## Dependencies

- `MatPlotLibNet` (core)
- `MatPlotLibNet.AspNetCore` (for the bidirectional path; it needs `FigureRegistry` and `ChartHub`)
- `Microsoft.AspNetCore.SignalR.Client`

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) — Copyright (c) 2026 H.P. Gansevoort
