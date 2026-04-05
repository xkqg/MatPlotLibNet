# MatPlotLibNet Documentation

MatPlotLibNet is a .NET 10 / .NET Standard 2.1 charting library inspired by matplotlib. It provides a fluent API for creating publication-quality charts with multi-platform rendering support.

## Getting started

See the [README](../README.md) for installation and quick-start examples.

## Guides

Each package includes a detailed how-to guide:

- **Core library** -- [howTo.md](../Src/MatPlotLibNet/howTo.md): fluent API, chart types, annotations, subplots, export, themes, DI
- **Skia** -- [howTo.md](../Src/MatPlotLibNet.Skia/README.md): PNG and PDF export via SkiaSharp
- **Blazor** -- [howTo.md](../Src/MatPlotLibNet.Blazor/howTo.md): MplChart, MplLiveChart, SignalR
- **ASP.NET Core** -- [howTo.md](../Src/MatPlotLibNet.AspNetCore/howTo.md): REST endpoints, SignalR hub, publishing
- **MAUI** -- [howTo.md](../Src/MatPlotLibNet.Maui/howTo.md): native rendering, MVVM, data binding
- **Interactive** -- [howTo.md](../Src/MatPlotLibNet.Interactive/howTo.md): browser popup, live updates
- **GraphQL** -- [howTo.md](../Src/MatPlotLibNet.GraphQL/howTo.md): HotChocolate queries and subscriptions
- **Angular** -- [howTo.md](../Src/matplotlibnet-angular/howTo.md): components, services, SignalR client
- **React** -- [howTo.md](../Src/matplotlibnet-react/howTo.md): hooks, components, SignalR client
- **Vue** -- [howTo.md](../Src/matplotlibnet-vue/howTo.md): composables, components, SignalR client

## Samples

Runnable sample projects are in the [Samples/](../Samples/) directory:

- **Console** -- export charts to SVG, PNG, PDF; subplots; themes; JSON round-trip
- **Blazor** -- static and real-time charts with SignalR
- **WebApi** -- REST endpoints + SignalR hub for Angular/React/Vue frontends
- **GraphQL** -- HotChocolate playground with queries and subscriptions

See [Samples/README.md](../Samples/README.md) for `dotnet run` instructions.

## Performance

### Server-side SVG + SignalR (not client-side re-rendering)

Most charting libraries work client-side: they ship raw data to the browser, then a JavaScript library renders the chart on a canvas or in the DOM. Every update means re-sending data and re-rendering. MatPlotLibNet takes a fundamentally different approach.

Charts are rendered **once on the server** as SVG. The finished SVG is pushed to connected clients via **SignalR**. The browser simply replaces the innerHTML -- no chart library, no JavaScript computation, no re-rendering.

**Why this is better:**

- **Less network traffic** -- a chart SVG is 5-15 KB. Compare that to shipping raw datasets (often 100KB+) plus a JavaScript charting library (200-500 KB). SignalR pushes only the charts that actually changed, only to subscribed clients. No polling, no redundant payloads.
- **Zero client CPU cost** -- the browser swaps one DOM string. No JavaScript parsing, no layout engine, no canvas draw calls, no WebGL shaders. A Raspberry Pi can display the same dashboard as a desktop workstation.
- **Inline SVG in the DOM** -- the chart is not a black-box canvas. It's real DOM content. It can be styled with CSS, selected and copied, picked up by screen readers for accessibility, and printed at any resolution without rasterization artifacts.
- **Works outside the visible viewport** -- canvas-based charts typically skip rendering when off-screen (hidden tabs, collapsed panels, below the scroll fold). SVG content exists in the DOM regardless of visibility. When the user scrolls to it or switches tabs, it's already there -- no lazy-load flash, no resize observer hacks.
- **Pixel-perfect consistency** -- every client sees the exact same chart. No browser rendering engine differences, no missing fonts, no platform-specific canvas quirks. The server controls 100% of the visual output.
- **Scales with server hardware** -- parallel subplot rendering uses all available cores. Adding more CPU to the server directly improves throughput for all connected clients simultaneously. Client hardware becomes irrelevant.
- **Simpler client code** -- your Angular/React/Vue component is a `<div innerHTML={svg}>`. No chart configuration objects, no data binding, no resize handlers, no theme synchronization. The complexity lives on the server where it's testable and debuggable.
- **Same output inside and outside the browser** -- the SVG rendered on the server is identical whether it's displayed inline in a Blazor page, pushed to a React dashboard via SignalR, saved as a `.svg` file from a console app, exported to PNG/PDF via SkiaSharp, or rendered natively in a MAUI mobile app. One rendering pipeline, every target.
- **Inline, expandable, or popup** -- charts render inline by default, but can be expanded in-place or popped out into a separate browser window for a larger view. The `DisplayMode` setting (`Inline`, `Expandable`, `Popup`) controls this per chart. The `Interactive` package goes further: `figure.ShowAsync()` opens a standalone browser window with live SignalR updates — no web app required.

**Performance:** a simple line chart renders in **52 microseconds**. A full 3x3 subplot grid in **224 microseconds**. All 13 technical indicators compute on 100K data points in under 8ms. JSON serialization round-trips in under 50us.

See [BENCHMARKS.md](../BENCHMARKS.md) for full results on AMD Ryzen 9 3950X.

## Architecture

See [ARCHITECTURE.md](../Src/MatPlotLibNet/ARCHITECTURE.md) for the full rendering pipeline, data flow, and design patterns.

## API Reference

Browse the [API Reference](../api/index.md) for detailed class and method documentation generated from XML doc comments.
