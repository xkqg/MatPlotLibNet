# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

All contributor rules — versions, TDD, class design, dead-code deletion, git workflow, CHANGELOG, the
documentation sweep — live in [**CONTRIBUTING.md**](CONTRIBUTING.md). Read it before any commit. This file
covers only what that document does not: the commands that actually work, and the shape of the code.
**Never add a `Co-Authored-By` line** — not to a commit, not to a doc, not to code (CONTRIBUTING.md:154). It overrides any
attribution default the tooling suggests. Commits stay local and several are fine; the maintainer says when to push
(one push per finished strand) and he alone tags (CONTRIBUTING.md:152-155).
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

**The ubuntu half runs locally in docker.** `docker run --rm -v <repo>:/mnt/repo:ro -v <wiki>:/mnt/wiki:ro
mcr.microsoft.com/dotnet/sdk:10.0-noble bash -c "<script>"`, started from PowerShell (Git Bash rewrites the mount paths).
The script installs `libfontconfig1 libfreetype6 git ca-certificates` (and `python3` for the Pages job), `git clone`s both
mounts beside each other (the committed state with LF endings, as on the runner, not the CRLF working copy), points
`origin` at the GitHub URL (without it SourceLink warns and the build is not 0 warnings), then runs the steps of `ci.yml`
(restore, build `MatPlotLibNet.CI.slnf`, the 12 suites with `dotnet run --no-build`) and, for `pages.yml`,
`dotnet tool install -g docfx`, `docfx metadata docs/docfx.json` and `docfx build docs/docfx.json` with
`DOTNET_ROLL_FORWARD=Major` (the image has the 10 runtime only). Measured on the 1.24.0 tree: 0 warnings, 0 errors,
all 12 suites green, docfx 0 errors.

`tools/coverage/thresholds.json` carries no exemptions by policy (`90/90`, `exemptions: {}`); a class that is
legitimately untestable gets `[ExcludeFromCodeCoverage]` at the source instead, so it drops out of the cobertura
report rather than being floor-exempted in config.

The `push-gate` agent (`.claude/agents/push-gate.md`; `.claude/` is gitignored, so a fresh clone has none) runs `tools/prepush.ps1` and reports ready / not ready; it
never commits or pushes. CI builds with the `10.0.x` SDK at `dotnet-quality: preview` (`ci.yml`).

## The two solution views, and why a green build can still be red

- `MatPlotLibNet.slnx` — the working set: the 15 packages, 15 test projects (incl. `Wpf`, `Uno`, `Maui`, which CI
  does not run), 7 samples, the benchmarks. It does **not** hold the three npm binding folders in `Src/`, nor
  `Tst/MatPlotLibNet.Fidelity` — a matplotlib-reference image-diff suite that no solution, workflow or coverage
  run builds or runs (it builds clean on its own: `dotnet build` on its csproj, 0 warnings). Nor does it hold
  `Samples/MatPlotLibNet.Playground`, `.Samples.AspNetCore` or `.Samples.Wpf` — three more csproj with no build
  verdict from slnx or CI.slnf; Playground still gets one, because `MatPlotLibNet.Tests.csproj` references it and
  Pages publishes it.
- `MatPlotLibNet.CI.slnf` — a **subset** (23 projects: 11 packages, their 11 test projects and the wiki suite). CI
  builds this and runs **12** suites. Wpf, Uno and Maui are outside it (Windows or a workload), and so is the frozen
  Notebooks.
- **The wiki is compiled by a test.** `Tst/MatPlotLibNet.Wiki` compiles every `csharp` block of the wiki against
  the packages as built (warnings fail it) and holds the shape of the wiki's `Examples.md`. It reads the wiki
  **beside the repository** — `git clone https://github.com/xkqg/MatPlotLibNet.wiki.git ../MatPlotLibNet.wiki` —
  and fails with that command when the clone is missing. ci.yml build-and-test clones it first; the coverage job
  and publish.yml do not run the suite (publish checks out the release tag, and the wiki has no tags). A marker
  comment on the line above a block steers it: `<!-- example: name -->`, `<!-- continues: name -->`,
  `<!-- a C# cell of a notebook -->`, `<!-- needs: Wpf -->` (or Uno, Maui); any other comment there is refused. It also
  holds the wiki's `images/` byte-for-byte to the repository's `images/`: the sample program draws every picture
  (`WikiPictures.cs` the ones no sample draws), so after a rendering change re-run it and copy what changed into the wiki.
- `docs/docfx.json` is the only docfx config Pages and `prepush.ps1` build; the `docfx.json` in the repo root (with
  the root `articles/`, `api/` and `toc.yml` it points at, plus the root `coverlet.runsettings` — no collector reads
  it, the coverage collector is `dotnet-coverage` and a contract test refuses any coverlet package) is older config
  that nothing reads. The live docfx loads **14** csproj, including Uno, Wpf and Notebooks, which are *not* in the
  CI filter (Mcp is built as net10.0, Wpf as net8.0-windows10.0.19041.0, the rest as net8.0). A project that builds
  in CI can still red the Pages job. `tools/prepush.ps1` runs docfx locally.
- Pages also publishes `Samples/MatPlotLibNet.Playground` (Blazor WASM), and `Tst/MatPlotLibNet` references it
  (`MatPlotLibNet.Tests.csproj`), so building the core suite builds the Playground too.
- `Directory.Build.props` sets `EnableWindowsTargeting=true` repo-wide on non-Windows: docfx passes the Wpf
  project's TargetFramework as a *global* MSBuild property, which spills onto every other project's reference
  resolution on the Linux Pages runner, so core and Skia would otherwise refuse with "set EnableWindowsTargeting"
  mid-build. One root cause, one root-level fix — never a per-project workaround.
- The runners are **ubuntu-latest**, and docker on a Windows box runs that half ("The ubuntu half runs locally in docker"
  above): a change to a TFM, a csproj property or `docfx.json` is judged in a Linux container, not read against the
  runner's OS and not pushed as a probe.

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

**A series lives in more than one file.** The class (`Models/Series/<family>/`) serialises and draws itself —
`ToSeriesDto()`, `internal static FromSeriesDto(Axes, SeriesDto)`, `Accept(ISeriesVisitor, RenderArea)`; see
`XY/LineSeries.cs` — but three central places still need a line: a `Visit(X, RenderArea)` overload on
`ISeriesVisitor` with its drawing in `SvgSeriesRenderer` (the only implementor in `Src/`), a
`Register("x", X.FromSeriesDto)` in `SeriesRegistry.RegisterDefaults`, and the `Axes` verb that constructs it.
Adding one turns `SeriesCountContractTests` red on purpose: bump `PublishedSeriesTypes` and the count in every
document that states it. The tests keep their own lists: add the instance to `AllSeriesTests.AllSeriesInstances`
(with its overload on the test-side `TestSeriesVisitor`) and the discriminator to the `ChartSerializerRoundTripTests`
Theory — `docs/COVERAGE.md` § "Cross-cutting Theory pattern": extend those, do not clone a per-type test file.

**A settable property on a series or axes must also reach the JSON.** `SeriesDrawnPropertiesRoundTripTests` and
`AxesDrawnPropertiesRoundTripTests` (`Tst/MatPlotLibNet/Serialization/`) reflect over every settable property of the
types in `DrawnSeriesFixtures` / `DrawnAxesFixtures` and go red, naming type and property, until the DTO carries it:
a DTO field, written in `ToSeriesDto`, read in `FromSeriesDto`, a default left out via `OrNullAtRest`. A property no
JSON can carry goes in `DrawnSeriesFixtures.NotCarried` with its reason; a series type outside the contract goes in
`NotUnderContract`.

**A chart composed of existing series is a verb, not a series.** Pareto, ridgeline and calendar heatmap are
extension methods on `AxesBuilder` in the family class that matches the caller's data (`Categorical`, `Distribution`,
`XY` or `GridAxesExtensions`, in `Builders/`): no `ISeries`, no `SeriesRegistry` key, `SeriesCountContractTests`
untouched. It needs a `ComposedChartShapes` entry and a row in `ARCHITECTURE.md` § "Where a verb lives"
(`VerbPlacementContractTests`); the full recipe is CONTRIBUTING.md § "Adding a chart verb".

**Shared behaviour is a C# 14 `extension(T value) { }` block, never a `this`-param static method.** Every
`*Extensions` class in `Src/` and the satellite packages is written this way (`LangVersion` is 14 repo-wide,
`Directory.Build.props`). Two traps that only show up past the call site: a member's reflected
`MethodInfo.DeclaringType` is a compiler-synthesized nested `<ContainingClass>+<G>$<hash>` skeleton type, not
the class as written — one `.DeclaringType` up recovers it — and the generated XML doc mirrors the split (the
entry under the clean name is a bare `<inheritdoc cref="...skeleton.../>` shim; the real summary/param/returns
content lives under the skeleton member, and `GetParameters()[0]` is always the receiver, documented once on
the block rather than per member). A bare `Foo.Bar(...)` where `Foo` is a namespace resolves only in a file
whose own `namespace` nests directly under `MatPlotLibNet` (C#'s enclosing-namespace lookup, not `using`) — true
everywhere in `Src/`, false in `Samples/`, the wiki and docs code-fences, which need the qualified form.

**A technical indicator takes one `OhlcvBars` value, not five parallel arrays.** `Indicators/OhlcvBars.cs` is a
record struct with one named factory per shape an indicator actually reads — `Hlc`, `Hl`, `Hlv`, `Cv`, `Ohlc`,
`Hlcv` — each validating only the arrays it uses via `ArrayLengthExtensions.ThrowIfMisaligned`. Every
`CandleIndicator<TResult>` subclass and the `AxesBuilder` verb that forwards to it takes `OhlcvBars bars`;
`MatPlotLibNet.DataFrame`'s column-name verbs keep their own string signature and build the value internally
from `df.DoubleCol(...)`.

**Colours and colormaps are three classes, not one.** `Colors` is a 40-name curated palette, `Css4Colors` the
full 148-name CSS4 table, and the maps live on `ColorMaps` (9 named + `Get(name)`), `DivergingColorMaps`,
`SequentialColorMaps`, `QualitativeColorMaps`, `CyclicColorMaps`. A reversed map is `new ReversedColorMap(map)`
or the registry's `"<name>_r"`.

**Satellite packages** each wrap the same `Figure`: Skia (export), AspNetCore (REST + SignalR hub), Blazor,
Interactive (browser popup), GraphQL, Avalonia/Uno/Wpf/Maui (native controls), DataFrame (indicators over
`Microsoft.Data.Analysis`), Geo (projections + Natural Earth), Mcp (a .NET tool MCP server), Verso (a notebook
formatter). **`MatPlotLibNet.Notebooks` is frozen** at the version it reached and is deliberately out of
`CI.slnf` and both workflows; `ReleaseContractTests.Frozen` holds that with its reason.

**The release contract is executable.** `Tst/MatPlotLibNet/Packaging/ReleaseContractTests.cs` (1200+ lines, 50+
tests) reads the csprojs, the workflows, `docs/docfx.json`, `docs/index.md`, the cookbook pages, the coverage
runsettings and `run.sh`, and fails when a hand-kept list drifts — every package must carry the same version, be
built and pushed by some workflow, be measured by the coverage gate, and be linked from the docs. It reaches past
packaging into SEO/accessibility (search-engine description, analytics tag, IndexNow key, social-card size, the
accessibility page naming every series with no tabular form), the MCP manifest's own version, the icon surviving
`.gitignore`, and the version named on ARCHITECTURE.md's/README's/SECURITY.md's first line. Adding a package or a
series means editing every list it checks; the test tells you which one you forgot.

Every `Src/` package packs on build (`GeneratePackageOnBuild`) and is checked for API breaks against
`PackageValidationBaselineVersion` in `Directory.Build.props` (always the release just before the current one —
`ThePackBaseline_IsTheReleaseJustBeforeThisOne` fails if it is not moved with the version). Notebooks (frozen)
and the Mcp .NET-tool package opt out; nothing else does. The SourceLink version is pinned once, in the same file
(`$(SourceLinkVersion)`), to dodge a CVE band in `Microsoft.Build.Tasks.Git` — never restate it per project.

**Tests that fail only on the ubuntu runner.** Six separated process-global statics — `ChartSerializerDefault`,
`ChartRendererDefault`, `SvgRendererDefault`, `FontMetricsDefault`, `GlyphPathProviderDefault`, each a single
replaceable `Current` slot, and the registry of file transforms in `FigureExtensions` (extension to exporter, seeded with a
refusal for `.png`, `.pdf` and `.gif`) — are read by every render test and by every `Save`. A test that swaps one must join a
`*GlobalState` collection (`ChartServicesGlobalStateCollection`, `DisableParallelization = true`), or it
races the render tests under the runner's slower scheduling and never on a fast Windows box; and no test may register
`.png`, `.pdf`, `.gif`, `.svg` or `.json` over the registry, which would change what every other `Save` writes (register a
made-up extension instead). Fixture data has to be
platform-deterministic too: no `Math.Sin/Cos/Exp/Log/Pow` in byte-compared goldens (Windows and glibc differ by one
ulp), no `new Uri("/x")` (Linux accepts it, Windows rejects it), `InvariantCulture` for anything formatted.
`MPL_REGEN_GOLDEN=1` makes `ChartSerializerGoldenCorpusTests` write its golden files instead of asserting on them.

## Code graph (roslyn-codelens)

The live source graph is the **`roslyn-codelens`** MCP server in `.mcp.json` (gitignored, so a fresh clone has none;
Roslyn over `MatPlotLibNet.slnx`, compiler-exact). Use `search_symbols`, `get_symbol_context`, `get_type_overview`, `find_callers`,
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
