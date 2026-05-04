# Project rules — see CONTRIBUTING.md

## Knowledge graph (graphify)

Two pre-built knowledge graphs are available via `.mcp.json`:
- **`graphify`** — source code graph: 11890 nodes, 14113 edges, 935 communities (`C:\Ait\MatPlotLibNet\.graphify\graph.json`)
- **`graphify-wiki`** — wiki docs graph: 95 nodes, 207 edges, 7 communities (`MatPlotLibNet.wiki\.graphify\graph.json`)

Use `query_graph`, `get_neighbors`, `god_nodes`, `shortest_path`, `get_community` before reading files.
Top god nodes: `AxesBuilder`, `Axes`, `FigureBuilder` — the fluent API core is the architectural centre.
Update with `/graphify C:\Ait\MatPlotLibNet --update` when significant code changes land.

All contributor rules for this repository (versions, TDD, engineering discipline, class design, dead-code deletion, git workflow, CHANGELOG, documentation sweep) live in [**CONTRIBUTING.md**](CONTRIBUTING.md). Read it before any PR or commit.
