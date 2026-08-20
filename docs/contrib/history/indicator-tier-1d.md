> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 1d (Adaptive + Volatility Breakout)

Final tier of **Tier 1**. Three indicators: two adaptive (MAMA/FAMA, Laguerre RSI) + one volatility-breakout (Squeeze Momentum LinReg). All close-only. After 1d lands, **Tier 1 is complete** — 12 indicators total across 1a–1d covering the high-impact OHLCV-only space.

**Target:** merge into `main` for v1.8.0 release as the final Tier 1 PR.

**Coverage gate:** ≥90% line AND ≥90% branch per public class. These three have more internal state than 1a–1c — read branch enumeration carefully before implementing.

---

## NaN / ±∞ policy (applies to all indicators in this tier)

**Do not add blanket `double.IsNaN` / `double.IsInfinity` guards inside compute loops.** NaN or ±∞ in the input arrays propagates through the math — sanitization is the **caller's responsibility**, handled at the rendering boundary (SeriesRenderer skips NaN points when drawing, computes safe axis bounds).

What indicators **do** validate (at construction or at compute entry — the boundary):

1. **Structural**: null/empty arrays, length mismatches, parameter ranges (period ≥ 2, alpha ∈ (0,1), etc.) → throw `ArgumentException`
2. **Semantic preconditions**: non-positive prices for log-based formulas, `H < L` for OHLC data → throw `ArgumentException`

What indicators **do** handle explicitly via math branches (enumerated per indicator below):

- Division-by-zero guards that produce a meaningful zero (Laguerre CU+CD=0, Kaufman volatility=0, Roll cov≥0)
- Degenerate math policies (MAMA/FAMA atan2 fallback to previous, Amihud zero-volume → +∞)
- Flat-input corner cases (all outputs zero, no NaN)

What indicators **do not** do:

- Check `IsNaN(price)` / `IsInfinity(price)` on every input element
- Pre-scan input arrays for badness
- Coerce NaN to 0 silently

**Rationale**: NaN in input is an upstream data-pipeline bug (corrupted DB row, failed API call, parser glitch). The indicator's job is to fail loudly so the bug is visible, not to silently paper over it. Adding per-element NaN checks costs ~2–3× perf on hot paths (the Hilbert transform in MAMA/FAMA, the permutation window in VPIN) and hides the real bug. The rendering layer already skips NaN points visually — that's where finite-value guarantees are needed, not in the indicator.

**If** a specific use case requires NaN-skip behavior (e.g. a sparse feed where missing bars = NaN), expose it as an explicit opt-in constructor flag — **not** as default behavior. Deferred to v1.8.0+.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Squeeze Momentum LinReg** | Volatility / Breakout | OHLC + BB period + KC period | `(squeezeOn, momentum)` per bar | Separate subplot |
| 2 | **MAMA/FAMA** | Adaptive MA | Close + fastLimit + slowLimit | `(mama, fama)` lines | Overlay on price |
| 3 | **Laguerre RSI** | Adaptive Oscillator | Close + alpha | Series in [0, 1] | Separate subplot |

All three inherit `PriceIndicator<TResult>` (2, 3) or `CandleIndicator<TResult>` (1 — uses HLC for Keltner ATR). Each has its own result record because the outputs aren't a plain `SignalResult`.

---

## 1. Squeeze Momentum LinReg (LazyBear)

**LazyBear's** Squeeze Momentum indicator — fires when Bollinger Bands contract inside Keltner Channels (low-volatility coiling → breakout imminent). Momentum direction is predicted via **linear regression** of close against the Donchian midpoint, revealing which direction the breakout is likely to go.

### Formula

Over rolling window `N` (typical 20):

```
# Bollinger Bands (std-dev based)
basis      = SMA(close, N)
dev        = bbMult × stddev(close, N)
bb_upper   = basis + dev
bb_lower   = basis - dev

# Keltner Channels (ATR based)
kc_basis   = SMA(close, N)
rangeMA    = SMA(trueRange, N)
kc_upper   = kc_basis + kcMult × rangeMA
kc_lower   = kc_basis - kcMult × rangeMA

# Squeeze state
squeezeOn  = (bb_lower > kc_lower) && (bb_upper < kc_upper)   # BB inside KC → squeeze on

# Momentum: linear regression of (close − reference_midpoint) over window
midHL      = (highest(high, N) + lowest(low, N)) / 2
midRef     = (midHL + SMA(close, N)) / 2
momentum   = LinReg(close − midRef, window=N, offset=0)
```

Output = per-bar `(squeezeOn: bool, momentum: double)`. Typical use: fire signal when `squeezeOn` flips from `true → false` AND `momentum > 0` (long) or `momentum < 0` (short).

**Source:** LazyBear (TradingView, 2014), *Squeeze Momentum Indicator*. Based on John Carter's squeeze concept (*Mastering the Trade*, 2005).

### Output record

```csharp
public readonly record struct SqueezeResult(bool[] SqueezeOn, double[] Momentum) : IIndicatorResult;
```

Commit to `Src/MatPlotLibNet/Indicators/SqueezeResult.cs`.

### Signature

```csharp
public sealed class SqueezeMomentum : CandleIndicator<SqueezeResult>
{
    private readonly int _period;
    private readonly double _bbMult;
    private readonly double _kcMult;

    public SqueezeMomentum(double[] high, double[] low, double[] close,
                           int period = 20, double bbMult = 2.0, double kcMult = 1.5)
        : base([], high, low, close, [])
    {
        _period = period;
        _bbMult = bbMult;
        _kcMult = kcMult;
        Label = $"Squeeze({period})";
    }

    public override SqueezeResult Compute() { /* returns parallel arrays length = n - period */ }
    public override void Apply(Axes axes)
    {
        var result = Compute();
        // Momentum line with color-by-sign; squeezeOn as markers along x-axis
        // or as a separate stem overlay with binary height.
    }
}
```

**Note on warmup:** needs `period` bars for SMA + `period` bars for TR average + 1 bar prior for TR → first valid at index `period + 1`. Output length = `close.Length - period - 1`.

### Branches to cover (≥90/90)

1. **Empty input** → both arrays empty
2. **HLC length mismatch** → throw `ArgumentException`
3. **`period <= 1`** → throw (need ≥2 for stddev / linreg)
4. **`bbMult <= 0` or `kcMult <= 0`** → throw
5. **Length <= period + 1** → empty output
6. **Length == period + 2** → boundary, one output row
7. **Non-positive price** — not strictly required (stddev works on any series), but validate HLC consistency (H >= L) → throw on violation
8. **Flat prices** — stddev = 0, TR = 0 → BB collapses to basis, KC collapses to basis → `squeezeOn = (basis > basis) && (basis < basis) = false`; momentum = 0 (LinReg on zero series)
9. **Strong squeeze** — synthetic narrow-range window → `squeezeOn = true`
10. **After squeeze release** — widening range → `squeezeOn` flips from true to false; verify transition detected
11. **Pure uptrend** — `momentum > 0`
12. **Pure downtrend** — `momentum < 0`

### LinReg helper

Extract a tiny `LinReg` helper **internal static** inside the indicator (or reuse an existing helper if one exists — grep `LinearRegression` in the repo before adding). The slope over a window of `y` against `x = [0, 1, …, N-1]`:

```
slope = (N · Σ(x·y) − Σx · Σy) / (N · Σ(x²) − (Σx)²)
```

Return the slope as the momentum value, signed.

### Test vectors

```csharp
// Flat prices → squeezeOn=false per tie-break (strict <), momentum=0
var flat = new SqueezeMomentum(
    [100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100],
    Enumerable.Repeat(100.0, 22).ToArray(),
    Enumerable.Repeat(100.0, 22).ToArray(),
    period: 20).Compute();
flat.SqueezeOn.ShouldAllBe(v => v == false);
flat.Momentum.ShouldAllBe(v => v == 0.0);

// Python reference for a canonical squeeze scenario:
// import pandas as pd, numpy as np
// (construct a narrow-range series followed by widening)
// Commit ≥3 expected (squeezeOn, momentum) tuples with tolerance 1e-6.

// H < L → throw
Assert.Throws<ArgumentException>(() =>
    new SqueezeMomentum([101, 102], [102, 103], [100, 101], period: 2));

// period < 2 → throw
Assert.Throws<ArgumentException>(() =>
    new SqueezeMomentum([102, 103], [100, 101], [101, 102], period: 1));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder SqueezeMomentum(double[] high, double[] low, double[] close,
    int period = 20, double bbMult = 2.0, double kcMult = 1.5,
    Action<Indicators.SqueezeMomentum>? configure = null)
{
    var indicator = new Indicators.SqueezeMomentum(high, low, close, period, bbMult, kcMult);
    if (IsBarSlotContext()) indicator.Offset = 0.5;
    configure?.Invoke(indicator);
    indicator.Apply(_axes);
    return this;
}
```

### Panel placement

Separate subplot. Momentum as a histogram (positive green, negative red). Optional dot markers along the zero-line for `squeezeOn == true`.

---

## 2. MAMA / FAMA (Mesa Adaptive Moving Average)

Ehlers' flagship adaptive MA. Uses the **Hilbert transform** to extract the instantaneous phase of the price series, then derives the dominant cycle period, then adapts the EMA smoothing factor `alpha` to that period. During trending regimes, alpha is high (tracks fast); during ranging regimes, alpha is low (filters noise). FAMA (Following Adaptive Moving Average) lags MAMA by half — crossovers are entry/exit signals.

### Formula (outline — full recurrence in reference below)

```
# Pre-processing
Price_t     = (H_t + L_t) / 2   # median price (or use close if HL unavailable)
Smooth_t    = (4·P_t + 3·P_{t-1} + 2·P_{t-2} + P_{t-3}) / 10

# Hilbert transform — detrends, extracts in-phase (I) and quadrature (Q)
Detrender_t = (0.0962·Smooth_t + 0.5769·Smooth_{t-2} − 0.5769·Smooth_{t-4} − 0.0962·Smooth_{t-6}) × (0.075·Period_{t-1} + 0.54)
Q1_t        = (0.0962·Detrender_t + ... same kernel ...) × (0.075·Period_{t-1} + 0.54)
I1_t        = Detrender_{t-3}

jI_t        = (0.0962·I1_t + ...) × (0.075·Period_{t-1} + 0.54)
jQ_t        = (0.0962·Q1_t + ...) × (0.075·Period_{t-1} + 0.54)

I2_t        = I1_t − jQ_t
Q2_t        = Q1_t + jI_t
I2_t        = 0.2·I2_t + 0.8·I2_{t-1}    # smoothing
Q2_t        = 0.2·Q2_t + 0.8·Q2_{t-1}

# Homodyne discriminator → dominant cycle period
Re_t        = I2_t·I2_{t-1} + Q2_t·Q2_{t-1}
Im_t        = I2_t·Q2_{t-1} − Q2_t·I2_{t-1}
Re_t        = 0.2·Re_t + 0.8·Re_{t-1}
Im_t        = 0.2·Im_t + 0.8·Im_{t-1}

if Im ≠ 0 and Re ≠ 0: Period = 2π / atan(Im / Re)
clamp Period to [0.67·Period_{t-1},  1.5·Period_{t-1}]
clamp Period to [6, 50]
Period_t    = 0.2·Period + 0.8·Period_{t-1}

# Phase-rate adaptive alpha
Phase_t     = atan(I1_t / Q1_t)   in degrees, handle Q1≈0
DeltaPhase  = Phase_{t-1} − Phase_t,  clamp to ≥1
alpha       = FastLimit / DeltaPhase,  clamp to [SlowLimit, FastLimit]

MAMA_t      = alpha·Price_t + (1 − alpha)·MAMA_{t-1}
FAMA_t      = 0.5·alpha·MAMA_t + (1 − 0.5·alpha)·FAMA_{t-1}
```

Defaults: `FastLimit = 0.5, SlowLimit = 0.05`.

**Source:** Ehlers, J. F. (2001). *Rocket Science for Traders*, Wiley, Ch. 15. Also *Cybernetic Analysis for Stocks and Futures* (2004), Ch. 14.

### Output record

```csharp
public readonly record struct MamaFamaResult(double[] Mama, double[] Fama) : IIndicatorResult;
```

Commit to `Src/MatPlotLibNet/Indicators/MamaFamaResult.cs`.

### Signature

```csharp
public sealed class MamaFama : PriceIndicator<MamaFamaResult>
{
    private readonly double _fastLimit;
    private readonly double _slowLimit;

    public MamaFama(double[] prices, double fastLimit = 0.5, double slowLimit = 0.05)
        : base(prices)
    {
        if (fastLimit <= 0 || fastLimit > 1) throw new ArgumentException("fastLimit in (0, 1]", nameof(fastLimit));
        if (slowLimit <= 0 || slowLimit >= fastLimit) throw new ArgumentException("slowLimit in (0, fastLimit)", nameof(slowLimit));
        _fastLimit = fastLimit;
        _slowLimit = slowLimit;
        Label = $"MAMA({fastLimit:0.00}/{slowLimit:0.00})";
    }

    public override MamaFamaResult Compute() { /* ... */ }

    public override void Apply(Axes axes)
    {
        var result = Compute();
        // Overlay on price panel: two lines (MAMA + FAMA), typically green + orange
        PlotSignal(axes, result.Mama, warmup: 6, label: "MAMA");
        PlotSignal(axes, result.Fama, warmup: 6, label: "FAMA");
    }
}
```

**Warmup:** 6 bars (the Hilbert transform kernel needs `t − 6`). Output length = `prices.Length - 6`.

**Implementation tip:** keep ALL state in local arrays (Smooth[], Detrender[], I1[], Q1[], I2[], Q2[], Re[], Im[], Period[], Phase[], MAMA[], FAMA[]) of length `prices.Length`. Fill in a single pass. Don't try to roll with circular buffers until after v1.8.0 ships — the memory cost is trivial (12 × 8B × n bars) and clarity matters for coverage testing.

### Branches to cover (≥90/90)

1. **Empty input** → both arrays empty
2. **Length < 7** (warmup barely met) → empty output
3. **Length == 7** → boundary, one output row
4. **`fastLimit <= 0`** → throw
5. **`fastLimit > 1`** → throw
6. **`slowLimit <= 0`** → throw
7. **`slowLimit >= fastLimit`** → throw
8. **Im == 0 and Re == 0** (degenerate atan2) → period falls back to previous; this branch must be explicitly tested with a synthetic series that causes Im=Re=0
9. **Q1 ≈ 0** (phase atan divide) — handle `Math.Atan2` correctly; test with a constant-I series
10. **Period clamp hit at upper bound** (1.5× prev) → test with a jump in cyclicality
11. **Period clamp hit at lower bound** (0.67× prev) → test with a jump
12. **Period clamp hit at absolute bounds** (6 and 50)
13. **DeltaPhase < 1 clamp** → synthetic stationary series
14. **Alpha clamp to FastLimit** (high DeltaPhase → alpha would be tiny) → test trending series
15. **Alpha clamp to SlowLimit** (low DeltaPhase → alpha would be huge) → test ranging series
16. **Flat prices** — all recurrences stay at zero → MAMA = FAMA = price (constant), no NaN/Inf

### Test vectors

```csharp
// Flat prices → MAMA = FAMA = price (everywhere, after warmup)
var flatPrices = Enumerable.Repeat(100.0, 100).ToArray();
var flatResult = new MamaFama(flatPrices).Compute();
flatResult.Mama.ShouldAllBe(v => Math.Abs(v - 100.0) < 1e-9);
flatResult.Fama.ShouldAllBe(v => Math.Abs(v - 100.0) < 1e-9);

// Monotonic — MAMA tracks fast, FAMA lags — known sine-wave regression test:
// Generate close = 100 + 10·sin(2π·i/20) for i in [0, 500]
// Compute via reference Python (TA-Lib MAMA or canonical Ehlers reference)
// Commit expected MAMA[30], MAMA[100], MAMA[499] to 6 decimals.

// ArgumentException branches
Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101.0], fastLimit: 0));
Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101.0], fastLimit: 1.5));
Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101.0], fastLimit: 0.5, slowLimit: 0));
Assert.Throws<ArgumentException>(() => new MamaFama([100.0, 101.0], fastLimit: 0.5, slowLimit: 0.5));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder MamaFama(double[] prices, double fastLimit = 0.5, double slowLimit = 0.05,
    Action<Indicators.MamaFama>? configure = null) { /* template */ }
```

### Panel placement

**Overlay on price panel** — MAMA + FAMA lines. Colors: MAMA = accent/green, FAMA = orange/amber. Typical crossover markers.

**Implementation warning:** this is the hardest of all four tier briefs. Plan ~2 days for careful implementation + coverage. Reference implementations: TA-Lib's `ta_MAMA.c` is the canonical cross-check; the Ehlers book's pseudocode has a few typos corrected by the TA-Lib port. If unsure about a line, consult the TA-Lib source, not the book.

---

## 3. Laguerre RSI

Ehlers' RSI variant using a **4-stage Laguerre IIR filter cascade**. Zero lookback (each L_k depends only on current + previous values), minimal lag. Output is bounded [0, 1]. Excellent for capturing fast momentum reversals without the sample-count dependency of classical RSI.

### Formula

Given input price series `p_t` and smoothing parameter `α ∈ (0, 1)` (typical 0.2):

```
L0_t = α·p_t + (1 − α)·L0_{t-1}
L1_t = −(1 − α)·L0_t + L0_{t-1} + (1 − α)·L1_{t-1}
L2_t = −(1 − α)·L1_t + L1_{t-1} + (1 − α)·L2_{t-1}
L3_t = −(1 − α)·L2_t + L2_{t-1} + (1 − α)·L3_{t-1}

CU = 0; CD = 0
if L0_t ≥ L1_t:   CU += L0_t − L1_t   else CD += L1_t − L0_t
if L1_t ≥ L2_t:   CU += L1_t − L2_t   else CD += L2_t − L1_t
if L2_t ≥ L3_t:   CU += L2_t − L3_t   else CD += L3_t − L2_t

LaguerreRSI_t = (CU + CD) > 0 ? CU / (CU + CD) : 0
```

Initial conditions: all L_k[0] = p_0. First output at index 1 (needs one prev value per stage).

**Source:** Ehlers, J. F. (2004). *Cybernetic Analysis for Stocks and Futures*, Wiley, Ch. 10.

### Signature

```csharp
public sealed class LaguerreRsi : PriceIndicator<SignalResult>
{
    private readonly double _alpha;

    public LaguerreRsi(double[] prices, double alpha = 0.2) : base(prices)
    {
        if (alpha <= 0 || alpha >= 1) throw new ArgumentException("alpha in (0, 1)", nameof(alpha));
        _alpha = alpha;
        Label = $"LagRSI({alpha:0.00})";
    }

    public override SignalResult Compute() { /* returns double[n - 1] */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

**Warmup:** 1 bar (first index used for L_k initialization). Output length = `prices.Length - 1`.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length == 1** → empty (need prev values)
3. **Length == 2** → boundary, one output row
4. **`alpha <= 0`** → throw
5. **`alpha >= 1`** → throw
6. **`alpha == 0.5`** → edge-case value (no special handling but good sanity)
7. **Flat prices** — all L_k = p, all diffs = 0 → CU + CD = 0 → **output 0** (guard branch)
8. **Monotonic rising** — L0 > L1 > L2 > L3 → all diffs positive → CU > 0, CD = 0 → output ~1.0
9. **Monotonic falling** — L0 < L1 < L2 < L3 → all diffs negative → CD > 0, CU = 0 → output 0.0
10. **Zigzag** — mixed diffs → output somewhere in [0, 1]

### Test vectors

```csharp
// Flat prices → output = 0 everywhere (CU + CD = 0 guard branch)
var flat = new LaguerreRsi(Enumerable.Repeat(100.0, 10).ToArray(), alpha: 0.2).Compute();
flat.ShouldAllBe(v => v == 0);

// Strong uptrend → LaguerreRSI → 1
// close: 100, 101, 102, 103, ..., 120 (21 bars)
// After enough warmup, LagRSI should approach 1 as L0 > L1 > L2 > L3 becomes firmly established
var rising = new LaguerreRsi(
    Enumerable.Range(0, 30).Select(i => 100.0 + i).ToArray(),
    alpha: 0.2).Compute();
rising[25].ShouldBe(1.0, 0.05);

// Strong downtrend → LaguerreRSI → 0 (with flat == 0 + guard branch overlap)
var falling = new LaguerreRsi(
    Enumerable.Range(0, 30).Select(i => 130.0 - i).ToArray(),
    alpha: 0.2).Compute();
falling[25].ShouldBeLessThanOrEqualTo(0.05);

// alpha boundary → throw
Assert.Throws<ArgumentException>(() => new LaguerreRsi([100.0, 101.0], alpha: 0));
Assert.Throws<ArgumentException>(() => new LaguerreRsi([100.0, 101.0], alpha: 1));

// Python reference for known vector:
// def laguerre_rsi(p, alpha=0.2):
//     L = [[p[0]]*4]
//     out = []
//     for i in range(1, len(p)):
//         L0 = alpha*p[i] + (1-alpha)*L[-1][0]
//         L1 = -(1-alpha)*L0 + L[-1][0] + (1-alpha)*L[-1][1]
//         L2 = -(1-alpha)*L1 + L[-1][1] + (1-alpha)*L[-1][2]
//         L3 = -(1-alpha)*L2 + L[-1][2] + (1-alpha)*L[-1][3]
//         L.append([L0,L1,L2,L3])
//         cu = max(L0-L1,0)+max(L1-L2,0)+max(L2-L3,0)
//         cd = max(L1-L0,0)+max(L2-L1,0)+max(L3-L2,0)
//         out.append(cu/(cu+cd) if cu+cd>0 else 0)
//     return out
// Commit ≥3 expected values from a hand-picked close-price sample to 8 decimals.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder LaguerreRsi(double[] prices, double alpha = 0.2,
    Action<Indicators.LaguerreRsi>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[0, 1]`. Typical reference lines callers add: `AxHLine(0.15)` (oversold) and `AxHLine(0.85)` (overbought) — do NOT hardcode.

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/SqueezeMomentumTests.cs`
- `Tst/MatPlotLibNet/Indicators/MamaFamaTests.cs`
- `Tst/MatPlotLibNet/Indicators/LaguerreRsiTests.cs`

Same xUnit v3 Theory pattern as prior tiers. MAMA/FAMA is the one where you'll need the most `[MemberData]` rows with Python-generated reference vectors — accept that and commit the vectors. For MAMA/FAMA, build the vectors from TA-Lib via `import talib; mama, fama = talib.MAMA(close, fastlimit=0.5, slowlimit=0.05)` — that's the canonical cross-check.

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `SqueezeMomentum`: ≥90/90
- `SqueezeResult`: trivial record, ensure test coverage via SqueezeMomentum tests
- `MamaFama`: ≥90/90 **← this one needs the most tests, budget accordingly**
- `MamaFamaResult`: trivial
- `LaguerreRsi`: ≥90/90

MAMA/FAMA has ~16 listed branches. Plan for ~25–30 Theory rows there — each branch clamp, each edge-case recurrence value, each throw path. Don't rush it.

---

## PR checklist

- [ ] 3 indicator classes + 2 result records under `Src/MatPlotLibNet/Indicators/`
- [ ] 3 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 3 test files under `Tst/MatPlotLibNet/Indicators/`
- [ ] Python reference snippets + expected values committed as static arrays in test files (MAMA/FAMA via TA-Lib)
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] No `thresholds.json` changes
- [ ] Changelog entry under `v1.8.0`: "Added Squeeze Momentum LinReg (LazyBear 2014), MAMA/FAMA (Ehlers 2001), Laguerre RSI (Ehlers 2004)"
- [ ] Wiki Indicators page updated

## What's NOT in this PR

Streaming variants for all three. MAMA/FAMA and Laguerre RSI are naturally streaming (O(1) per bar, constant state); Squeeze has fixed-window stddev + SMA. All three deferred to Tier 1e.

**After this PR lands, Tier 1 is complete** — 12 indicators total. Tier 2 (regime + entropy + wavelet + Ehlers cycle family, 13 indicators) would be the v1.8.0 candidate.

---

## Motivation / why these three finish Tier 1

- **Squeeze Momentum LinReg** is the most widely-used volatility-breakout indicator in retail trading after Bollinger Bands. John Carter's book + LazyBear's TradingView port together have hundreds of thousands of adopters. No .NET lib ships a clean implementation.
- **MAMA/FAMA** is Ehlers' canonical adaptive MA — the reference point for every adaptive-filter discussion. Shipping it elevates MatPlotLibNet from "has some indicators" to "has the serious quant-finance ones". The MAMA-FAMA crossover is a standard entry signal; having this built-in unlocks a lot of strategy research.
- **Laguerre RSI** completes the Ehlers pair. Together with MAMA they form the "adaptive momentum & adaptive MA" duo — if you teach a new quant the Ehlers family, these are the two they learn first.

v1.8.0 release, after Tier 1a-d land, advertises: *"12 new indicators across volatility, regime, microstructure, and adaptive filtering — Lopez de Prado + Ehlers + LazyBear primitives out of the box"*. That's a compelling release narrative for the quant-finance community.
