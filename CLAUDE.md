# Project rules — see CONTRIBUTING.md

## Code graph (roslyn-codelens)

The live source graph is the **`roslyn-codelens`** MCP server in `.mcp.json` (Roslyn over
`MatPlotLibNet.slnx`, compiler-exact). Use `search_symbols`, `get_symbol_context`, `get_type_overview`,
`find_callers`, `get_call_graph`, `find_references`, `analyze_change_impact` before reading files.
⚠ no file-watcher — call `rebuild_solution` after edits. (`codegraph` was removed fleet-wide 2026-08-29.)
Top god nodes: `AxesBuilder`, `Axes`, `FigureBuilder` — the fluent API core is the architectural centre.

A graphify snapshot remains at `.graphify/graph.json` but predates the v1.11/v1.12 series — treat it as stale; refresh on demand with `/graphify C:\Ait\MatPlotLibNet --update`. The former wiki graph was generated from a clone under `%TEMP%` that no longer exists — to regenerate, clone `https://github.com/xkqg/MatPlotLibNet.wiki.git` to a durable path first.

All contributor rules for this repository (versions, TDD, engineering discipline, class design, dead-code deletion, git workflow, CHANGELOG, documentation sweep) live in [**CONTRIBUTING.md**](CONTRIBUTING.md). Read it before any PR or commit.
