# Coverage Policy

MatPlotLibNet enforces **≥90% line coverage AND ≥90% branch coverage on every public class**. The CI build fails if any class drops below its threshold or regresses against the committed baseline.

**Status (v1.7.2, Phase X complete):** **7 072 tests green (4 known-bug skips)** across 9 test projects (was 6 203 pre-Phase-X, 5 385 post-Phase-P, 3 967 at v1.7.0 baseline). Default-mode regression gate **PASSES** against the regenerated baseline. **64 classes still below absolute 90/90** (was 110 mid-Phase-W, 169 at v1.7.2 release) — Phase X graduated 46 classes through stacked-OO test bases (`ModifierTestBase<T>`, `RendererCoverageTestBase<T>`) + 6 documented exemptions for provably-unreachable defensive arms. Total project coverage: **94.8% line / 84.3% branch** (was 92.5% / 81.3% pre-Phase-X). Strict-mode flip remains the next coverage milestone — the largest remaining substantive sub-90 classes (`AxesRenderer` 76L/62B, `CartesianAxesRenderer` 83L/73B, `AxesBuilder` 84L/60B, complexity 100+ each) are deferred to a focused future phase since they need a deep dive of their own.

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

Exemptions in `tools/coverage/thresholds.json`: 19 → **25** (Phase X added BrowserLauncher [pre-existing X.0], Sinusoidal, Stereographic, StreamingChartSession, HoverModifier, DataCursorModifier, ZoomModifier, InteractionController, PieSeriesRenderer, BarSeriesRenderer, SvgRenderContext — each with documented `reason`).

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
