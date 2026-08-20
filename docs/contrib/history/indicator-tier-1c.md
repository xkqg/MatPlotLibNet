> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 1c (Microstructure, OHLC + Volume)

Follows the patterns established by Tier 1a and 1b. Scope: **four microstructure indicators** — liquidity, spread, informed-trading, and autocovariance-spread estimators. All derive from OHLC + Volume alone (no tick-level data needed).

**Target:** merge into `main` for v1.8.0, after Tier 1b.

**Coverage gate:** ≥90% line AND ≥90% branch per public class, enforced per `docs/COVERAGE.md`. Follow branch enumerations; `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** validate structural + semantic inputs at the boundary (throw on bad config / data), handle known math degeneracies explicitly via enumerated branches, but **do not** add per-element `IsNaN` / `IsInfinity` guards in compute loops. Sanitization belongs at the rendering boundary. See `indicator-tier-1d.md` for the full policy — same rules apply to every tier.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Amihud Illiquidity** | Microstructure | Close + Volume + window | Series (illiquidity) | Separate subplot |
| 2 | **Corwin-Schultz Spread** | Microstructure | High, Low + window | Series (spread %) | Separate subplot |
| 3 | **VPIN (BVC)** | Microstructure | Close + Volume + window + sigma window | Series (0–1) | Separate subplot |
| 4 | **Roll Spread** | Microstructure | Close + window | Series (spread) | Separate subplot |

All inherit `CandleIndicator<SignalResult>` (1, 2, 3) or `PriceIndicator<SignalResult>` (4). Match the pattern in [`Src/MatPlotLibNet/Indicators/Atr.cs`](../../Src/MatPlotLibNet/Indicators/Atr.cs) and [`Rsi.cs`](../../Src/MatPlotLibNet/Indicators/Rsi.cs).

---

## 1. Amihud Illiquidity

Ratio of absolute return to dollar volume. High = illiquid (large price moves per unit volume); low = deep, liquid market. Foundational for market-microstructure analysis; the most-cited illiquidity measure in academic finance.

### Formula

For each bar:
```
ILLIQ_t  =  |r_t|  /  DollarVolume_t
  where r_t = ln(Close_t / Close_{t-1})
        DollarVolume_t = Close_t × Volume_t
```

Rolling window average:
```
Output_t = mean(ILLIQ) over bars [t-N+1 .. t]
```

**Source:** Amihud, Y. (2002). *Illiquidity and stock returns: cross-section and time-series effects*. Journal of Financial Markets, 5(1), 31–56.

### Signature

```csharp
public sealed class AmihudIlliquidity : CandleIndicator<SignalResult>
{
    private readonly int _period;

    public AmihudIlliquidity(double[] close, double[] volume, int period = 20)
        : base([], [], [], close, volume) { /* Label = $"ILLIQ({period})" */; _period = period; }

    public override SignalResult Compute() { /* returns double[n - period] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period);
}
```

**Note on warmup:** needs prev-close for returns → first ILLIQ at index 1, rolling window needs `period` samples → first output at index `period`. Output length = `close.Length - period`.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **`close.Length != volume.Length`** → throw `ArgumentException`
3. **Length <= period** → empty output
4. **Length == period + 1** → boundary, one output
5. **`period <= 0`** → throw `ArgumentException`
6. **Zero volume** on any bar in window → **skip that bar's contribution** (avoid divide-by-zero); requires at least one non-zero-volume bar in the window or output is `double.NaN` → **policy: return `double.PositiveInfinity`** (matches academic convention: no volume = maximally illiquid)
7. **Non-positive close** (`close[t] <= 0`) → throw `ArgumentException` (log undefined)
8. **Flat prices** — all equal → all returns 0 → ILLIQ = 0 per bar → output 0, not NaN
9. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices → all zeros
var flat = new AmihudIlliquidity([100, 100, 100, 100, 100], [1000, 1000, 1000, 1000, 1000], period: 3).Compute();
flat.ShouldAllBe(v => v == 0);

// Known vector (period=3, hand-computed):
// close: [100, 101, 103, 102, 105]
// volume: [1000, 1000, 500, 1500, 1000]
// returns (log, skipping index 0): [0.00995, 0.01961, -0.00974, 0.02899]
// |r|/($vol) per bar (after idx 0):
//   1: 0.00995 / (101*1000)   = 9.851e-8
//   2: 0.01961 / (103*500)    = 3.807e-7
//   3: 0.00974 / (102*1500)   = 6.366e-8
//   4: 0.02899 / (105*1000)   = 2.761e-7
// Rolling mean, window=3 (first output at idx 3):
//   Output[0] = (9.851e-8 + 3.807e-7 + 6.366e-8) / 3 = 1.810e-7
//   Output[1] = (3.807e-7 + 6.366e-8 + 2.761e-7) / 3 = 2.402e-7
// Verify via Python reference snippet.

// Zero volume → PositiveInfinity
var zv = new AmihudIlliquidity([100, 101], [1000, 0], period: 1).Compute();
zv[0].ShouldBe(double.PositiveInfinity);

// Length mismatch → throw
Assert.Throws<ArgumentException>(() => new AmihudIlliquidity([100, 101], [1000], period: 1));

// period <= 0 → throw
Assert.Throws<ArgumentException>(() => new AmihudIlliquidity([100, 101, 102], [1, 1, 1], period: 0));
```

### AxesBuilder shortcut

```csharp
/// <summary>Adds an Amihud illiquidity panel indicator.</summary>
public AxesBuilder AmihudIlliquidity(double[] close, double[] volume, int period = 20,
                                      Action<Indicators.AmihudIlliquidity>? configure = null)
{
    var indicator = new Indicators.AmihudIlliquidity(close, volume, period);
    if (IsBarSlotContext()) indicator.Offset = 0.5;
    configure?.Invoke(indicator);
    indicator.Apply(_axes);
    return this;
}
```

### Panel placement

Separate subplot. Often displayed on log-scale Y (values can vary by orders of magnitude across regimes). No hard Y-range.

---

## 2. Corwin-Schultz Bid-Ask Spread Estimator

Recovers the bid-ask spread from daily H/L only — no tick data required. Exploits the property that the overnight close-to-open move reflects spread + drift, while intraday H/L reflects spread + drift + volatility. Two consecutive bars decomposes spread from volatility.

### Formula

For two consecutive bars (t-1, t):
```
β_t  = [ln(H_{t-1}/L_{t-1})]²  +  [ln(H_t/L_t)]²
γ_t  = [ln(max(H_{t-1}, H_t) / min(L_{t-1}, L_t))]²

α_t  =  (√(2β_t) − √β_t) / (3 − 2√2)  −  √(γ_t / (3 − 2√2))

S_t  =  2 · (exp(α_t) − 1) / (1 + exp(α_t))
```

If `S_t < 0`, clamp to 0 (invalid estimate, policy per Corwin-Schultz §II.D).

Output = rolling mean of `S_t` over the window.

**Source:** Corwin, S. A., & Schultz, P. (2012). *A simple way to estimate bid-ask spreads from daily high and low prices*. Journal of Finance, 67(2), 719–760.

### Signature

```csharp
public sealed class CorwinSchultz : CandleIndicator<SignalResult>
{
    private readonly int _period;

    public CorwinSchultz(double[] high, double[] low, int period = 20)
        : base([], high, low, [], []) { /* Label = $"CS({period})" */; _period = period; }

    public override SignalResult Compute() { /* returns double[n - period] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period);
}
```

**Note on warmup:** per-bar spread `S_t` needs bar `t-1`, first valid at index 1. Rolling window needs `period` consecutive `S_t` samples. Output length = `high.Length - period`.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **`high.Length != low.Length`** → throw `ArgumentException`
3. **Length <= period** → empty output
4. **Length == period + 1** → boundary, one output
5. **`period < 2`** → throw `ArgumentException` (need ≥2 for rolling estimate to make sense)
6. **Non-positive H or L** → throw `ArgumentException` (log undefined)
7. **H < L** on any bar → throw `ArgumentException` (data corrupt)
8. **Negative α** → `S_t < 0` → clamp to 0, verify the clamp branch is exercised
9. **Flat H == L** — zero range → β = 0, γ = 0 → α → −∞ → `S_t → −2` → clamped to 0
10. **Normal multi-bar path**

### Test vectors

```csharp
// Flat H == L → all zeros (clamp branch)
var flat = new CorwinSchultz([100, 100, 100, 100], [100, 100, 100, 100], period: 2).Compute();
flat.ShouldAllBe(v => v == 0);

// Known canonical: S&P 500 example from Corwin-Schultz 2012 Table 1
// Or build via Python reference:
//   import numpy as np
//   def cs_spread(h, l):
//       beta = np.log(h[1:]/l[1:])**2 + np.log(h[:-1]/l[:-1])**2
//       hh = np.maximum(h[1:], h[:-1]); ll = np.minimum(l[1:], l[:-1])
//       gamma = np.log(hh/ll)**2
//       denom = 3 - 2*np.sqrt(2)
//       alpha = (np.sqrt(2*beta) - np.sqrt(beta))/denom - np.sqrt(gamma/denom)
//       s = 2*(np.exp(alpha) - 1)/(1 + np.exp(alpha))
//       return np.where(s<0, 0, s)
// Commit ≥3 expected values to 8 decimals with a hand-picked HL sample.

// H < L → throw
Assert.Throws<ArgumentException>(() => new CorwinSchultz([100, 99], [101, 100], period: 2).Compute());

// period < 2 → throw
Assert.Throws<ArgumentException>(() => new CorwinSchultz([100, 101, 102], [99, 100, 101], period: 1));
```

### AxesBuilder shortcut

Follows the same pattern — `CorwinSchultz(high, low, period)`. Copy-paste from `AmihudIlliquidity` above, adjust parameters.

### Panel placement

Separate subplot. Y-axis is **percentage** spread (S_t ∈ [0, 2) theoretically; values rarely exceed 0.03 = 3%). Label `CS({period})`.

---

## 3. VPIN — Volume-Synchronized Probability of Informed Trading (BVC approximation)

Estimates the probability that any given trade is from an informed trader. High VPIN precedes liquidity crises (the May 2010 Flash Crash spiked VPIN to >0.8 in the hours before the crash). Uses **Bulk Volume Classification (BVC)** — doesn't need trade-sign data, derives buy/sell split from return-normalized volume.

### Formula

Bulk Volume Classification:
```
z_t = (r_t - μ_r) / σ_r             ← standardize returns
BuyVolume_t  = Volume_t × Φ(z_t)     ← Φ = standard normal CDF
SellVolume_t = Volume_t − BuyVolume_t
```

Where `μ_r`, `σ_r` are rolling return mean and std over a normalization window (typically the same as or larger than the VPIN window).

VPIN over a window of N bars:
```
VPIN_t = Σ_{i=t-N+1}^{t} |BuyVolume_i − SellVolume_i|   /   Σ_{i=t-N+1}^{t} Volume_i
```

Output range: [0, 1]. Typical equilibrium: 0.2–0.4. Pre-crisis spike: >0.5.

**Source:** Easley, D., Lopez de Prado, M., & O'Hara, M. (2012). *Flow toxicity and liquidity in a high frequency world*. Review of Financial Studies, 25(5), 1457–1493. BVC approximation from same authors, §3.

### Signature

```csharp
public sealed class Vpin : CandleIndicator<SignalResult>
{
    private readonly int _bucketPeriod;
    private readonly int _sigmaPeriod;

    public Vpin(double[] close, double[] volume, int bucketPeriod = 50, int sigmaPeriod = 50)
        : base([], [], [], close, volume)
    {
        _bucketPeriod = bucketPeriod;
        _sigmaPeriod = sigmaPeriod;
        Label = $"VPIN({bucketPeriod})";
    }

    public override SignalResult Compute() { /* returns double[n - max(bucket,sigma) - 1] */ }
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), Math.Max(_bucketPeriod, _sigmaPeriod) + 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

**Warmup:** max(bucketPeriod, sigmaPeriod) + 1 (returns need prev-close).

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **`close.Length != volume.Length`** → throw
3. **`bucketPeriod <= 0` or `sigmaPeriod <= 0`** → throw
4. **Length <= max(bucketPeriod, sigmaPeriod)** → empty output
5. **Non-positive close** → throw
6. **Zero total bucket volume** → return 0 (not NaN) via explicit guard
7. **`σ_r == 0`** (all returns identical within sigma window) — z_t undefined (0/0) → policy: treat all bars as buy (`BuyVolume = Volume`) → VPIN = 0 for that bucket
8. **Flat prices** — all returns 0, σ_r = 0 → VPIN = 0 throughout
9. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices → VPIN = 0 (σ=0 branch, all volume classified as buy by convention)
var flat = new Vpin(
    [100, 100, 100, 100, 100, 100, 100, 100],
    [1000, 1000, 1000, 1000, 1000, 1000, 1000, 1000],
    bucketPeriod: 3, sigmaPeriod: 3).Compute();
flat.ShouldAllBe(v => v == 0);

// Known vector — Python reference:
//   import numpy as np
//   from scipy.stats import norm
//   def vpin(close, volume, N, sig_N):
//       r = np.diff(np.log(close))
//       # rolling std over sig_N
//       # ...
// Commit ≥3 expected values with a hand-picked series.

// Zero total bucket volume → 0 (guard branch)
// bucketPeriod = 3, all volumes = 0 in a bucket window → ensure no divide-by-zero, output = 0
```

### AxesBuilder shortcut

```csharp
public AxesBuilder Vpin(double[] close, double[] volume, int bucketPeriod = 50, int sigmaPeriod = 50,
                        Action<Indicators.Vpin>? configure = null) { /* copy-paste template */ }
```

### Panel placement

Separate subplot with Y-range `[0, 1]`. Typical threshold markers (do NOT hardcode in indicator — let callers add):
- `AxHLine(0.4)` — elevated toxicity
- `AxHLine(0.6)` — critical threshold (pre-crisis)

---

## 4. Roll Spread Estimator

Recovers the effective bid-ask spread from **serial autocovariance of price changes** — without any quote data. Simpler than Corwin-Schultz (needs only close), but works only when `Cov(Δp_t, Δp_{t-1}) < 0` (the bid-ask bounce signature). When the covariance is non-negative, the spread is not identifiable from closes alone → returns 0.

### Formula

For a rolling window of size N:
```
Δp_t = Close_t − Close_{t-1}
cov  = E[(Δp_t − Δp̄)(Δp_{t-1} − Δp̄)]    ← first-order serial covariance over window

S = 2 · √(−cov)    if cov < 0
  = 0               otherwise
```

Output = `S` per window end.

**Source:** Roll, R. (1984). *A Simple Implicit Measure of the Effective Bid-Ask Spread in an Efficient Market*. Journal of Finance, 39(4), 1127–1139.

### Signature

```csharp
public sealed class RollSpread : PriceIndicator<SignalResult>
{
    private readonly int _period;

    public RollSpread(double[] prices, int period = 20) : base(prices)
    {
        _period = period;
        Label = $"Roll({period})";
    }

    public override SignalResult Compute() { /* returns double[n - period - 1] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period + 1);
}
```

**Warmup:** `_period + 1` — needs two consecutive returns per window end.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length <= period + 1** → empty output
3. **Length == period + 2** → boundary, one output
4. **`period < 2`** → throw `ArgumentException` (covariance needs ≥2 return pairs)
5. **Non-positive price** — **not** a throw here (Roll uses raw differences, not logs) — but document that data should be positive prices
6. **Flat prices** — all Δp = 0 → cov = 0 → S = 0 (not NaN; division-by-zero-avoided branch)
7. **`cov >= 0`** — positive autocorrelation → S = 0 (non-identifiable branch)
8. **`cov < 0`** — normal branch → S = 2√(−cov)
9. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices → S = 0
var flat = new RollSpread([100, 100, 100, 100, 100, 100], period: 3).Compute();
flat.ShouldAllBe(v => v == 0);

// Bid-ask bounce simulation:
// prices alternate: 100.0, 100.2, 100.0, 100.2, 100.0, 100.2  (bounce between bid and ask)
// Δp: [+0.2, -0.2, +0.2, -0.2, +0.2]
// cov(Δp_t, Δp_{t-1}) ≈ -0.04  (perfectly anti-correlated)
// S ≈ 2 · √0.04 = 0.4  (exactly the spread)
var bounce = new RollSpread([100.0, 100.2, 100.0, 100.2, 100.0, 100.2], period: 4).Compute();
bounce[0].ShouldBe(0.4, 0.01);

// Monotonic rise (positive autocorrelation) → S = 0 (non-identifiable branch)
var rising = new RollSpread([100, 101, 102, 103, 104, 105, 106], period: 4).Compute();
rising.ShouldAllBe(v => v == 0);

// period < 2 → throw
Assert.Throws<ArgumentException>(() => new RollSpread([100, 101, 102], period: 1));
```

### AxesBuilder shortcut

Same pattern as the others: `RollSpread(prices, period)`.

### Panel placement

Separate subplot. Y-axis auto (spread in price units). Label `Roll({period})`.

---

## Test file structure

One test file per indicator under `Tst/MatPlotLibNet/Indicators/`:
- `AmihudIlliquidityTests.cs`
- `CorwinSchultzTests.cs`
- `VpinTests.cs`
- `RollSpreadTests.cs`

xUnit v3 `[Theory]` + `[InlineData]` for simple boundary cases; `[MemberData]` for multi-bar regression vectors generated via Python. Name each row after the branch it exercises.

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `AmihudIlliquidity`: ≥90/90
- `CorwinSchultz`: ≥90/90
- `Vpin`: ≥90/90
- `RollSpread`: ≥90/90

Every branch in the enumeration above must have a dedicated test row. Do not add threshold exemptions.

---

## PR checklist

- [ ] 4 indicator classes under `Src/MatPlotLibNet/Indicators/`
- [ ] 4 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 4 test files under `Tst/MatPlotLibNet/Indicators/`, covering all branches
- [ ] Reference vectors committed with Python-snippet comments
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] No `thresholds.json` changes
- [ ] Changelog entry under `v1.8.0`: "Added Amihud illiquidity (2002), Corwin-Schultz spread (2012), VPIN / BVC (Easley-Lopez de Prado-O'Hara 2012), Roll spread (1984)"
- [ ] Wiki Indicators page updated with usage snippets

## What's NOT in this PR

Tier 1d (Ehlers MAMA/FAMA + Laguerre RSI), Tier 1e streaming variants. All four of Tier 1c have meaningful streaming potential (each is O(1)-appendable with ring-buffer state), but defer to Tier 1e for consistency.

---

## Motivation / why these four together

Microstructure is the most underserved category in mainstream charting libraries — no .NET lib carries these four, and only a handful of Python packages (mostly research-quality, not production). Together they answer four distinct quant questions:

- **"How liquid is this market right now?"** → Amihud
- **"What's the effective spread without quote data?"** → Corwin-Schultz (uses HL), Roll (uses close only, weaker but broader applicability)
- **"Is toxic order flow building up?"** → VPIN (the Flash Crash early-warning indicator)

Shipping all four in v1.8.0 establishes MatPlotLibNet as the first .NET library with production-ready microstructure indicators — a niche strongly in demand from quant research desks and crypto-exchange analytics teams.
