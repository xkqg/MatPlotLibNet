# MatPlotLibNet.Mcp

An [MCP](https://modelcontextprotocol.io) (Model Context Protocol) server for [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet).
It gives an AI agent — Claude Code, VS Code, Claude Desktop, any MCP host — four tools:

| tool | what it does |
|---|---|
| `render_chart` | renders a chart spec to a PNG image the model can look at |
| `save_chart` | writes the chart as PNG, SVG or PDF to a file and returns the path it wrote |
| `list_chart_types` | the chart types the spec accepts, read off the library's own registry |
| `describe_chart_schema` | the fields of the spec, and a worked example for a chart type |

The server speaks stdio. It is a .NET tool: a host runs it with `dnx MatPlotLibNet.Mcp` (the .NET 10 SDK
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

The spec is the library's own figure JSON — the same document `figure.ToJson()` writes and
`ChartSerializer.FromJson` reads — so anything the library can draw, the agent can ask for:

```json
{
  "width": 800, "height": 600, "title": "Revenue",
  "subPlots": [{
    "series": [{ "type": "line", "xData": [1, 2, 3, 4, 5], "yData": [2, 4, 3, 5, 1], "label": "Q1" }]
  }]
}
```

The server checks the spec before it renders: an unknown field, an unknown chart type, a misspelled enum
value, a colour the library does not know, a missing size — each is refused with a message that names the
offending field, so the model can correct itself instead of receiving a blank picture.

## What comes back

`render_chart` returns the PNG as an image block and, beside it, a short text summary (chart types, series,
axis ranges) so a model that cannot see the image still knows what it made. SVG and PDF are large; they go
to a file through `save_chart`, never inline.

The SVG this server writes carries text as glyph outlines (`<path>`), not `<text>` elements — the PNG
backend is loaded in the same process, and the library then embeds its own font so the drawing is identical
everywhere.

## Limits

- `save_chart` writes only under one directory: the system temp directory, or the one named by the
  `MATPLOTLIBNET_MCP_OUTPUT_ROOT` environment variable. It never overwrites an existing file unless asked.
- A canvas larger than the documented ceiling is refused with the ceiling in the message.

## License

MIT — see the repository.
