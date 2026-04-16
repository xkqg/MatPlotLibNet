# Awesome List Submissions

Pre-written submission texts for .NET community awesome lists. Copy-paste into a PR or issue on each repo.

---

## 1. awesome-dotnet (quozd/awesome-dotnet — 19k+ stars)

**Section:** Graphics / Charting

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) — matplotlib-inspired charting for .NET 10. 67 series types, 104 colormaps, 3D projection pipeline, MathText (LaTeX-like labels), native Avalonia/Uno/MAUI controls, bidirectional SignalR, SVG/PNG/PDF/GIF export. MIT licensed.
```

**PR title:** Add MatPlotLibNet to Graphics section

---

## 2. awesome-dotnet-core (thangchung/awesome-dotnet-core — 20k+ stars)

**Section:** Graphics

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) — Code-first charting library inspired by matplotlib. Fluent API, 67 series types (including 12 3D types), 104 colormaps, SIMD numerics, native UI controls (Avalonia 12, Uno, MAUI), SignalR interactivity, and headless SVG/PNG/PDF rendering.
```

---

## 3. awesome-blazor (AdrienTorworthy/awesome-blazor)

**Section:** Libraries & Extensions / Charts

```markdown
* [MatPlotLibNet.Blazor](https://github.com/xkqg/MatPlotLibNet) — Blazor charting with `MplChart` and `MplLiveChart` Razor components. 67 series types rendered as SVG, bidirectional SignalR for server-authoritative pan/zoom/hover, no JavaScript charting library dependency.
```

---

## 4. awesome-avalonia (AvaloniaCommunity/awesome-avalonia)

**Section:** Libraries & Extensions

```markdown
* [MatPlotLibNet.Avalonia](https://github.com/xkqg/MatPlotLibNet) — Native Avalonia 12 charting control (`MplChartControl`) with SkiaSharp backend. 67 series types, local pan/zoom/reset/brush-select via managed interaction layer, optional SignalR server mode. MIT licensed.
```

---

## 5. NuGet blog community spotlight

**Email to:** nuget-team@microsoft.com (or submit via https://devblogs.microsoft.com/nuget/)

**Subject:** Community spotlight: MatPlotLibNet — matplotlib-inspired charting for .NET

**Body:**

MatPlotLibNet is a new MIT-licensed charting library for .NET 10 that brings matplotlib's code-first philosophy to the .NET ecosystem. Key features:

- 67 series types covering line, bar, scatter, heatmap, contour, candlestick, Sankey, treemap, sunburst, polar, and 12 3D chart types
- 104 colormaps (viridis, plasma, turbo, etc.)
- LaTeX-like MathText for publication-quality labels
- Native UI controls for Avalonia 12, Uno Platform, and MAUI
- Bidirectional SignalR for server-authoritative interactive charts
- SVG, PNG, PDF, and animated GIF export
- 11 NuGet packages, 4,000+ tests

NuGet: https://www.nuget.org/packages/MatPlotLibNet
GitHub: https://github.com/xkqg/MatPlotLibNet
Cookbook: https://xkqg.github.io/MatPlotLibNet/cookbook/

---

## 6. Reddit r/dotnet + r/csharp

**Title:** MatPlotLibNet v1.3.0 — matplotlib-inspired charting for .NET with 67 series types, 3D, MathText, and native Avalonia/Uno controls

**Body:**

I've been building a charting library for .NET that takes matplotlib's code-first approach and brings it to C#. After several months of development, v1.3.0 is out with:

- **67 series types** — everything from line/bar/scatter to Sankey diagrams, treemaps, and 12 3D chart types
- **Native UI controls** — Avalonia 12 (`MplChartControl`) and Uno Platform (`MplChartElement`) render via SkiaSharp with local pan/zoom/brush-select
- **MathText** — LaTeX-like labels with fractions, square roots, accents, Greek letters (96 symbol mappings)
- **SignalR interactivity** — server-authoritative charts with mutation + notification events
- **104 colormaps**, SIMD numerics, DataFrame integration, Jupyter notebooks

MIT licensed, .NET 10, 4,000+ tests.

- GitHub: https://github.com/xkqg/MatPlotLibNet
- NuGet: https://www.nuget.org/packages/MatPlotLibNet
- Cookbook: https://xkqg.github.io/MatPlotLibNet/cookbook/

Would love feedback!
