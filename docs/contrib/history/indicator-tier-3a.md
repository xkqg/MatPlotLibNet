> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.9.0 Indicator Pack — Tier 3a (Volume & Money Flow)

First PR of **Tier 3**, the "complete-the-set" tier that closes out the v1.9.0 release. Scope: **four volume-based money-flow indicators**. All classic retail-trading staples that belong in a complete charting library.

**Target:** merge into `main` for v1.9.0 (the next release after Tier 2a-2d lands in v1.8.0). After 3a/b/c land, the v1.9.0 NuGet push ships **12 Tier 3 indicators**, bringing the 2026 total to 36.

**Coverage gate:** ≥90% line AND ≥90% branch per public class. `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** same as Tier 1 — see `indicator-tier-1d.md` for the full policy.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Klinger Volume Oscillator** | Volume / Momentum | HLC + Volume + fast/slow periods + signal | (KVO, Signal) pair | Separate subplot |
| 2 | **Twiggs Money Flow** | Volume / Accumulation | HLC + Volume + period | Series in [-1, 1] | Separate subplot |
| 3 | **Ease of Movement** | Volume / Efficiency | H, L, Volume + period + scale | Signed EMV series | Separate subplot |
| 4 | **VWAP Z-Score** | Volume / Mean-reversion | Close + Volume + window | Standard-deviations series | Separate subplot |

All four inherit `CandleIndicator<TResult>`. Three of the four produce simple `SignalResult`; Klinger returns a `(KVO, Signal)` pair.

---

## 1. Klinger Volume Oscillator

Stephen Klinger's 1977 indicator combining **volume direction with cumulative money flow**. Captures long-term cumulative trend vs short-term momentum; crossovers signal buying/selling pressure reversals. Harder than it looks — the "volume force" calculation has a few subtle branches.

### Formula

Per bar:
```
hlc_t  = (H_t + L_t + C_t) / 3
trend_t = sign(hlc_t − hlc_{t-1})                   # +1, 0, or -1

# Cumulative measurement (CM) — resets when trend direction changes
if trend_t == trend_{t-1}:
    CM_t = CM_{t-1} + (H_t − L_t)
else:
    CM_t = (H_{t-1} − L_{t-1}) + (H_t − L_t)

# Volume force
if CM_t > 0:
    VF_t = Volume_t × abs(2 × (H_t − L_t) / CM_t − 1) × trend_t × 100
else:
    VF_t = 0

# KVO = fast EMA − slow EMA of volume force
KVO_t = EMA(VF, fastPeriod) − EMA(VF, slowPeriod)   # defaults 34/55

# Signal line
Signal_t = EMA(KVO, signalPeriod)                   # default 13
```

**Source:** Klinger, S. J. (1977). *Volume Oscillator*. Developed for the BurgerKlinger newsletter; popularized in *Technical Analysis of Stocks & Commodities*.

### Output record

```csharp
public readonly record struct KlingerResult(double[] Kvo, double[] Signal) : IIndicatorResult;
```

File: `Src/MatPlotLibNet/Indicators/KlingerResult.cs`.

### Signature

```csharp
public sealed class KlingerVolumeOscillator : CandleIndicator<KlingerResult>
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;

    public KlingerVolumeOscillator(double[] high, double[] low, double[] close, double[] volume,
        int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13)
        : base([], high, low, close, volume)
    {
        if (fastPeriod < 2) throw new ArgumentException("fastPeriod >= 2", nameof(fastPeriod));
        if (slowPeriod <= fastPeriod) throw new ArgumentException("slowPeriod > fastPeriod", nameof(slowPeriod));
        if (signalPeriod < 1) throw new ArgumentException("signalPeriod >= 1", nameof(signalPeriod));
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
        Label = $"KVO({fastPeriod}/{slowPeriod}/{signalPeriod})";
    }

    public override KlingerResult Compute() { /* returns pair of double[BarCount - 1] */ }

    public override void Apply(Axes axes)
    {
        var r = Compute();
        PlotSignal(axes, r.Kvo, warmup: 1, label: "KVO");
        PlotSignal(axes, r.Signal, warmup: 1, label: "Signal", color: Colors.Tab10Orange);
    }
}
```

**Warmup:** 1 bar (needs prev-bar HLC). EMAs handle their own warmup (SMA seed pattern).

### Branches to cover (≥90/90)

1. **Empty input** → both arrays empty
2. **HLCV length mismatch** → throw (base class)
3. **BarCount < 2** → empty output
4. **`fastPeriod < 2`** → throw
5. **`slowPeriod <= fastPeriod`** → throw
6. **`signalPeriod < 1`** → throw
7. **Flat HLC** — trend = 0 throughout → VF = 0 → KVO = 0, Signal = 0
8. **`CM_t == 0`** (flat bars with equal H=L) → VF = 0 (guard branch)
9. **Trend reversal** — verify CM resets correctly when trend flips
10. **Trend sustains** — verify CM accumulates when trend stays the same
11. **Normal multi-bar path**

### Test vectors

```csharp
// Flat HLC → zero output everywhere
var f = new KlingerVolumeOscillator(
    Enumerable.Repeat(100.0, 100).ToArray(),
    Enumerable.Repeat(100.0, 100).ToArray(),
    Enumerable.Repeat(100.0, 100).ToArray(),
    Enumerable.Repeat(1000.0, 100).ToArray()).Compute();
f.Kvo.ShouldAllBe(v => v == 0);
f.Signal.ShouldAllBe(v => v == 0);

// Python reference for regression:
//   def klinger(h, l, c, v, fast=34, slow=55, sig=13):
//       hlc = (h + l + c) / 3
//       trend = np.sign(np.diff(hlc))
//       cm = np.zeros(len(h) - 1)
//       range_ = h[1:] - l[1:]
//       prev_range = h[:-1] - l[:-1]
//       for i in range(len(cm)):
//           if i == 0 or trend[i] != trend[i-1]:
//               cm[i] = prev_range[i] + range_[i]
//           else:
//               cm[i] = cm[i-1] + range_[i]
//       vf = np.where(cm > 0, v[1:] * np.abs(2*range_/cm - 1) * trend * 100, 0)
//       kvo = pd.Series(vf).ewm(span=fast, adjust=False).mean() - pd.Series(vf).ewm(span=slow, adjust=False).mean()
//       signal = kvo.ewm(span=sig, adjust=False).mean()
//       return kvo, signal
// Commit ≥3 expected value pairs from a fixed-seed OHLCV.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder KlingerVolumeOscillator(double[] high, double[] low, double[] close, double[] volume,
    int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13,
    Action<Indicators.KlingerVolumeOscillator>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, auto Y-range, zero-line reference. KVO above Signal = bullish; KVO crossing below Signal = bearish.

---

## 2. Twiggs Money Flow (TMF)

Colin Twiggs' improvement over Chaikin Money Flow — uses **true range** rather than high-low, which handles overnight gaps better. Output bounded in [-1, 1]; positive = accumulation, negative = distribution.

### Formula

```
TH_t = max(H_t, C_{t-1})                      # True High
TL_t = min(L_t, C_{t-1})                      # True Low
TR_t = TH_t − TL_t                            # True Range

if TR_t > 0:
    AD_t = ((2·C_t − TH_t − TL_t) / TR_t) × Volume_t
else:
    AD_t = 0

TMF_t = EMA(AD, period) / EMA(Volume, period)  # default period = 21
```

Both EMAs use Wilder-style smoothing (`α = 1/period`) per Twiggs' spec. The ratio is bounded in [-1, 1].

**Source:** Twiggs, C. (2002–2004). *Twiggs Money Flow*, Incredible Charts documentation.

### Signature

```csharp
public sealed class TwiggsMoneyFlow : CandleIndicator<SignalResult>
{
    private readonly int _period;

    public TwiggsMoneyFlow(double[] high, double[] low, double[] close, double[] volume, int period = 21)
        : base([], high, low, close, volume)
    {
        if (period < 2) throw new ArgumentException("period >= 2", nameof(period));
        _period = period;
        Label = $"TMF({period})";
    }

    public override SignalResult Compute() { /* returns double[BarCount - 1] */ }
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = -1;
        axes.YAxis.Max = 1;
    }
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **HLCV length mismatch** → throw
3. **BarCount < 2** → empty (need prev-close)
4. **`period < 2`** → throw
5. **Flat prices + non-zero volume** — TR = 0 on all bars → AD = 0 (guard) → EMA(AD) = 0 → TMF = 0
6. **Flat prices + zero volume** — both EMAs = 0 → division-by-zero guard → TMF = 0
7. **Pure uptrend** (C consistently near TH) → TMF → +1
8. **Pure downtrend** (C consistently near TL) → TMF → -1
9. **Normal multi-bar path**

### Test vectors

```csharp
// Flat → all zeros (TR=0 branch + EMA denominator handling)
var flat = new TwiggsMoneyFlow(
    Enumerable.Repeat(100.0, 50).ToArray(),
    Enumerable.Repeat(100.0, 50).ToArray(),
    Enumerable.Repeat(100.0, 50).ToArray(),
    Enumerable.Repeat(1000.0, 50).ToArray(),
    period: 10).Compute();
flat.ShouldAllBe(v => v == 0);

// Python reference:
//   def twiggs_mf(h, l, c, v, n=21):
//       th = np.maximum(h[1:], c[:-1])
//       tl = np.minimum(l[1:], c[:-1])
//       tr = th - tl
//       ad = np.where(tr > 0, (2*c[1:] - th - tl) / tr * v[1:], 0)
//       alpha = 1 / n
//       ema_ad = pd.Series(ad).ewm(alpha=alpha, adjust=False).mean()
//       ema_v  = pd.Series(v[1:]).ewm(alpha=alpha, adjust=False).mean()
//       return (ema_ad / ema_v.replace(0, np.nan)).fillna(0)
// Commit ≥3 expected values.
```

### AxesBuilder shortcut

Same pattern as the others: `TwiggsMoneyFlow(h, l, c, v, period)`.

### Panel placement

Separate subplot, Y-range `[-1, 1]`. Typical reference lines callers add: `AxHLine(0.25)` (strong accumulation), `AxHLine(-0.25)` (strong distribution) — do NOT hardcode.

---

## 3. Ease of Movement (EMV)

Richard Arms' 1970s indicator measuring **how easily** price moves a given distance relative to volume. High positive EMV = price rises easily on light volume (strong accumulation); high negative = easy decline (strong distribution).

### Formula

```
Midpoint_t    = (H_t + L_t) / 2
MidpointMove  = Midpoint_t − Midpoint_{t-1}
BoxRatio      = (Volume_t / scale) / (H_t − L_t)    # scale typical 10^8 for daily
EMV_1_t       = MidpointMove / BoxRatio
EMV_t         = SMA(EMV_1, period)                  # period typical 14
```

The `scale` factor normalizes volume to similar order of magnitude as price moves; for crypto 1-hour data, typical scale is 10^6.

**Source:** Arms, R. W. (1975). *Volume Cycles in the Stock Market*. Also *Trading Without Fear* (1999).

### Signature

```csharp
public sealed class EaseOfMovement : CandleIndicator<SignalResult>
{
    private readonly int _period;
    private readonly double _scale;

    public EaseOfMovement(double[] high, double[] low, double[] volume,
        int period = 14, double scale = 1_000_000)
        : base([], high, low, [], volume)
    {
        if (period < 2) throw new ArgumentException("period >= 2", nameof(period));
        if (scale <= 0) throw new ArgumentException("scale > 0", nameof(scale));
        _period = period;
        _scale = scale;
        Label = $"EMV({period})";
    }

    public override SignalResult Compute() { /* returns double[BarCount - period] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _period);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **HLV length mismatch** → throw
3. **BarCount <= period** → empty
4. **`period < 2`** → throw
5. **`scale <= 0`** → throw
6. **Zero volume on any bar** — BoxRatio = 0 → division guard → EMV_1 = 0 for that bar
7. **Zero range** (`H == L`) — BoxRatio = infinity → EMV_1 = 0 (physical interpretation: vertical line)
8. **Flat midpoints** — MidpointMove = 0 → EMV_1 = 0 → EMV = 0
9. **Normal multi-bar path**

### Test vectors

```csharp
// Flat H/L + positive volume → EMV = 0 (MidpointMove = 0)
var flat = new EaseOfMovement(
    Enumerable.Repeat(100.0, 30).ToArray(),
    Enumerable.Repeat(99.0, 30).ToArray(),
    Enumerable.Repeat(1000.0, 30).ToArray(),
    period: 10).Compute();
flat.ShouldAllBe(v => v == 0);

// Zero volume → EMV = 0 via division-by-zero guard
// Commit targeted Python-derived vectors.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder EaseOfMovement(double[] high, double[] low, double[] volume,
    int period = 14, double scale = 1_000_000,
    Action<Indicators.EaseOfMovement>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot centered on 0. No hard Y-range — scale-dependent. Callers often add zero-line reference.

---

## 4. VWAP Z-Score

Standardized deviation from rolling VWAP. Quantifies **how far price has wandered from its volume-weighted fair value** in units of standard deviations. Mean-reversion traders use extreme values (±2σ) as entry signals.

### Formula

Over rolling window `W` (default 20):

```
VWAP_t     = Σ(Price_i × Volume_i) / Σ(Volume_i)   for i in window
Deviation  = Price_t − VWAP_t
RollingStd = stddev(Deviation_{t-W+1 : t})
Z_t        = RollingStd > 0 ? Deviation / RollingStd : 0
```

Output typically ranges [-3, +3]; extreme values beyond ±3 indicate rare dislocations.

**Source:** Modern quant-microstructure technique; not attributable to one source. Used in market-making, mean-reversion, and high-frequency analytics.

### Signature

```csharp
public sealed class VwapZScore : CandleIndicator<SignalResult>
{
    private readonly int _window;

    public VwapZScore(double[] close, double[] volume, int window = 20)
        : base([], [], [], close, volume)
    {
        if (window < 2) throw new ArgumentException("window >= 2", nameof(window));
        _window = window;
        Label = $"VwapZ({window})";
    }

    public override SignalResult Compute() { /* returns double[BarCount - window + 1] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _window - 1);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **CV length mismatch** → throw
3. **BarCount < window** → empty
4. **BarCount == window** → boundary, one output
5. **`window < 2`** → throw
6. **Zero total volume** in a window → VWAP guard → Z = 0
7. **Constant price** — all deviations = 0 → stddev = 0 → Z = 0 (guard)
8. **Pure upward drift** — deviations positive, growing → Z positive
9. **Strong dislocation** — recent price jumps far from VWAP → Z exceeds ±3
10. **Normal multi-bar path**

### Test vectors

```csharp
// Constant price → Z = 0 everywhere
var flat = new VwapZScore(
    Enumerable.Repeat(100.0, 50).ToArray(),
    Enumerable.Repeat(1000.0, 50).ToArray(),
    window: 20).Compute();
flat.ShouldAllBe(v => v == 0);

// Python reference:
//   def vwap_z(c, v, w=20):
//       result = []
//       for t in range(w-1, len(c)):
//           window_c = c[t-w+1:t+1]; window_v = v[t-w+1:t+1]
//           vwap = np.sum(window_c * window_v) / np.sum(window_v)
//           dev = c[t] - vwap
//           recent_dev = [c[i] - np.sum(c[i-w+1:i+1]*v[i-w+1:i+1])/np.sum(v[i-w+1:i+1])
//                         for i in range(t-w+2, t+1) if i-w+1 >= 0]
//           sd = np.std(recent_dev, ddof=1) if len(recent_dev) > 1 else 0
//           result.append(dev / sd if sd > 0 else 0)
//       return result
// Commit ≥3 expected values.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder VwapZScore(double[] close, double[] volume, int window = 20,
    Action<Indicators.VwapZScore>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range often `[-3.5, 3.5]` but not hard-bounded. Typical reference lines: `AxHLine(2)` and `AxHLine(-2)` (mean-reversion thresholds).

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/KlingerVolumeOscillatorTests.cs`
- `Tst/MatPlotLibNet/Indicators/TwiggsMoneyFlowTests.cs`
- `Tst/MatPlotLibNet/Indicators/EaseOfMovementTests.cs`
- `Tst/MatPlotLibNet/Indicators/VwapZScoreTests.cs`

Four test files. No shared infrastructure beyond existing EMA / SMA (which are already tested). Klinger is the only one with a result record needing dedicated coverage.

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass on all four classes + `KlingerResult`.

**Budget: comparable to Tier 2d.** All four are straightforward. Klinger has the most branches (trend-reversal CM reset logic) but still ~60 LOC. Expect similar time to 2d.

---

## PR checklist

- [ ] 4 indicator classes + 1 result record under `Src/MatPlotLibNet/Indicators/`
- [ ] 4 AxesBuilder shortcuts (inserted alphabetically)
- [ ] 4 test files under `Tst/MatPlotLibNet/Indicators/`
- [ ] Python reference snippets + expected values for Klinger and Twiggs (the two with subtle formula branches)
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] Changelog entry under `v1.9.0`: "Added Klinger Volume Oscillator (Klinger 1977), Twiggs Money Flow (Twiggs 2002), Ease of Movement (Arms 1975), VWAP Z-Score"
- [ ] Wiki page updated

## What's NOT in this PR

Streaming variants — all four are natural streaming candidates (O(1) per bar with ring buffer for EMV window). Defer to a future streaming tier.

---

## Motivation / why these four together

Volume analysis is the **most requested** category in charting-library feature surveys after classic momentum. Every mainstream toolkit (TradingView, MetaTrader, NinjaTrader) ships all four; every .NET lib is missing at least two. Tier 3a puts MatPlotLibNet on equal footing with the TradingView indicator library for volume analytics.

After 3a, Tier 3b queues: **Classic Supertrend, CG Oscillator, Inverse Fisher Transform, Yang-Zhang Vol Ratio** (4 trend + transform indicators). Then Tier 3c: **Ehlers iTrend, Decycler, SuperSmoother (public), Transfer Entropy** (3 Ehlers + 1 cross-asset). Total Tier 3 = 12 indicators.

**v1.8.0 ships first** (Tier 1a–d + Tier 2a–d, 24 indicators):

- **Tier 1a-d** (12): GK/YZ vol, KER, Cusum, FFD, Amihud, Corwin-Schultz, VPIN, Roll, Squeeze, MAMA/FAMA, Laguerre RSI
- **Tier 2a** Regime (3): BOCPD, Turbulence, Dispersion
- **Tier 2b** Statistical (3): Permutation Entropy, Wavelet Energy, Wavelet Entropy
- **Tier 2c** Ehlers cycle (4): Cyber Cycle, Roofing, Sinewave, Adaptive Stochastic
- **Tier 2d** Classics (3): Elder Force, Aroon, RVI

**v1.9.0 adds Tier 3** (12 more):

- **Tier 3a** Volume (4): Klinger, Twiggs MF, Ease of Movement, VWAP Z-Score
- **Tier 3b** Trend/Transform (4): Supertrend, CG Oscillator, Inverse Fisher, YZ Vol Ratio
- **Tier 3c** Advanced (4): Ehlers iTrend, Decycler, SuperSmoother (public), Transfer Entropy

**Total: 12 indicators in v1.9.0.** Combined with v1.8.0's 24 = **36 production-grade indicators in 2026.**

Release narrative for v1.9.0 NuGet push:
*"Volume analytics, trend transforms, and advanced Ehlers indicators — 12 indicators completing the quant-finance spectrum on top of v1.8.0's 24, with comprehensive test coverage and zero external dependencies."*

That's a compelling v1.9.0 story for the NuGet announcement — and a reason for the quant-finance .NET community to take MatPlotLibNet seriously.
