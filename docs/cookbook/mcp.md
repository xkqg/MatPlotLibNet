# MCP server — charts for an AI agent

`MatPlotLibNet.Mcp` is a [Model Context Protocol](https://modelcontextprotocol.io) server: a .NET tool that an MCP
host — Claude Code, Claude Desktop, VS Code — starts over stdio, so a model can render this library's charts and
look at the result.

## Install

The package is a .NET tool; the .NET 10 SDK resolves and runs it, so there is nothing to install by hand.

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

For Claude Code: `claude mcp add matplotlibnet -- dnx MatPlotLibNet.Mcp --yes`.

One optional setting, `MATPLOTLIBNET_MCP_OUTPUT_ROOT`: the only directory `save_chart` may write under. It
defaults to the system temp directory.

## The five tools

| tool | what it does |
|---|---|
| `render_chart(spec, format)` | renders the spec and returns a PNG the model can see, with a text summary beside it |
| `save_chart(spec, path, format, overwrite)` | writes PNG, SVG or PDF to a file and returns the path it wrote |
| `chart_data_table(spec)` | the chart's DATA as markdown tables — the numbers the picture is drawn from |
| `list_chart_types()` | the chart types a spec may name |
| `describe_chart_schema(seriesType)` | the fields of a spec, or of one chart type, with a worked example |

SVG and PDF are not returned inline — a single SVG of a normal chart is tens of thousands of characters of the
model's context. `save_chart` writes them to a file instead.

## The spec

A spec is the library's own figure JSON: the same document `figure.ToJson()` writes and `ChartSerializer.FromJson`
reads. There is one definition of a chart, so anything the library draws, an agent can ask for.

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
    .SetXLabel("Quarter")
    .SetYLabel("€M")
    .Plot([1, 2, 3, 4], [12, 18, 15, 22], s => { s.Label = "2026"; s.Color = Color.FromName("steelblue"); })
    .Build()
    .ToJson(indented: true);
```

That is the fastest way to learn the format: build the chart you want with the fluent API, print its JSON, and
hand that shape to the model.

## It refuses before it renders

`FromJson` is a round-trip reader for the library's own writer: it drops an unknown series type, skips an unknown
property and ignores a misspelled enum value, because a newer document must still load in an older build. Handed a
document a *model* typed, that lenience turns every typo into a blank chart reported as a success. So the server
validates first, and the message names the field:

| what the model wrote | what it gets back |
|---|---|
| `"type": "lien"` | `'lien' at $.subPlots[0].series[0].type is not a chart type this server lists — did you mean 'line'?` |
| `"type": "Line"` | the same, with `'line'` — the serializer's lookup is case-sensitive |
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
picture cannot disagree. A count is omitted rather than guessed: a pie has slices, not points.

`chart_data_table` answers what the PNG cannot: **the values.** A model can see that a line rises; it cannot
read 18.4 off it. The tool returns the figure's own data tables as markdown — one table per group of series
that share an x, captioned with the chart's name:

```
**Revenue**

| Quarter | 2026 |
| --- | --- |
| 1 | 12 |
| 2 | 18 |
| 3 | 15 |
```

It is the same `figure.ToDataTables()` every other host serves (see the
[accessibility page](accessibility.md)), so the numbers a model reads and the table a screen reader reads are
one thing. A chart with more rows than the server's row ceiling is refused with the count, the ceiling and the
way around it — `save_chart`, then read the file. A table is text, and text is context.

## Two things worth knowing

**The SVG this server writes carries glyph outlines, not `<text>`.** The PNG backend is loaded in the same
process, and it installs a glyph-path provider so text is drawn with the library's own embedded font everywhere.
Self-contained, and identical to the PNG — but not machine-readable as text.

**Seven chart types are not available through the spec.** `sankey`, `sunburst`, `treemap`, `polarbar`,
`polarline`, `polarscatter` and `treegrid` have a JSON reader that builds a fixed placeholder instead of reading
the document, so the server refuses them by name rather than draw someone else's data. `list_chart_types` names
them with the reason.
