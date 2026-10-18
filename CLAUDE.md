# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

All contributor rules — versions, TDD, class design, dead-code deletion, git workflow, CHANGELOG, the
documentation sweep — live in [**CONTRIBUTING.md**](CONTRIBUTING.md). Read it before any commit. This file
covers only what that document does not: the commands that actually work, and the shape of the code.
**Never add a `Co-Authored-By` line** — not to a commit, not to a doc, not to code (CONTRIBUTING.md:121). It overrides any
attribution default the tooling suggests.
CONTRIBUTING.md's "Running Tests" section still says `dotnet test` — that is the dead signal described below.
Deeper design notes: `Src/MatPlotLibNet/ARCHITECTURE.md` (there is no root-level ARCHITECTURE.md).

## Commands

⚠ **`dotnet test` runs NOTHING here and exits 0.** The suites are xunit **v3** console executables, so
`dotnet test` restores, prints no summary, and reports success — a green from a dead signal. Measured on
`MatPlotLibNet.Verso.Tests`: `dotnet test` → no test summary line; `dotnet run --project` → 25 tests.

```bash
dotnet build MatPlotLibNet.CI.slnf -c Release          # what CI builds; must end 0 warnings / 0 errors

dotnet run --project Tst/MatPlotLibNet/MatPlotLibNet.Tests.csproj -c Release    # one suite, the way CI runs it

# one test class (or one test) — the built exe takes an xunit v3 filter
Tst/MatPlotLibNet/bin/Release/net10.0/MatPlotLibNet.Tests.exe -filter "/*/*/LegendTests/*"
```

```powershell
tools/coverage/run.ps1 -Check -Strict    # all suites + the per-class gate (>=90/90 line+branch, no regression)
tools/prepush.ps1                        # everything CI judges, incl. the docfx site; -SkipCoverage for docs-only
```

**Run coverage through native PowerShell, never `bash tools/coverage/run.sh`** — under Git Bash the
dotnet-coverage profiler fails to attach ("Profiler was not initialized") and measures nothing.

## The two solution views, and why a green build can still be red

- `MatPlotLibNet.slnx` — everything (18 `Src/` folders: 15 packages + 3 npm binding folders; 15 test projects
  incl. `Fidelity`, `Wpf`, `Uno`, `Maui`, which CI does not run).
- `MatPlotLibNet.CI.slnf` — a **subset** (22 projects). CI builds this and runs **11** suites. Wpf, Uno and
  Maui are outside it: they need Windows or a workload.
- `docs/docfx.json` — loads **15** csproj, including Uno (net8.0) and Wpf (net8.0-windows), which are *not* in
  the filter. A project that builds in CI can still red the Pages job. `tools/prepush.ps1` runs docfx locally.
- The runners are **ubuntu-latest**. A change to a TFM, a csproj property or `docfx.json` has no local verdict:
  read it against the runner's OS instead of pushing a probe.

## Architecture

**The fluent core is the whole library.** `Plt` is the entry point; everything else hangs off two builders, and
the split between them is the single thing that trips both callers and documentation:

```
Plt.Create()  ->  FigureBuilder        figure-level: WithTitle, WithTheme, WithSize, WithGridSpec,
                                       TightLayout, Save/ToSvg/ToJson, AddSubPlot
   .AddSubPlot(r, c, i, ax => ax ...)  ->  AxesBuilder
                                       axes-level: SetXLabel, SetYLabel, WithLegend, WithGrid,
                                       tick locators/formatters, and most series verbs
Plt.Mosaic("AB\nCD") -> MosaicFigureBuilder   panels by letter; the figure underneath is reached
                                              through .Configure(fig => ...), NOT by chaining
Plt.OpsDashboard()   -> OpsDashboardBuilder   tiles + timelines + one shared time window
```

`FigureBuilder` carries convenience overloads of many series verbs that forward to a single implicit axes, so
`Plt.Create().Plot(x, y)` works — but `WithLegend()`, `SetXLabel()` and friends exist **only** on `AxesBuilder`.
Guessing here is how 94 documentation samples came to not compile. Check the member list before writing a chain
(`get_type_overview("FigureBuilder")`), and look at `Samples/MatPlotLibNet.Samples.Console/Program.cs` for the
idiom the library actually ships.

**Rendering.** `FigureBuilder.Build()` → `Figure` → `ToSvg()`: SVG is the native output and the only one core
produces. `MatPlotLibNet.Skia` adds PNG/PDF/GIF and installs a glyph-path provider through a `[ModuleInitializer]`
when its assembly loads — which silently changes core's own output from `<text>` to `<path>` outlines. Render
tests that assert on `>label<` assume the no-Skia default.

**Colours and colormaps are three classes, not one.** `Colors` is a 40-name curated palette, `Css4Colors` the
full 148-name CSS4 table, and the maps live on `ColorMaps` (9 named + `Get(name)`), `DivergingColorMaps`,
`SequentialColorMaps`, `QualitativeColorMaps`, `CyclicColorMaps`. A reversed map is `new ReversedColorMap(map)`
or the registry's `"<name>_r"`.

**Satellite packages** each wrap the same `Figure`: Skia (export), AspNetCore (REST + SignalR hub), Blazor,
Interactive (browser popup), GraphQL, Avalonia/Uno/Wpf/Maui (native controls), DataFrame (indicators over
`Microsoft.Data.Analysis`), Geo (projections + Natural Earth), Mcp (a .NET tool MCP server), Verso (a notebook
formatter). **`MatPlotLibNet.Notebooks` is frozen** at the version it reached and is deliberately out of
`CI.slnf` and both workflows; `ReleaseContractTests.Frozen` holds that with its reason.

**The release contract is executable.** `Tst/MatPlotLibNet/Packaging/ReleaseContractTests.cs` reads the csprojs,
the workflows, `docs/docfx.json`, `docs/index.md`, the cookbook pages, the coverage runsettings and `run.sh`, and
fails when a hand-kept list drifts — every package must carry the same version, be built and pushed by some
workflow, be measured by the coverage gate, and be linked from the docs. Adding a package means editing all of
those lists; the test tells you which one you forgot.

## Code graph (roslyn-codelens)

The live source graph is the **`roslyn-codelens`** MCP server in `.mcp.json` (Roslyn over `MatPlotLibNet.slnx`,
compiler-exact). Use `search_symbols`, `get_symbol_context`, `get_type_overview`, `find_callers`,
`get_call_graph`, `find_references`, `analyze_change_impact` before reading files.
⚠ no file-watcher — call `rebuild_solution` after edits.
Top god nodes: `AxesBuilder`, `Axes`, `FigureBuilder` — the fluent API core is the architectural centre.

A graphify snapshot remains at `.graphify/graph.json` but predates the v1.11/v1.12 series — treat it as stale;
refresh on demand with `/graphify C:\Ait\MatPlotLibNet --update`. The wiki is a separate git repository, cloned
durably at `C:\Ait\MatPlotLibNet.wiki` (branch `master`); a documentation change lands there in the same
increment as the repo.

## Documentation samples are compiled, not eyeballed

Every fenced `csharp` block in `docs/`, `articles/`, `README.md` and the per-package READMEs is real code a
reader will copy. 94 of them had drifted away from the API before they were last swept. When you touch a sample,
compile it. The method and its trap are recorded in
`.path-manifests/adhoc-20260923-doc-samples-do-not-compile.md`: **csc suppresses diagnostics in phases**, so one
parse error anywhere hides every member error in the whole compilation, and a naive batch compile reads green.
