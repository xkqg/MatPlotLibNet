# MatPlotLibNet Samples

Runnable sample projects demonstrating the MatPlotLibNet charting library. All samples use `<ProjectReference>` to build from source — no NuGet packages required.

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
