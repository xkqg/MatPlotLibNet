# Coverage Policy

MatPlotLibNet enforces **≥90% line coverage AND ≥90% branch coverage on every public class**. The CI build fails if any class drops below its threshold or regresses against the committed baseline.

## Why 90/90 (not 80/80)

Industry consensus is "80% is good enough"; we don't accept that. Branch coverage of 80% means 1 in 5 conditional paths is never tested — exactly where silent-failure bugs hide. v1.7.0 shipped four such bugs (geo extensions, broken-axis, symlog, playground grid) under a 3,967-test suite that was 85% line / 68% branch. The threshold lives in [`tools/coverage/thresholds.json`](../tools/coverage/thresholds.json).

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

The script in [`tools/coverage/check-thresholds.ps1`](../tools/coverage/check-thresholds.ps1) reads the merged Cobertura XML produced by `coverlet.console` + `dotnet-reportgenerator-globaltool` (both installed as global .NET tools).

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

Use these instead of inlining your own — keeps "what's an edge case" consistent across the suite.
