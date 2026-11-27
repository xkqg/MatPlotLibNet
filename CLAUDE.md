# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

All contributor rules (versions, TDD, class design, dead-code deletion, git workflow, CHANGELOG, the documentation sweep) live in
[**CONTRIBUTING.md**](CONTRIBUTING.md). Read it before any commit. This file covers only what that document does not: the commands that
actually work, and the shape of the code. Procedures that sit beside the build (prepush, the ubuntu half in docker, a release, the Fidelity
references) are in [tools/README.md](tools/README.md).

**Never add a `Co-Authored-By` line** to a commit, a doc or code. It overrides any attribution default the tooling suggests. Commits stay
local and several are fine (the maintainer's standing word, looser than the stage-only default in CONTRIBUTING.md § "Git — main repo"); he
says when to push (one push per finished strand) and he alone tags.
Deeper design notes: `Src/MatPlotLibNet/ARCHITECTURE.md` (there is no root-level ARCHITECTURE.md).

## Commands

⚠ **`dotnet test` runs NOTHING here and exits 0.** The suites are xunit **v3** console executables, so `dotnet test` restores, prints no
summary and reports success: a green from a dead signal. Measured on `MatPlotLibNet.Verso.Tests`: `dotnet test` → no summary line;
`dotnet run --project` → 25 tests.

```bash
dotnet build MatPlotLibNet.CI.slnf -c Release          # what CI builds; must end 0 warnings / 0 errors
dotnet build MatPlotLibNet.CI.slnf -c Release -t:Rebuild   # what a fresh checkout does: the API comparison (CP0001) runs when a package is packed afresh, and a build that only compiled the project (a test project's reference) does not run it
dotnet build MatPlotLibNet.NoMaui.slnf -c Release -t:Rebuild -warnaserror   # every project of the slnx but the Maui head: the build to read when you changed anything outside the CI set

dotnet run --project Tst/MatPlotLibNet/MatPlotLibNet.Tests.csproj -c Release    # one suite, the way CI runs it

# one test class (or one test) — the built exe takes an xunit v3 filter
Tst/MatPlotLibNet/bin/Release/net10.0/MatPlotLibNet.Tests.exe -filter "/*/*/LegendTests/*"
```

Core multi-targets `net10.0;net8.0`, so the exe sits under the `net10.0` folder. A change to how something is *drawn* also needs
`Tst/MatPlotLibNet.Fidelity` (CI runs it; how its references are made: tools/README.md).

```powershell
tools/coverage/run.ps1 -Check -Strict    # all suites + the per-class gate (>=90/90 line+branch, no regression)
tools/prepush.ps1                        # everything CI judges, incl. the docfx site; -SkipCoverage for docs-only
```

**Run coverage through native PowerShell, never `bash tools/coverage/run.sh`** (under Git Bash the profiler fails to attach and measures
nothing). **The ubuntu half runs locally in docker, and the runner's `coverage` job is part of it**: a Linux-only coverage miss turned a
release red while every suite was green here (procedure: tools/README.md). The `push-gate` agent (`.claude/agents/push-gate.md`; `.claude/`
is gitignored, so a fresh clone has none) runs `tools/prepush.ps1` and reports ready / not ready; it never commits or pushes. CI builds with
the `10.0.x` SDK at `dotnet-quality: preview`.

**A release does not publish itself.** The maintainer pushes the tag, then dispatches `publish.yml` by hand with its `tag` input
(tools/README.md, "A release").

## The solution views, and why a green build can still be red

- `MatPlotLibNet.slnx` — the working set: the 15 packages, 17 test projects (incl. `Wpf`, `Uno`, `Maui` and `Fidelity`), 7 samples, the
  benchmarks. It does **not** hold the four npm binding folders in `Src/`, nor `Samples/MatPlotLibNet.Playground`, `.Samples.AspNetCore`
  or `.Samples.Wpf` (no build verdict from slnx or CI.slnf; Playground still gets one, because `MatPlotLibNet.Tests.csproj` references it
  and Pages publishes it).
- `MatPlotLibNet.NoMaui.slnf` — the slnx minus the Maui head (39 projects): the one build of everything a box without the maui workload
  can build. `tools/prepush.ps1` builds it, and `ReleasePackagingContractTests` holds it to the slnx with `ReleaseFiles.NeedsWorkload` as the
  one list of what is left out. The bare slnx build needs the workload and otherwise stops at NETSDK1147 before it compiles a project, so a
  filter on that error reads a tree with errors as clean.
- `MatPlotLibNet.CI.slnf` — a **subset** (26 projects: 11 packages, their 11 suites, the wiki, Fidelity, Maui and benchmark-program
  suites). CI builds this and runs **15** suites on ubuntu. The Wpf, Uno and Maui *packages* are outside it (Windows or a workload): the
  `windows-heads` job of `ci.yml` builds them on every push with the steps `publish.yml` uses and runs their suites. The frozen Notebooks is
  outside the CI filter and both workflows.
- **The wiki is compiled by a test.** `Tst/MatPlotLibNet.Wiki` compiles every `csharp` block of the wiki against the packages as built
  (warnings fail it) and reads the wiki **beside the repository** (clone command: CONTRIBUTING.md); it fails with that command when the
  clone is missing. A marker comment on the line above a block steers it: `<!-- example: name -->`, `<!-- continues: name -->`,
  `<!-- a C# cell of a notebook -->`, `<!-- needs: Wpf -->` (or Uno, Maui); any other comment there is refused. It also holds the wiki's
  `images/` byte-for-byte to the repository's (`SamplePicturesTests`; Windows only, details in CONTRIBUTING.md § Documentation). The
  coverage job and publish.yml do not run this suite.
- `docs/docfx.json` is the only docfx config Pages and `prepush.ps1` build; the `docfx.json` in the repo root, the root `articles/`, `api/`,
  `toc.yml` and `coverlet.runsettings` are older config that nothing reads (a contract test refuses any coverlet package; the collector is
  `dotnet-coverage`). The live docfx loads **14** csproj, including Uno, Wpf and Notebooks, which are *not* in the CI filter: a project that
  builds in CI can still red the Pages job. Pages also publishes `Samples/MatPlotLibNet.Playground`.
- `Directory.Build.props` sets `EnableWindowsTargeting=true` repo-wide on non-Windows: docfx passes the Wpf project's TargetFramework as a
  *global* MSBuild property, which spills onto every other project's reference resolution on the Linux Pages runner. One root cause, one
  root-level fix, never a per-project workaround.
- The runners are **ubuntu-latest**: a change to a TFM, a csproj property or `docfx.json` is judged in a Linux container, not read against
  the runner's OS and not pushed as a probe.

## Architecture

**The fluent core is the whole library.** `Plt` is the entry point; everything else hangs off two builders, and the split between them is
the single thing that trips both callers and documentation:

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

`FigureBuilder` carries convenience overloads of many series verbs that forward to a single implicit axes, so `Plt.Create().Plot(x, y)`
works, but `WithLegend()`, `SetXLabel(label)` and friends exist **only** on `AxesBuilder`. Guessing here is how 94 documentation samples came
to not compile. Check the member list before writing a chain (`get_type_overview("FigureBuilder")`), and look at
`Samples/MatPlotLibNet.Samples.Console/Program.cs` for the idiom the library ships.

**Rendering.** `FigureBuilder.Build()` → `Figure` → `ToSvg()`: SVG is the native output and the only one core produces.
`MatPlotLibNet.Skia` adds PNG/PDF/GIF and installs a glyph-path provider through a `[ModuleInitializer]` when its assembly loads, which
silently changes core's own output from `<text>` to `<path>` outlines. Render tests that assert on `>label<` assume the no-Skia default.

**A series lives in more than one file.** The class (`Models/Series/<family>/`) serialises and draws itself (`ToSeriesDto()`, `internal
static FromSeriesDto(Axes, SeriesDto)`, `Accept(ISeriesVisitor, RenderArea)`; see `XY/LineSeries.cs`), but three central places still need
a line: a `Visit(X, RenderArea)` overload on `ISeriesVisitor` with its drawing in `SvgSeriesRenderer`, a `Register(X.FromSeriesDto)` in
`SeriesRegistry.RegisterDefaults`, and the `Axes` verb that constructs it. Adding one turns `SeriesCountContractTests` red on purpose: bump
`PublishedSeriesTypes` and the count in every document that states it. The tests keep one roster: a row in `DrawnSeriesFixtures` (with its
overload on the test-side `TestSeriesVisitor`) runs every theory over the shipped kinds (`docs/COVERAGE.md` § "Cross-cutting Theory
pattern"): extend it, do not clone a per-type test file. A settable property on a series or axes must also reach the JSON: the drawn-properties contracts are in CONTRIBUTING.md § "The JSON
keeps what is drawn".

**A chart composed of existing series is a verb, a shape is a patch, a second unit is a link; none is a series.** Verbs (Pareto,
ridgeline, calendar heatmap) are extension methods on `AxesBuilder` in the family class matching the caller's data, with a
`ComposedChartShapes` entry and a row in `ARCHITECTURE.md` § "Where a verb lives" (recipe: CONTRIBUTING.md § "Adding a chart verb"). A
rectangle, polygon, arrow, circle, ellipse, wedge or arc is a `Patch` (`Models/Patches/`, sealed leaves only the library derives) added
with the one `AddPatch`; a new kind is a leaf with its `Outline()` and `Describe()` (a `PatchDto` leaf), never a new builder method. An
axis in another unit is `Link(AxisTransform)` on `SecondaryXAxisBuilder` or `YAxisSlotBuilder`. A new public builder instance method takes
at most four parameters (`InstanceMethodBudgetContractTests`).

**Shared behaviour is a C# 14 `extension(T value) { }` block, never a `this`-param static method** (`LangVersion` 14 repo-wide). Two traps
past the call site: a member's reflected `MethodInfo.DeclaringType` is a compiler-synthesized nested `<ContainingClass>+<G>$<hash>`
skeleton type (one `.DeclaringType` up recovers the class), and the generated XML doc mirrors the split (the clean name is a bare
`<inheritdoc>` shim, the real content lives under the skeleton member, and `GetParameters()[0]` is always the receiver). A bare `Foo.Bar(...)`
where `Foo` is a namespace resolves only in a file whose own `namespace` nests directly under `MatPlotLibNet`: true everywhere in `Src/`,
false in `Samples/`, the wiki and docs code-fences, which need the qualified form.

**A technical indicator takes one `OhlcvBars` value, not five parallel arrays** (`Indicators/OhlcvBars.cs`: one named factory per shape an
indicator reads, each validating only the arrays it uses). Every `CandleIndicator<TResult>` subclass and the `AxesBuilder` verb that
forwards to it takes `OhlcvBars bars`; `MatPlotLibNet.DataFrame` adds one call per shape (`df.Hlc(...)` and the other five) and
`df.Values(column)` for one column, and nothing per indicator: `new Atr(df.Hlc("high", "low", "close")).Compute()`.

**A streaming indicator says the same as its batch indicator, and shares its arithmetic.** 57 of the 59 indicators have a form in
`Indicators/Streaming/` (a window the batch indicator is run over, or an internal step struct in `Indicators/Steps/` that the batch folds
over too). `StreamingMap` classifies every calculator, `StreamingEqualsBatchTests` holds each prefix to 1e-9, and `StepArithmeticPinsTests`
pins the step: the pins are not redundant, because the theory runs both through the same step and a step change moves both. A new
indicator turns `StreamingMapTests` red until the map says how it is streamed.

**The native controls share one `InteractionHost`** (`Interaction/InteractionHost.cs`, composed): a control forwards its input and hands
over the figure and layout of every paint; the host builds a controller for a new figure and only replaces the layout for the same one,
which is what lets Home go back to the first view. A control exposes the host's handler as `ChartEventHandler` and its failures as
`ChartEventHandlerFaulted`. The Uno elements cannot be constructed in a unit test; the `windows-heads` job compiles them.

**Colours and colormaps are three classes:** `Colors` (47-name palette), `Css4Colors` (148-name CSS4 table), and the maps on `ColorMaps`
(9 named + `Get(name)`), `DivergingColorMaps`, `SequentialColorMaps`, `QualitativeColorMaps`, `CyclicColorMaps`. A reversed map is
`new ReversedColorMap(map)` or the registry's `"<name>_r"`.

**Satellite packages** each wrap the same `Figure`: Skia (export), AspNetCore (REST + SignalR hub), Blazor, Interactive (browser popup),
GraphQL, Avalonia/Uno/Wpf/Maui (native controls), DataFrame (indicators over `Microsoft.Data.Analysis`), Geo (projections + Natural
Earth), Mcp (a .NET tool MCP server), Verso (a notebook formatter). **`MatPlotLibNet.Notebooks` is frozen** and deliberately out of
`CI.slnf` and both workflows (`ReleaseFiles.Frozen` holds that, with its reason).

**The release contract is executable.** `ReleaseVersionContractTests`, `ReleasePackagingContractTests` and `ReleaseDocumentContractTests`
(`Tst/MatPlotLibNet/Packaging/`, over the one `ReleaseFiles`) read the csprojs, workflows, `docs/`,
runsettings and `run.sh`, and fails when a hand-kept list drifts: every package carries the same version, is built and pushed by some
workflow, is measured by the coverage gate and is linked from the docs; it reaches into SEO/accessibility, the MCP manifest's version and
the version named on the first line of ARCHITECTURE.md, README and SECURITY.md. Adding a package or a series means editing every list it
checks; the test tells you which one you forgot. Every `Src/` package packs on build and is checked for API breaks against
`PackageValidationBaselineVersion` in `Directory.Build.props` (always the release just before the current one;
`ThePackBaseline_IsTheReleaseJustBeforeThisOne` fails if it is not moved with the version). Notebooks and the Mcp tool package opt out. The
SourceLink and `System.Numerics.Tensors` versions are pinned once in the same file (`$(SourceLinkVersion)`,
`$(SystemNumericsTensorsVersion)`), never restated per project: a floating `10.*` resolves only inside the core's own restore and breaks
the Wpf and Uno suites under `-warnaserror` (MSB3277).

**Tests that fail only on the ubuntu runner.** Six process-global statics (`ChartSerializerDefault`, `ChartRendererDefault`,
`SvgRendererDefault`, `FontMetricsDefault`, `GlyphPathProviderDefault`, each a single replaceable `Current` slot, and the registry of file
transforms in `FigureExtensions`, seeded with a refusal for `.png`, `.pdf` and `.gif`) are read by every render test and by every `Save`.
A test that swaps one must join a `*GlobalState` collection (`ChartServicesGlobalStateCollection`, `DisableParallelization = true`) or it
races the render tests under the runner's slower scheduling; and no test may register `.png`, `.pdf`, `.gif`, `.svg` or `.json` over the
registry (register a made-up extension). Fixture data has to be platform-deterministic: no `Math.Sin/Cos/Exp/Log/Pow` in byte-compared
goldens (Windows and glibc differ by one ulp), no `new Uri("/x")` (Linux accepts it, Windows rejects it), `InvariantCulture` for anything
formatted. `MPL_REGEN_GOLDEN=1` makes `ChartSerializerGoldenCorpusTests` write its golden files instead of asserting on them.

## Code graph (roslyn-codelens)

The live source graph is the **`roslyn-codelens`** MCP server in `.mcp.json` (gitignored, so a fresh clone has none; Roslyn over
`MatPlotLibNet.slnx`, compiler-exact). Use `search_symbols`, `get_symbol_context`, `get_type_overview`, `find_callers`, `get_call_graph`,
`find_references`, `analyze_change_impact` before reading files. ⚠ no file-watcher: call `rebuild_solution` after edits. Top god nodes:
`AxesBuilder`, `Axes`, `FigureBuilder`.

The wiki is a separate git repository, cloned beside the repository as `../MatPlotLibNet.wiki` (branch `master`); a documentation change
lands there in the same increment as the repo.

## Documentation samples are compiled, not eyeballed

Every fenced `csharp` block in `docs/`, `articles/`, `README.md` and the per-package READMEs is real code a reader will copy. 94 of them
had drifted away from the API before they were last swept. `WrittenCallsTests` in the wiki suite compiles them, every call a sentence or a
table writes and the code of the XML comments, and holds each matplotlib call beside them to `Tst/MatPlotLibNet.Wiki/Python/signatures.txt`
(written by `tools/mpl_reference/generate_signatures.py`). When you touch a sample, compile it. The method and its trap are recorded in
`.path-manifests/adhoc-20260923-doc-samples-do-not-compile.md`: **csc suppresses diagnostics in phases**, so one parse error anywhere hides
every member error in the whole compilation, and a naive batch compile reads green.
