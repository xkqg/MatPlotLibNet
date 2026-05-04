# Cookbook

> **Want to try these live?** Open the [Playground](https://xkqg.github.io/MatPlotLibNet/playground/) — pick an example, change the theme, resize the chart, and see the SVG update instantly in your browser. No install needed.

Code examples with rendered output. Every example uses the fluent `Plt.Create()` API and can be copy-pasted into a .NET 10 console app with `dotnet add package MatPlotLibNet` (and `MatPlotLibNet.Skia` for PNG/PDF/GIF).

## Categories

| Category | Examples |
|---|---|
| [Line Charts](line-charts.md) | Line styles, markers, smooth interpolation, step functions, PropCycler, LTTB downsampling, grid/spine control, secondary Y-axis, outside legend |
| [Bar Charts](bar-charts.md) | Vertical/horizontal bars, stacked, grouped, bar labels, hatching, width/alpha control |
| [Pie & Donut](pie-donut.md) | Pie, donut, nested pie (sunburst), explode, shadow, percentage labels, custom colors, hatches |
| [Distribution](distribution.md) | Histogram (density, cumulative, step, hatched, overlapping), box plot, violin, KDE, rug plot |
| [Polar Charts](polar.md) | Polar line, multi-series, radar, polar bar, polar scatter, polar heatmap |
| [Heatmaps & Colormaps](heatmaps.md) | Heatmap, colorbar customization, 104 colormaps, color normalization, 2D histogram, pcolormesh |
| [Contour Plots](contour.md) | Contour lines with labels, filled contour, custom levels, contour overlay, triangulated contour |
| [Subplots & GridSpec](subplots.md) | Grid subplots, GridSpec ratios, mosaic layout, spanning subplots, insets, shared axes, spacing |
| [Financial Charts](financial.md) | Candlestick + MACD + RSI dashboard, technical indicators (Bollinger, Williams %R, OBV, SAR, CCI) |
| [Sankey Diagrams](sankey.md) | Income statement, customer journey, process distribution, vertical orientation, severity cascade |
| [Treemaps & Hierarchical](treemaps.md) | Nested pie, treemap with drilldown |
| [Dendrograms](dendrograms.md) | Hierarchical-clustering tree, four orientations, cut-height with cluster colours |
| [Clustermap](clustermap.md) | Heatmap + row/column dendrograms, automatic reordering, panel ratio control |
| [Pair Grid (PairPlot)](pairplot.md) | N×N matrix of histograms (or KDE) on the diagonal + scatters off-diagonal, hue grouping, triangular suppression |
| [Network Graph](network-graph.md) | Nodes + edges in 2D, three deterministic layouts (Manual/Circular/Hierarchical), directed-edge arrowheads, per-node colour and size scalars, DataFrame edge-list ingestion |
| [3D Charts](threed.md) | Surface, scatter, bar, Line3D, Trisurf, Contour3D, Quiver3D, Voxels, Text3D, camera, lighting, pane styling |
| [Math Text](mathtext.md) | Greek letters, fractions, square roots, accents, font variants, operator limits (∫ Σ Π), matrices |
| [Error Bars](error-bars.md) | Symmetric, asymmetric, cap size, combined scatter+error, confidence bands |
| [Broken Axes](broken-axes.md) | X/Y breaks, zigzag/diagonal/none styles, combined breaks, outlier handling |
| [Symlog Axis](symlog.md) | Symmetric log Y/X, financial P&L, scale comparison (linear vs log vs symlog) |
| [Geographic Maps](geographic.md) | 13 projections, globe view, coastlines, borders, land/ocean fill, custom GeoJSON, edge handling |
| [Themes](themes.md) | 26 built-in themes, custom ThemeBuilder, PropCycler, 3D pane colors |
| [Interactive SVG](interactive-svg.md) | Browser pan/zoom/tooltips/legend toggle, highlight, selection, 3D rotation, SignalR |
| [Animation](animation.md) | FuncAnimation GIF, multi-page PDF, individual frame export, rotating 3D |
| [Streaming & Realtime](streaming.md) | Live data append, ring buffers, streaming indicators, axis scaling, Rx integration |
| [Annotations](annotations.md) | Text, arrows, reference lines (horizontal/vertical), shaded spans, math text in annotations |
| [Styling & Themes](styling.md) | 26 themes, PropCycler, grid/spine control, tight margins, tick mirroring, templates |
| [Tick Formatting](tick-formatting.md) | Engineering notation, date axes, custom locators, minor ticks |
| [Accessibility](accessibility.md) | Color-blind safe palette, high-contrast theme, ARIA semantics, keyboard navigation |
