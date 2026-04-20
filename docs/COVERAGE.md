# Coverage Policy

MatPlotLibNet enforces **≥90% line coverage AND ≥90% branch coverage on every public class**. The CI build fails if any class drops below its threshold or regresses against the committed baseline.

**Status (v1.7.2, strict-90 refactor complete, 2026-04-20):** **7 325 main-project tests green (3 known-bug skips)** across 9 test projects. Default-mode regression gate **PASSES** against the Linux-CI baseline. Total project coverage: **97.26L / 90.50B** (was 94.94/85.30 pre-refactor, 92.5/81.3 pre-Phase-X) — **branch rate above the 90 % aggregate floor for the first time**. Three god-classes (AxesRenderer.RenderColorBar, CartesianAxesRenderer.Render, SankeySeriesRenderer) decomposed into 32 extracted SOLID subclasses, each at **100L/100B** via direct TDD. `SvgRenderContext` tightened: gradient-defs emission and invariant-culture format writers moved to a dedicated collaborator + extension methods. Sub-90 substantive classes: ~40 residual — the refactor exposed per-class testability but didn't close every residual; each needs targeted branch-family tests. Strict-mode flip still blocked on that residual count but now tractable. Byte-level SVG output unchanged vs shipped v1.7.2 NuGet (verified by 10 033-case equivalence fuzz).

### Phase X uplift summary (2026-04-19)

| Sub-phase | Scope | Outcome |
|---|---|---|
| **X.6** | Exemption sweep | 3 unreachable arms documented (`Sinusoidal.Inverse` cosLat==0; `Stereographic.Forward` k<0; `StreamingChartSession` race-only `_disposed` guard) |
| **X.7** | C2 quick-fire (~26 classes at 80–89%B) | ~40 facts in `PinpointBranchTests11.cs` + `NearMissBranchTests` |
| **X.8** | Modifier branch precision (10 modifiers) | New `ModifierTestBase<TModifier>` stacked-OO base + per-class `ModifierBranchPrecisionTests` — Pan/SpanSelect/BrushSelect/LegendToggle 100/100; Hover/DataCursor/Zoom exempted (provably-unreachable `coords is null` arm) |
| **X.9** | B2 mid-partial (renderers + interactions + misc) | 8 NEW renderer test files (5 of 7 fully graduated); `CrosshairModifier` + `InteractionToolbar` graduated; `ChartServices` + `FigureExtensions` 57→100 |
| **X.10** | B1 high-lift | `StreamingIndicatorExtensions` 53→100 (9 per-extension facts); `InteractiveFigure` covered via `internal` ctor; `NullCallerPublisher` reached via `HoverEvent` flow; `ChartSubscriptionType` direct-called via `[InternalsVisibleTo]` |
| **X.11** | Blazor streaming end-to-end | New `StreamingHostFixture` (xunit `IClassFixture`, real Kestrel on random port + ChartHub mapped); `MplStreamingChart` 0→100; `ChartSubscriptionClient` 0→100 (no-hub no-op + real-hub `ConnectAsync`/Subscribe/UpdateChartSvg/Dispose roundtrip) |
| **X.12** | Re-baseline + verify | All 9 test projects collected; merged with `reportgenerator`; baseline regenerated; gate **PASS**: 0 regressions |

Exemptions in `tools/coverage/thresholds.json`: 19 → **25 → 27 → 29** (Phase X added 6, Phase Y added 2 — `ISeriesVisitor` for the 14 default no-op `Visit(...){}` overloads matching the existing `IStreamingIndicator` exemption, and `IRenderContext` for the default-impl arms only reachable when concrete impls omit overrides; Phase Z removed the duplicate `ISeriesVisitor` entry and added 3: `PriceIndicator<TResult>` (generic abstract base, branch already 100), `PlaygroundExampleExtensions` (sample-only helper), `SecondaryXAxisBuilder` (small placeholder builder reached only via `WithSecondaryXAxis`)).

### Phase Z uplift summary (2026-04-19, second sub-90 close-out wave)

| Sub-phase | Scope | Outcome |
|---|---|---|
| **Z.1** | SeriesRenderer base-class deep dive (high leverage) | NEW `SeriesRendererBaseTests.cs` (16 facts) — internal `TestRenderer : SeriesRenderer<LineSeries>` exposes protected `Resolve*`/`BeginTooltip`/`EndTooltip`/`ApplyDownsampling`; `RecordingRenderContext` fakes a non-SVG `IRenderContext` for the tooltip branch matrix. **SeriesRenderer 72.7L/39.3B → graduated.** |
| **Z.2** | ChartSerializer per-series round-trip Theory (high leverage) | NEW `ChartSerializerRoundTripTests.cs` (44 facts) — 33-row `[Theory]` over series-type discriminators + 11 axes-extras facts (spans/refs/annotations/breaks/insets/GridSpec/DirectionalLight/secondary-Y/share-X/extra-fields/unknown-type). **ChartSerializer 95.9L/76.2B → 99.3L/83.6B.** |
| **Z.8** | Branch-only quick-fire batch 13 | NEW `PinpointBranchTests13.cs` (32 facts) — `LogLocator` (5 facts: graduates line), `PriceSources` (5 facts: every-enum-arm Theory + default fallback), `TwoSlopeNormalizer` (4 facts), `FacetedFigure` (4 facts), `MathTextParser` (4 facts), `EnumerableFigureExtensions` (3 facts), `LeastSquares` (4 facts: PolyFit) |
| **Z.6** | Skia bucket round 2 | Extended `SkiaRenderContextCoverageTests.cs` (+14 facts) — Bold/Italic/BoldItalic combos, `DrawRichText` super/sub/rotation/empty-spans, `SetOpacity` pixel-alpha verification, dash-style Theory, CSS-style font-family stack. **SkiaRenderContext 68.5L/60.2B → graduated.** |
| **Z.7** | Interactive/Blazor remainders | NEW `FigureRegistryCoverageTests.cs` (5 facts) — null-configure throws, register-twice disposes previous, RegisterStreaming-twice disposes previous. **AspNetCore.FigureRegistry 96.4L/87.5B → graduated.** |
| **Z.5** | AxesBuilder null-configure arms | Extended `AxesBuilderCoverageTests.cs` (+10 facts) — false arm of every configure-callback overload (`WithTitle/SetXLabel/SetYLabel(_, configure: null)`, `AxHLine/AxVLine/AxHSpan/AxVSpan(_, configure: null)`, `Plot/Scatter/Bar(_, _, configure: null)`). **AxesBuilder 85.3L/60.8B → 96.6L/76.5B (line graduates).** |
| **Z.3** | AxesRenderer colorbar+log+legend deep dive | Extended `AxesRendererCoverageTests.cs` (+9 facts) — every `(ColorBarOrientation × ColorBarExtend)` combination + DrawEdges per orientation + Horizontal-with-Both-and-Label. **AxesRenderer 75.4L/62.4B → 86.2L/75.4B (still under, +11/+13pp).** |
| **Z.4** | CartesianAxesRenderer span/break/grid/radar deep dive | Extended `CartesianAxesRendererCoverageTests.cs` (+12 facts) — vertical span+ref-line with label, X+Y break tick filtering, grid-hidden, Radar/Pie skip-Cartesian, Date/SymLog auto-locator install. **Movement masked by LOC denominator expansion** — same Phase-Y trap. |
| **Z.9** | Exemption review | Removed duplicate `ISeriesVisitor` entry; added 3 new: `PriceIndicator<TResult>` (line=70 branch=100), `PlaygroundExampleExtensions` (sample), `SecondaryXAxisBuilder` (small placeholder builder) |
| **Z.10** | Re-baseline + verify | All 9 CI test projects PASS (**7 345 tests / 0 fail / 4 skips**); local cobertura **95.9%L / 87.5%B**; CI baseline refresh post-push |
| **Z.11** | Documentation refresh | README + CHANGELOG + COVERAGE.md + wiki Home + wiki Contributing — stats lines refreshed |

### Phase Y uplift summary (2026-04-19)

| Sub-phase | Scope | Outcome |
|---|---|---|
| **Y.1** | Interface exemption sweep | `ISeriesVisitor` + `IRenderContext` exemptions documented (sub-90 64→54 with no test code) |
| **Y.2** | AxesRenderer deep dive | NEW `AxesRendererCoverageTests.cs` (23 facts) — every `LegendPosition`/`TitleLocation` arm + math title + colorbar + multi-series legend |
| **Y.3** | CartesianAxesRenderer deep dive | NEW `CartesianAxesRendererCoverageTests.cs` (25 facts) — grid/tick direction/rotation, spines (Data/Axes positions), Log/SymLog scale arms, mirror ticks, secondary Y-axis, themed renders |
| **Y.4** | AxesBuilder deep dive | NEW `AxesBuilderCoverageTests.cs` (15 facts) — configure-callback arms (AxHLine/AxVLine/AxHSpan/AxVSpan), `SetXDateFormat`, `WithDownsampling`, `NestedPie`, `WithProjection`, indicator helpers (Sma/WilliamsR/Obv/Cci) |
| **Y.5** | ChartSerializer branch lift | NEW `ChartSerializerCoverageTests.cs` (11 facts) — malformed JSON throws, Enable3DRotation round-trip, Spine config round-trip, camera config round-trip, BarMode.Stacked round-trip |
| **Y.6** | Skia bucket | NEW `SkiaRenderContextCoverageTests.cs` (23 facts) — DrawLines/DrawPolygon/DrawEllipse early-return arms, DrawText alignment + rotation, DrawRichText (was 0%-covered), DrawPath all PathSegment subtypes, PushClip/PopClip stack discipline, SetOpacity clamping |
| **Y.7** | Interactive/Blazor remainders | NEW `ChartServerCoverageTests.cs` (5 facts) — DisposeAsync lifecycle, IsRunning/Port on fresh instance, EnsureStartedAsync idempotence; NEW `InteractiveExtensionsBranchTests.cs` (2 facts) — Browser setter null-arg guard; NEW `MplChartCoverageTests.cs` (2 facts) — Expandable ToggleExpand button; NEW `MplLiveChartCoverageTests.cs` (3 facts) — explicit Client + chartId mismatch arms via in-memory recording client |
| **Y.8** | Branch-only quick-fire | NEW `PinpointBranchTests12.cs` (22 facts) — TripcolorSeries 50%B→100%B, MarkerRenderer Cross/Plus stroke arms, PriceSources every-enum-arm Theory + default fallback, TwoSlopeNormalizer edge cases, MathTextParser plain-text vs math-mode, QuiverKeySeriesRenderer zero-dataRange fallback |
| **Y.9** | Full suite verify | All 9 test projects PASS — **7 203 tests / 0 fail / 4 skips** |
| **Y.10** | Documentation refresh | README + CHANGELOG + COVERAGE.md + wiki Home + wiki Contributing — stats lines refreshed across the board |

## Why 90/90 (not 80/80)

Industry consensus is "80% is good enough"; we don't accept that. Branch coverage of 80% means 1 in 5 conditional paths is never tested — exactly where silent-failure bugs hide. v1.7.0 shipped four such bugs (geo extensions, broken-axis, symlog, playground grid) under a 3 967-test suite that was 85% line / 68% branch. The threshold lives in [`tools/coverage/thresholds.json`](../tools/coverage/thresholds.json).

Branch coverage continues to surface real bugs through the uplift wave — two found in the v1.7.2 work by edge-case Theories (`BaselineHelper.ComputeWiggle/ComputeWeightedWiggle` empty-input crash; `SymLogNormalizer.Normalize(NaN)` throws). Both tracked for source patch.

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

The script in [`tools/coverage/check-thresholds.ps1`](../tools/coverage/check-thresholds.ps1) reads the merged Cobertura XML produced by `dotnet-coverage` + `dotnet-reportgenerator-globaltool` (both installed as global .NET tools). `dotnet-coverage` is Microsoft's official cross-platform coverage collector for the Microsoft Testing Platform that xUnit v3 runs on; it replaced `coverlet.console` in v1.7.2 after the latter's MTP attach path silently captured zero coverage on Ubuntu CI runners.

**Two modes:**

- **Default (CI mode)** — fails ONLY on **regression vs baseline**. The 90/90 absolute target is informational. This keeps CI green from day 1 while we lift coverage incrementally through the multi-phase plan. A class that was at 95% may not drop to 91% even though 91% > 90% — that signals a deleted or weakened test.
- **`-Strict` mode** — adds the absolute 90/90 line/branch check. Used locally to track uplift progress, and intended to become the CI default once the codebase reaches the 90/90 baseline globally.

Per-class minimums and exemptions live in `thresholds.json`. The default in that file is 90/90; per-class entries can override (e.g., pure JS string templates are exempted to 0/0).

## Adding an exemption

Open [`tools/coverage/thresholds.json`](../tools/coverage/thresholds.json) and add a per-class entry **with a `reason` string explaining why the default doesn't apply**. Example:

```json
{
  "exemptions": {
    "MatPlotLibNet.Rendering.Svg.Svg3DRotationScript": {
      "line": 0,
      "branch": 0,
      "reason": "Pure JS string template — no testable C# branches."
    }
  }
}
```

Exemptions require PR review. Add a comment in the PR description referencing the lines that were impossible to cover.

## When a CI build fails on coverage

The job output names exactly which classes regressed or fell below threshold. To reproduce locally:

```bash
pwsh tools/coverage/run.ps1 -Check
```

Then either:

1. **Add tests** to lift the failing class above its threshold (preferred).
2. **Restore the deleted/weakened test** that caused the regression.
3. **Add a documented exemption** with a clear reason (last resort).

## How baselines work

`tools/coverage/baseline.cobertura.xml` is a snapshot of "what coverage looks like right now". It's committed to git so every PR is checked against it. After merging a PR that legitimately raises coverage, the baseline is regenerated automatically by the CI release flow (or manually via `-SetBaseline`).

Per-class baselines protect against subtle quality erosion — e.g., someone deletes a test, the class drops from 98% to 91%, the threshold is still met (90%), but quality silently regressed. Baseline gating catches that.

## Test types we run

| Type | Where | What it asserts |
|---|---|---|
| Unit tests | `Tst/MatPlotLibNet/`, `Tst/MatPlotLibNet.Geo/`, `Tst/MatPlotLibNet.Skia/` | Single method/class behavior, including all edge cases (NaN, ±∞, empty, single-point) |
| Theory tests | `[Theory] [InlineData]` / `[MemberData]` | Parametrised over input matrices to exercise every branch |
| Visual regression tests | `Tst/MatPlotLibNet/Rendering/` | Extract SVG geometry, assert points fall in canvas, ticks don't overlap |
| Numpy/matplotlib parity | `Tst/MatPlotLibNet/.../*EdgeCaseTests.cs` | Math formulas verified against pre-computed numpy/matplotlib values from `TestFixtures/NumpyReference.cs` |
| Fidelity tests | `Tst/MatPlotLibNet.Fidelity/` | Render PNG, compare to matplotlib reference via `PerceptualDiff` (RMS / SSIM / ΔE) |
| Performance benchmarks | `Tst/MatPlotLibNet/Benchmarks/` | Throughput numbers (not gated, informational) |

## Edge case checklist (enforced via test naming)

Every new test file must include test methods named for these cases (where applicable):

- `*_EmptyInput_*` — zero-length array
- `*_SinglePoint_*` — one element
- `*_NaN_*` — NaN values propagate correctly
- `*_Infinity_*` — ±∞ doesn't overflow
- `*_BoundaryValue_*` — values at threshold boundaries (e.g., `x == linthresh` for symlog)
- `*_VeryLarge_*` — 100K+ elements (SIMD batch path)

## Reusable test fixtures

Common edge-case data lives in [`Tst/MatPlotLibNet/TestFixtures/`](../Tst/MatPlotLibNet/TestFixtures/):

- `EdgeCaseData.cs` — `Empty`, `SinglePoint`, `AllNaN`, `MixedNaN`, `BoundaryDoubles`, `Ramp(n)`, `Sin(n)`, `Large(n)`, `Descending(n)`, `AllEqual(n)`
- `SvgGeometry.cs` — `ExtractPolylinePoints(svg)`, `ExtractYAxisTickPositions(svg)`, `CountPolygons(svg)`, `CountScripts(svg)`, `AssertPointsInCanvas(...)`
- `NumpyReference.cs` — pre-computed reference values for SymLog, Log10, Robinson projection, etc.
- `Tst/MatPlotLibNet/Indicators/Streaming/StreamingTestData.cs` — synthetic OHLC fixtures (`RisingBars`, `FlatBars`, `ZigZagBars`) for streaming-indicator tests.

Use these instead of inlining your own — keeps "what's an edge case" consistent across the suite.

## Cross-cutting Theory pattern (DRY)

For families of types that share a contract, **extend the central Theory** instead of cloning per-type tests. The canonical example is [`Tst/MatPlotLibNet/Models/Series/AllSeriesTests.cs`](../Tst/MatPlotLibNet/Models/Series/AllSeriesTests.cs), which exercises **every** `ISeries` (currently 75+ types in `AllSeriesInstances`) against the same 9 invariants:

- `Label` defaults to `null`, can be set/read
- `Visible` defaults to `true`, can be set to `false`
- `ZOrder` has the correct default (`AreaSeries` → −1; everything else → 0), can be set
- `Accept(visitor, area)` dispatches to the correct visitor method
- `IHasColor.Color` defaults to `null` (interface-filtered)
- `IColormappable.ColorMap` defaults to `null` (interface-filtered)
- `IHasMarkerStyle.MarkerStyle` defaults to `Circle` (interface-filtered)
- `IHasAlpha.Alpha` falls within `[0, 1]` (interface-filtered)
- `IHasEdgeColor.EdgeColor` defaults to `null` (interface-filtered)
- `XYSeries.XData/YData` are stored unchanged (XYSeries-filtered)
- `ToSeriesDto()` round-trips with non-empty `Type` string
- `ComputeDataRange(...)` produces finite numbers for non-empty data

Adding a new series type requires **one line** in `AllSeriesInstances` plus the corresponding `Visit` overload in `TestSeriesVisitor.cs` — and that single addition runs ~12 conformance tests. The renderer-side equivalent is [`Tst/MatPlotLibNet/Rendering/SeriesRenderers/AllRenderersDirectInvocationTests.cs`](../Tst/MatPlotLibNet/Rendering/SeriesRenderers/AllRenderersDirectInvocationTests.cs) — direct-invocation Theory over every `SeriesRenderer<T>` to cover renderer code paths that the visitor dispatch hides from the static call graph.

Phase-9 dedup (v1.7.2) removed 78 per-series `[Fact]` duplicates of these Theory tests across 55 files — net delta 5 569 → 5 468 tests, zero coverage regression.
