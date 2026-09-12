# MatPlotLibNet.AspNetCore

ASP.NET Core integration for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. It serves chart JSON specs and SVG output from minimal API endpoints, and it pushes real-time updates to connected clients over SignalR. Since **v1.2.0** it also accepts interaction events in the other direction. Wheel-zoom, drag-pan, reset and legend-toggle travel from the browser through `ChartHub` to a server-authoritative `Figure`. The server mutates that figure and re-publishes it automatically.

## Installation

```
dotnet add package MatPlotLibNet.AspNetCore
```

## Quick Start — static endpoints + one-way push

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMatPlotLibNetSignalR();

var app = builder.Build();

app.MapChartEndpoint();    // GET /chart?id=... -> JSON
app.MapChartSvgEndpoint(); // GET /chart/svg?id=... -> SVG
app.MapChartHub();         // SignalR hub at /charts-hub

app.Run();
```

Publish a one-way update to every subscribed client:

```csharp
app.MapPost("/update", async (IChartPublisher publisher) =>
{
    var figure = Plt.Create()
        .WithTitle("Live Data")
        .Plot(x, y)
        .Build();

    await publisher.PublishSvgAsync("dashboard-1", figure);
});
```

## Bidirectional SignalR (v1.2.0)

The server can receive zoom, pan, reset and legend-toggle events from a connected browser. It mutates the authoritative `Figure` and pushes the updated SVG back through the existing publish pipeline. One-way push leaves that loop open. With the loop closed, axis limits stay in sync with the viewer, LTTB downsampling can react to the current zoom level, and toggled series survive a reload because the server holds the model.

```csharp
using MatPlotLibNet.AspNetCore;
using MatPlotLibNet.Interaction;

// 1. Build a figure that opts into server-authoritative interaction.
var figure = Plt.Create()
    .WithTitle("Bidirectional demo")
    .Plot(xs, ys)
    .WithServerInteraction("live-1", i => i.All())   // Zoom + Pan + Reset + LegendToggle
    .Build();

figure.SubPlots[0].XAxis.Min = xs[0];
figure.SubPlots[0].XAxis.Max = xs[^1];
figure.SubPlots[0].YAxis.Min = yMin;
figure.SubPlots[0].YAxis.Max = yMax;

// 2. Register the figure with the per-chart channel-based pub/sub pipeline.
var registry = app.Services.GetRequiredService<FigureRegistry>();
registry.Register("live-1", figure);

// 3. Serve the initial SVG.
app.MapGet("/api/chart/live.svg", (ISvgRenderer svg) =>
    Results.Content(svg.Render(figure), "image/svg+xml"));

app.MapChartHub();
```

### How the round-trip works

1. The browser loads the initial SVG. Because `ServerInteraction = true`, the SVG embeds `SvgSignalRInteractionScript`. That script is a single IIFE, and it wires listeners for `wheel`, `pointerdown`, `keydown` and `click`.
2. The script finds the JS-side `HubConnection` on `window.__mpl_signalr_connection`, which the host page sets. It then invokes `OnZoom`, `OnPan`, `OnReset` or `OnLegendToggle`. The payload carries the new axis limits, or the index of the toggled series.
3. `ChartHub` calls `FigureRegistry.Publish`, which writes the event to the per-chart `Channel<FigureInteractionEvent>`, and then returns. The hub method never blocks on rendering.
4. One background reader task per chart (`ChartSession`) drains the channel in order. It applies each event with `FigureInteractionEvent.ApplyTo(figure)` and calls `IChartPublisher.PublishSvgAsync` once per drained batch. Bursts therefore coalesce: 50 wheel events over one frame produce exactly one re-render.
5. `PublishSvgAsync` sends the new SVG to every subscriber of the chart's SignalR group. Several viewers of the same chart stay in sync through that existing group mechanism.

### Event hierarchy (`MatPlotLibNet.Interaction` in the core package)

The events are stacked records, and each one applies itself. The hierarchy follows SOLID-OCP: to add a new interaction, add a new subclass.

```
FigureInteractionEvent           (abstract root — ChartId, AxesIndex, abstract ApplyTo)
├── AxisRangeEvent               (abstract tier-2 — sealed ApplyTo overwrites X/Y limits)
│   ├── ZoomEvent                (wheel — new absolute limits)
│   └── ResetEvent               (Home key — original limits captured at render time)
├── PanEvent                     (drag — delta translation of current limits)
└── LegendToggleEvent            (click on data-series-index — flips ChartSeries.Visible)
```

### `FigureBuilder.WithServerInteraction`

```csharp
.WithServerInteraction("chart-id", i => i
    .EnableZoom()
    .EnablePan()
    .EnableReset()
    .EnableLegendToggle())

// or:
.WithServerInteraction("chart-id", i => i.All())
```

Opting in sets `Figure.ChartId` and `Figure.ServerInteraction = true`. It also turns on the matching existing `EnableZoomPan` / `EnableLegendToggle` flags. `SvgTransform` then emits `SvgSignalRInteractionScript` instead of the local `SvgInteractivityScript` + `SvgLegendToggleScript`. An SVG carries one of the two scripts, never both.

### Frontend hookup

Any environment that can create a `@microsoft/signalr` `HubConnection` will work. The script inside the SVG looks up `window.__mpl_signalr_connection` when the first event fires. The host page therefore only has to expose the connection globally once:

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"></script>
<script>
    const conn = new signalR.HubConnectionBuilder()
        .withUrl('/charts-hub')
        .withAutomaticReconnect()
        .build();
    window.__mpl_signalr_connection = conn;
    conn.on('UpdateChartSvg', (id, svg) => {
        if (id === 'live-1') document.getElementById('chart-host').innerHTML = svg;
    });
    conn.start().then(() => conn.invoke('Subscribe', 'live-1'));
</script>
```

For Blazor, see `MplLiveChart` in `MatPlotLibNet.Blazor` and the `Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor` example.

## API reference

| Type | Description |
|------|-------------|
| `SignalRExtensions.AddMatPlotLibNetSignalR()` | Registers SignalR + renderer + `IChartPublisher` + `FigureRegistry` + `IChartSubscriptions` |
| `SignalRExtensions.MapChartHub()` | Maps the `ChartHub` SignalR endpoint |
| `MatPlotLibNetEndpoints.MapChartEndpoint()` | Maps a JSON chart endpoint |
| `MatPlotLibNetEndpoints.MapChartSvgEndpoint()` | Maps an SVG chart endpoint |
| `IChartPublisher` | Service for broadcasting chart updates (`PublishAsync`, `PublishSvgAsync`) |
| `IChartSubscriptions` | The hub's record of subscriptions per chart: `HasSubscribers(chartId)` / `Count(chartId)`. Ask it before rendering a frame that no client has subscribed to, because a SignalR group does not report how many members it has |
| `ChartHub` | SignalR hub: `Subscribe` / `Unsubscribe` / `OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` |
| `FigureRegistry` | Per-chart registry with channel-based pub/sub: `Register` / `UnregisterAsync` / `Publish` |
| `FigureBuilder.WithServerInteraction` | Fluent opt-in to bidirectional interaction |
| `ServerInteractionBuilder` | Small fluent selector: `EnableZoom`/`EnablePan`/`EnableReset`/`EnableLegendToggle`/`All` |

## Samples

- **`Samples/MatPlotLibNet.Samples.AspNetCore`** — minimal ASP.NET Core app with a static HTML page that demonstrates the full bidirectional loop without any frontend framework. Run it with `dotnet run`, open the browser, and wheel-zoom the chart.
- **`Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor`** — Blazor equivalent at route `/interactive`.
- **`Samples/MatPlotLibNet.Samples.ControlRoom/Components/Pages/ControlRoom.razor`** — the control room sample: a tile row, a drill-down from fleet to bus to process to lanes, two panels that render only while a tab shows them (`IChartSubscriptions`), and latency percentiles on a log scale.

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) — Copyright (c) 2026 H.P. Gansevoort
