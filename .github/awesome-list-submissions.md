# Awesome List Submissions

Pre-written submission texts for .NET community awesome lists. Copy-paste into a PR or issue on each repo.

Every entry below was checked against the live README and the live CONTRIBUTING of that repository, and each
one names the exact place it goes. Two things measured there that an entry has to respect: the four lists want
a SHORT description — the earlier drafts here ran three lines and would have been sent back — and every list
asks for one link per pull request.

---

## 1. awesome-dotnet (quozd/awesome-dotnet — 21.6k stars)

**Section:** `## Graphics` (README line 520) · **Format, read from CONTRIBUTING:** `* [LIBRARY](LINK) - DESCRIPTION`,
descriptions concise, one link per pull request, and the PR body says why it belongs on the list. The section is
NOT alphabetical — measured: Oxyplot, OpenTK, Aspose.Drawing, ScottPlot, LiveCharts2, Helix Toolkit — so the
entry goes at the end of the section. Entries there carry no closing period.

**Status:** prepared, not submitted. MatPlotLibNet is absent (0 hits in the live README).

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) - matplotlib-inspired charting for .NET: 83 chart types, server-rendered SVG/PNG/PDF/GIF, and native controls for Blazor, WPF, MAUI, Avalonia and Uno
```

**PR title:** Add MatPlotLibNet to Graphics

---

## 2. awesome-dotnet-core (thangchung/awesome-dotnet-core — 21.4k stars)

**Section:** `### Graphics` (README line 414) · **Format, read from contributing.md:** `* [LIBRARY](LINK) - DESCRIPTION.`,
and *"make sure the alphabetical order is maintained"* — the section is alphabetical, so the entry goes between
`MagicScaler` and `QRCoder`.

**Status:** prepared, not submitted. MatPlotLibNet is absent (0 hits in the live README).

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) - Code-first charting inspired by matplotlib, rendering SVG, PNG and PDF without a browser.
```

**PR title:** Add MatPlotLibNet to Graphics

---

## 3. awesome-blazor (AdrienTorris/awesome-blazor — 9.4k stars)

**Section:** `#### Charts` (README line 205) · **Format, read from CONTRIBUTING:** `* [NAME](LINK) - DESCRIPTION.`,
descriptions simple, every description ends with a period, one suggestion per pull request, squashed to one
commit. The star and last-commit badges the existing rows carry are not in the guidelines and are not ours to
add — leave them to the maintainer.

**Status:** prepared, not submitted. MatPlotLibNet is absent (0 hits in the live README).

```markdown
* [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) - Charting for Blazor as inline SVG, with no JavaScript charting library.
```

**PR title:** Add MatPlotLibNet to Charts

---

## 4. awesome-avalonia (AvaloniaCommunity/awesome-avalonia — 3.2k stars)

**Section:** `### Charts & Plots & Diagrams` (README line 259) — NOT "Libraries & Extensions", which an earlier
draft here named · **Format, read from CONTRIBUTING:** `- [Lib Name](link) - Description.` with a hyphen marker
rather than an asterisk, short description, and *"we try to keep the list sorted"* — so the entry goes between
`LiveCharts2` and `Microcharts`.

**Status:** prepared, not submitted. MatPlotLibNet is absent (0 hits in the live README).

```markdown
- [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet) - Native Avalonia charting control with a SkiaSharp backend.
```

**PR title:** Add MatPlotLibNet to Charts & Plots & Diagrams

---

## 5. NuGet blog community spotlight

**Email to:** nuget-team@microsoft.com (or submit via https://devblogs.microsoft.com/nuget/)

**Subject:** Community spotlight: MatPlotLibNet — matplotlib-inspired charting for .NET

**Body:**

MatPlotLibNet is a new MIT-licensed charting library for .NET 10 that brings matplotlib's code-first philosophy to the .NET ecosystem. Key features:

- 83 series types covering line, bar, scatter, heatmap, contour, candlestick, Sankey, treemap, sunburst, polar, and 12 3D chart types
- 148 colormaps (viridis, plasma, turbo, etc.)
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

**Title:** MatPlotLibNet — matplotlib-inspired charting for .NET, with 3D, MathText and native Avalonia/Uno controls

**Body:**

I've been building a charting library for .NET that takes matplotlib's code-first approach and brings it to C#.
Where it stands now:

- **83 series types** — everything from line/bar/scatter to Sankey diagrams, treemaps, and 12 3D chart types
- **Native UI controls** — Avalonia 12 (`MplChartControl`) and Uno Platform (`MplChartElement`) render via SkiaSharp with local pan/zoom/brush-select
- **MathText** — LaTeX-like labels with fractions, square roots, accents, Greek letters (96 symbol mappings)
- **Text in any script** — Arabic and Hebrew read the right way round, because the Unicode bidirectional algorithm and HarfBuzz shaping are both in there
- **SignalR interactivity** — server-authoritative charts with mutation + notification events
- **148 colormaps**, SIMD numerics, DataFrame integration with 58 technical indicators, Jupyter notebooks
- **An MCP server** so an agent can draw one of these charts and then look at it

MIT licensed, .NET 10 and .NET 8, 11,757 tests, and every class has to clear 90 % line and branch coverage.

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

**Submit at:** https://mcpservers.org/submit — five fields, one optional paid upgrade.

The dropdown has no data-visualisation category. Measured from the page itself: Development, Productivity,
Database, Search, Web Scraping, File System, Version Control, Communication, Cloud Service, Cloud Storage,
Marketing, Finance, Design, Memory, Other. **Development** is the honest fit — Design is where the Figma-style
tools sit.

| field | what to type |
|---|---|
| Server Name | `MatPlotLibNet` |
| Category | `Development` |
| Short Description | `Hand it the JSON for a chart and it renders a PNG the model can actually look at — 83 chart types, SVG and PDF written to file, running locally as a .NET tool.` |
| Repository, Website or Documentation | `https://github.com/xkqg/MatPlotLibNet` |
| Contact Email | the maintainer's own address |
| Premium Submit (€/$39) | leave unticked — it buys a queue jump, a badge and a dofollow link, none of which the listing needs |

**Status:** submitted by the maintainer — the form answered *“Your MCP server ‘MatPlotLibNet’ has been submitted successfully… reviewed within 12 hours”* and promised an email on approval. Nothing to chase before that mail arrives; if it does not, the form takes a second submission.

The longer description below is for anywhere that takes more than one line.

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
