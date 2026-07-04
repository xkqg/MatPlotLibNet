# Project rules — see CONTRIBUTING.md

## Knowledge graph (codegraph)

The live source graph is the **`codegraph`** MCP server in `.mcp.json` (`.codegraph/codegraph.db`, delta-synced by the codegraph daemon — no manual rebuild needed).
Use `codegraph_search`, `codegraph_context`, `codegraph_callers`, `codegraph_callees`, `codegraph_impact`, `codegraph_explore` before reading files.
Top god nodes: `AxesBuilder`, `Axes`, `FigureBuilder` — the fluent API core is the architectural centre.

A graphify snapshot remains at `.graphify/graph.json` but predates the v1.11/v1.12 series — treat it as stale; refresh on demand with `/graphify C:\Ait\MatPlotLibNet --update`. The former wiki graph was generated from a clone under `%TEMP%` that no longer exists — to regenerate, clone `https://github.com/xkqg/MatPlotLibNet.wiki.git` to a durable path first.

All contributor rules for this repository (versions, TDD, engineering discipline, class design, dead-code deletion, git workflow, CHANGELOG, documentation sweep) live in [**CONTRIBUTING.md**](CONTRIBUTING.md). Read it before any PR or commit.
