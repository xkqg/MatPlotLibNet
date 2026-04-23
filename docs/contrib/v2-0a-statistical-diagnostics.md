# v2.0a — Statistical Diagnostics Pack (Chart Types)

First PR of the **v2.0 "Quant & Stats Pack"** — targeted chart-type additions for quant/scientific workflows. Scope: **three statistical-diagnostic chart types** that matplotlib and seaborn users have come to expect but are absent from current .NET charting libraries.

**Target:** merge into `main` for v2.0 (after v1.9.0 Tier 3 ships). This PR adds the first 3 of an 8-10 chart-type expansion.

**Coverage gate:** ≥90% line AND ≥90% branch per public class.

**Key difference from v1.8/v1.9 indicator tiers:** these are **chart series types**, not indicators. Each needs a `*Series` model, a `*SeriesRenderer`, an `AxesBuilder` extension, and tests at each layer. Grep the repo for an existing series like `BoxSeries` + `BoxSeriesRenderer` before starting — copy-paste the skeleton and adapt.

---

## Chart types to add

| # | Type | Category | Inputs | Panel | Layering / reuse |
|---|---|---|---|---|---|
| 1 | **ACF / PACF** | Time-series diagnostic | Returns series + maxLag | Separate subplot | Stem + confidence band |
| 2 | **QQ Plot** | Distribution diagnostic | Sample + theoretical distribution | Separate subplot | Scatter + reference line |
| 3 | **Ridgeline (Joyplot)** | Multi-group distribution | Multiple labeled samples | Single subplot | Stack of translucent KDEs |

All three are close-only inputs for your immediate quant use case (return series, residuals, month-grouped returns).

---

## Why these three together

They answer **three distinct diagnostic questions** that every quant ends up asking:

- *"Is my residual / return series autocorrelated?"* → **ACF / PACF** (time-series stationarity check)
- *"Is the return distribution normal, heavy-tailed, or skewed?"* → **QQ plot** (distribution goodness-of-fit)
- *"How do return distributions differ across months/symbols/regimes?"* → **Ridgeline plot** (multi-group comparison)

Together they give users the standard seaborn/statsmodels diagnostic stack natively in .NET. No mainstream .NET charting library ships all three; shipping them makes MatPlotLibNet the obvious choice for .NET quant research.

---

## 1. ACF / PACF Plot

**Autocorrelation Function / Partial Autocorrelation Function** — the foundational stationarity + model-order diagnostics for time series. Shows correlation of a series with itself at lags 1..N, with a confidence band around zero. Classic Box-Jenkins tooling.

### Visual form

Lollipop/stem chart: vertical stems from y=0 up (or down) to the correlation value at each lag. Confidence band as a dotted horizontal line pair at `±1.96/√N` (95% CI). Lags with stems inside the band are "not significantly autocorrelated".

### Formula

```
# ACF: autocorrelation at lag k
acf[k] = Σ((x_t − x̄) · (x_{t-k} − x̄)) / Σ((x_t − x̄)²)    for k in [0, maxLag]

# PACF: partial autocorrelation via Durbin-Levinson recursion
# (standard algorithm — not reinventing; reference statsmodels.tsa.stattools.pacf for the exact recurrence)
```

`acf[0] = 1` always (trivially). Confidence band: `±1.96/√N` where N is series length.

**Source:** Box, G. E. P., Jenkins, G. M. (1970). *Time Series Analysis: Forecasting and Control*. Durbin-Levinson recursion in Brockwell & Davis *Time Series: Theory and Methods* (1991).

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/CorrelogramSeries.cs
public sealed class CorrelogramSeries : Series
{
    public double[] Correlations { get; init; } = [];
    public double ConfidenceBand { get; init; }          // e.g. 1.96/√N
    public CorrelogramKind Kind { get; init; }            // Acf | Pacf
    public Color? StemColor { get; set; }
    public Color? BandColor { get; set; }
    public double StemWidth { get; set; } = 1.5;
}

public enum CorrelogramKind { Acf, Pacf }
```

### Compute helper (pure, internal)

```csharp
internal static class Correlogram
{
    public static double[] Acf(ReadOnlySpan<double> x, int maxLag);
    public static double[] Pacf(ReadOnlySpan<double> x, int maxLag);  // Durbin-Levinson
    public static double ConfidenceBand(int n) => 1.96 / Math.Sqrt(n);
}
```

### Renderer

`Rendering/CorrelogramSeriesRenderer.cs` — draws stems at integer x-positions (lag = 1, 2, ..., maxLag) with y = correlation value. Adds two dashed horizontal lines at `±ConfidenceBand`. Handles both Acf (includes lag 0 at y=1) and Pacf (starts at lag 1).

### AxesBuilder shortcut

```csharp
public AxesBuilder Acf(double[] series, int maxLag = 40, Action<CorrelogramSeries>? configure = null);
public AxesBuilder Pacf(double[] series, int maxLag = 40, Action<CorrelogramSeries>? configure = null);
```

### Branches to cover (≥90/90)

1. **Empty series** → throw
2. **`maxLag < 1`** → throw
3. **`maxLag >= series.Length`** → throw (can't compute that far)
4. **Constant series** (variance 0) → all correlations = 0 except lag 0 (which stays at 1)
5. **White noise** (random series) → correlations all near 0, most within confidence band
6. **Known AR(1)** — correlation at lag 1 ≈ ρ, decays geometrically
7. **Pacf branch** — Durbin-Levinson with rank-deficient matrix (all-constant input) → gracefully return zeros

### Test vectors

```csharp
// Constant series → acf = [1, 0, 0, ...]
var flat = Correlogram.Acf(Enumerable.Repeat(5.0, 100).ToArray(), maxLag: 5);
flat[0].ShouldBe(1.0);
flat[1..].ShouldAllBe(v => Math.Abs(v) < 1e-9);

// Reference: statsmodels.tsa.stattools.acf + pacf
//   import numpy as np
//   from statsmodels.tsa.stattools import acf, pacf
//   np.random.seed(42); x = np.random.randn(100)
//   acf_ref = acf(x, nlags=10, fft=False)
//   pacf_ref = pacf(x, nlags=10, method='ywmle')
// Commit expected values to 6 decimals.
```

---

## 2. QQ Plot

**Quantile-Quantile plot** — scatter of sample quantiles against theoretical quantiles of a reference distribution (usually standard normal). Points on the 45° line = sample matches theoretical; deviations reveal heavy tails, skew, bimodality.

### Visual form

Scatter plot. Fat tails bow upward on the right + downward on the left. Right-skew bows upward consistently. Includes the reference `y = x` line (or linear fit through middle quantiles).

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/QqSeries.cs
public sealed class QqSeries : Series
{
    public double[] SampleQuantiles { get; init; } = [];
    public double[] TheoreticalQuantiles { get; init; } = [];
    public QqReference Reference { get; init; }              // ReferenceLine | LinearFit | None
    public Color? MarkerColor { get; set; }
    public Color? LineColor { get; set; }
    public MarkerStyle Marker { get; set; } = MarkerStyle.Circle;
}

public enum QqReference { ReferenceLine, LinearFit, None }
```

### Compute helper

```csharp
internal static class QqComputation
{
    // Given sample, return (sampleQuantiles, theoreticalQuantiles) sorted + paired
    public static (double[] Sample, double[] Theoretical) Normal(ReadOnlySpan<double> sample);
    // Extension point for future: Exponential, Uniform, etc.
}
```

Uses the standard plotting-position formula: `p_i = (i − 0.5) / n` for the i-th sorted sample, then `theoretical_i = Φ⁻¹(p_i)` (use the same `Beasley-Springer-Moro` inverse normal CDF already added for `DeflatedSharpe` in v1.7's Tier 2 — or grep your codebase for existing `InverseNormalCdf` to reuse).

### Renderer

`QqSeriesRenderer.cs` — scatter the paired quantiles. If `Reference == ReferenceLine`, draw `y = x` across the axis range. If `LinearFit`, compute least-squares through the middle 50% of quantiles (robust to tail deviations) and draw that.

### AxesBuilder shortcut

```csharp
public AxesBuilder Qq(double[] sample, QqReference reference = QqReference.ReferenceLine,
    Action<QqSeries>? configure = null);
```

### Branches to cover (≥90/90)

1. **Empty sample** → throw
2. **Length == 1** → throw (can't form quantile pairs)
3. **Constant sample** — all quantiles equal → degenerate but shouldn't throw; visually a horizontal line
4. **Normal sample** (synthetic) → points near y=x
5. **Heavy-tailed sample** (e.g. t-distribution samples) → tails deviate predictably
6. **`Reference == None`** → no reference line drawn (separate test)
7. **`Reference == LinearFit`** — verify fit robust to outliers

### Test vectors

```csharp
// Exact normal samples → theoretical and sample should match to high precision
// Python: scipy.stats.norm.ppf((np.arange(n) + 0.5) / n)
// Commit expected theoretical quantiles for n=100 to 8 decimals.
```

---

## 3. Ridgeline (Joyplot)

**Stacked density plots** — one KDE per labeled group, overlapped vertically with a small offset. The classic "Joy Division album cover" look. Lets you compare distribution shapes across many groups in compact vertical space.

### Visual form

Each group renders as a filled KDE curve. Groups stack top-to-bottom with adjustable overlap (typically 50-80% overlap so curves peek through each other). Group labels on the left axis. Common x-axis.

### Series model

```csharp
// Src/MatPlotLibNet/Models/Series/RidgelineSeries.cs
public sealed class RidgelineSeries : Series
{
    public IReadOnlyList<RidgelineGroup> Groups { get; init; } = [];
    public double Overlap { get; init; } = 0.7;           // 0 = no overlap, 1 = full stack
    public double FillAlpha { get; set; } = 0.5;
    public Color[]? Palette { get; set; }                  // one color per group, cycled
    public int KdePoints { get; set; } = 256;              // resolution of each KDE
    public double? KdeBandwidth { get; set; }              // null = Silverman auto
}

public sealed record RidgelineGroup(string Label, double[] Sample);
```

### Compute helper

Reuse the existing **Gaussian KDE** from `KdeSeries` — this is one of the rare series-model reuses. Each `RidgelineGroup.Sample` goes through the same KDE compute path; only the rendering stacks them.

### Renderer

`RidgelineSeriesRenderer.cs` — iterates groups top-to-bottom. For each:
1. Compute KDE over `KdePoints` x-positions on a shared x-range (max of all groups)
2. Translate the curve down by `groupIndex × (1 − overlap) × peakHeight`
3. Fill under the curve with palette color + alpha
4. Draw group label on left axis at the baseline y-position

Calculate peak height from first group to set the shared stacking offset.

### AxesBuilder shortcut

```csharp
public AxesBuilder Ridgeline(IEnumerable<(string Label, double[] Sample)> groups,
    Action<RidgelineSeries>? configure = null);
```

### Branches to cover (≥90/90)

1. **Empty groups** → throw
2. **Single group** → behaves like a single filled KDE at y=0
3. **Overlap == 0** → no overlap, groups fully separated
4. **Overlap == 1** → full overlap (same baseline)
5. **`Overlap < 0` or `> 1`** → throw
6. **Empty sample in a group** → skip that group (or throw — pick a convention; "skip with warning" is the seaborn convention)
7. **`KdePoints < 2`** → throw
8. **Palette shorter than group count** → cycle through available colors

### Test vectors

Visual regression tests are awkward here — use the existing `KdeSeries` tests as the oracle for per-group KDE correctness, then test:
- Group positions (y-offsets) match expected stacking given overlap setting
- Palette cycling works when groups > palette size
- Top group's peak y-value == first group's max KDE density

---

## Test file structure

- `Tst/MatPlotLibNet/Models/Series/CorrelogramSeriesTests.cs` (model)
- `Tst/MatPlotLibNet/Rendering/CorrelogramSeriesRendererTests.cs` (SVG output assertions)
- `Tst/MatPlotLibNet/Indicators/CorrelogramTests.cs` (internal compute helper: Acf + Pacf)
- Similar 3-file set for QqSeries and RidgelineSeries

Nine test files total (3 per chart type). Align with the existing pattern — grep `BoxSeriesTests` / `BoxSeriesRendererTests` for the reference structure.

---

## Coverage verification

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `CorrelogramSeries`, `CorrelogramSeriesRenderer`, `Correlogram` (compute helper): ≥90/90
- `QqSeries`, `QqSeriesRenderer`, `QqComputation`: ≥90/90
- `RidgelineSeries`, `RidgelineSeriesRenderer`: ≥90/90

**Budget:** ACF/PACF is the hardest (Durbin-Levinson recursion + confidence-band rendering). QQ and Ridgeline are straightforward — both reuse existing scatter + KDE infrastructure.

---

## PR checklist

- [ ] 3 series classes under `Src/MatPlotLibNet/Models/Series/`
- [ ] 3 renderer classes under `Src/MatPlotLibNet/Rendering/`
- [ ] 2 compute helpers: `Correlogram` (ACF + PACF), `QqComputation` (quantile pairing)
- [ ] `InverseNormalCdf` reused from existing location (grep before adding; currently lives in `DeflatedSharpe` — may warrant extraction to a shared numerics helper if this is the second consumer)
- [ ] 3 AxesBuilder shortcuts: `.Acf()`, `.Pacf()`, `.Qq()`, `.Ridgeline()` (4 methods, 3 chart types)
- [ ] 9 test files
- [ ] Python reference vectors for Correlogram (statsmodels) and QQ (scipy.stats)
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] Changelog entry under `v2.0a`: "Added ACF/PACF correlogram, QQ plot, Ridgeline plot — statsmodels/seaborn-equivalent statistical diagnostics"
- [ ] Wiki `Chart-Types.md` updated with the three new types + usage snippets

---

## v2.0 release roadmap (context for reviewers)

v2.0 adds ~10 chart types across **four sub-PRs**:

| Sub-PR | Theme | Chart types | Status |
|---|---|---|---|
| **v2.0a** (this PR) | Statistical Diagnostics | ACF/PACF, QQ, Ridgeline | ← now |
| v2.0b | Financial Motion | Renko, Heikin-Ashi, Point & Figure | brief TBD |
| v2.0c | Analysis Tools | Parallel coordinates, Forest plot | brief TBD |
| v2.0d | Time-Series Visualization | Horizon chart, Calendar heatmap | brief TBD |

Full v2.0 release narrative after all four land:
*"10 new chart types for quant and scientific workflows — ACF/PACF diagnostics, Q-Q distributional checks, ridgeline group comparisons, three price-motion financial charts, parallel-coordinates and forest plots for experiment analysis, horizon chart and calendar heatmap for compact time-series view. Totals: 84 series + 24 indicators across v1.8 → v1.9 → v2.0."*

v2.0b-d briefs will be drafted when v2.0a lands. **Do not front-load** — keep per-PR scope small so each ships cleanly.

## What's NOT in v2.0

Explicitly out of scope for the entire v2.0 release:
- Infographic / business-dashboard types (pictogram, word cloud, Venn, tally, Gantt)
- Geospatial types (already covered by `MatPlotLibNet.Geo`)
- Network/graph types (would need a dedicated graph-layout subsystem — v2.1+)
- 3D additions beyond current 12 types
- Animation types

Stays focused on what quant / scientific users actually need, not chart-type breadth.

---

## Motivation / why this PR first

ACF/PACF in particular is **the** missing tool in .NET quant workflows. Every Python quant uses `statsmodels.graphics.tsaplots.plot_acf(residuals)` as a reflex check after fitting a model. No .NET library ships this. The user's own RL project (per `project_overfit_v2` memory) would immediately benefit: return-correlation diagnostics for train/val/test residuals plug directly into the Phase 3 train-val correlation work.

QQ plot + Ridgeline round out the "distribution diagnostics" story — together with the existing Violin and Box plots, that's the full quant-distributional-analysis stack in one library.

**Release narrative after v2.0a alone:**
> *"v2.0a adds ACF/PACF correlogram, QQ plot, and Ridgeline (joyplot). The .NET ecosystem's first native Box-Jenkins + distribution-diagnostic toolkit — no Python dependency for residual analysis."*

That's a compelling PR announcement for the quant-finance .NET community, even before v2.0b-d land.
