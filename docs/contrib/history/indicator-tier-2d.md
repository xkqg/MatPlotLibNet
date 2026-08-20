> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 2d (Classic Momentum & Volume)

Fourth and **final** PR of Tier 2 ("Regime & Cycles" release). Scope: **three classic technical indicators** that round out the v1.8.0 release. All three are mainstream, widely-used, and would normally be in "Tier 1" of any charting library — we just saved them for last because the big research-grade indicators in 2a–2c were the priority.

After this PR lands, **v1.8.0 ships** as a coherent 12-indicator release spanning regime detection (2a), information theory (2b), Ehlers cycle analysis (2c), and classic momentum/volume (2d).

**Target:** merge into `main` for v1.8.0, after Tier 2c (Ehlers cycle family).

**Coverage gate:** ≥90% line AND ≥90% branch per public class. `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** same as Tier 1 — validate at the boundary, explicit math branches for known degeneracies, no blanket guards in compute loops. See `indicator-tier-1d.md` for the full policy.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Elder Force Index** | Volume / Momentum | Close + Volume + smoothing | Signed force series | Separate subplot |
| 2 | **Aroon Oscillator** | Trend / Freshness | High, Low + period | Series in [-100, 100] | Separate subplot |
| 3 | **Relative Vigor Index (RVI)** | Momentum | OHLC + period | (RVI, Signal) pair | Separate subplot |

All three are simpler than anything in Tier 2a-c — no shared DSP, no regime math. This tier is mostly about volume and type completeness.

---

## 1. Elder Force Index

Alexander Elder's 1993 momentum indicator that combines **price change + volume** into a single signed series. Positive force = buyers in control, negative = sellers. Typically smoothed with a 13-period EMA for the long-term signal; raw or 2-period smooth for short-term entry signals. Price/force **divergences** are Elder's canonical reversal signal.

### Formula

```
Raw_t       = Volume_t × (Close_t − Close_{t-1})
ForceIdx_t  = EMA(Raw, period)                            # typical period = 13
```

First valid output at index 1 (needs prev close for the difference). Full-length output via EMA's standard warmup (SMA seed for first `period` bars, then recursive EMA).

**Source:** Elder, A. (1993). *Trading for a Living*, Wiley. §7 Force Index.

### Signature

```csharp
public sealed class ForceIndex : CandleIndicator<SignalResult>
{
    private readonly int _period;

    public ForceIndex(double[] close, double[] volume, int period = 13)
        : base([], [], [], close, volume)
    {
        if (period < 1) throw new ArgumentException("period >= 1", nameof(period));
        _period = period;
        Label = $"Force({period})";
    }

    public override SignalResult Compute() { /* returns double[close.Length - 1] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 1);
}
```

**Warmup:** 1 (prev-close). Output length = `close.Length - 1`. The `period` controls smoothing, not warmup.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length == 1** → empty (need prev-close)
3. **Length == 2** → boundary, one output
4. **`close.Length != volume.Length`** → throw
5. **`period < 1`** → throw
6. **`period == 1`** → no smoothing, Force = raw Volume × ΔClose
7. **Flat prices** (all close equal) → all ΔClose = 0 → all force = 0
8. **Zero volume** on a bar → that bar's raw force = 0 (legitimate — no buying/selling pressure)
9. **Negative volume** → not semantically valid; validate at ctor or let it propagate
10. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices → force = 0 throughout (no price change)
var flat = new ForceIndex(
    [100, 100, 100, 100, 100],
    [1000, 1000, 1000, 1000, 1000],
    period: 1).Compute();
flat.ShouldAllBe(v => v == 0);

// Simple known vector (period=1, no smoothing):
// close: [100, 102, 101, 103], volume: [500, 800, 400, 600]
// raw force: [800×2, 400×-1, 600×2] = [1600, -400, 1200]
var raw = new ForceIndex([100.0, 102.0, 101.0, 103.0], [500.0, 800.0, 400.0, 600.0], period: 1).Compute();
raw.ShouldBe([1600.0, -400.0, 1200.0], 1e-9);

// EMA(13) smoothed version — use reference Python snippet:
//   import pandas as pd
//   def force_index(c, v, period=13):
//       raw = v[1:] * c.diff().iloc[1:]
//       return raw.ewm(span=period, adjust=False).mean()
// Commit ≥3 expected values from a fixed-seed random walk.

// Length mismatch → throw
Assert.Throws<ArgumentException>(() => new ForceIndex([100, 101, 102], [1000, 1000], period: 1));

// period < 1 → throw
Assert.Throws<ArgumentException>(() => new ForceIndex([100, 101], [1000, 1000], period: 0));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder ForceIndex(double[] close, double[] volume, int period = 13,
    Action<Indicators.ForceIndex>? configure = null) { /* standard template */ }
```

### Panel placement

Separate subplot centered on 0. No hard Y-range. Callers often add a zero-line marker — divergences with price are the main use case.

---

## 2. Aroon Oscillator

Tushar Chande's 1995 indicator measuring **trend freshness**. Counts bars since the highest high (Aroon Up) and lowest low (Aroon Down) over the lookback window. The oscillator `Up − Down` ranges from −100 to +100: high positive = new uptrend, high negative = new downtrend, near zero = range/choppy.

### Formula

Over a rolling window of `period` bars (default 25):

```
bars_since_high_t = argmax(H[t-period+1 : t+1]) offset from newest
bars_since_low_t  = argmin(L[t-period+1 : t+1]) offset from newest

AroonUp_t    = 100 × (period − bars_since_high_t) / period
AroonDown_t  = 100 × (period − bars_since_low_t)  / period
AroonOsc_t   = AroonUp_t − AroonDown_t                        # range [-100, +100]
```

`bars_since_high = 0` means "today is the new high". `bars_since_high = period` means "the high is the oldest bar in the window". The scaling makes 100 = fresh high today, 0 = period bars since a new high.

**Source:** Chande, T. S. (1995). *A new technical indicator: Aroon*. Stocks & Commodities 13(9), 369–374.

### Signature

```csharp
public sealed class AroonOscillator : CandleIndicator<SignalResult>
{
    private readonly int _period;

    public AroonOscillator(double[] high, double[] low, int period = 25)
        : base([], high, low, [], [])
    {
        if (period < 2) throw new ArgumentException("period >= 2", nameof(period));
        _period = period;
        Label = $"Aroon({period})";
    }

    public override SignalResult Compute() { /* returns double[BarCount - period] */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _period);
        axes.YAxis.Min = -100;
        axes.YAxis.Max = 100;
    }
}
```

**Warmup:** `period` bars. Output length = `BarCount - period`.

**Tie-breaking convention:** when multiple bars share the highest high (ties), use the **most recent** occurrence (standard Aroon convention — ties mean "still making new highs").

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **HL length mismatch** → throw
3. **`period < 2`** → throw
4. **BarCount <= period** → empty output
5. **BarCount == period + 1** → boundary, one output row
6. **`H < L`** on any bar → throw (data corruption)
7. **Ties on highest high** — most recent bar wins
8. **Ties on lowest low** — most recent bar wins
9. **Flat prices** (all H == L == constant) — ties everywhere → both Up and Down = 100 → oscillator = 0
10. **Monotonic rise** — new high every bar → Up = 100, Down decays → oscillator near +100
11. **Monotonic fall** — new low every bar → Down = 100, Up decays → oscillator near -100
12. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices → ties everywhere → Up = Down = 100 → osc = 0
var flatH = Enumerable.Repeat(100.0, 30).ToArray();
var flatL = Enumerable.Repeat(100.0, 30).ToArray();
var flatOsc = new AroonOscillator(flatH, flatL, period: 10).Compute();
flatOsc.ShouldAllBe(v => v == 0);

// Monotonic rise — every bar is a new high, oldest low is at the start
// With period=5, window moves forward each step. Aroon Up always = 100 (new high today),
// Aroon Down decays based on when within-window lowest L occurred.
// At first output (index = period): window is bars [0..5], highest = bar 5 (today) → Up=100;
// lowest = bar 0 (oldest) → bars_since_low = 5 → Down = 0 → osc = +100
var rising = Enumerable.Range(0, 20).Select(i => 100.0 + i).ToArray();
var risingOsc = new AroonOscillator(rising, rising, period: 5).Compute();
risingOsc[0].ShouldBe(100.0, 1e-9);

// Param validation
Assert.Throws<ArgumentException>(() => new AroonOscillator([100.0, 101.0], [99.0, 100.0], period: 1));
Assert.Throws<ArgumentException>(() =>
    new AroonOscillator([100.0, 99.0], [101.0, 100.0], period: 2).Compute());  // H < L
```

### AxesBuilder shortcut

```csharp
public AxesBuilder AroonOscillator(double[] high, double[] low, int period = 25,
    Action<Indicators.AroonOscillator>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[-100, 100]`. Typical reference lines callers add: `AxHLine(50)` and `AxHLine(-50)` (strong-trend thresholds).

---

## 3. Relative Vigor Index (RVI)

Ehlers' 2002 momentum indicator based on the observation that **in uptrends, close > open** (strong close within the day's range) **and in downtrends, close < open**. Quantifies this by taking the ratio of close-open to high-low, smoothed.

Similar to Stochastic %K in shape but measures a different aspect — intraday vigor rather than price position in range.

### Formula

```
Value_t     = Close_t − Open_t
Range_t     = High_t − Low_t

# Smoothed numerator and denominator (classic 4-period weighted smoothing)
NumSmooth_t = (Value_t + 2·Value_{t-1} + 2·Value_{t-2} + Value_{t-3}) / 6
DenSmooth_t = (Range_t + 2·Range_{t-1} + 2·Range_{t-2} + Range_{t-3}) / 6

# Rolling SMA over period (default 10)
Num_t = SMA(NumSmooth, period)
Den_t = SMA(DenSmooth, period)

RVI_t    = Den_t > 0 ? Num_t / Den_t : 0          # explicit division-by-zero guard
Signal_t = (RVI_t + 2·RVI_{t-1} + 2·RVI_{t-2} + RVI_{t-3}) / 6
```

Output = `(RVI, Signal)` aligned arrays. Crossover of RVI over Signal = momentum shift.

**Source:** Ehlers, J. F. (2002). *Relative Vigor Index*. Stocks & Commodities 20(1).

### Output record

```csharp
public readonly record struct RviResult(double[] Rvi, double[] Signal) : IIndicatorResult;
```

Commit to `Src/MatPlotLibNet/Indicators/RviResult.cs`.

### Signature

```csharp
public sealed class RelativeVigorIndex : CandleIndicator<RviResult>
{
    private readonly int _period;

    public RelativeVigorIndex(double[] open, double[] high, double[] low, double[] close, int period = 10)
        : base(open, high, low, close, [])
    {
        if (period < 2) throw new ArgumentException("period >= 2", nameof(period));
        _period = period;
        Label = $"RVI({period})";
    }

    public override RviResult Compute() { /* returns pair of double[BarCount - period - 3] */ }

    public override void Apply(Axes axes)
    {
        var result = Compute();
        // Warmup: 3 (weighted smooth) + period − 1 (SMA) + 3 (signal smooth) = period + 5
        PlotSignal(axes, result.Rvi, warmup: _period + 3, label: "RVI");
        PlotSignal(axes, result.Signal, warmup: _period + 6, label: "Signal");
    }
}
```

**Warmup:** `period + 3` bars for RVI; `period + 6` for Signal. Output length accounting for the signal's extra 3-bar warmup.

### Branches to cover (≥90/90)

1. **Empty input** → both arrays empty
2. **OHLC length mismatch** → throw (base class)
3. **`period < 2`** → throw
4. **BarCount <= period + 3** → empty
5. **Flat prices** (all OHLC equal) → Value = 0, Range = 0 → Den = 0 → RVI = 0 via guard → no NaN
6. **Constant non-zero range, zero close-open** — pure ranging market → RVI = 0
7. **Uptrend** (Close > Open consistently) → RVI positive
8. **Downtrend** (Close < Open consistently) → RVI negative
9. **Den == 0 guard branch** — must be tested explicitly
10. **Normal multi-bar path**

### Test vectors

```csharp
// All-flat OHLC → both output arrays all zeros (Den = 0 guard fires)
var flat = Enumerable.Repeat(100.0, 30).ToArray();
var flatRvi = new RelativeVigorIndex(flat, flat, flat, flat, period: 5).Compute();
flatRvi.Rvi.ShouldAllBe(v => v == 0);
flatRvi.Signal.ShouldAllBe(v => v == 0);

// Python reference:
//   def rvi(o, h, l, c, period=10):
//       v = c - o; r = h - l
//       vs = pd.Series(v); rs = pd.Series(r)
//       vn = (vs + 2*vs.shift(1) + 2*vs.shift(2) + vs.shift(3)) / 6
//       rn = (rs + 2*rs.shift(1) + 2*rs.shift(2) + rs.shift(3)) / 6
//       num = vn.rolling(period).mean()
//       den = rn.rolling(period).mean()
//       rvi = (num / den.replace(0, np.nan)).fillna(0)
//       sig = (rvi + 2*rvi.shift(1) + 2*rvi.shift(2) + rvi.shift(3)) / 6
//       return rvi, sig
// Commit ≥3 expected (rvi, signal) pairs.

// Param validation
Assert.Throws<ArgumentException>(() => new RelativeVigorIndex(flat, flat, flat, flat, period: 1));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder RelativeVigorIndex(double[] open, double[] high, double[] low, double[] close,
    int period = 10,
    Action<Indicators.RelativeVigorIndex>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot centered on 0. Typical Y-range `[-1, 1]` though not hard-bounded. Two lines (RVI + Signal) with shaded fill between them when RVI > Signal (bullish) vs < Signal (bearish).

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/ForceIndexTests.cs`
- `Tst/MatPlotLibNet/Indicators/AroonOscillatorTests.cs`
- `Tst/MatPlotLibNet/Indicators/RelativeVigorIndexTests.cs`

Three test files. None require shared infrastructure (the RVI result record is trivial, covered via RVI tests).

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `ForceIndex`: ≥90/90
- `AroonOscillator`: ≥90/90
- `RelativeVigorIndex`: ≥90/90

**Budget: easiest tier of 2a–2d.** All three indicators are straightforward implementations. Each should be ~50–80 lines of code, ~15 test rows. Total time ~half of a 2a-2c tier.

---

## PR checklist

- [ ] 3 indicator classes + 1 result record (`RviResult`) under `Src/MatPlotLibNet/Indicators/`
- [ ] 3 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 3 test files under `Tst/MatPlotLibNet/Indicators/`
- [ ] Python reference snippets for RVI and smoothed Force Index
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] Changelog entry under `v1.8.0`: "Added Elder Force Index (Elder 1993), Aroon Oscillator (Chande 1995), Relative Vigor Index (Ehlers 2002)"
- [ ] Wiki page updated
- [ ] **v1.8.0 release notes** summarizing all 12 Tier 2 indicators (2a-2d combined narrative)

## What's NOT in this PR

Streaming variants — all three are natural streaming candidates (O(1) per bar for Force and RVI; Aroon needs a ring buffer of the last `period` highs/lows which is still O(period) per bar). Defer to Tier 2e.

---

## Motivation / why these three together

These are the **classic momentum-and-volume indicators** that belong in any charting library's default palette. v1.8.0 shipped the big modern ones (Bollinger, Keltner, MACD, RSI, ATR) + the quant-grade additions (Kaufman ER, GK vol, Amihud, etc.); v1.8.0 adds the regime/cycle stack (2a-2c). **2d fills the final classical gap** — Aroon, RVI, and Force Index are in every textbook and every retail trader's toolkit.

After 2d, **v1.8.0 ships as a complete release**:

- **2a Regime:** BOCPD, Turbulence, Dispersion
- **2b Statistical:** Permutation Entropy, Wavelet Energy, Wavelet Entropy
- **2c Ehlers cycle:** Cyber Cycle, Roofing Filter, Sinewave, Adaptive Stochastic
- **2d Classics:** Elder Force, Aroon, RVI

**12 indicators total**, ~100+ test rows per tier, ~50 unique tests at the class level. Combined with v1.8.0's 12 Tier-1 indicators, MatPlotLibNet will have **24 new quant-grade indicators** shipped in 2026 — putting it ahead of every mainstream .NET charting library and on par with the Python TA-Lib + pywt + pyentrp stack.

Release narrative for v1.8.0: *"Regime detection, information theory, Ehlers cycle analysis, and classic momentum — 12 indicators spanning the full quant-finance indicator spectrum."*

After v1.8.0 lands, **Tier 3 becomes the v1.9.0 scope** — Klinger, Twiggs MF, Ease of Movement, VWAP Z-Score, Supertrend, CG Oscillator, Ehlers iTrend/SuperSmoother-public/Decycler, Inverse Fisher, Transfer Entropy (12 more indicators).
