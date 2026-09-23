---
title: "MCP chart server: let an AI agent draw charts"
description: "Let an AI agent draw charts. The MatPlotLibNet MCP server renders any chart type to PNG, SVG or PDF from a JSON spec, over stdio."
---

# MCP server — charts for an AI agent

> **Install:** `dotnet tool install --global MatPlotLibNet.Mcp`

`MatPlotLibNet.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io) server. It is a .NET tool that an
MCP host (Claude Code, Claude Desktop, VS Code) starts over stdio. Through it, a model can render this library's
charts and look at the result.

## Install

The package is a .NET tool. The .NET 10 SDK resolves and runs it, so there is nothing to install by hand.

```json
{
  "servers": {
    "MatPlotLibNet.Mcp": {
      "type": "stdio",
      "command": "dnx",
      "args": ["MatPlotLibNet.Mcp", "--yes"]
    }
  }
}
```

For Claude Code, run `claude mcp add matplotlibnet -- dnx MatPlotLibNet.Mcp --yes`.

There are two optional settings. `MATPLOTLIBNET_MCP_OUTPUT_ROOT` is the only directory `save_chart` may write under.
It defaults to the system temp directory. `MATPLOTLIBNET_FONTS` names font files, or directories of font files,
separated by `;`. The server registers them when it starts, so a chart can use a script that the bundled font lacks
(Devanagari, Thai, Chinese). Arabic and Hebrew work without it. See [International text](international-text.md).

## The five tools

| tool | what it does |
|---|---|
| `render_chart(spec, format)` | renders the spec and returns a PNG the model can see, with a text summary beside it |
| `save_chart(spec, path, format, overwrite)` | writes PNG, SVG or PDF to a file and returns the path it wrote |
| `chart_data_table(spec)` | returns the chart's data twice: markdown tables to read, and the same values as structured content to compute with |
| `list_chart_types()` | lists the chart types a spec may name |
| `describe_chart_schema(seriesType)` | describes the fields of a spec, or of one chart type, with a worked example |

SVG and PDF are not returned inline, because a single SVG of a normal chart is tens of thousands of characters of
the model's context. `save_chart` writes them to a file instead.

Each tool also tells the host what it does to the machine it runs on, which is what decides whether a host runs it
without asking. Four of the five change nothing at all, so they are marked read-only. `save_chart` writes a file,
and with `overwrite: true` it replaces one, so it is marked as changing things and as able to destroy something.
None of the five reaches out to the internet or to anything else outside its own arguments. Left unsaid, the
protocol assumes the opposite on both counts, so a host would warn about a tool that only draws a picture.

## The spec

A spec is the library's own figure JSON. It is the same document that `figure.ToJson()` writes and
`ChartSerializer.FromJson` reads. Because there is one definition of a chart, an agent can ask for any chart the
library draws.

```json
{
  "width": 800,
  "height": 600,
  "title": "Revenue",
  "subPlots": [
    {
      "xAxis": { "label": "Quarter" },
      "yAxis": { "label": "€M" },
      "series": [
        { "type": "line", "xData": [1, 2, 3, 4], "yData": [12, 18, 15, 22], "label": "2026", "color": "steelblue" }
      ]
    }
  ]
}
```

The same JSON in C#:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

string spec = Plt.Create()
    .WithSize(800, 600)
    .WithTitle("Revenue")
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXLabel("Quarter")
        .SetYLabel("€M")
        .Plot([1, 2, 3, 4], [12, 18, 15, 22], s => { s.Label = "2026"; s.Color = Color.FromName("steelblue"); }))
    .Build()
    .ToJson(indented: true);
```

That is the fastest way to learn the format. Build the chart you want with the fluent API, print its JSON, and hand
that JSON to the model.

## It refuses before it renders

`FromJson` is a round-trip reader for the library's own writer. It drops an unknown series type, skips an unknown
property and ignores a misspelled enum value, because a newer document must still load in an older build. When a
model wrote the document, that lenience turns every typo into a blank chart that is reported as a success. So the
server validates the document before it renders, and the error message names the field:

| what the model wrote | what it gets back |
|---|---|
| `"type": "lien"` | `'lien' at $.subPlots[0].series[0].type is not a chart type this server lists — did you mean 'line'?` |
| `"type": "Line"` | the same message, with `'line'`; the serializer's lookup is case-sensitive |
| `"x": [...]` | `Unknown field 'x' at $.subPlots[0].series[0]. describe_chart_schema lists the fields a spec accepts.` |
| `"scale": "logarithmic"` | `'logarithmic' … is not an accepted value; accepted: Linear, Log, SymLog, Logit, Date.` |
| `"color": "reddish"` | `'reddish' … is not a colour the library knows: use a CSS4 colour name or #RRGGBB.` |
| `xData` of 3, `yData` of 1 | `The series at $.subPlots[0].series[0] has 3 'xData' values and 1 'yData' values; they must match.` |
| no `width` | rendered at the library default, 800×600 |
| `"width": 16000` | `'width' must be between 1 and 4000 … the picture goes into a context window.` |

Colour names go through the library's own CSS4 table, and `#f00` is expanded, so a model may write `"red"`,
`"CornflowerBlue"` or `#6495ED`.

## What comes back

`render_chart` returns two blocks: the PNG, and a summary of what was drawn.

```
"Revenue" — 800×600, 1 subplot, 1 series
  · line "2026", 4 points, x 1…4, y 12…22
```

The ranges come from the same `ComputeDataRange` call the renderer makes to place the axes, so the text and the
picture cannot disagree. A count is left out rather than guessed. A pie, for example, has slices, not points.

`chart_data_table` returns the values, which the PNG cannot give. A model can see that a line rises, but it cannot
read the value 18.4 off the picture. The tool returns the figure's own data tables as markdown. There is one table
per group of series that share an x, captioned with the chart's name:

```
**Revenue**

| Quarter | 2026 |
| --- | --- |
| 1 | 12 |
| 2 | 18 |
| 3 | 15 |
```

The tables come from the same `figure.ToDataTables()` call that every other host serves (see the
[accessibility page](accessibility.md)), so the numbers a model reads and the table a screen reader reads are the
same data. The same tables come back a second time beside the markdown, as structured content, so a model
that wants to add a column up does not have to parse a pipe table back into numbers first:

```json
{
  "tables": [
    {
      "caption": "Revenue",
      "columns": [
        { "header": "Quarter", "kind": "number", "axis": "x" },
        { "header": "2026", "kind": "number", "axis": "y" }
      ],
      "rows": [[1, 12], [2, 18], [3, 15]]
    }
  ]
}
```

A column says what it holds — `number`, `date` or `text` — and which axis its values are positions on. The two
forms differ deliberately in one place: markdown is for reading, so a date is spelled the way a person reads it,
while this is for a machine, so a date is written out in full as `2026-09-12T12:00:00.000`, which sorts correctly
as text. A chart with more rows than the server's row ceiling is refused. The refusal gives the row count, the
ceiling and the way around it: call `save_chart`, then read the file. A table is text, and text takes up the
model's context.

## Two things worth knowing

**The SVG this server writes carries glyph outlines, not `<text>`.** The PNG backend is loaded in the same
process, and it installs a glyph-path provider, so text is drawn with the library's own embedded font everywhere.
The SVG is self-contained and identical to the PNG, but it is not machine-readable as text.

**Seven chart types are not available through the spec.** `sankey`, `sunburst`, `treemap`, `polarbar`,
`polarline`, `polarscatter` and `treegrid` have a JSON reader that builds a fixed placeholder instead of reading
the document. The server refuses these types by name, so that it does not draw the placeholder's data in place of
the data in the spec. `list_chart_types` names them with the reason.
