# Roadmap

What MatPlotLibNet has built and what it will build, in the order the work is done: what is done first, and below it
what is still to come. The one rule behind the order is that nothing stands above a row it needs; among the rows that
are free to move, the one a reader reaches for most often, and pays least for, comes first. There are no dates — a row
moves when it is true, and the [changelog](CHANGELOG.md) is where it says so. Releases follow what people need, so a
[Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or an [Issue](https://github.com/xkqg/MatPlotLibNet/issues)
that needs a row is what moves it up.

| | What it is | In one line |
|---|---|---|
| **Done** | The fluent core | `Plt.Create()`, `FigureBuilder` and `AxesBuilder`: a chart in one chain, drawn as SVG, written to JSON and read back. See [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet). |
| **Done** | Themes and colour maps | 30 themes, matplotlib's own looks among them, and 148 colormaps counting their reversed forms. See [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling). |
| **Done** | Blazor and ASP.NET Core | `<MplChart>` and `<MplLiveChart>`, a SignalR hub, REST endpoints, and clients for Angular, React and Vue. See [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map). |
| **Done** | The chart types | 83 series types: lines and bars, statistical, financial, grid, 3-D, polar, hierarchical, flow and the control room's. See [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types). |
| **Done** | PNG, PDF and GIF | `MatPlotLibNet.Skia`: the same figure as a PNG, a PDF of one page or several, and an animated GIF. See [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started). |
| **Done** | Technical indicators | 58 indicators computed from a price series and drawn on it, from a moving average to transfer entropy. See [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types). |
| **Done** | GraphQL | `MatPlotLibNet.GraphQL`: charts as HotChocolate queries and subscriptions. See [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map). |
| **Done** | 3-D and polar axes | Surfaces, bars and points in 3-D under a camera that turns, and charts on polar axes. See [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced). |
| **Done** | Axes, scales and ticks | Linear, log, symlog, date and broken axes, limits drawn exactly as set, reversed ones too, a second axis on the right or on top, and tick locators and formatters of your own. See [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling). |
| **Done** | Layout | GridSpec, mosaic layouts, insets, and a constrained layout that makes room for titles, ticks and legends. See [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced). |
| **Done** | [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) | Charts inline in Polyglot Notebooks and Jupyter; the package stands still at the version it reached. |
| **Done** | MathText | LaTeX-style math in any label: Greek letters, sub- and superscripts, fractions, roots, operator limits and matrices. See [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced). |
| **Done** | [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) | Alt text and a description in every SVG, keyboard navigation, safe palettes, and the chart's data as a table beside it. |
| **Done** | DataFrame | `MatPlotLibNet.DataFrame`: plots and indicators straight from a `Microsoft.Data.Analysis` `DataFrame`. See [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map). |
| **Done** | Drawn as matplotlib draws | matplotlib's own look by default, its font bundled, and a suite that compares the output with images matplotlib drew. See [MatplotlibThemes](https://github.com/xkqg/MatPlotLibNet/wiki/MatplotlibThemes). |
| **Done** | Interaction through the server | Zoom, pan, legend clicks, selection, hover and the data cursor in the browser, answered by the server over SignalR in the units of the data, on log and symlog axes too. See [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR). |
| **Done** | Native controls | Avalonia, Uno, WPF and MAUI controls that draw natively, without a WebView; the first three pan, zoom and select in the units of the data. See [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls). |
| **Done** | [Streaming](https://github.com/xkqg/MatPlotLibNet/wiki/Streaming) | Live series in ring buffers, indicators that follow each new bar, and a streaming control in Blazor and on every native head. |
| **Done** | Maps | `MatPlotLibNet.Geo`: 13 map projections and Natural Earth coastlines and countries, embedded. See [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types). |
| **Done** | The control room | `Plt.OpsDashboard()`: KPI tiles, state timelines, a service-topology panel and one shared time window. See [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types). |
| **Done** | The MCP server | `MatPlotLibNet.Mcp`: an AI agent renders this library's charts from a JSON spec and reads their data back. See [MCP Server](https://github.com/xkqg/MatPlotLibNet/wiki/MCP-Server). |
| **Done** | Text in any script | Arabic, Hebrew and every other script drawn in its own order and its own letter shapes. See [International Text](https://github.com/xkqg/MatPlotLibNet/wiki/International-Text). |
| **Done** | Charts in Verso | `MatPlotLibNet.Verso`: a Verso notebook cell that ends on a figure shows the chart. See [Verso](https://github.com/xkqg/MatPlotLibNet/wiki/Verso). |
| **Done** | Examples that compile | The wiki's C# examples compiled against the packages on every CI run, and a page of each release's calls. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Done** | Direct labels and axis colours | A line's name at its end instead of in the legend, and an axis in its line's colour, on all four axes. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Later** | Save settings that reach the file | `WithDpi` and `SaveOptions` are accepted and kept, and no export reads them yet. |
| **Later** | Every chart type back from JSON | Seven chart types come back from JSON as a placeholder, and two are written and never read back. |
| **Later** | Live series back from JSON | A streaming series is written to JSON and dropped when the JSON is read. |
| **Later** | A spine's line style kept in JSON | A spine's colour and width are written to JSON; its line style is not, so it is lost in a round trip. |
| **Later** | A live chart that sends its own gestures | `MplLiveChart` shows the server's redraws; the page still has to hand its script a SignalR connection. |
| **Later** | Light with a strength of its own | The ambient and diffuse arguments of `WithLighting` change nothing in the picture yet. |
| **Later** | More kinds of series on a second axis | The right-hand and top axes carry lines and points; a bar or an area there is not kept. |
| **Later** | Zoom and pan on a second axis | Zoom and pan change the main X and Y axes; the right-hand and top axes keep their range. |
| **Later** | Home that returns to the first view in the native controls | After a zoom and a repaint, Home returns to the view on screen, not the one first drawn. |
| **Later** | The MAUI view on a par with the others | It draws and streams a chart, and does not pan, zoom or select, nor draw a hatch. |
| **Later** | Streaming forms of more indicators | 11 of the 58 indicators have a streaming form that follows each new bar. |
| **Later** | Transfer entropy over time | A rolling window, a continuous estimator and a conditional form beside the scalar that ships. |
| **Later** | Three plots matplotlib has | `fill_betweenx`, `matshow` and `spy`. |
| **Later** | A handler of your own for a chart's gestures | One interface a chart's events go to, beyond the callbacks a chart session takes today. |
| **Later** | Several viewers as a designed feature | Viewers of one chart that see each other's gestures by design, not as a side effect where the last one wins. |
| **Later** | The Windows heads tested by CI | The WPF, Uno and MAUI suites run on Windows by hand or before a push, never on the CI runners. |
| **Later** | The matplotlib comparison run by CI | The suite that compares the output with images matplotlib drew is run by hand. |

Every row has a section of its own on the [roadmap in the wiki](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap): what
it is, why it stands where it does, what a row still to come waits for, and what is left out on purpose. This table is
the wiki's, and a test holds the two to one another.
