# MatPlotLibNet.AspNetCore

ASP.NET Core integration for the [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) charting library. Serves chart JSON specs and SVG output via minimal API endpoints, server-pushes real-time updates via SignalR, and as of **v1.2.0** accepts bidirectional interaction events — wheel-zoom, drag-pan, reset, and legend-toggle round-trip from the browser through `ChartHub` to a server-authoritative `Figure` that is mutated and re-published automatically.

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

The server can now receive zoom / pan / reset / legend-toggle events from a connected browser, mutate the authoritative `Figure`, and push the updated SVG back through the existing publish pipeline. This closes the loop that one-way push leaves open: axis limits stay in sync with the viewer, LTTB downsampling can react to the current zoom level, toggled series survive reloads via the server model.

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

1. The browser loads the initial SVG. Because `ServerInteraction = true`, the SVG embeds `SvgSignalRInteractionScript` — a single IIFE that wires `wheel` / `pointerdown` / `keydown` / `click` listeners.
2. The script discovers the JS-side `HubConnection` via `window.__mpl_signalr_connection` (set by the host page) and invokes `OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` with a payload carrying the new axis limits or toggled series index.
3. `ChartHub` writes the event to the per-chart `Channel<FigureInteractionEvent>` via `FigureRegistry.Publish` and returns — the hub method never blocks on rendering.
4. A single background reader task per chart (`ChartSession`) drains the channel in order, applies each event via `FigureInteractionEvent.ApplyTo(figure)`, and calls `IChartPublisher.PublishSvgAsync` once per drained batch. Bursts coalesce naturally — 50 wheel events over one frame produce exactly one re-render.
5. `PublishSvgAsync` fans out the new SVG to every subscriber of the chart's SignalR group, so multi-viewer sync falls out as a side-effect of the existing group machinery.

### Event hierarchy (`MatPlotLibNet.Interaction` in the core package)

Stacked records, self-applying, SOLID-OCP — add a new interaction by adding a new subclass:

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

Opting in sets `Figure.ChartId` and `Figure.ServerInteraction = true`, flips the matching existing `EnableZoomPan` / `EnableLegendToggle` flags, and makes `SvgTransform` emit `SvgSignalRInteractionScript` instead of the local `SvgInteractivityScript` + `SvgLegendToggleScript` — the two scripts are mutually exclusive, never both.

### Frontend hookup

Any environment that can create a `@microsoft/signalr` `HubConnection` will work. The script inside the SVG looks up `window.__mpl_signalr_connection` when the first event fires, so the host page only has to expose the connection globally once:

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
| `SignalRExtensions.AddMatPlotLibNetSignalR()` | Registers SignalR + renderer + `IChartPublisher` + `FigureRegistry` |
| `SignalRExtensions.MapChartHub()` | Maps the `ChartHub` SignalR endpoint |
| `MatPlotLibNetEndpoints.MapChartEndpoint()` | Maps a JSON chart endpoint |
| `MatPlotLibNetEndpoints.MapChartSvgEndpoint()` | Maps an SVG chart endpoint |
| `IChartPublisher` | Service for broadcasting chart updates (`PublishAsync`, `PublishSvgAsync`) |
| `ChartHub` | SignalR hub: `Subscribe` / `Unsubscribe` / `OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` |
| `FigureRegistry` | Per-chart registry + channel-based pub/sub: `Register` / `UnregisterAsync` / `Publish` |
| `FigureBuilder.WithServerInteraction` | Fluent opt-in to bidirectional interaction |
| `ServerInteractionBuilder` | Small fluent selector: `EnableZoom`/`EnablePan`/`EnableReset`/`EnableLegendToggle`/`All` |

## Samples

- **`Samples/MatPlotLibNet.Samples.AspNetCore`** — minimal ASP.NET Core + static HTML page demonstrating the full bidirectional loop without any frontend framework. Run with `dotnet run`, open the browser, wheel-zoom the chart.
- **`Samples/MatPlotLibNet.Samples.Blazor/Components/Pages/Interactive.razor`** — Blazor equivalent at route `/interactive`.

## License

[MIT](https://github.com/xkqg/MatPlotLibNet/blob/main/LICENSE) — Copyright (c) 2026 H.P. Gansevoort
