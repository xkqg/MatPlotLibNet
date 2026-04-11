# MatPlotLibNet Documentation

MatPlotLibNet is a .NET 10 / .NET 8 charting library inspired by matplotlib. Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG/PNG/PDF/GIF), and multi-platform output to Blazor, MAUI, ASP.NET Core, Angular, React, Vue, and standalone browser popups.

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**:

| Wiki page | Covers |
|---|---|
| [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) | Installation, quick start, output formats |
| [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) | All 60 series types with code examples |
| [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) | Themes, colormaps, PropCycler |
| [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) | Layouts, date axes, math text, animations, real-time |
| [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) | All 8 NuGet packages + 3 JS packages |
| [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) | Polyglot Notebooks + Jupyter inline rendering |
| [Benchmarks](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks) | SVG rendering, SIMD transforms, indicators, PNG/PDF |
| [Roadmap](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) | Version history and planned phases |
| [Contributing](https://github.com/xkqg/MatPlotLibNet/wiki/Contributing) | Build, test, coding conventions |

## Samples

Runnable sample projects are in the [Samples/](../Samples/) directory:

- **Console** — export charts to SVG, PNG, PDF; subplots; themes; JSON round-trip
- **Blazor** — static and real-time charts with SignalR
- **WebApi** — REST endpoints + SignalR hub for Angular/React/Vue frontends
- **GraphQL** — HotChocolate playground with queries and subscriptions

See [Samples/README.md](../Samples/README.md) for `dotnet run` instructions.

## Architecture

See [ARCHITECTURE.md](../Src/MatPlotLibNet/ARCHITECTURE.md) for the full rendering pipeline, data flow, and design patterns.

## API Reference

Browse the [API Reference](../api/index.md) for detailed class and method documentation generated from XML doc comments.
