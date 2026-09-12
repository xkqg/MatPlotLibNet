# Awesome List Submissions

Pre-written submission texts for .NET community awesome lists. Copy-paste into a PR or issue on each repo.

---

## 1. awesome-dotnet (quozd/awesome-dotnet — 19k+ stars)

**Section:** Graphics / Charting

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) — matplotlib-inspired charting for .NET 10. 83 series types, 142 colormaps, 3D projection pipeline, MathText (LaTeX-like labels), native Avalonia/Uno/MAUI controls, bidirectional SignalR, SVG/PNG/PDF/GIF export. MIT licensed.
```

**PR title:** Add MatPlotLibNet to Graphics section

---

## 2. awesome-dotnet-core (thangchung/awesome-dotnet-core — 20k+ stars)

**Section:** Graphics

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) — Code-first charting library inspired by matplotlib. Fluent API, 83 series types (including 12 3D types), 142 colormaps, SIMD numerics, native UI controls (Avalonia 12, Uno, MAUI), SignalR interactivity, and headless SVG/PNG/PDF rendering.
```

---

## 3. awesome-blazor (AdrienTorworthy/awesome-blazor)

**Section:** Libraries & Extensions / Charts

```markdown
* [MatPlotLibNet.Blazor](https://github.com/xkqg/MatPlotLibNet) — Blazor charting with `MplChart` and `MplLiveChart` Razor components. 83 series types rendered as SVG, bidirectional SignalR for server-authoritative pan/zoom/hover, no JavaScript charting library dependency.
```

---

## 4. awesome-avalonia (AvaloniaCommunity/awesome-avalonia)

**Section:** Libraries & Extensions

```markdown
* [MatPlotLibNet.Avalonia](https://github.com/xkqg/MatPlotLibNet) — Native Avalonia 12 charting control (`MplChartControl`) with SkiaSharp backend. 83 series types, local pan/zoom/reset/brush-select via managed interaction layer, optional SignalR server mode. MIT licensed.
```

---

## 5. NuGet blog community spotlight

**Email to:** nuget-team@microsoft.com (or submit via https://devblogs.microsoft.com/nuget/)

**Subject:** Community spotlight: MatPlotLibNet — matplotlib-inspired charting for .NET

**Body:**

MatPlotLibNet is a new MIT-licensed charting library for .NET 10 that brings matplotlib's code-first philosophy to the .NET ecosystem. Key features:

- 83 series types covering line, bar, scatter, heatmap, contour, candlestick, Sankey, treemap, sunburst, polar, and 12 3D chart types
- 142 colormaps (viridis, plasma, turbo, etc.)
- LaTeX-like MathText for publication-quality labels
- Native UI controls for Avalonia 12, Uno Platform, and MAUI
- Bidirectional SignalR for server-authoritative interactive charts
- SVG, PNG, PDF, and animated GIF export
- An MCP server (`MatPlotLibNet.Mcp`) so an AI agent can render any of these chart types from a JSON spec
- 14 NuGet packages, 11,000+ tests

NuGet: https://www.nuget.org/packages/MatPlotLibNet
GitHub: https://github.com/xkqg/MatPlotLibNet
Cookbook: https://xkqg.github.io/MatPlotLibNet/cookbook/

---

## 6. Reddit r/dotnet + r/csharp

**Title:** MatPlotLibNet v1.3.0 — matplotlib-inspired charting for .NET with 82 series types, 3D, MathText, and native Avalonia/Uno controls

**Body:**

I've been building a charting library for .NET that takes matplotlib's code-first approach and brings it to C#. After several months of development, v1.3.0 is out with:

- **82 series types** — everything from line/bar/scatter to Sankey diagrams, treemaps, and 12 3D chart types
- **Native UI controls** — Avalonia 12 (`MplChartControl`) and Uno Platform (`MplChartElement`) render via SkiaSharp with local pan/zoom/brush-select
- **MathText** — LaTeX-like labels with fractions, square roots, accents, Greek letters (96 symbol mappings)
- **SignalR interactivity** — server-authoritative charts with mutation + notification events
- **142 colormaps**, SIMD numerics, DataFrame integration, Jupyter notebooks

MIT licensed, .NET 10, 4,000+ tests.

- GitHub: https://github.com/xkqg/MatPlotLibNet
- NuGet: https://www.nuget.org/packages/MatPlotLibNet
- Cookbook: https://xkqg.github.io/MatPlotLibNet/cookbook/

Would love feedback!

---

## 7. MCP audience — where an MCP server actually gets found

The canonical one is done: the server is listed in the official
[MCP Registry](https://registry.modelcontextprotocol.io) as `io.github.xkqg/matplotlibnet`, which is what a host
resolves against when it looks a server up by name. `modelcontextprotocol/servers` no longer takes community
pull requests — its README now points at the registry — so what is left is the human-browsable side.

### 7a. mcpservers.org (submission form, feeds wong2/awesome-mcp-servers)

**Submit at:** https://mcpservers.org/submit

**Name:** MatPlotLibNet
**Repository:** https://github.com/xkqg/MatPlotLibNet
**Category:** Data Visualization

**Description:**

```
Charts an agent can look at. Hand it the JSON for a figure and it renders a PNG and describes what it drew;
ask for SVG or PDF and it writes the file and tells you where. It knows 83 chart types — line, bar, scatter,
heatmap, candlestick, treemap, and a dozen 3-D ones — and it checks the spec before it draws, so a typo comes
back as "unknown field 'x', did you mean 'xData'" instead of a blank picture. Runs on your machine as a .NET
tool; no browser, no service, nothing leaves the box.
```

### 7b. punkpeye/awesome-mcp-servers (pull request)

**Section:** Data Visualization · **Format:** `[owner/repo](url) <emoji> - description`, `#️⃣` marks a C# codebase.

```markdown
* [xkqg/MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) #️⃣ 🏠 🍎 🪟 🐧 - Charts an agent can look at: hand it the JSON for a figure and it returns a PNG with a short description of what it drew, or writes SVG/PDF to a file. 83 chart types, from a line plot to a candlestick to a 3-D surface, and the spec is checked before anything is drawn so a typo comes back named instead of blank. Runs locally as a .NET tool.
```

**Status:** opened as [punkpeye/awesome-mcp-servers#14213](https://github.com/punkpeye/awesome-mcp-servers/pull/14213).
Their CONTRIBUTING asks an automated contributor to end the PR title with three robot emoji to opt into the
fast-track lane, so the title carries them.

### 7c. appcypher/awesome-mcp-servers — CLOSED, do not try again

The repository is **archived** (measured 2026-09-12: `archived: true`, issues off), so it takes no pull request;
the API answers a PR attempt with a bare 404. Its list is frozen wherever it stood. The text that was written
for it is folded into 7a instead.

### 7d. r/mcp, r/dotnet, Hacker News — for the owner to post

**Title:** MatPlotLibNet now ships an MCP server — a .NET charting library an agent can draw with

**Body:**

```
I maintain a matplotlib-inspired charting library for .NET, and the thing I kept running into is that a model
can ask for a chart but never gets to see one. So the library now ships an MCP server.

You hand it the same JSON the library's own `figure.ToJson()` writes, and it hands back a PNG plus a line of
text saying what it drew — "Revenue — 800x600, 1 subplot, 1 series; line '2026', 4 points, x 1..4, y 12..22" —
so a model that cannot see the image still knows what it made. SVG and PDF go to a file instead, because one
SVG is tens of thousands of characters of context.

The part I spent the most time on is refusing well. The library's JSON reader is lenient by design: it skips an
unknown property so a newer document still loads in an older build. Handed a document a model typed, that turns
every typo into a blank chart reported as a success. So the server validates first and names the field —
"'lien' is not a chart type, did you mean 'line'?" — because a model that is told nothing retries the same
mistake.

It is a .NET tool, so a host starts it with `dnx MatPlotLibNet.Mcp`. MIT, and it is in the MCP registry as
io.github.xkqg/matplotlibnet.
```

**Note:** section 6 above still describes v1.3.0 (82 series types, 4,000 tests) — rewrite it before posting.
