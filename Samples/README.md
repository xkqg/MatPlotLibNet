# MatPlotLibNet Samples

Runnable sample projects demonstrating the MatPlotLibNet charting library. All samples use `<ProjectReference>` to build from source — no NuGet packages required.

> **Browser interactions are automatic.** Calling `FigureBuilder.WithBrowserInteraction()`
> (or, in WPF/Avalonia/Uno, ticking the **Interactive** checkbox) wires every interaction
> the chart needs in one switch: pan/zoom, **legend toggle + press-and-hold legend drag**
> (Phase S, v1.7.2), treemap drilldown, sankey hover, 3D rotation, rich tooltips,
> highlight, brush selection. The library detects which scripts are relevant per chart
> and emits only those — no per-feature toggle for the user to manage.

## Playground

Blazor WebAssembly interactive explorer. 16 example charts, flip themes, toggle series styling, copy the generated C# to paste into your own project. Also hosted live at [xkqg.github.io/MatPlotLibNet/playground](https://xkqg.github.io/MatPlotLibNet/playground/).

```
dotnet run --project MatPlotLibNet.Playground
```

Browse to <http://localhost:5000>. Set `<base href="/">` in `wwwroot/index.html` is auto-detected for localhost; the GitHub Pages deploy uses the `/MatPlotLibNet/playground/` subpath.

## Console

Creates every sample image shipped with the wiki and cookbook — ~60 SVG/PNG pairs covering every chart family. Also the generator for the Sankey / Treemap / 3D / MathText / Geo gallery.

```
dotnet run --project MatPlotLibNet.Samples.Console
```

Outputs to the repository root `images/` directory. Useful to re-run whenever a rendering change would alter committed sample output (e.g., the Phase G.7 fix that cleaned up stacked `data-*` attributes required a full regen).

## Blazor

Blazor Server app with static and real-time charts.

```
dotnet run --project MatPlotLibNet.Samples.Blazor
```

- `/` — static bar chart and scatter plot using `MatPlotLibNet.Blazor` control
- `/live` — real-time chart updating every 3 seconds via SignalR

## Control room

A sample of its own, because it is not an example of a control — it is a reference implementation of a SCREEN, with its own domain (bus → process → lane, alarm conditioning, a staleness clock) and a simulated federation that keeps running whether or not a browser is looking.

```
dotnet run --project MatPlotLibNet.Samples.ControlRoom
```

A simulated 15-bus federation on `Plt.OpsDashboard()`: quiet KPI tiles that carry no colour until something needs attention, a hatched tile for a source that has gone silent, and two rolling panels (throughput and latency percentiles) on a pinned time window. Window 1/5/15 min or 1 hour; refresh throttles the charts only — the tiles never slow down.

**It descends.** Fleet → bus → process → lanes, and nothing is ever replaced: the level you leave becomes the rail on the left, still coloured, so a sibling is one click away and you never lose sight of what stands next to the thing you are reading.

Two gestures, which is why either alone always felt stuck. Clicking a **block** is a drill-down — one level down the hierarchy. Clicking the **max** or the **min** in the strip is a drill-through: it leaves the aggregate for the member that produced it (the *exemplar*), and it is the reason an aggregate is worth clicking at all. At the bottom both stop being doorways, because a door that opens onto nothing is worse than no door.

A block is ONE size at every level and at every count — a fixed track, never a fraction of the row. Two buses are two blocks with an empty row beside them; the emptiness is itself the information. Lanes are rows rather than cards because they are the bottom, and they are judged on a different question: a bus and a process are asked how hard they are working, a lane is asked whether it is keeping up, so it carries backlog, latency and errors instead.

The whole state is the URL (`?bus=`, `?process=`): it survives every redraw, every block is an anchor so the descent is keyboard-reachable, and it can be pasted to a colleague mid-incident.

**Alarms have a lifecycle.** The Alarms tile is a doorway onto the panel that lists the same book the tile counts: a condition raises an alarm, the operator's one gesture is **ack** — seen, not gone, still counted on the card as `firing · N acked` — and only the condition clearing resolves it. Acking may never make the wall look better on its own.

## WPF

Native WPF window with `MplChartControl` (Windows). Uses `MatPlotLibNet.Wpf`.

```
dotnet run --project MatPlotLibNet.Samples.Wpf
```

Four chart-type buttons (Line / Bar / Scatter / 3D Surface) swap the bound `Figure` at runtime. The **Interactive** checkbox toggles `IsInteractive` so you can compare passive vs pan/zoom/3D-rotate behaviour on the same figure.

## Avalonia

Avalonia native control (Windows / macOS / Linux). Uses `MatPlotLibNet.Avalonia`.

```
dotnet run --project MatPlotLibNet.Samples.Avalonia
```

Demonstrates the `FigureControl` XAML element, theme switching, and runtime figure mutation.

## Uno

Uno Platform (Windows / macOS / Linux / WebAssembly / iOS / Android). Uses `MatPlotLibNet.Uno`.

```
dotnet run --project MatPlotLibNet.Samples.Uno
```

## ASP.NET Core

Server-side figure registry + SignalR hub. Charts render server-side and stream SVG updates to connected clients (Blazor / Angular / React / Vue).

```
dotnet run --project MatPlotLibNet.Samples.AspNetCore
```

- Figure registry pattern — register once, mutate, clients receive live updates
- `WithServerInteraction()` wires pan/zoom/reset/legend-toggle through the hub

## Web API

ASP.NET Core minimal API with REST endpoints and SignalR hub. Aimed at non-.NET frontends.

```
dotnet run --project MatPlotLibNet.Samples.WebApi
```

- `GET /api/chart/sales` — chart as JSON
- `GET /api/chart/sales.svg` — chart as SVG
- `/charts-hub` — SignalR hub (subscribe to `sensor-1` for live updates)

## GraphQL

HotChocolate GraphQL server with queries and subscriptions.

```
dotnet run --project MatPlotLibNet.Samples.GraphQL
```

- `/graphql` — BananaCakePop playground
- Query: `{ chartSvg(chartId: "demo") }`
- Subscription: `subscription { onChartSvgUpdated(chartId: "live-sensor") }`

## Packages without dedicated sample projects

These NuGet packages don't yet have runnable samples — contributions welcome:

| Package | How to try it today |
|---|---|
| `MatPlotLibNet.Maui` | Add `<mpl:FigureView Figure="{Binding Figure}" />` in a MAUI page |
| `MatPlotLibNet.DataFrame` | `df.PlotBar()` / `df.PlotLine()` extensions on `Microsoft.Data.Analysis.DataFrame` — covered in the DataFrame cookbook page |
| `MatPlotLibNet.Skia` | `SkiaTransform` renders to `SKBitmap`; see unit tests in `Tst/MatPlotLibNet.Skia/` |
| `MatPlotLibNet.Geo` | `Geo.Extensions.WithNaturalEarth()` + any of 13 projections; see the `geo_*` cookbook pages |
| `MatPlotLibNet.Notebooks` | Polyglot Notebooks: `#r "nuget: MatPlotLibNet.Notebooks"`, then any `.ToSvg()` renders inline |
| `MatPlotLibNet.Interactive` | .NET Interactive kernel extension; same inline rendering as Notebooks |

Full sample projects for these are planned (no ETA). Until then the patterns above and the cookbook pages are the reference.

## Note

All samples use `<ProjectReference>` to build from source. No NuGet packages required — changes to `Src/` propagate immediately.
