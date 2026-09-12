# MatPlotLibNet Samples

Runnable sample projects that demonstrate the MatPlotLibNet charting library. All samples use `<ProjectReference>` to build from source, so no NuGet packages are required.

> **Browser interactions are automatic.** Calling `FigureBuilder.WithBrowserInteraction()`
> (or, in WPF/Avalonia/Uno, ticking the **Interactive** checkbox) enables every interaction
> the chart needs at once: pan/zoom, **legend toggle + press-and-hold legend drag**
> (Phase S, v1.7.2), treemap drilldown, sankey hover, 3D rotation, rich tooltips,
> highlight, brush selection. The library detects which scripts each chart needs and
> emits only those. There is no per-feature toggle for the user to manage.

## Playground

A Blazor WebAssembly interactive explorer with 16 example charts. You can flip themes, toggle series styling, and copy the generated C# to paste into your own project. It is also hosted live at [xkqg.github.io/MatPlotLibNet/playground](https://xkqg.github.io/MatPlotLibNet/playground/).

```
dotnet run --project MatPlotLibNet.Playground
```

Browse to <http://localhost:5000>. The `<base href="/">` setting in `wwwroot/index.html` is auto-detected for localhost; the GitHub Pages deploy uses the `/MatPlotLibNet/playground/` subpath.

## Console

A console app that creates every sample image shipped with the wiki and cookbook: 74 SVG/PNG pairs covering every chart family. It is also the generator for the Sankey / Treemap / 3D / MathText / Geo gallery.

```
dotnet run --project MatPlotLibNet.Samples.Console
```

Output goes to the `images/` directory at the repository root. Re-run it whenever a rendering change would alter committed sample output. For example, the Phase G.7 fix that cleaned up stacked `data-*` attributes required a full regeneration.

## Blazor

A Blazor Server app with static and real-time charts.

```
dotnet run --project MatPlotLibNet.Samples.Blazor
```

- `/` — a static bar chart and a scatter plot using the `MatPlotLibNet.Blazor` control
- `/live` — a real-time chart that updates every 3 seconds via SignalR

## Control room

This sample stands on its own because it is not an example of a control. It is a reference implementation of a complete screen. It has its own domain (a hierarchy of bus, process and lane; alarm conditioning; a staleness clock) and a simulated federation that keeps running whether or not a browser is connected.

```
dotnet run --project MatPlotLibNet.Samples.ControlRoom
```

The sample runs a simulated 15-bus federation on `Plt.OpsDashboard()`. The KPI tiles carry no colour until something needs attention. A tile is hatched when its source has gone silent. Two rolling panels show throughput and latency percentiles over a pinned time window. The window is 1, 5 or 15 minutes, or 1 hour. The refresh setting throttles the charts only; the tiles never slow down.

**Navigation descends the hierarchy.** The screen goes from fleet to bus to process to lanes. Nothing is replaced on the way down: the level you leave becomes a rail on the left, still coloured. A sibling is one click away, and you always keep sight of what stands next to the item you are reading.

There are two click gestures, and the screen needs both. Clicking a **block** is a drill-down: it goes one level down the hierarchy. Clicking the **max** or the **min** in the strip is a drill-through: it leaves the aggregate and opens the member that produced that value (the *exemplar*). The drill-through is what makes an aggregate worth clicking. At the bottom level neither click opens anything, because there is nothing below.

A block has one size at every level and at every count. It occupies a fixed track, never a fraction of the row. Two buses are two blocks with an empty row beside them; the empty space is itself information, because it shows that there are only two. Lanes are rows rather than cards, because they are the bottom level and they answer a different question. A bus and a process show how hard they are working. A lane shows whether it is keeping up, so it carries backlog, latency and errors instead.

The whole navigation state is in the URL (`?bus=`, `?process=`). It survives every redraw. Every block is an anchor, so the descent is reachable from the keyboard. The URL can be pasted to a colleague during an incident.

**Alarms have a lifecycle.** The Alarms tile opens a panel that lists the same alarms the tile counts. A condition raises an alarm. The operator's one action is **ack**. An acked alarm is marked as seen but not removed, and the card still counts it as `firing · N acked`. Only the clearing of the condition resolves the alarm. Acking must never make the dashboard look calmer on its own.

## WPF

A native WPF window with `MplChartControl` (Windows). It uses `MatPlotLibNet.Wpf`.

```
dotnet run --project MatPlotLibNet.Samples.Wpf
```

Four chart-type buttons (Line / Bar / Scatter / 3D Surface) swap the bound `Figure` at runtime. The **Interactive** checkbox toggles `IsInteractive`, so you can compare passive behaviour with pan/zoom/3D-rotate behaviour on the same figure.

## Avalonia

An Avalonia native control (Windows / macOS / Linux). It uses `MatPlotLibNet.Avalonia`.

```
dotnet run --project MatPlotLibNet.Samples.Avalonia
```

The sample demonstrates the `FigureControl` XAML element, theme switching, and runtime figure mutation.

## Uno

Uno Platform on its Skia renderer, hosted in a WPF window (`Uno.WinUI.Skia.Wpf`). It uses `MatPlotLibNet.Uno`.

```
dotnet run --project MatPlotLibNet.Samples.Uno
```

The sample uses a Skia head rather than a WinUI head. `MatPlotLibNet.Uno` draws on Uno's Skia backend, and `SKCanvasElement` exists only there. A WinUI head cannot load the library at all: its `Uno.UI` reference has no WinUI facade, and the build stops with *"Type universe cannot resolve assembly: Uno.UI, Version=255.255.255.255"*. The App and the page are Uno XAML, declared as `UnoApplicationDefinition` / `UnoPage` items. Uno's generator reads those items directly. WPF's own markup compiler in the same project never sees them, in a real build or in a design-time build.

## ASP.NET Core

A server-side figure registry and a SignalR hub. Charts render on the server and stream SVG updates to connected clients (Blazor / Angular / React / Vue).

```
dotnet run --project MatPlotLibNet.Samples.AspNetCore
```

- Figure registry pattern — register a figure once, mutate it, and clients receive live updates
- `WithServerInteraction()` wires pan/zoom/reset/legend-toggle through the hub

## Web API

An ASP.NET Core minimal API with REST endpoints and a SignalR hub. It is aimed at non-.NET frontends.

```
dotnet run --project MatPlotLibNet.Samples.WebApi
```

- `GET /api/chart/sales` — chart as JSON
- `GET /api/chart/sales.svg` — chart as SVG
- `GET /api/chart/sales.table` — the same chart as an HTML data table, for a reader who cannot use the SVG
- `GET /api/chart/international.svg` — a chart with an Arabic title and a Hebrew legend entry, rendered without the Skia package. The SVG carries `<text>` with `direction="rtl"`, and the browser shapes the text.
- `/charts-hub` — SignalR hub (subscribe to `sensor-1` for live updates)

## GraphQL

A HotChocolate GraphQL server with queries and subscriptions.

```
dotnet run --project MatPlotLibNet.Samples.GraphQL
```

- `/graphql` — BananaCakePop playground
- Query: `{ chartSvg(chartId: "demo") }`
- Query: `{ chartDataTable(chartId: "demo") }` — the same chart as an HTML data table
- Subscription: `subscription { onChartSvgUpdated(chartId: "live-sensor") }`

## Packages without dedicated sample projects

These NuGet packages do not yet have runnable samples. Contributions are welcome:

| Package | How to try it today |
|---|---|
| `MatPlotLibNet.Maui` | Add `<mpl:FigureView Figure="{Binding Figure}" />` in a MAUI page |
| `MatPlotLibNet.DataFrame` | `df.PlotBar()` / `df.PlotLine()` extensions on `Microsoft.Data.Analysis.DataFrame` — covered in the DataFrame cookbook page |
| `MatPlotLibNet.Skia` | `SkiaTransform` renders to `SKBitmap`; see unit tests in `Tst/MatPlotLibNet.Skia/` |
| `MatPlotLibNet.Geo` | `Geo.Extensions.WithNaturalEarth()` + any of 13 projections; see the `geo_*` cookbook pages |
| `MatPlotLibNet.Notebooks` | Polyglot Notebooks: `#r "nuget: MatPlotLibNet.Notebooks"`, then any `.ToSvg()` renders inline |
| `MatPlotLibNet.Interactive` | .NET Interactive kernel extension; same inline rendering as Notebooks |
| `MatPlotLibNet.Mcp` | Point an MCP host at it (`dnx MatPlotLibNet.Mcp`) and ask the model for a chart; see the [MCP cookbook page](../docs/cookbook/mcp.md) |

Full sample projects for these are planned (no ETA). Until then, the patterns above and the cookbook pages are the reference.

## Note

All samples use `<ProjectReference>` to build from source. No NuGet packages are required, and changes to `Src/` propagate immediately.
