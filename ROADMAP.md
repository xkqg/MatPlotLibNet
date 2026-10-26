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
| **Done** | A third and fourth Y axis, outward-stacked two to a side | `WithOutwardYAxis` stacks a second axis past the first on either side — four scales total, two left and two right, each with its own range, ticks and series. See [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling). |
| **Done** | More kinds of series on a second axis | A secondary axis restores through the same registry the primary one does, so any registered kind — a bar, an area — survives there too, not only lines and points. See [MCP Server](https://github.com/xkqg/MatPlotLibNet/wiki/MCP-Server). |
| **Done** | Zoom and pan on a second axis | A wheel zoom, a drag pan, a rectangle zoom and Home move every outward-stacked axis' own range, each by its own proportional amount, whether the server answers them or a native control does. See [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR) and [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls). |
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
| **Done** | A figure that reads back from JSON whole | Every setting a figure draws, its theme, colour maps, tick locators and legend among them, comes back from JSON, the MCP server and every transport. See [MCP Server](https://github.com/xkqg/MatPlotLibNet/wiki/MCP-Server). |
| **Done** | A spine's line style kept in JSON | A spine's line style travels with its colour and width; a spine written without one reads back solid. See [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling). |
| **Done** | Missing values | NaN is the one missing value: a line breaks at a gap, nothing is drawn at NaN, and ranges, colour maps and JSON leave it out or carry it. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Done** | Save settings that reach the file | `WithDpi` and `SaveOptions` reach the file: a PNG is drawn at the figure's dpi, and a title and an author go into the SVG, the PDF and the PNG. See [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling). |
| **Done** | Charts in one call | Seventeen verbs, from `Pareto` and `Ridgeline` to `Raincloud` and `Horizon`, draw a chart from the data as you hold it. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Done** | Every series kind and every axes setting back from JSON | A Sankey's nodes and links, a treemap's, sunburst's, dendrogram's and clustermap's trees, a tree grid's rows, a polar chart's data, a streaming chart's current points and a planar 3-D bar's data all come back from JSON as themselves, not a placeholder. An axes' Z axis, its 3-D pane colours and its tooltip switch come back too. See [MCP Server](https://github.com/xkqg/MatPlotLibNet/wiki/MCP-Server). |
| **Done** | Series that share one colour | A series' `ColorGroup` puts it in the same rendered colour as every other series holding the group, as long as the run is contiguous; a Kaplan-Meier curve's band and censor marks, a `Raincloud`'s violin/box/points and a `Horizon`'s folds all use it. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Done** | Raincloud | `Raincloud` draws a half violin, a narrow box and a group's raw, jittered points side by side, all sharing one colour, with one legend entry for the group. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Done** | Horizon chart | `Horizon` folds a signed series into bands of two hues, one for its non-negative part and one for its negative part. See [Examples](https://github.com/xkqg/MatPlotLibNet/wiki/Examples). |
| **Later** | A legend placed where it covers the least data | `LegendPosition.Best` is the upper right corner; it does not look at the data, as matplotlib's `loc='best'` does. |
| **Later** | Annotations in the theme's text size | An annotation is drawn at size 10 whatever the theme, and cannot be moved by a number of pixels from its point. |
| **Later** | Colour bar ticks at round numbers | A colour bar puts its ticks at fifths of its range, so a range of 0 to 11 reads 0.0, 2.2, 4.4, 6.6, 8.8 and 11.0 where matplotlib's locator gives 0, 2, 4, 6, 8 and 10. |
| **Later** | Turned tick labels given room sideways | A turned tick label gets room below the axis but not beside it, and a Y tick label is measured upright. |
| **Later** | A neutral colour the theme owns | Reference lines, levels, trendlines and the connectors of the composed charts share one fixed grey, below 3 : 1 on five themes. |
| **Later** | A grid changed with WithGrid that follows the theme after JSON | A changed grid is written as it draws under the theme of that moment, so a theme set later does not restyle it. |
| **Later** | A filled line contour that follows the axes' range | The colour backdrop of a line contour drawn with `Filled` covers the whole plot while its lines follow a limit or a zoom. |
| **Later** | A bar's value label stands beyond the bar's end on an axis that runs backwards | A bar's value label is put 4 px above its end, or 4 px right of it on a horizontal bar, as the screen draws it; on an axis that runs backwards that is inside the bar, where matplotlib's `bar_label` puts it beyond the end. |
| **Later** | A density curve inside its own range | A `Kde` curve is drawn on a wider grid than the range its axes are given: for the sample 1 to 6 and 30 in a 500 × 350 figure it runs 82.66 px beyond the plot on each side. |
| **Later** | A relative rotation graph that always shows its centre | The plot ranges over the tail points only, so with data away from 100 it shows one quadrant and no crosshair. |
| **Later** | An inset built with the builder that keeps its axes settings | `AddInset` copies the series, title, labels, limits, spines and grid; the legend, tick locators, scales, overlays and colour bar stay behind. |
| **Later** | A PNG saved without the Skia package | Without `MatPlotLibNet.Skia`, `Save("chart.png")` writes SVG text into the file. |
| **Later** | A map's polygons back from JSON | `MatPlotLibNet.Geo`'s `GeoPolygonSeries` writes its own discriminator, but nothing is registered to answer it, so a map is dropped whole on the way back — the one series this release's JSON-round-trip work left out, because its projection carries its own construction parameters (a Lambert's two standard parallels, an orthographic's centre point) that need a shape of their own in the wire format first. |
| **Later** | Series that refuse input that does not fit when they are built | Some series take data they cannot draw and throw only when the figure is drawn. |
| **Later** | Series that draw without the axes around them | A KPI tile in a mosaic built by hand draws ticks and a frame; only `Plt.OpsDashboard()` hides them. |
| **Later** | A legend entry that fits a state timeline | A state timeline's legend swatch wears one colour, which none of its segments wears. |
| **Later** | A Marimekko chart's table that names its columns | The table keeps each column's edges and names its segments; the column names stand under the centres of the columns, where no point of the chart stands, so no row is named by them. |
| **Later** | A calendar heatmap's table that gives each day's date | The table names each cell's weekday and numbers its week column; the chart travels as a heatmap of its cells, which does not keep the dates it was laid out from. |
| **Later** | Violin density from a kernel estimate | A violin's outline counts the observations in a window around each of 21 heights; it is not a density estimate. |
| **Later** | A survival chart's risk table and log-rank test | `KaplanMeier` draws the curve, its confidence band and its censor marks; the number at risk under the plot and a test between the groups are not drawn. |
| **Later** | More control charts | The run rules beyond the first, the moving-range chart, and charts of subgroups (X-bar and R, p, c). |
| **Later** | A Q-Q plot against other distributions | `QQPlot` compares a sample with the normal distribution only. |
| **Later** | Parallel coordinates in each axis's own units | Each column is scaled to 0..1; an axis per column with its own ticks needs a scale per column. |
| **Later** | Stacked-area layers in the legend and through MCP | A `StackPlot` layer's name does not reach the legend, and the MCP server refuses a stacked-area spec. |
| **Later** | One radial scale for polar charts | The rings of a polar grid and each polar series are scaled by different rules, so the ring labels do not read the data's radii; the rings stand at fifths of 1.1 times the largest radius, and their labels are printed at three decimals on the 0° spoke, where matplotlib's are round numbers at 22.5°. |
| **Later** | Stacked polar bars | Wind-rose bins stacked outward from the centre; polar bars overlay, each series on its own radial scale. |
| **Later** | Bars stacked on both sides of zero | Positive layers grow up from zero and negative layers down; a stacked bar adds every layer to one running sum. |
| **Later** | A waterfall's target | A budget marker beside each step; totals are marked with `TotalIndices`. |
| **Later** | Two plots matplotlib has | `fill_betweenx`, and `matshow`, whose square cells need an aspect setting. |
| **Later** | Gradient fills | An area or a bar that fades from one colour to another or to clear; only Sankey links draw a gradient. |
| **Later** | Range frames | An axis line that spans only the data's range; a spine always runs the full length of the plot. |
| **Later** | Annotations that move out of the way | Text notes placed so they do not cover each other, the data or the legend; an annotation is drawn where it is anchored. |
| **Later** | Arrows along a connected scatter | An arrow head on each segment to show direction; no series draws arrow heads along a line. |
| **Later** | A waffle chart | Equal squares coloured by share, or an icon repeated once per unit; markers are sized in points, so they do not tile. |
| **Later** | Stem-and-leaf plot | The digits of a sample laid out in rows by their leading digits; no series draws aligned columns of text. |
| **Later** | Calendar of events | Marks inside a day's cell need sizes in cell units, which markers lack. |
| **Later** | Word cloud | Words sized by weight and packed without overlap need a text-packing layout. |
| **Later** | Chord diagram | Ribbons between arcs around a circle need a series of their own. |
| **Later** | Arc diagram | Nodes on a line joined by arcs above it need a series of their own. |
| **Later** | Icicle chart | A hierarchy in rows by depth; a layout beside the treemap and sunburst. |
| **Later** | Ternary plot | Three parts that sum to one, on triangular axes. |
| **Later** | Piper diagram | Two ternary triangles and a diamond; it needs ternary axes first. |
| **Later** | Anchored VWAP | The volume-weighted price counted from a bar you pick; `Vwap` and its streaming form start at the first bar. |
| **Later** | Volume profile | Volume per price as bars beside the price; a bar on a second axis is not kept. |
| **Later** | Renko | Bricks of a fixed price step, in columns that rise and fall. |
| **Later** | Kagi | One line that thickens and thins at each reversal; a line has one width. |
| **Later** | Point and Figure | Columns of X and O boxes; markers are sized in points, so boxes do not tile. |
| **Later** | Map regions coloured by value | A polygon layer with a colour per region and a colour bar; the map layer paints every region in one colour. |
| **Later** | Bubble, flow and cartogram maps | Points, bubbles, hexagon bins, flow lines and resized regions on a projection; the map layer draws polygons. |
| **Later** | Linked selection | Choose points in one chart and see the same rows lit in the others. |
| **Later** | Step-through SVG | One SVG file with a slider or a play button that steps through frames without a server. |
| **Later** | A live chart that sends its own gestures | `MplLiveChart` shows the server's redraws; the page still has to hand its script a SignalR connection. |
| **Later** | Light with a strength of its own | The ambient and diffuse arguments of `WithLighting` change nothing in the picture yet. |
| **Later** | Home that returns to the first view in the native controls | After a zoom and a repaint, Home returns to the view on screen, not the one first drawn. |
| **Later** | The MAUI view on a par with the others | It draws and streams a chart, and does not pan, zoom or select, nor draw a hatch. |
| **Later** | Streaming forms of more indicators | 11 of the 58 indicators have a streaming form that follows each new bar. |
| **Later** | Transfer entropy over time | A rolling window, a continuous estimator and a conditional form beside the scalar that ships. |
| **Later** | A handler of your own for a chart's gestures | One interface a chart's events go to, beyond the callbacks a chart session takes today. |
| **Later** | Several viewers as a designed feature | Viewers of one chart that see each other's gestures by design, not as a side effect where the last one wins. |
| **Later** | One verb per side that stacks a Y axis, not three | `WithSecondaryYAxis` (right slot 0), `WithOutwardYAxis(AxisSide.Right, ...)` (right slot 1+) and the primary `Axes.YAxis` (not in any list) are three different things a caller reaches for depending on how many axes already stand on that side. A single `WithAddRightYAxis`/`WithAddLeftYAxis`, called as many times as there are axes wanted, would let the container (primary axis included, as its own slot 0) stack each call past the one before it on its own — no "second" or "third" in a verb name, nothing for the caller to count. The margin the layout reserves for that side falls out the same way: the sum of every axis' own band in the container, counted once, whatever its length, instead of one formula for the primary axis and another summed on top for the rest. |
| **Later** | The Windows heads tested by CI | The WPF, Uno and MAUI suites run on Windows by hand or before a push, never on the CI runners. |
| **Later** | The matplotlib comparison run by CI | The suite that compares the output with images matplotlib drew is run by hand. Its waterfall test draws other values than its reference script and passes only on a wide tolerance. |

Every row has a section of its own on the [roadmap in the wiki](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap): what
it is, why it stands where it does, what a row still to come waits for, and what is left out on purpose. This table is
the wiki's, and a test holds the two to one another.
