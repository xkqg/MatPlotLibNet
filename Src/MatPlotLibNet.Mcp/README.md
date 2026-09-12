# MatPlotLibNet.Mcp

<!-- The MCP Registry proves that whoever owns the server name also owns the NuGet package by looking for this
     exact line in the PUBLISHED package's README. It is the ownership handshake, not decoration: without it the
     registry refuses the listing with "must appear as 'mcp-name: ...' in the package README". -->
mcp-name: io.github.xkqg/matplotlibnet

An [MCP](https://modelcontextprotocol.io) (Model Context Protocol) server for [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet).
It gives an AI agent (Claude Code, VS Code, Claude Desktop, or any other MCP host) five tools:

| tool | what it does |
|---|---|
| `render_chart` | renders a chart spec to a PNG image the model can look at |
| `save_chart` | writes the chart as PNG, SVG or PDF to a file and returns the path it wrote |
| `chart_data_table` | returns the chart's data as markdown tables the model can read, and the same values as structured content it can compute with |
| `list_chart_types` | lists the chart types the spec accepts; the list is read from the library's own registry |
| `describe_chart_schema` | describes the fields of the spec and gives a worked example for a chart type |

The server communicates over stdio. It is a .NET tool: a host runs it with `dnx MatPlotLibNet.Mcp` (the .NET 10 SDK
resolves and starts it), or with `dotnet tool exec MatPlotLibNet.Mcp`.

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

## The spec

The spec is the library's own figure JSON: the same document that `figure.ToJson()` writes and
`ChartSerializer.FromJson` reads. The agent can therefore ask for any chart the library can draw:

```json
{
  "width": 800, "height": 600, "title": "Revenue",
  "subPlots": [{
    "series": [{ "type": "line", "xData": [1, 2, 3, 4, 5], "yData": [2, 4, 3, 5, 1], "label": "Q1" }]
  }]
}
```

The server validates the spec before it renders. It rejects an unknown field, an unknown chart type, a misspelled
enum value, a colour the library does not know, or a missing size. The error message names the field that is
wrong, so the model can correct the spec instead of receiving a blank picture.

## What comes back

`render_chart` returns the PNG as an image block. Beside it, it returns a short text summary (chart types, series,
axis ranges), so a model that cannot see the image still knows what it made. SVG and PDF output is large, so it
goes to a file through `save_chart` and is never returned inline.

The SVG this server writes stores text as glyph outlines (`<path>`), not as `<text>` elements. This is because the
PNG backend is loaded in the same process, and the library then embeds its own font so that the drawing is
identical everywhere.

## Limits

- `save_chart` writes only under one directory: the system temp directory, or the one named by the
  `MATPLOTLIBNET_MCP_OUTPUT_ROOT` environment variable. It never overwrites an existing file unless asked.
- Arabic and Hebrew text work without setup. For Devanagari, Thai, Chinese, Japanese or Korean, name font files
  or directories of font files in the `MATPLOTLIBNET_FONTS` environment variable, separated by `;`; they are
  registered when the server starts. A bad entry is logged and skipped.
- The server refuses a canvas larger than the documented ceiling, and the error message states the ceiling.

## License

MIT — see the repository.
