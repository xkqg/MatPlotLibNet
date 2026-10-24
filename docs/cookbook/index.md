---
title: "C# chart examples for .NET"
description: "Copy-paste chart examples for C# and .NET, each with the code and the picture it produces. Line, bar, heatmap, financial, 3D, map and dashboard charts."
---

# Cookbook

> **Try these examples live.** Open the [Playground](https://xkqg.github.io/MatPlotLibNet/playground/), pick an example, change the theme, resize the chart, and see the SVG update instantly in your browser. No install needed.

This cookbook contains code examples with their rendered output. Every example uses the fluent `Plt.Create()` API. You can copy and paste any of them into a .NET 10 console app after running `dotnet add package MatPlotLibNet` (and `MatPlotLibNet.Skia` for PNG/PDF/GIF).

## Categories

| Category | Examples |
|---|---|
| [From matplotlib to C#](matplotlib-to-csharp.md) | The pyplot call you already know and the line that draws the same chart here — figures, plots, labels, colour, statistics, images, 3D and polar |
| [Line Charts](line-charts.md) | Line styles, markers, gaps at missing values, smooth interpolation, step functions, PropCycler, LTTB downsampling, grid/spine control, secondary Y-axis, outside legend, legend value display; in one call each: bump chart, control chart, stream graph, horizon chart; connected scatter |
| [Direct labels & axis colours](direct-labels-and-axis-colours.md) | A line's name at the end of the line instead of in the legend, each Y axis coloured like its line, a dual-axis chart that needs no legend, what is kept in JSON |
| [Bar Charts](bar-charts.md) | Vertical/horizontal bars, bars below zero, bars on a log scale, stacked, grouped, bar labels, hatching, width/alpha control; in one call each: Pareto, lollipop, dumbbell, slope graph, dot plot, parallel coordinates, Marimekko; waterfall with totals |
| [Pie & Donut](pie-donut.md) | Pie, donut, nested pie (sunburst), explode, shadow, percentage labels, custom colors, hatches |
| [Distribution](distribution.md) | Histogram (density, cumulative, step, hatched, overlapping), box plot, violin, placing them, KDE, rug plot, density scatter; in one call each: ridgeline, Q-Q plot, Kaplan-Meier curve, raincloud |
| [Polar Charts](polar.md) | Polar line, multi-series, radar, polar bar, polar scatter, polar heatmap, wind rose |
| [Heatmaps & Colormaps](heatmaps.md) | Heatmap, colorbar customization, 148 colormaps, color normalization, 2D histogram, pcolormesh; in one call each: calendar heatmap from dates, spy plot of a matrix |
| [Latency Heatmaps](latency-heatmap.md) | A histogram over time: a real clock across, log-scaled latency bucket edges up the side, counts in the cells, with a percentile line on top |
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
| [Error Bars](error-bars.md) | Symmetric, asymmetric, cap size, combined scatter+error, confidence bands, A/B test |
| [Broken Axes](broken-axes.md) | X/Y breaks, zigzag/diagonal/none styles, combined breaks, outlier handling |
| [Symlog Axis](symlog.md) | Symmetric log Y/X, financial P&L, scale comparison (linear vs log vs symlog) |
| [Geographic Maps](geographic.md) | 13 projections, globe view, coastlines, borders, land/ocean fill, custom GeoJSON, edge handling |
| [Themes](themes.md) | 30 built-in themes, custom ThemeBuilder, PropCycler, 3D pane colors |
| [Interactive SVG](interactive-svg.md) | Browser pan/zoom/tooltips/legend toggle, highlight, selection, 3D rotation, SignalR |
| [Animation](animation.md) | FuncAnimation GIF, multi-page PDF, individual frame export, rotating 3D |
| [Streaming & Realtime](streaming.md) | Live data append, ring buffers, streaming indicators, axis scaling, Rx integration |
| [Annotations](annotations.md) | Text, arrows, reference lines (horizontal/vertical), shaded spans, threshold convenience (line + breach span in one call), math text in annotations |
| [Styling & Themes](styling.md) | 30 themes, PropCycler, grid/spine control, tight margins, tick mirroring, templates |
| [Tick Formatting](tick-formatting.md) | Engineering notation, date axes, custom locators, minor ticks |
| [State timeline in WPF](wpf-timeline.md) | A WPF window with a state timeline: tooltip and click per event, pan and zoom along time, stacked categories on one time axis, a marker for the playback position |
| [Dashboard tiles & timelines](dashboard.md) | Stat tile (KPI headline number), state timeline (discrete coloured segments), mosaic dashboard composition, topology panel (the service map) |
| [MCP server (AI agents)](mcp.md) | Render charts from Claude, VS Code or any MCP host; the spec format, the five tools, the refusal messages |
| [Verso notebooks](verso.md) | A chart inside a notebook cell: end the cell on a figure or a builder, how tall a chart may be, what a failed render says |
| [Accessibility](accessibility.md) | The data table beside the chart (`ToDataTables()` → HTML/markdown/CSV), color-blind safe palette, high-contrast theme, ARIA semantics, keyboard navigation |
| [International text](international-text.md) | Arabic and Hebrew out of the box, registering a font file for other scripts (`SkiaFonts.Register`), the `MATPLOTLIBNET_FONTS` variable for the MCP server, what kerning changed |
