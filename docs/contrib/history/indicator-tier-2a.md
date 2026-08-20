> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 2a (Regime Detection)

First PR of Tier 2 (*"Regime & Cycles"* release, targeted at v1.8.0). Scope: **three regime-detection indicators** that answer distinct questions:

- *"Did the process just change?"* → **BOCPD** (Bayesian Online Changepoint Detection)
- *"How anomalous is the current state vs historical normal?"* → **Turbulence Index**
- *"How much do the regime signals disagree?"* → **Dispersion Index**

These three together form a **regime stack**: BOCPD flags change events, Turbulence quantifies "distance from normal", Dispersion measures agreement among regime features. All three are prominently used in institutional quant research.

**Target:** merge into `main` for v1.8.0 after Tier 1 ships. First Tier 2 PR.

**Coverage gate:** ≥90% line AND ≥90% branch per public class, enforced per `docs/COVERAGE.md`. `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** same as Tier 1 — validate at the boundary, explicit math branches for known degeneracies, no blanket `IsNaN` / `IsInfinity` guards in compute loops. See `indicator-tier-1d.md` for the full policy.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **BOCPD** | Regime / Changepoint | Close + hazard rate | Changepoint probability series [0,1] | Separate subplot |
| 2 | **Turbulence Index** | Regime / Anomaly | Feature matrix (multivariate) + window | Mahalanobis distance series (positive) | Separate subplot (log-scale typical) |
| 3 | **Dispersion Index** | Regime / Meta | Multiple regime signal series | Disagreement score [0,1] | Separate subplot |

All three inherit `Indicator<SignalResult>` (BOCPD takes prices only) or a new multivariate base class (Turbulence and Dispersion take feature matrices — see implementation note below).

---

## Multivariate base class

Turbulence and Dispersion both take a **matrix of features** (one feature per column, one time point per row) rather than OHLC arrays. Add a small base class:

```csharp
public abstract class MultivariateIndicator<TResult> : Indicator<TResult>
    where TResult : IIndicatorResult
{
    protected double[][] Features { get; }  // [time][feature]
    protected int BarCount { get; }
    protected int FeatureCount { get; }

    protected MultivariateIndicator(double[][] features)
    {
        if (features is null || features.Length == 0)
        {
            Features = [];
            BarCount = 0;
            FeatureCount = 0;
            return;
        }
        Features = features;
        BarCount = features.Length;
        FeatureCount = features[0].Length;

        // Validate rectangular
        foreach (var row in features)
            if (row.Length != FeatureCount)
                throw new ArgumentException("All feature rows must have the same length.", nameof(features));
    }
}
```

Put in `Src/MatPlotLibNet/Indicators/MultivariateIndicator.cs`.

---

## 1. BOCPD — Bayesian Online Changepoint Detection

Adams & MacKay's online algorithm for detecting structural breaks in a time series **in real time**. Maintains a posterior distribution over "run length" `r_t` (how many bars since the last change). When a new observation arrives that's inconsistent with the current run's statistics, `P(r_t = 0)` spikes — that's the changepoint signal.

Used in quant research to detect regime shifts with minimal lag. The output is an interpretable **probability** (not a heuristic score).

### Formula (simplified Normal-likelihood variant)

Maintain a distribution `P(r_t = r)` over run lengths at each bar. Update rule at each new observation `x_t`:

```
# Predictive probability of x_t given the run of length r
π(r)_t  =  Normal(x_t | μ_r, σ²_r + κ²)       # predictive variance includes prior κ

# Hazard rate (prior prob of changepoint; constant here)
H = 1 / expected_run_length     # e.g. H = 1/100 = 0.01

# Growth probability: run continues
P(r_t = r+1) = P(r_{t-1} = r) · π(r) · (1 − H)

# Changepoint probability: run resets to 0
P(r_t = 0) = Σ_r P(r_{t-1} = r) · π(r) · H

# Normalize
P(r_t = r) /= Σ_r P(r_t = r)
```

Where `μ_r`, `σ²_r` are sufficient statistics of the last `r` observations (running mean/variance). The **changepoint probability output** is simply `P(r_t = 0)` — ranges [0, 1], spikes at true breaks.

**Simplification for v1.8.0:** truncate run length at a `maxRunLength` (default 500). Keeps memory bounded at O(maxRunLength) per bar rather than O(T).

**Source:** Adams, R. P., & MacKay, D. J. C. (2007). *Bayesian Online Changepoint Detection*. arXiv:0710.3742.

### Signature

```csharp
public sealed class Bocpd : PriceIndicator<SignalResult>
{
    private readonly double _hazard;           // 1 / expected run length
    private readonly double _priorVariance;    // κ² in the predictive
    private readonly int _maxRunLength;

    public Bocpd(double[] prices, double hazard = 0.01, double priorVariance = 1.0, int maxRunLength = 500)
        : base(prices)
    {
        if (hazard <= 0 || hazard >= 1) throw new ArgumentException("hazard in (0, 1)", nameof(hazard));
        if (priorVariance <= 0) throw new ArgumentException("priorVariance > 0", nameof(priorVariance));
        if (maxRunLength < 1) throw new ArgumentException("maxRunLength >= 1", nameof(maxRunLength));
        _hazard = hazard;
        _priorVariance = priorVariance;
        _maxRunLength = maxRunLength;
        Label = $"BOCPD(h={hazard:0.###})";
    }

    public override SignalResult Compute() { /* returns double[n-1] — P(changepoint) per bar */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

**Warmup:** 1 bar (needs prev observation to start the first run).

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length == 1** → empty (need two observations)
3. **Length == 2** → boundary, one output value
4. **`hazard <= 0`** → throw
5. **`hazard >= 1`** → throw
6. **`priorVariance <= 0`** → throw
7. **`maxRunLength < 1`** → throw
8. **Flat prices** — all observations identical → residual = 0 → predictive spikes sharply at true mean; changepoint prob stays low throughout (test: should never exceed ~`hazard × 2`)
9. **Strong breakpoint** — 20 bars @ 100 then 20 bars @ 200 → changepoint prob spikes >0.5 around bar 21
10. **Normalization** — output sum across all bars stays finite, no NaN
11. **Run length truncation** — with `maxRunLength=5` and 50 bars of flat data, the distribution never exceeds length 5 (verify internal bound hit)

### Test vectors

```csharp
// Flat prices → no changepoint signal
var flat = new Bocpd(Enumerable.Repeat(100.0, 50).ToArray()).Compute();
flat.Max().ShouldBeLessThan(0.05);  // noise floor near hazard

// Strong break → spike around transition point
var prices = new double[40];
for (int i = 0; i < 20; i++) prices[i] = 100.0;
for (int i = 20; i < 40; i++) prices[i] = 200.0;
var result = new Bocpd(prices).Compute();
// Spike expected at index 19 (the output for bar 20)
result[19].ShouldBeGreaterThan(0.5);

// Python reference for regression test:
//   import numpy as np
//   (canonical numpy BOCPD snippet — commit expected output vector for a fixed seed series)

// Parameter validation
Assert.Throws<ArgumentException>(() => new Bocpd([100, 101], hazard: 0));
Assert.Throws<ArgumentException>(() => new Bocpd([100, 101], hazard: 1));
Assert.Throws<ArgumentException>(() => new Bocpd([100, 101], priorVariance: 0));
Assert.Throws<ArgumentException>(() => new Bocpd([100, 101], maxRunLength: 0));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder Bocpd(double[] prices, double hazard = 0.01,
    double priorVariance = 1.0, int maxRunLength = 500,
    Action<Indicators.Bocpd>? configure = null) { /* standard template */ }
```

### Panel placement

Separate subplot, Y-range `[0, 1]`. Typical threshold markers callers add: `AxHLine(0.5)` (strong changepoint).

---

## 2. Turbulence Index

Kritzman & Li's multivariate Mahalanobis distance — measures how far the current feature vector is from the historical multivariate mean, accounting for feature covariance. High turbulence = anomalous / crisis-like conditions.

Used by Kritzman-Li (2010) as a market-stress indicator; correlates with crisis periods (2008, COVID-19 crash) independently of any single market variable.

### Formula

For each bar `t` given a rolling window of `N` past feature vectors:

```
μ = mean(X[t-N:t])                      # feature-wise mean
Σ = covariance(X[t-N:t])                # N × N covariance matrix
TI_t = (X_t − μ)ᵀ · Σ⁻¹ · (X_t − μ)     # Mahalanobis distance squared
```

Under the null (features ~ multivariate normal), `TI_t` follows a chi-squared distribution with `nFeatures` degrees of freedom. Values >> `nFeatures` are anomalous.

**Implementation detail:** use **regularized inverse** `Σ⁻¹ ≈ (Σ + λI)⁻¹` with small `λ` (default 1e-6) to avoid numerical issues when features are near-collinear. Implement matrix inversion via LU decomposition for small matrices (≤10 features typical use); document this as the feature-count bound.

**Source:** Kritzman, M., & Li, Y. (2010). *Skulls, Financial Turbulence, and Risk Management*. Financial Analysts Journal, 66(5), 30–41.

### Signature

```csharp
public sealed class TurbulenceIndex : MultivariateIndicator<SignalResult>
{
    private readonly int _window;
    private readonly double _regularization;

    public TurbulenceIndex(double[][] features, int window = 252, double regularization = 1e-6)
        : base(features)
    {
        if (window < 2) throw new ArgumentException("window >= 2", nameof(window));
        if (regularization < 0) throw new ArgumentException("regularization >= 0", nameof(regularization));
        if (FeatureCount > 10)
            throw new ArgumentException("Max 10 features supported — see docs for larger-scale regularization options.", nameof(features));
        _window = window;
        _regularization = regularization;
        Label = $"Turb({window})";
    }

    public override SignalResult Compute() { /* returns double[BarCount - window] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _window);
}
```

**Warmup:** `window` bars (need full history for the first mean/cov estimate).

### Branches to cover (≥90/90)

1. **Empty feature matrix** → empty output
2. **Non-rectangular features** → throw (already validated in base class, verify test hits that branch)
3. **`BarCount <= window`** → empty
4. **`BarCount == window + 1`** → boundary, one output
5. **`window < 2`** → throw
6. **`regularization < 0`** → throw
7. **`FeatureCount > 10`** → throw
8. **`FeatureCount == 1`** (univariate degenerate) → reduces to `(x - μ)² / σ²` — verify
9. **Singular covariance** (e.g. one feature is zero everywhere) → `Σ + λI` saves it; verify no NaN
10. **Normal multi-bar path**

### Test vectors

```csharp
// Univariate (1 feature) → Turbulence = z-score²
// features = [[1], [2], [3], [4], [5], [100]], window=5
// Window [1,2,3,4,5] has μ=3, σ²=2.5 → (100-3)²/2.5 ≈ 3763.6
var uni = new TurbulenceIndex([[1], [2], [3], [4], [5], [100]], window: 5).Compute();
uni[0].ShouldBe(3763.6, 1.0);

// Bivariate with correlated features — verify a synthetic shock correctly triggers TI
// Python reference:
//   import numpy as np
//   def turbulence(X, window):
//       result = []
//       for t in range(window, len(X)):
//           H = X[t-window:t]
//           mu = H.mean(axis=0); cov = np.cov(H.T) + 1e-6*np.eye(H.shape[1])
//           diff = X[t] - mu
//           ti = diff @ np.linalg.inv(cov) @ diff
//           result.append(ti)
//       return np.array(result)
// Commit ≥3 expected values with a hand-constructed bivariate shock scenario.

// Validation
Assert.Throws<ArgumentException>(() => new TurbulenceIndex([[1,2]], window: 1));
Assert.Throws<ArgumentException>(() => new TurbulenceIndex([[1,2]], regularization: -0.1));
// More than 10 features
var big = new double[5][];
for (int i = 0; i < 5; i++) big[i] = new double[11];
Assert.Throws<ArgumentException>(() => new TurbulenceIndex(big));
```

### LU decomposition helper

Add `internal static` helper `InvertSmallMatrix(double[,] m, double[,] result, double regularization)` in the Turbulence class (or extracted to a shared `LinAlg` helper if one exists — check first). Use partial-pivot LU. Cover with a dedicated test on a 2×2 and 3×3 matrix against hand-computed inverse.

### AxesBuilder shortcut

```csharp
public AxesBuilder TurbulenceIndex(double[][] features, int window = 252,
    double regularization = 1e-6,
    Action<Indicators.TurbulenceIndex>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot. Y-axis often shown on log scale (values span orders of magnitude). No hard bound. Callers often add chi-squared reference lines at `nFeatures` (median under null) and `3 × nFeatures` (1% tail).

---

## 3. Dispersion Index

**Meta-indicator** — measures how much a set of regime signals *disagree* with each other. When all signals say "bull" the dispersion is low; when signals split, dispersion is high (regime transition / uncertainty).

Useful as a confidence gate in regime-aware strategies: high dispersion means "the regime is unclear, size down".

### Formula

Given `K` regime signals, each normalized to `[0, 1]` (or a probability), at each time `t`:

```
# Normalize each signal column across its full range (so signals are comparable)
# Optional — skip if signals are already on [0, 1]

# Compute per-bar dispersion as either:
# (a) Standard deviation across signals (simpler, my recommendation)
Dispersion_t = stddev(signals_1_t, signals_2_t, ..., signals_K_t)

# (b) Entropy of the normalized distribution (alternative — needs normalization)
# p_i = signal_i / Σ signal_j  (if signals non-negative)
# Dispersion_t = -Σ p_i · log(p_i) / log(K)      # normalized to [0, 1]
```

I recommend (a) for v1.8.0 — simpler, always defined, no normalization edge cases. Entropy variant can be a v1.10+ option.

**Source:** Dispersion as a regime-uncertainty measure appears in many papers; common example is **VIX-term-structure dispersion**. The stddev-of-signals variant is standard in ensemble-classifier uncertainty literature.

### Signature

```csharp
public sealed class DispersionIndex : MultivariateIndicator<SignalResult>
{
    public DispersionIndex(double[][] signals) : base(signals)
    {
        if (FeatureCount < 2) throw new ArgumentException("DispersionIndex requires at least 2 signals", nameof(signals));
        Label = $"Dispersion({FeatureCount})";
    }

    public override SignalResult Compute() { /* returns double[BarCount], stddev per row */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 0);
}
```

**Warmup:** 0 (each bar computed independently from the signals at that bar).

### Branches to cover (≥90/90)

1. **Empty feature matrix** → empty output
2. **`FeatureCount < 2`** → throw (need at least 2 signals to measure disagreement)
3. **`FeatureCount == 2`** boundary
4. **Perfect agreement** — all signals identical → dispersion = 0
5. **Maximum disagreement** — half signals = 0, half = 1 → dispersion = 0.5 for K=2, larger for K>2 (depends on split)
6. **Non-rectangular features** → throw via base class
7. **Normal multi-bar path**

### Test vectors

```csharp
// Perfect agreement → all zeros
var agreed = new DispersionIndex([[0.7, 0.7, 0.7], [0.3, 0.3, 0.3]]).Compute();
agreed.ShouldAllBe(v => v == 0);

// K=2 binary split → stddev of [0, 1] = 0.5
var binary = new DispersionIndex([[0.0, 1.0]]).Compute();
binary[0].ShouldBe(0.5, 1e-9);

// K=4 mixed → known stddev
// signals [0.2, 0.3, 0.7, 0.8] → mean=0.5, variance=0.1 → stddev = √0.1 ≈ 0.3162
var mixed = new DispersionIndex([[0.2, 0.3, 0.7, 0.8]]).Compute();
mixed[0].ShouldBe(Math.Sqrt(0.1), 1e-9);

// Validation
Assert.Throws<ArgumentException>(() => new DispersionIndex([[0.5]]));  // only 1 signal
```

### AxesBuilder shortcut

```csharp
public AxesBuilder DispersionIndex(double[][] signals,
    Action<Indicators.DispersionIndex>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot. Y-range typically `[0, ~1]` but depends on signal scale. Callers often display above BOCPD/Turbulence as an agreement barometer.

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/BocpdTests.cs`
- `Tst/MatPlotLibNet/Indicators/TurbulenceIndexTests.cs` + `TurbulenceMatrixInversionTests.cs` (separate file for the LU helper)
- `Tst/MatPlotLibNet/Indicators/DispersionIndexTests.cs`
- `Tst/MatPlotLibNet/Indicators/MultivariateIndicatorTests.cs` (for the base class rectangular-validation branch)

xUnit v3 Theory + MemberData pattern. Python-generated regression vectors for BOCPD and Turbulence (bivariate case).

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `Bocpd`: ≥90/90
- `TurbulenceIndex`: ≥90/90
- `DispersionIndex`: ≥90/90
- `MultivariateIndicator` (base): ≥90/90 (one test hitting each constructor branch is enough)
- Turbulence LU helper: ≥90/90 (via internal test access)

**Budget warning:** Turbulence Index is the harder of the three because of the matrix-inverse helper and bivariate test vectors. Plan ~1.5× the time of BOCPD or Dispersion.

---

## PR checklist

- [ ] 3 indicator classes + 1 base class under `Src/MatPlotLibNet/Indicators/`
- [ ] 3 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 4 test files under `Tst/MatPlotLibNet/Indicators/` (one per class + base)
- [ ] LU matrix inversion helper — either new internal static in `TurbulenceIndex` or reused from existing LinAlg helper (grep first)
- [ ] Python reference snippets + regression vectors for BOCPD and bivariate Turbulence
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] No `thresholds.json` changes
- [ ] Changelog entry under `v1.8.0`: "Added Bayesian Online Changepoint Detection (Adams & MacKay 2007), Turbulence Index (Kritzman & Li 2010), Dispersion Index"
- [ ] Wiki page updated

## What's NOT in this PR

- Tier 2b (Entropy + Wavelet pack): Permutation Entropy, Wavelet Energy Ratio, Wavelet Entropy — deferred; wavelet transforms need a Haar/Daubechies helper that's a design call on its own.
- Tier 2c (Ehlers cycle family): Sinewave, Cyber Cycle, Roofing Filter, Adaptive Stochastic — deferred to a dedicated Ehlers follow-up; these share Hilbert-transform plumbing already in MAMA/FAMA that we want to factor cleanly first.
- Tier 2d (Classic): Elder Force, Aroon, RVI — deferred; these are easy and can be a quick follow-up PR.
- Streaming variants — all three here are naturally streaming but defer to Tier 2e.

---

## Motivation / why these three together

Regime detection is the **single most cited gap** in quant-finance charting libraries. BOCPD is the textbook online-changepoint algorithm that everyone reaches for; Turbulence is the Kritzman-Li "crisis detector" institutions use for tail-risk alerts; Dispersion is the meta-indicator that gates both (and any regime-classifier output) by confidence.

Shipping all three in one PR means MatPlotLibNet becomes the first .NET charting library with a production-ready **regime stack**. For crypto specifically, this unlocks building regime-aware strategies in .NET without pulling in Python/scipy.

Next tier (2b) will bring entropy + wavelet analysis (the "statistical" column), and tier 2c the Ehlers cycle family. Together they close the v1.8.0 "Regime & Cycles" release.
