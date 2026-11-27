# Coverage Policy

MatPlotLibNet enforces **≥90% line coverage AND ≥90% branch coverage on every public class**. The CI build fails if any class drops below its threshold or regresses against the committed baseline.

**Status (v1.30.0, a figure drawn again draws only what changed: every change reports itself, an unchanged subplot is handed out again in SVG and replayed in a native control, every target is drawn by one sequence, and over SignalR a viewer that patches is sent only the parts that changed; and the whole library put in order, one owner for each rule):** **1,287/1,287 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 1287 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.8L / 99.1B**; **33,185 tests, 0 failures** (4 skipped) across the 12 measured suites (the wiki suite, the fidelity suite, the WPF suite, the Uno suite
and the benchmark-program suite run beside the gate and are not measured by it). The gate counts 126 more classes than it did at 1.29.0, among them the change
clock and the stamped list, the copy of a series' collections (`DataSnapshot`), the kept draw of a subplot (`KeptAxesDraw`), the targets a figure is drawn onto
(`FrameTarget`, `KeepingFrameTarget`, `SvgFrameTarget`, `SkiaFrameTarget`), the frame of one draw (`FigureFrame`), the painter of a native control
(`SkiaChartPainter`), the document in its parts (`SvgFigureDocument`), the parts a publisher sends (`ChartSvgParts`), and the owners the refactor gave each
rule: the name tables (`NamedTable`), the sealed shapes that replaced repeated switches (markers, arrow heads, connections, callouts, layouts, scale modes),
the math-text command families (`MathCommand`), the hover targets, the event batch of a server session, the one streaming follower of the native controls,
the base of the interaction modifiers (`InteractionModifier`), the internal interfaces beside the public ones and the one date rule (`OleDateExtensions`).
One class read short on the first run: a check in the mosaic parser that the shared row rule had made unreachable, so the branch was deleted instead of
tested. The Linux run of the same gate, collected in a container and checked on the host, passes with the same total.

**Status (v1.29.0, the last three roadmap rows: shapes in data coordinates, circles and arcs, and a second axis in other units; plus the cell of a subplot kept through JSON):** **1,161/1,161 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 1161 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.8L / 98.9B**; **28,839 tests, 0 failures** (4 skipped) across the 12 measured suites (the wiki suite, the fidelity suite, the WPF suite and the Uno suite
run beside the gate and are not measured by it). The gate counts 41 more classes than it did at 1.28.0, among them the patch family (`Patch`, `FilledPatch`, the seven
shapes and the curve builder they share), the link of an axis (`AxisTransform` and its leaves, the placement of a linked tick, the range a link shows), their JSON
records, and the legend rows of a series and of a patch. The Linux run of the same gate, collected in a container and checked on the host, passes with the same
total, and its per-class figures agree with the Windows merge but for two classes that stay above 90/90 on both. The first strict run read two classes short on
branches. Each held a branch no caller can reach (an empty label a legend row never gets, and a null check on an inset that reading a figure already leaves out),
so the branch was deleted instead of tested. The tests of each class were written before its code, and a mutation of a production line turned them red before the line
was put back.

**Status (v1.28.0, nine roadmap rows: the signal-analysis verbs, stairs, line segments and the triangulation mesh, contour labels along their line, text, a legend and an image on the figure, and the zoom indicator of an inset):** **1,120/1,120 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 1120 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.8L / 98.9B**; **27,290 tests, 0 failures** (4 skipped) across the 12 measured suites (the wiki suite, the fidelity suite, the WPF suite and the Uno suite
run beside the gate and are not measured by it). The gate counts 26 more classes than it did at 1.27.0, among them the windows (`SpectralWindow`), the Welch estimator
(`Welch`), the single-transform spectrum (`Spectrum`), the correlation (`Correlation`), the segment and staircase paths (`SegmentPath`, and the staircase `Stairs` lays out), the contour label
placement (`ContourLabelPlacement`), the legend and image-cell drawing the figure legend and figure image share (`LegendRenderer`, `ImageCells`), and the zoom indicator
(`ZoomIndicator`, `ZoomIndicatorPath`, `DrawnView`). The first strict run passed with no class short and none under its baseline. The tests of each class were written before
its code, and a mutation of a production line (a sign, a doubled bin, a dropped check) turned them red before the line was put back.

**Status (v1.27.0, the last fourteen roadmap rows: new charts, streaming forms for 57 indicators, one interaction host for the native heads, and the Windows heads in CI):** **1,094/1,094 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 1094 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.8L / 98.8B**; **25,667 tests, 0 failures** across the 12 measured suites (the wiki suite, the fidelity suite, the WPF suite and the Uno suite run beside the
gate and are not measured by it: they hold no product code, or need Windows). Two things changed in what the gate sees. The MAUI suite joined the measured suites,
and the Avalonia suite now loads the package itself instead of a copy of its sources linked into the suite's own assembly, which the collector leaves out; so the
classes of both heads are measured for the first time. Where a member can only run on a platform's own thread or event system (Avalonia's input events, dispatcher
posts and render thread, MAUI's main-thread dispatch) it carries `[ExcludeFromCodeCoverage]` with the reason, and what it hands on is held where that logic lives; where
the logic could be pulled out it was (the Avalonia button rules are plain functions of the enum, and both timers raise their tick through a method a test calls).
The first strict run read 54 classes short: 39 below 90/90 and 15 under their baseline. Most were the code of the release that no test reached: the argument guards of
the 46 new streaming forms, the six hub routes of the native controls, the figure-level forms of the new charts, the failure paths of the Blazor client and of the live
chart, and the layout seams of the interaction layer. A test per arm closed them, and four changes that the tests showed to be right went in with them: the interaction
host delivers a brush with the figure the event was raised for, a step-through frame is sized by its figure and not by parsing its own view box, the word cloud packing
skips a box larger than the room left, and a timer keeps one field where it had two. The packing test is bounded by a limit only a blow-up reaches: the same call takes
ten times as long under the collector the CI job runs.

**Status (v1.26.0, marks in data units, decorated lines and fills, the frame of a plot, more layouts, and the comparison with matplotlib in CI):** **948/948 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 948 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.8L / 98.7B**; **24,382 tests, 0 failures** across the 11 measured suites (the wiki suite, the fidelity suite, the WPF suite and the Uno suite run beside the
gate and are not measured by it: they hold no product code, or need Windows). The fifteen roadmap rows of this release added the series and rules behind a waffle
chart, Point and Figure, an icicle chart, Renko and Kagi, a stem-and-leaf plot and a calendar of events (`WaffleSeries`, `PointAndFigureSeries`,
`IcicleSeries`, `PriceStepExtensions`, `LargestRemainderExtensions`, `TreeNodePartitionExtensions`), the one owner of a plot's aspect
(`PlotAspectExtensions`), the one owner of the ids an SVG writes (`SvgIdScope`) and the gradient a fill carries. Its first strict run reported two classes below
90/90 and six that regressed from 100 %: the arms left without a test were the weight written over an arc, the colour a sunburst node names, the marks of a
waterfall's targets and of a step the axes cannot place, a gradient laid under a hatch on a circle, an ellipse and a path, the table of a fill that lies down,
and a record with no data. A test per arm closed them. Three guards were not tested but deleted, because another rule already decided the same thing: the checks
on the spans of a plot's aspect (a ratio that is not positive and finite is left alone by `WithBoxAspect`, which now decides it alone), the finite check on the
side of a waffle's cell, and the empty lower edge of a gradient fill, which no fill can have.

**Status (v1.24.0, a container for the Y axes, the placement of texts, the scales and the maps):** **896/896 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 896 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.7L / 98.7B**; **22,297 tests, 0 failures** across the 11 measured suites (the wiki suite, the WPF suite and the Uno suite run beside the gate and
are not measured by it). The new classes are the container itself (`YAxisContainer`, `YAxisStack`, `YAxisSlot`), the one slot part that draws every
Y axis, and the title-clearance rules both sides now share. The growth in tests covers the stacking of any number of axes per side, the two sides
sharing one title gap, the old members pinned one by one in a single file (the one place that still calls them), the JSON of the version before, kept
byte for byte, and the spec reader's refusal of a null axis. The first strict run reported one class short on branches (`ChartSession`, 100 % to
91.7 %): the guard that keeps one failing event from ending a chart session left two arms without a test, a closed channel and a handler that
finishes later than the call. A test per arm closed it, and the closed-channel one was shown to fail when its arm is flipped.

The placement of texts added the classes that hold the marks a legend and an annotation keep clear of (`MarkObstacles`,
`MarkCollectingContext`), the rule for the rectangle a text occupies (`TextBox`), the layout of the annotations that move
(`CartesianAnnotationsPart.PlaceText`) and the pixels of a bar as drawn. Its first strict run reported two classes below 90/90 and five
that regressed from 100 %: `MarkCollectingContext` had no test of its own (an ellipse, a Bezier step, the clip calls and the
measuring calls were never made), and the marks of the error bar, the kernel density, the regression and the residual plot left
one arm each without a test (an error bar every third point and one with errors along X; a series with nothing to fit). A test
per arm closed them. `CartesianAnnotationsPart` had two arms untested (a text that stays put beside one that moves, and the colour of a leader line) and
lost a property no caller used.

The thirteen roadmap rows of this release added classes of their own, among them the colour bar's scale and its choice of provider (`ColorBarScale`,
`ResolvedColorBarExtensions`), the polar scale (`RadialScale`, `RadialExtent`), the legend's swatch and part (`LegendSwatch`,
`LegendPart`), the contour band fill, the state segment fill, the Sankey link check, and in the Geo package the package data of a map
(`GeoPackageData`, `GeoPackage`), the great circle, the projected region, a flow, a polygon and a position, and the GeoJSON writer. Its first
strict run reported seven classes below 90/90 and four that regressed: the arms left without a test were geometries built from rings
alone, a geometry with no type, a polygon with no rings, the writer for lines, the great circle of two places that coincide or lie
opposite each other, every way the package data of a map can fail to describe one, the numbers, equality and description of a
projection, bubbles of no value, flows given a colour, a cartogram of no region, a polar heatmap whose `RMax` cannot be drawn, a filled contour whose
levels bound no width and a series from outside the hierarchy in the legend. A test per arm closed them, each shown to fail when its line is changed.
Two arms were not behaviour and were deleted instead: the legend repeated the empty-label rule its entry list already holds, and the comparison of two
projections looked up a key the same kind always has. Making a projection from its name was a string `switch`, whose equality jumps no test can take
both ways; it is a table of the 13 kinds now, and each parametric projection knows how to be made again from numbers.

**Status (v1.22.0, series that share one colour):** **817/817 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 817 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.7L / 98.6B**; **19,890 tests, 0 failures** across the 11 measured suites (the wiki suite and the WPF suite run beside the gate and are not
measured by it). The two new classes are `RaincloudParts` and `HorizonParts`, the non-positional record structs the
`Raincloud` and `Horizon` verbs hand their callback. The growth in tests covers the new `ColorGroup`/`ColorSlots`
mechanism (mutation-proof: the old ungrouped colour and the new shared one are proven to diverge once a group is
set), the Kaplan-Meier curve's Greenwood band and censor marks, and both new verbs' own suites plus their
byHand/byVerb and pixel-comparison proofs.

**Status (v1.21.0, every chart type and axes setting whole through JSON):** **815/815 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 815 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.7L / 98.6B**; **19,810 tests, 0 failures** across the 11 measured suites (the wiki suite and the WPF suite run beside the gate and are not
measured by it). The growth in tests comes from the contracts that now guard the work: a reflected test that every drawn property of every series
survives JSON, one that every enum field has a vocabulary row, one that every concrete series type is under the contract, round trips of every
one-call chart through JSON, the MCP reader, the transports and the PNG export. The first strict run reported one class short on branches
(`HistogramSeriesRenderer`, 97.1 % to 96.4 %), because moving the height rule out of the renderer left one arm of the outline width without a test; a
test per arm closed it, shown to fail when the arm is flipped.

**Status (v1.19.0, direct labels, axis colours and the interaction layer in data units):** **729/729 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 729 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.6L / 97.9B**; **12,441 tests, 0 failures** across the 11 measured suites (the wiki suite and the WPF suite run beside the gate and are not
measured by it). The first strict run of the release reported four classes short on branches (`DataTransform`, `ChartLayout`,
`SvgSeriesRenderer`, `ChartSerializer`), all fallbacks the interaction work had added. Each is now pinned by a test that was
shown to fail when its branch is flipped, and two guards no caller could reach were deleted instead.

**Status (v1.16.0, the chart data table + the streaming windows):** **689/689 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 689 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.6L / 97.3B**; **11,497 tests, 0 failures** across the 10 CI suites. The ten classes the data table adds
went in test-first, but the row that made the gate interesting was the existing one: `ToDataTable()` on
63 series types (8 family bases + 55 leaves) is 63 new branches spread thin across classes already at the
threshold, and the first strict run reported **14 below and 16 regressed** — every one of them a fallback arm no test walked (a pie
with fewer labels than slices, a contour whose axes are shorter than its grid, an error bar with no X error).
They are covered by a test each that says what the fallback IS, not by a smoke test that merely reaches the
line. One reported gap was neither: `RelativeRotationSeries` guarded a label index its own constructor makes
impossible, so the guard was deleted rather than tested.

**Status (v1.15.0, the MCP server):** **679/679 classes at ≥90/90**, strict gate **PASSES** with no
regressions (`PASS: All 679 classes meet 90/90 threshold + no regressions`). Total project coverage:
**99.6L / 97.3B**; **11,022 tests, 0 failures** across the 10 CI suites. The twenty classes the
`MatPlotLibNet.Mcp` package adds went in test-first and reached the gate on the increment that introduced them,
which is the only way a new project can: a brand-new class is absent from the baseline, so the regression check
does not apply to it and the absolute 90/90 is its whole gate from the first push. Two of the gaps the first
run reported were not missing tests but code that could not run — a media-type table with three arms where only
PNG travels inline, and an exclusion set that was a constant rather than an input — and were removed rather
than tested. The `MatPlotLibNet.Mcp.Tests` suite joins `coverage.runsettings`, both runners and both workflows;
its absence from the collector's module allowlist would have let the gate pass over an unmeasured assembly.

**Status (v1.14.2, measured 3-D axis-title clearance, 2026-08-17):** **659/659 classes at ≥90/90**,
strict gate **PASSES** with no regressions. **10,714 tests, 0 failures** across the 9 CI suites of that release, plus
152 matplotlib pixel-fidelity tests. The two new branches in the 3-D axes renderer's title clearance (a custom
tick `LabelSize` and a custom `TickFormatter`) are covered by a behavioural test — the title must
still clear the band when either widens it — not by a coverage-shaped smoke test.

**Status (v1.14.1, camera-derived 3-D faces, 2026-08-15):** **659/659 classes at ≥90/90**, strict gate
**PASSES** with no regressions (`pwsh tools/coverage/run.ps1 -Check -Strict` → `PASS: All 659 classes
meet 90/90 threshold + no regressions`). Total project coverage: **99.6L / 97.2B**; **10,709 tests, 0
failures** across all 9 CI suites, plus 152 matplotlib pixel-fidelity tests. The three classes added
this release (`CubeFaceSelection`, `CubePlane`, `AxisEdge3D` — plus the internal `PinnedAxes`) are
covered by a 144-row matplotlib ground-truth theory, so every branch of the face-selection rule
including its edge-on tie handling is exercised by a camera matplotlib itself was asked about.

**Status (v1.14.0, control-room pack, 2026-07-12):** **656/656 classes at ≥90/90**, strict gate
**PASSES** with no regressions. Total project coverage: **99.6L / 97.2B**; **10,252 tests, 0
failures** across all 9 suites (figures per the release commit message, `94f8a56`). The committed
[`tools/coverage/baseline.cobertura.xml`](https://github.com/xkqg/MatPlotLibNet/blob/main/tools/coverage/baseline.cobertura.xml) snapshot itself
was last regenerated at `ce38dd4` (pre-1.13.1) — the two releases since added fully-covered code
without moving the rounded aggregate, but re-run `pwsh tools/coverage/run.ps1 -Check -Strict` for a
current per-class breakdown before relying on exact per-class numbers.

**Status (v1.13.0, FromSeriesDto attribution restore, 2026-07-04):** **651 classes, all at ≥90/90.** Strict gate **PASSES** — `check-thresholds.ps1 -Strict` reports 0 classes below threshold. Total project coverage: **99.6L / 97.2B** (21 055/21 149 lines, 9 751/10 033 branches). The v1.13.0 refactor moved per-series JSON deserialization out of the monolithic `ChartSerializer`/`SeriesRegistry` factory methods into a `FromSeriesDto(Axes, SeriesDto)` static on each series class — the optional-field `if (dto.X.HasValue)` / `dto.X ?? []` arms moved with it, and branches that were diluted inside `ChartSerializer`'s large denominator now count against each small receiving series class individually, dropping 31 classes below the 90/90 gate with zero behavior change. Restored via per-series "no data" raw-JSON tests (the `?? []` false arm), a `ColorMap`/`ShowWireframe` raw-JSON arm for `Contour3DSeries`/`Trisurf3DSeries` (their `ToSeriesDto` never emits `ColorMapName`, so only hand-crafted JSON — not a JSON round-trip — reaches that arm), a real 3D-render test for `Quiver3DSeries`'s `I3DPointSeries` dispatch, and direct edge-case unit tests for `Histogram2DSeries`/`SignalSeries`/`StackedAreaSeries`. Three branches (`Histogram2DSeries` bx/by negative-index clamp ×2, `WaterfallSeries`'s `0 < yMin` guard) are mathematically unreachable given their surrounding guards and were left uncovered — each class still clears 90/90 on the remaining branches.

**Status (v1.9.0, indicator-expansion release complete, 2026-04-23):** **622 classes, all at ≥90/90.** Strict gate **PASSES on CI** — `--strict` flag is active in `.github/workflows/ci.yml`. Total project coverage: **98.7L / 95.7B**. v1.9.0 adds 12 indicators (Tier 3a Volume / Tier 3b Trend+Transform / Tier 3c Advanced+Cross-asset) bringing the total to 52; every new class ships with its own ≥90/90 test file and the `AxesBuilder` shortcuts have paired tests. No framework refactors this release — rendering paths are unchanged, SVG output byte-identical to v1.8.0.

## Where a test that reaches a branch goes

A test is written for a type or for a behaviour, never for the round of work that found a missed branch. A branch the class's own
tests did not reach is reached by a test in those tests: one test class per library type, named `<Type>Tests`, in a file of the same
name, and a behaviour that spans types in a class named for the behaviour. Three guards hold it:

| Guard | Refuses |
|---|---|
| `TestClassContractTests` (`Tst/MatPlotLibNet/Packaging/`) | a second test class for one library type (`<Type>EdgeTests`, `<Type>UnitTests`, `<Type>IntegrationTests` beside `<Type>Tests`), and a file that holds a class of another name |
| `TestCodeRules` (`Tst/MatPlotLibNet/Packaging/OwnedRules/`) | a class named for a phase of work (`*CoverageTests`, `*BranchTests`, `*EdgeCaseTests`, `*NearMissTests`, `Phase*`, `V170*`, `*TestsV122`), a test whose only check is that some SVG was drawn (`Assert.Contains("<svg", svg)`, `Assert.NotNull(figure.ToSvg())`), a drawn-token row whose only token is the svg element, and a comment of a test that names the line of a library file (`line 45`, `L45`, `File.cs:45`; a matplotlib citation with its `.py` file on the same line is allowed) |
| `CodeCloneContractTests` | a block of eight lines of code that stands in two files of `Src` or `Tst` (outside a declared family), or twice in one file of the library or of its tests (budget 0), or three facts of one shape in a file, or two facts of one shape in a folder, in one file or in several, whose names end alike (the same last part) or start alike (the same parts before the last) (a difference in a literal or in any name, such as a member, a type, a local or an argument, is a row) |

A test that pins a figure names the token the case draws, or compares the picture with one it must equal. A test double or a
fixture with no logic of its own has no test file: the tests that use it are its tests.

## Why 90/90 (not 80/80)

Industry consensus is "80% is good enough"; we don't accept that. Branch coverage of 80% means 1 in 5 conditional paths is never tested — exactly where silent-failure bugs hide. v1.7.0 shipped four such bugs (geo extensions, broken-axis, symlog, playground grid) under a 3 967-test suite that was 85% line / 68% branch. The threshold lives in [`tools/coverage/thresholds.json`](https://github.com/xkqg/MatPlotLibNet/blob/main/tools/coverage/thresholds.json).

Branch coverage continues to surface real bugs through the uplift wave — two found in the v1.7.2 work by edge-case Theories. The empty-input crash of the wiggle baselines (`ComputeWiggle` / `ComputeWeightedWiggle`) is fixed: a wiggle baseline of no layers is no baselines. `SymLogNormalizer.Normalize(NaN, min, max)` still throws, by contract: a normaliser is never handed a missing value, because a colormap and every renderer leave it out first, and `NormalizerTests` says so.

## Run coverage locally

```bash
# Full report (HTML auto-opens in browser)
pwsh tools/coverage/run.ps1 -Report

# Just the gate check (exits non-zero on any failure)
pwsh tools/coverage/run.ps1 -Check

# Update the baseline after legitimately reducing scope (rare)
pwsh tools/coverage/run.ps1 -SetBaseline
```

Linux/macOS: replace `pwsh tools/coverage/run.ps1` with `bash tools/coverage/run.sh` (same flags but without the `-` prefix: `--report`, `--check`, `--baseline`).

## What the gate checks

The script in [`tools/coverage/check-thresholds.ps1`](https://github.com/xkqg/MatPlotLibNet/blob/main/tools/coverage/check-thresholds.ps1) reads the merged Cobertura XML produced by `dotnet-coverage` + `dotnet-reportgenerator-globaltool` (both installed as global .NET tools). `dotnet-coverage` is Microsoft's official cross-platform coverage collector for the Microsoft Testing Platform that xUnit v3 runs on; it replaced `coverlet.console` in v1.7.2 after the latter's MTP attach path silently captured zero coverage on Ubuntu CI runners.

**Two modes:**

- **Default (CI mode)** — fails ONLY on **regression vs baseline**. The 90/90 absolute target is informational. This keeps CI green from day 1 while we lift coverage incrementally through the multi-phase plan. A class that was at 95% may not drop to 91% even though 91% > 90% — that signals a deleted or weakened test.
- **`-Strict` mode** — adds the absolute 90/90 line/branch check. Used locally to track uplift progress, and intended to become the CI default once the codebase reaches the 90/90 baseline globally.

The minimum lives in `thresholds.json`: 90/90 for every class, and its `exemptions` list is empty by policy.

## A class that cannot be tested

A class that is legitimately untestable (an interface, a generic abstract base, the code at a browser or process boundary) carries
`[ExcludeFromCodeCoverage]` at its declaration, with the reason in its documentation. It then drops out of the Cobertura report, so the
gate never reads it, and the reason stands beside the code it is about. No class is given a lower floor in `thresholds.json`.

## When a CI build fails on coverage

The job output names exactly which classes regressed or fell below threshold. To reproduce locally:

```bash
pwsh tools/coverage/run.ps1 -Check
```

Then either:

1. **Add tests** to lift the failing class above its threshold (preferred).
2. **Restore the deleted/weakened test** that caused the regression.
3. **Mark the class `[ExcludeFromCodeCoverage]`** with its reason, when it cannot be tested at all (last resort).

## How baselines work

`tools/coverage/baseline.cobertura.xml` is a snapshot of "what coverage looks like right now". It's committed to git so every PR is checked against it. After merging a PR that legitimately raises coverage, the baseline is regenerated automatically by the CI release flow (or manually via `-SetBaseline`).

Per-class baselines protect against subtle quality erosion — e.g., someone deletes a test, the class drops from 98% to 91%, the threshold is still met (90%), but quality silently regressed. Baseline gating catches that.

## Test types we run

| Type | Where | What it asserts |
|---|---|---|
| Unit tests | `Tst/MatPlotLibNet/`, `Tst/MatPlotLibNet.Geo/`, `Tst/MatPlotLibNet.Skia/` | Single method/class behavior, including all edge cases (NaN, ±∞, empty, single-point) |
| Theory tests | `[Theory] [InlineData]` / `[MemberData]` | Parametrised over input matrices to exercise every branch |
| Visual regression tests | `Tst/MatPlotLibNet/Rendering/` | Extract SVG geometry, assert points fall in canvas, ticks don't overlap |
| Numpy/matplotlib parity | `SymlogTransformTests`, `LogTickFormatterTests`, `NormalizerTests` | Math formulas verified against pre-computed numpy/matplotlib values from `TestFixtures/NumpyReference.cs` |
| Drawn-property contracts | `Tst/MatPlotLibNet/Serialization/` (`SeriesDrawnPropertiesRoundTripTests`, `AxesDrawnPropertiesRoundTripTests`), linked into `Tst/MatPlotLibNet.Mcp/` | Every settable property of every series type and every part of an axes, found by reflection, comes back from a JSON round trip with the value it was given; a property added without its DTO field turns a Theory red and names itself |
| Byte pins | `Tst/MatPlotLibNet/Serialization/` (`ChartSerializerGoldenCorpusTests`, `GivenSeriesCorpusTests`, `GivenAxesCorpusTests`, `ThemeCorpusTests`; files under `Golden/`) | The JSON a series at rest, a series with every property given, the axes with every property given and every shipped theme write, byte for byte; a refactor that must not move the wire passes them without a file regenerated |
| Resting values | `Tst/MatPlotLibNet/Serialization/` (`RestingValueCorpusTests` over the roster of `RestingSubjects`, files under `Golden/Resting/`; `RestingValueContractTests`) | What every series type, every part of an axes or a figure and every settings record a part is made of starts from, setting by setting, and that each setting with a `Resting` constant starts from it; a test of one start, of one stored setting or of one series' trip through JSON adds nothing, and rows of `TestCodeRules` refuse its name and the shape of a fact that only gives a series a setting and reads it back |
| One owner per rule | `Tst/MatPlotLibNet/Packaging/` (`OneOwnerSourceContractTests` over the rows of `OwnedRules/`, one file per area, the calculations in `NumericsRules`: an angle in the other unit, the spread of a run, a range of no width, a distribution's domain, a power of two, a moving average, a linear system; `OpacityContractTests` in `Models/`, which finds every opacity setting by reflection and refuses a setting that takes a number outside 0 to 1) | A rule worked out in one place is spelled in that place only: a pattern per rule, the files that may match it and the lines it must find and must pass, read over every source file, and over every test file for the rules of the tests (the repository root is found by `RepositoryRoot` only); a name the library deleted (`DeletedName`, `DeletedNameTests`) is named in no source or test line or comment, no document of the repository and no wiki page |
| Composed charts | `Tst/MatPlotLibNet/Rendering/` (`ComposedChartShapes`, `ComposedChartContractTests`), linked into `Tst/MatPlotLibNet.Mcp/` | Every chart verb's figure is written as JSON, read back to the same picture, wears one colour for each drawn unit and lists the legend entries it should, and a missing value is never written into the picture; each verb's SVG equals the SVG of the public calls it replaces, a call it refuses names its parameter and leaves the axes as they were, and the limits are the caller's |
| Verbs with a callback | `Tst/MatPlotLibNet/TestFixtures/VerbCatalogue.cs`, `Tst/MatPlotLibNet/Builders/VerbContractTests.cs` | Every verb that takes a callback, at every layer, is one row: the callback runs once on each unit the verb added, after the verb, so what it sets wins; no callback draws what an empty one does, or is refused by name; the verb returns its builder; a figure verb draws what the axes verb of its name draws on the one subplot |
| Document claims | `Tst/MatPlotLibNet/Packaging/` (`ReleaseVersionContractTests`, `ReleasePackagingContractTests`, `ReleaseDocumentContractTests`, `VerbPlacementContractTests`), `Tst/MatPlotLibNet.Geo/` | The numbers and lists the documents state (the colormap count, the version a document names, the verbs and where they live, the map projections) are held against the assembly and the project files |
| Fidelity tests | `Tst/MatPlotLibNet.Fidelity/` | Render PNG, compare to matplotlib reference via `PerceptualDiff` (RMS / SSIM / ΔE read from every pixel). Run by CI and by the publish job; no coverage is measured for it, since it holds no product code |
| Performance benchmarks | `Tst/MatPlotLibNet/Benchmarks/` | Throughput numbers (not gated, informational) |

## Edge case checklist (enforced via test naming)

Every new test file must include test methods named for these cases (where applicable):

- `*_EmptyInput_*` — zero-length array
- `*_SinglePoint_*` — one element
- `*_NaN_*` — NaN is the missing value: left out of a range and a fit, a gap in a line, and never written into the picture as a coordinate (the JSON carries it as the string "NaN")
- `*_Infinity_*` — ±∞ doesn't overflow
- `*_BoundaryValue_*` — values at threshold boundaries (e.g., `x == linthresh` for symlog)
- `*_VeryLarge_*` — 100K+ elements (SIMD batch path)

## Reusable test fixtures

Common edge-case data lives in [`Tst/MatPlotLibNet/TestFixtures/`](https://github.com/xkqg/MatPlotLibNet/tree/main/Tst/MatPlotLibNet/TestFixtures):

- `EdgeCaseData.cs` — `Empty`, `SinglePoint`, `AllNaN`, `MixedNaN`, `BoundaryDoubles`, `Ramp(n)`, `Sin(n)`, `Large(n)`, `Descending(n)`, `AllEqual(n)`
- `Rendering/SvgMarkupExtensions.cs` — the one reader of a drawn SVG: `svg.Elements(name)`, `svg.SeriesGroup(index)`, `svg.PlotArea()`, `svg.Scripts()`, `svg.LegendEntries()`
- `NumpyReference.cs` — pre-computed reference values for SymLog, Log10, Robinson projection, etc.
- `Tst/MatPlotLibNet/Indicators/Streaming/StreamingTestData.cs` — synthetic OHLC fixtures (`RisingBars`, `FlatBars`, `ZigZagBars`) for streaming-indicator tests.

Use these instead of inlining your own — keeps "what's an edge case" consistent across the suite.

## Cross-cutting Theory pattern (DRY)

For families of types that share a contract, **extend the central Theory** instead of cloning per-type tests. The canonical example is [`Tst/MatPlotLibNet/Models/Series/AllSeriesTests.cs`](https://github.com/xkqg/MatPlotLibNet/blob/main/Tst/MatPlotLibNet/Models/Series/AllSeriesTests.cs), which exercises **every** `ISeries` the library ships, read from the suite's one roster: its rows are `DrawnSeriesFixtures.TypeNames`, each case takes the kind at rest from `DrawnSeriesFixtures.Instance`, and `SeriesDrawnPropertiesRoundTripTests` fails when a shipped type is missing from the roster. The invariants:

- `Label` defaults to `null`, can be set/read
- `Visible` defaults to `true`, can be set to `false`
- `ZOrder` has the correct default (`AreaSeries` → −1; everything else → 0), can be set
- `Accept(visitor, area)` dispatches to the correct visitor method
- `IHasColor.Color` defaults to `null` (interface-filtered)
- `IColormappable.ColorMap` defaults to `null` (interface-filtered)
- `IHasMarkerStyle.MarkerStyle` defaults to `Circle` (interface-filtered)
- `IHasAlpha.Alpha` falls within `[0, 1]` (interface-filtered)
- `IHasEdgeColor.EdgeColor` defaults to `null` (interface-filtered)
- the data a base holds (`XYSeries`, `DatasetSeries`, `HierarchicalSeries`, `OhlcSeries` and the other data bases) is what the kind's own constructor was given, one case per base
- `ToSeriesDto()` is written under a type tag the registry reads (the tag's spelling is the golden corpus')
- `ComputeDataRange(...)` produces finite numbers under axes with and without limits
- Rendering to SVG neither crashes nor writes a coordinate that is not finite, and every optional setting given a value still renders

Adding a new series type requires **one row** in `DrawnSeriesFixtures` (`DrawnTypes` and its `Build` row, or a name in `NotUnderContract` with a reason) plus the corresponding `Visit` overload in `TestSeriesVisitor.cs` — and that single addition runs the conformance tests above and holds each of its settable properties to the JSON round trip. Its discriminator needs a row in `SampleSeries.AddSampleSeries`, the data the golden corpus pins for every registered kind (and puts on every secondary Y axis seat); its verb, when it takes a callback, needs a row in `VerbCatalogue` (`VerbContractTests` names the overload until it has one); and `SeriesCountContractTests` stays red until its published count and every document that states the count are bumped. A chart verb adds no series type, so it adds none of these: it adds one shape to `ComposedChartShapes`, which `ComposedChartContractTests` runs through the promises for a composed chart and `VerbContractTests` through the promises of a verb with a callback. The renderer-side equivalent is [`Tst/MatPlotLibNet/Rendering/SeriesRenderers/SeriesRenderersTests.cs`](https://github.com/xkqg/MatPlotLibNet/blob/main/Tst/MatPlotLibNet/Rendering/SeriesRenderers/SeriesRenderersTests.cs) — direct-invocation Theory over every `SeriesRenderer<T>` to cover renderer code paths that the visitor dispatch hides from the static call graph.

Indicators follow the same pattern. Every calculator of the package is one case of `IndicatorCases` (`Tst/MatPlotLibNet/Indicators/IndicatorCases.cs`): how it is made over a walk of bars, what it draws and under which labels, the range it pins its pane to, the bar of its first value and the fewest bars it needs, what it says over bars that never move, its name when made with its defaults, and the arguments it refuses with the name each refusal gives, its streaming form beside it. `IndicatorContractTests` holds every case to all of them, one case an indicator, each listing every rule it breaks, and its guard fails when a calculator has no case or the streaming map holds another list. Adding an indicator is one case there (and a line of the streaming map); its own test file holds only its arithmetic against reference values.

Phase-9 dedup (v1.7.2) removed 78 per-series `[Fact]` duplicates of these Theory tests across 55 files — net delta 5 569 → 5 468 tests, zero coverage regression.
