> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.9.0 Indicator Pack — Tier 3c (Advanced & Cross-Asset)

Third and **final** PR of Tier 3. Scope: **four advanced indicators** that complete v1.9.0 — three remaining Ehlers pieces (iTrend, Decycler, SuperSmoother public exposure) plus one cross-asset information-theoretic indicator (Transfer Entropy).

After this PR, **v1.9.0 ships** as a coherent 12-indicator release: Volume (3a) + Trend/Transform (3b) + Advanced/Cross-asset (3c).

**Target:** merge into `main` for v1.9.0, after Tier 3b (Trend & Transform).

**Coverage gate:** ≥90% line AND ≥90% branch per public class.

**NaN / ±∞ policy:** same as Tier 1 — see `indicator-tier-1d.md`.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Ehlers iTrend** | Adaptive / Trend | Close | Trend series | Overlay on price |
| 2 | **Ehlers Decycler** | Adaptive / Trend | Close + cutoff period | De-cycled trend | Overlay on price |
| 3 | **SuperSmoother** (public) | Smoother | Input series + period | Smoothed series | Varies |
| 4 | **Transfer Entropy** | Cross-asset / Causality | Two aligned series + bins + lag | Directional info flow | Separate subplot |

Three of the four **reuse existing Ehlers DSP infrastructure** from Tier 2c (`HilbertDiscriminator`, `SuperSmoother`, `HighPassFilter`). Transfer Entropy is new territory — information-theoretic cross-asset measure.

---

## 1. Ehlers iTrend (Instantaneous Trend)

**Ehlers 2001 adaptive trend filter** — uses Hilbert-derived dominant period to set its own lookback. Follows trends with minimal lag in trending regimes, smooths noise in ranging regimes. One of the cleanest trend visualizations in quant finance.

### Formula

```
(period, _, _, _) = HilbertDiscriminator.Compute(close)    # reuse Tier 2c helper

# iTrend: weighted-tail moving average with period-adaptive weights
for t in [6, len):
    n = period[t]
    if n < 2: trend_t = trend_{t-1}  # fallback
    else:
        # Ehlers' asymmetric weighting — dominant period sets natural filter length
        trend_t = Σ_{i=0..n-1} ((n − i)/n) × close_{t-i} / Σ_{i=0..n-1} ((n − i)/n)
        # This simplifies to a weighted SMA where weights decrease linearly toward older bars
```

Effectively an adaptive linearly-weighted moving average where the window size IS the detected dominant cycle.

**Source:** Ehlers, J. F. (2001). *Rocket Science for Traders*, Wiley, ch. 16.

### Signature

```csharp
public sealed class EhlersITrend : PriceIndicator<SignalResult>
{
    public EhlersITrend(double[] prices) : base(prices) { Label = "iTrend"; }

    public override SignalResult Compute() { /* uses HilbertDiscriminator; returns double[prices.Length - 6] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 6);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length < 7** → empty (Hilbert warmup)
3. **Length == 7** → boundary, single output
4. **Flat prices** — detected period at clamp floor → trend = constant (flat) → tracks input
5. **Pure sinusoid** — detected period matches input cycle → trend smooths to center line
6. **Period clamp to minimum** (6) — verify no index-out-of-range when `n < required lookback`
7. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices → iTrend = price (smoothing a constant = constant)
var flat = Enumerable.Repeat(100.0, 50).ToArray();
var it = new EhlersITrend(flat).Compute();
it.ShouldAllBe(v => Math.Abs(v - 100.0) < 1e-6);

// Python reference: TA-Lib's ta.HT_TRENDLINE (same algorithm) for regression vector
//   import talib
//   trend = talib.HT_TRENDLINE(close)
// Commit ≥3 expected values post-warmup.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder EhlersITrend(double[] prices,
    Action<Indicators.EhlersITrend>? configure = null) { /* template */ }
```

### Panel placement

**Overlay on price panel.** Usually rendered as a bold line slightly below/above price, showing the adaptive trend. Pairs well with MAMA/FAMA for a full adaptive-MA stack.

---

## 2. Ehlers Decycler

**Removes the dominant cycle from a signal to leave the pure trend.** Uses high-pass + low-pass in specific combination to subtract the cycle band from the input. Cleaner than iTrend for pure-trend extraction; less reactive because it removes cycle info entirely.

### Formula

```
hp = HighPassFilter.Apply(prices, hpPeriod)     # reuse Tier 2c helper; default hpPeriod = 60
decycler_t = prices_t − hp_t                     # subtract the high-frequency content
```

Essentially `price − highFrequencyContent = lowFrequencyContent (trend)`. Very simple formula; complexity is in the high-pass filter's transient behavior.

**Source:** Ehlers, J. F. (2015). *Decycler Oscillator*. Stocks & Commodities 33(6).

### Signature

```csharp
public sealed class Decycler : PriceIndicator<SignalResult>
{
    private readonly int _hpPeriod;

    public Decycler(double[] prices, int hpPeriod = 60) : base(prices)
    {
        if (hpPeriod < 4) throw new ArgumentException("hpPeriod >= 4", nameof(hpPeriod));
        _hpPeriod = hpPeriod;
        Label = $"Decycler({hpPeriod})";
    }

    public override SignalResult Compute() { /* uses HighPassFilter from Tier 2c */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 2);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length < 3** → empty (filter needs prev values)
3. **`hpPeriod < 4`** → throw
4. **Flat prices** — HP output = 0 after settling → decycler = prices (unchanged)
5. **Pure high-frequency** — HP passes everything → decycler = 0
6. **Pure low-frequency trend** — HP removes nothing → decycler ≈ prices
7. **Mixed** — trend preserved, short-term swings removed

### Test vectors

```csharp
// Flat → decycler = prices (after transient)
var flat = Enumerable.Repeat(100.0, 100).ToArray();
var dec = new Decycler(flat, hpPeriod: 20).Compute();
dec.Skip(40).ShouldAllBe(v => Math.Abs(v - 100.0) < 1e-6);

// Param validation
Assert.Throws<ArgumentException>(() => new Decycler([100.0, 101.0], hpPeriod: 3));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder Decycler(double[] prices, int hpPeriod = 60,
    Action<Indicators.Decycler>? configure = null) { /* template */ }
```

### Panel placement

**Overlay on price panel.** Draws as a very smooth line running through price. Larger `hpPeriod` → smoother, more lag.

---

## 3. SuperSmoother (public exposure)

**Expose the internal `SuperSmoother` helper** from Tier 2c as a first-class public indicator. Same math, public API surface, users can now apply it directly to any input series.

Users want this because SuperSmoother is a **great general-purpose 2-pole Butterworth low-pass** — better noise filter than EMA, minimal lag, no ringing. Good on any series (not just price): indicator outputs, residuals, volume.

### Formula

Already defined in the `Indicators/Ehlers/SuperSmoother.cs` internal helper (from Tier 2c):

```
a1 = exp(-1.414·π / period)
b1 = 2·a1·cos(1.414·180° / period)
c2 = b1;  c3 = -a1²;  c1 = 1 − c2 − c3
SS_t = c1·(x_t + x_{t-1})/2  +  c2·SS_{t-1}  +  c3·SS_{t-2}
```

**Source:** Ehlers, J. F. (2013). *Cycle Analytics for Traders*, Wiley, ch. 3.

### Signature

```csharp
public sealed class EhlersSuperSmoother : Indicator<SignalResult>
{
    private readonly double[] _input;
    private readonly int _period;

    public EhlersSuperSmoother(double[] input, int period = 10)
    {
        if (input is null) throw new ArgumentException("input required", nameof(input));
        if (period < 2) throw new ArgumentException("period >= 2", nameof(period));
        _input = input;
        _period = period;
        Label = $"SS({period})";
    }

    public override SignalResult Compute()
    {
        // Delegate to the internal helper from Tier 2c
        return Indicators.Ehlers.SuperSmoother.Apply(_input, _period);
    }

    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 2);
}
```

**Note:** `Indicator<T>` base (not `PriceIndicator`) because SuperSmoother accepts any numerical series.

### Branches to cover (≥90/90)

Most branches already covered by the internal SuperSmoother tests from Tier 2c. Additional tests for the public wrapper:

1. **Null input** → throw
2. **Empty input** → empty output
3. **`period < 2`** → throw
4. **Delegate call** — verify output matches internal helper exactly

### Test vectors

Minimal — rely on the internal helper's tests. Just verify the wrapper doesn't drop values or alter behavior.

### AxesBuilder shortcut

```csharp
public AxesBuilder SuperSmoother(double[] input, int period = 10,
    Action<Indicators.EhlersSuperSmoother>? configure = null) { /* template */ }
```

### Panel placement

Varies — depends on what's being smoothed. Typically renders wherever the source signal renders, with identical scale.

---

## 4. Transfer Entropy (cross-asset)

**Information-theoretic measure of directional influence** from one time series to another. Unlike correlation (symmetric, linear), transfer entropy captures **asymmetric, nonlinear information flow** — does BTC lead ETH, or vice versa? Does dollar-index lead gold?

Heavy math, but foundational for cross-asset research. Used in Lopez de Prado 2018 Ch. 8 for feature importance across related assets.

### Formula

Given two aligned series X and Y, lag τ, binning with k bins:

```
# Discretize both series into k bins (equal-width or quantile-based)
x_bin = binning(X, k)
y_bin = binning(Y, k)

# Transfer entropy from X to Y at lag τ:
TE(X → Y, τ) = Σ p(y_{t+1}, y_t, x_{t-τ+1}) ·
               log( p(y_{t+1} | y_t, x_{t-τ+1}) / p(y_{t+1} | y_t) )

# Where all probabilities are estimated from joint histograms over the binned data
```

Symbolically: **"how much extra information about Y's future you get from knowing X's past, beyond what Y's own past tells you."**

Output ≥ 0, in nats (natural-log units). TE(X→Y) and TE(Y→X) typically asymmetric — that asymmetry is the whole point.

**Source:** Schreiber, T. (2000). *Measuring Information Transfer*. Physical Review Letters 85(2), 461. Applied to finance: Marschinski & Kantz (2002), Kwon & Yang (2008).

### Signature

```csharp
public sealed class TransferEntropy : Indicator<SignalResult>
{
    private readonly double[] _source;
    private readonly double[] _target;
    private readonly int _bins;
    private readonly int _lag;

    public TransferEntropy(double[] source, double[] target, int bins = 8, int lag = 1)
    {
        if (source is null || target is null) throw new ArgumentException("source and target required");
        if (source.Length != target.Length) throw new ArgumentException("source and target must be same length");
        if (bins < 2) throw new ArgumentException("bins >= 2", nameof(bins));
        if (lag < 1) throw new ArgumentException("lag >= 1", nameof(lag));
        _source = source;
        _target = target;
        _bins = bins;
        _lag = lag;
        Label = $"TE(bins={bins},lag={lag})";
    }

    public override SignalResult Compute()
    {
        // Returns a SINGLE-ELEMENT array — transfer entropy is a scalar over the window.
        // Rolling-window version could be a future addition.
        return new double[] { ComputeScalar() };
    }

    internal double ComputeScalar() { /* full TE formula */ }

    public override void Apply(Axes axes)
    {
        // Single value → render as a bar or annotation, not a line
        // For now: no default render; users typically compute and display as a number
    }
}
```

**Note:** Unlike other indicators, TE produces a **scalar** (not a series). Future expansion: rolling-window TE that produces a series. For v1.9.0, scalar is enough.

### Branches to cover (≥90/90)

1. **Null inputs** → throw
2. **Length mismatch** → throw
3. **Length < lag + 2** → throw (can't form triples)
4. **`bins < 2`** → throw
5. **`lag < 1`** → throw
6. **Identical series** (X == Y) → TE should be ≈ H(Y|past Y) — non-zero but bounded
7. **Independent series** (X uncorrelated to Y) → TE ≈ 0
8. **Pure causation** (Y_t = f(X_{t-1})) → TE(X→Y) large, TE(Y→X) small
9. **Edge case: all values in one bin** → entropy = 0 → TE = 0

### Test vectors

```csharp
// Independent random series → TE ≈ 0
var rng = new Random(42);
var x = Enumerable.Range(0, 1000).Select(_ => rng.NextDouble()).ToArray();
var y = Enumerable.Range(0, 1000).Select(_ => rng.NextDouble()).ToArray();
var te = new TransferEntropy(x, y, bins: 8, lag: 1).Compute();
te[0].ShouldBeLessThan(0.05);  // low TE for independent series

// Synthetic causation: Y_t = X_{t-1}
var causal_y = new double[1000];
for (int i = 1; i < 1000; i++) causal_y[i] = x[i-1];
var teCausal = new TransferEntropy(x, causal_y, bins: 8, lag: 1).Compute();
teCausal[0].ShouldBeGreaterThan(0.5);  // high TE when Y really does depend on X

// Reverse: TE(Y→X) should be much smaller
var teReverse = new TransferEntropy(causal_y, x, bins: 8, lag: 1).Compute();
teReverse[0].ShouldBeLessThan(teCausal[0] * 0.3);
```

### AxesBuilder shortcut

```csharp
public AxesBuilder TransferEntropy(double[] source, double[] target, int bins = 8, int lag = 1,
    Action<Indicators.TransferEntropy>? configure = null) { /* template */ }
```

### Panel placement

Scalar output — no line rendering. Callers typically:
1. Compute TE(X→Y) and TE(Y→X) separately
2. Display as annotations on an existing chart, or as text labels
3. Or: use the scalar output as a cross-asset feature in ML models

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/EhlersITrendTests.cs`
- `Tst/MatPlotLibNet/Indicators/DecyclerTests.cs`
- `Tst/MatPlotLibNet/Indicators/EhlersSuperSmootherTests.cs`
- `Tst/MatPlotLibNet/Indicators/TransferEntropyTests.cs`

Four test files. SuperSmoother wrapper tests are minimal (delegate test only) because the internal helper has its own tests from Tier 2c.

---

## Coverage verification

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass on all four classes.

**Budget:** Transfer Entropy is the hardest (new math territory, histogram-based probability estimation). The three Ehlers extensions reuse Tier 2c infrastructure and are quick. Plan ~1.5× the time of Tier 3a for this PR, mostly due to TE.

---

## PR checklist

- [ ] 4 indicator classes under `Src/MatPlotLibNet/Indicators/`
- [ ] 4 AxesBuilder shortcuts (alphabetical)
- [ ] 4 test files
- [ ] Python reference for TE (use `pyinform` or `copent` library): commit expected scalar values for known independent/causal pairs
- [ ] `SuperSmoother` and `HighPassFilter` helpers from Tier 2c confirmed accessible (grep for them)
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] Changelog entry under `v1.9.0`: "Added Ehlers iTrend (2001), Decycler (2015), SuperSmoother (public exposure, Ehlers 2013), Transfer Entropy (Schreiber 2000)"
- [ ] Wiki updated
- [ ] **v1.9.0 release notes** summarizing all 12 Tier 3 indicators (3a-3c combined narrative)

## What's NOT in this PR

- **Rolling-window Transfer Entropy** — TE as a time-series rather than single scalar. Defer to v1.10 when streaming indicators are fleshed out.
- **Kraskov-Stögbauer-Grassberger TE** — continuous-variable TE estimator (more accurate than binned). Heavy implementation, defer.
- **Conditional Transfer Entropy** (controlling for a third variable) — academic extension, defer.

---

## Motivation / why these four together

**Tier 3c closes v1.9.0** by filling two specific gaps:

1. **Remaining Ehlers corpus.** Tier 1d + 2c covered MAMA/FAMA, Laguerre RSI, Cyber Cycle, Roofing, Sinewave, Adaptive Stoch. Tier 3c adds iTrend, Decycler, and public SuperSmoother — **finishing the Ehlers set**. After this, MatPlotLibNet carries 9 Ehlers indicators, more than any .NET library.

2. **Cross-asset analysis.** Transfer Entropy is the standard nonlinear-causality measure in quant research. No .NET lib has it. Shipping it makes MatPlotLibNet the first .NET choice for multi-asset studies.

**After 3c lands, v1.9.0 ships as a complete 12-indicator release:**

- **Tier 3a** Volume (4): Klinger, Twiggs MF, Ease of Movement, VWAP Z-Score
- **Tier 3b** Trend/Transform (4): Supertrend, CG Oscillator, Inverse Fisher, YZ Vol Ratio
- **Tier 3c** Advanced (4): Ehlers iTrend, Decycler, SuperSmoother (public), Transfer Entropy

**v1.9.0 total: 12 indicators. Combined with v1.8.0's 24: 36 production-grade indicators in 2026.**

Release narrative for v1.9.0 NuGet push:
*"Volume analytics, trend-following primitives, transform meta-indicators, the complete Ehlers corpus, and cross-asset causality via transfer entropy — 12 new indicators bringing the 2026 total to 36. The most complete quant-grade indicator library in the .NET ecosystem."*

That's the pitch for the v1.9.0 announcement — and it's earned, not marketing.
