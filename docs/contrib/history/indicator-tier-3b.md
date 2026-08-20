> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.9.0 Indicator Pack — Tier 3b (Trend & Transform)

Second PR of **Tier 3**. Scope: **four trend-following + transform indicators**. Mix of classic trend (Supertrend), Ehlers adaptive (CG Oscillator, Inverse Fisher), and regime detection (Yang-Zhang Vol Ratio).

**Target:** merge into `main` for v1.9.0, after Tier 3a (Volume Pack).

**Coverage gate:** ≥90% line AND ≥90% branch per public class. `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** same as Tier 1 — see `indicator-tier-1d.md`.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Classic Supertrend** | Trend / Stop | HLC + period + multiplier | (Line, Direction, Trend) | Overlay on price |
| 2 | **CG Oscillator** | Adaptive / Momentum | Close + period | Oscillator series | Separate subplot |
| 3 | **Inverse Fisher Transform** | Transform | Input series + scale | Series in [-1, +1] | Separate subplot |
| 4 | **Yang-Zhang Vol Ratio** | Volatility / Regime | OHLC + short window + long window | Ratio series | Separate subplot |

Inverse Fisher is a **meta-indicator** — it transforms any bounded oscillator (RSI, stochastic, CCI, etc.) into a cleaner signal. Yang-Zhang Vol Ratio **depends on** the `YangZhang` volatility indicator from Tier 1a.

---

## 1. Classic Supertrend

**Olivier Seban's 2008 ATR-based trend-following stop.** Mainstream trading toolkit staple — flips direction at ATR-distance trailing stops, produces a cleaner trend line than MA crossovers. Used by every retail platform; absent from .NET.

### Formula

Given `period` (default 10) and `multiplier` (default 3.0):

```
atr = Atr(high, low, close, period)                # reuse existing Tier 1 Atr
basicUpper = (H + L) / 2 + multiplier × atr
basicLower = (H + L) / 2 − multiplier × atr

# Iterative upper/lower bounds (Seban's recurrence)
finalUpper_t = basicUpper_t < finalUpper_{t-1} or close_{t-1} > finalUpper_{t-1}
               ? basicUpper_t
               : finalUpper_{t-1}
finalLower_t = basicLower_t > finalLower_{t-1} or close_{t-1} < finalLower_{t-1}
               ? basicLower_t
               : finalLower_{t-1}

# Direction flip at crossovers
if close_t > finalUpper_{t-1}: direction_t = +1
else if close_t < finalLower_{t-1}: direction_t = −1
else: direction_t = direction_{t-1}

# Supertrend line
line_t = direction_t > 0 ? finalLower_t : finalUpper_t
```

**Source:** Seban, O. (2008). *La méthode magique des turtles modernes*. Popularized on TradingView as `ta.supertrend()`.

### Output record

```csharp
public readonly record struct SupertrendResult(
    double[] Line,       // The stop line value at each bar
    int[] Direction,     // +1 (uptrend) or -1 (downtrend)
    bool[] Flipped)      // true when direction flipped this bar
    : IIndicatorResult;
```

File: `Src/MatPlotLibNet/Indicators/SupertrendResult.cs`.

### Signature

```csharp
public sealed class Supertrend : CandleIndicator<SupertrendResult>
{
    private readonly int _period;
    private readonly double _multiplier;

    public Supertrend(double[] high, double[] low, double[] close, int period = 10, double multiplier = 3.0)
        : base([], high, low, close, [])
    {
        if (period < 1) throw new ArgumentException("period >= 1", nameof(period));
        if (multiplier <= 0) throw new ArgumentException("multiplier > 0", nameof(multiplier));
        _period = period;
        _multiplier = multiplier;
        Label = $"ST({period},{multiplier:0.#})";
    }

    public override SupertrendResult Compute() { /* reuse Atr internally */ }

    public override void Apply(Axes axes)
    {
        var r = Compute();
        // Overlay on price panel: line color green in uptrend, red in downtrend
        // Optional: scatter markers at Flipped=true bars as entry/exit signals
    }
}
```

### Branches to cover (≥90/90)

1. **Empty input** → all arrays empty
2. **HLC length mismatch** → throw
3. **BarCount <= period** → empty output
4. **`period < 1`** → throw
5. **`multiplier <= 0`** → throw
6. **Flat prices** — direction stays at initial +1, line = midpoint − multiplier·ATR (always below flat price)
7. **Strong uptrend** — direction +1 throughout, lower band trails price
8. **Strong downtrend** — direction −1 throughout, upper band trails price
9. **Direction flip** — synthetic reversal triggers `Flipped[t] = true`
10. **`close_t` between upper and lower** — direction unchanged
11. **Initial bars** (before first valid ATR) — direction defaults +1, line = lower band

### Test vectors

```csharp
// Monotonic rise → all +1 direction, no flips
var rising = Enumerable.Range(0, 50).Select(i => 100.0 + i).ToArray();
var r = new Supertrend(rising, rising, rising, period: 10, multiplier: 3.0).Compute();
r.Direction.Skip(10).ShouldAllBe(d => d == 1);
r.Flipped.Skip(10).ShouldAllBe(f => f == false);

// Python reference:
//   def supertrend(h, l, c, period=10, mult=3.0):
//       # standard TradingView algorithm
//       ...
// Commit ≥3 expected (line, direction) pairs around a known reversal.

// Param validation
Assert.Throws<ArgumentException>(() => new Supertrend([100.0], [100.0], [100.0], period: 0));
Assert.Throws<ArgumentException>(() => new Supertrend([100.0], [100.0], [100.0], multiplier: 0));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder Supertrend(double[] high, double[] low, double[] close,
    int period = 10, double multiplier = 3.0,
    Action<Indicators.Supertrend>? configure = null) { /* template */ }
```

### Panel placement

**Overlay on price panel.** Line renders in green above price (downtrend stop) or red below price (uptrend stop). Users often add marker overlays at flip points.

---

## 2. CG Oscillator (Center of Gravity)

**Ehlers' 2002 weighted-price-average oscillator.** Weights recent prices more heavily than older ones; shifts in the center of gravity signal momentum turns. Leads RSI slightly because of the weighting scheme.

### Formula

Given `period` (default 10):

```
# Numerator: weighted sum with linear weights
num_t = Σ_{i=0..period-1} (i + 1) × price_{t-i}     # price_{t-0} weighted by 1, price_{t-period+1} weighted by period

# Denominator: simple sum
den_t = Σ_{i=0..period-1} price_{t-i}

# Raw CG
cg_t = − num_t / den_t + (period + 1) / 2           # centered on zero
```

The `(period+1)/2` offset centers the oscillator on zero so positive/negative halves have symmetric meaning.

**Source:** Ehlers, J. F. (2002). *The CG Oscillator*. Stocks & Commodities 20(3). Later ch. 7 of *Cybernetic Analysis for Stocks and Futures* (2004).

### Signature

```csharp
public sealed class CgOscillator : PriceIndicator<SignalResult>
{
    private readonly int _period;

    public CgOscillator(double[] prices, int period = 10) : base(prices)
    {
        if (period < 2) throw new ArgumentException("period >= 2", nameof(period));
        _period = period;
        Label = $"CG({period})";
    }

    public override SignalResult Compute() { /* returns double[prices.Length - period + 1] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _period - 1);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **prices.Length < period** → empty
3. **`period < 2`** → throw
4. **Denominator == 0** (all prices zero or negative-summing to zero) → throw or return 0? Convention: throw since prices should be positive
5. **Constant prices** — num and den both constant → cg = constant value (offset by `(period+1)/2` places it at zero)
6. **Monotonic rise** — recent prices higher → weighted sum grows → cg positive
7. **Normal multi-bar path**

### Test vectors

```csharp
// Constant → cg = 0 (offset cancels out)
var flat = new CgOscillator(Enumerable.Repeat(100.0, 30).ToArray(), period: 10).Compute();
flat.ShouldAllBe(v => Math.Abs(v) < 1e-9);

// Python reference:
//   def cg(p, n=10):
//       num = sum((i+1) * p[-(i+1)] for i in range(n))
//       den = sum(p[-n:])
//       return -num/den + (n+1)/2
// Commit expected values from a hand-constructed series.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder CgOscillator(double[] prices, int period = 10,
    Action<Indicators.CgOscillator>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot centered on 0. No hard Y-range.

---

## 3. Inverse Fisher Transform

**Ehlers' universal oscillator conditioner** — squashes any series bounded in [-1, +1] through the `(e^(2x) - 1) / (e^(2x) + 1) = tanh(x)` transform. Turns sluggish oscillator curves into sharp transitions between +1 and -1 states, making crossovers of extreme levels cleaner.

**This is a META-indicator.** Apply it to RSI, stochastic, CCI, or any bounded oscillator output. Doesn't compute on price directly.

### Formula

Given a pre-normalized input series `x_t ∈ [-1, +1]` (the caller's responsibility — typically from a normalized oscillator):

```
y_t = (e^(2 × x_t) − 1) / (e^(2 × x_t) + 1)    # identical to tanh(x_t)
```

For oscillators that aren't already in [-1, +1], the caller pre-normalizes. For RSI (0-100): `x = 0.1 × (rsi − 50)` rescales to roughly [-5, +5] which tanh flattens nicely.

**Source:** Ehlers, J. F. (2004). *The Inverse Fisher Transform*. Stocks & Commodities 22(5).

### Signature

```csharp
public sealed class InverseFisherTransform : Indicator<SignalResult>
{
    private readonly double[] _input;
    private readonly double _scale;

    public InverseFisherTransform(double[] input, double scale = 1.0)
    {
        if (input is null || input.Length == 0) throw new ArgumentException("input required", nameof(input));
        if (scale <= 0) throw new ArgumentException("scale > 0", nameof(scale));
        _input = input;
        _scale = scale;
        Label = $"IFT(scale={scale:0.##})";
    }

    public override SignalResult Compute() { /* returns double[input.Length] — same length */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 0);
        axes.YAxis.Min = -1;
        axes.YAxis.Max = 1;
    }
}
```

**Note:** `Indicator<T>` base (not `PriceIndicator`) because it doesn't take price data — it takes any numerical series.

### Branches to cover (≥90/90)

1. **Null input** → throw
2. **Empty input** → throw (can't transform nothing)
3. **`scale <= 0`** → throw
4. **Single value** → length-1 output
5. **Very large positive input** (e.g. +100) → output approaches +1 (no overflow — tanh clips naturally)
6. **Very large negative input** (e.g. -100) → output approaches -1
7. **Zero input** → output = 0 (tanh(0) = 0)
8. **Normal mixed input**

### Test vectors

```csharp
// Known tanh values
var ift = new InverseFisherTransform([0.0, 0.5, 1.0, -0.5, -1.0]).Compute();
ift[0].ShouldBe(0.0, 1e-9);
ift[1].ShouldBe(Math.Tanh(0.5), 1e-9);
ift[2].ShouldBe(Math.Tanh(1.0), 1e-9);

// Large inputs clip to ±1
var large = new InverseFisherTransform([100.0, -100.0]).Compute();
large[0].ShouldBe(1.0, 1e-6);
large[1].ShouldBe(-1.0, 1e-6);

// scale > 1 steepens the transition
var steep = new InverseFisherTransform([0.5], scale: 2.0).Compute();
steep[0].ShouldBe(Math.Tanh(2.0 * 0.5), 1e-9);
```

### AxesBuilder shortcut

```csharp
public AxesBuilder InverseFisherTransform(double[] input, double scale = 1.0,
    Action<Indicators.InverseFisherTransform>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[-1, 1]`. Typical reference lines callers add: `AxHLine(0.5)` and `AxHLine(-0.5)` (signal thresholds).

---

## 4. Yang-Zhang Vol Ratio

**Regime-detection ratio** comparing short-term Yang-Zhang volatility to long-term YZ volatility. Ratio > 1 = vol expansion (breakout imminent); ratio < 1 = vol contraction (consolidation).

Reuses the `YangZhang` indicator from Tier 1a — don't duplicate the math.

### Formula

```
shortVol_t = YangZhang(prices, shortWindow).Compute()   # e.g. 20-bar YZ
longVol_t  = YangZhang(prices, longWindow).Compute()    # e.g. 60-bar YZ

# Align lengths (longer warmup wins)
ratio_t = longVol_t > 0 ? shortVol_t / longVol_t : 1.0
```

Output: ratio series. Typical values 0.3–3.0. Spikes above 2.0 are noteworthy.

**Source:** Yang & Zhang (2000) for the underlying vol; the ratio framing is a common practitioner adaptation.

### Signature

```csharp
public sealed class YangZhangVolRatio : CandleIndicator<SignalResult>
{
    private readonly int _shortWindow;
    private readonly int _longWindow;

    public YangZhangVolRatio(double[] open, double[] high, double[] low, double[] close,
        int shortWindow = 20, int longWindow = 60)
        : base(open, high, low, close, [])
    {
        if (shortWindow < 2) throw new ArgumentException("shortWindow >= 2", nameof(shortWindow));
        if (longWindow <= shortWindow) throw new ArgumentException("longWindow > shortWindow", nameof(longWindow));
        _shortWindow = shortWindow;
        _longWindow = longWindow;
        Label = $"YZRatio({shortWindow}/{longWindow})";
    }

    public override SignalResult Compute() { /* delegates to YangZhang internally */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _longWindow);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **OHLC length mismatch** → throw (base class)
3. **BarCount <= longWindow** → empty
4. **`shortWindow < 2`** → throw
5. **`longWindow <= shortWindow`** → throw
6. **Constant prices** — both YZ vols = 0 → ratio = 1 (guard branch)
7. **Vol expansion scenario** — synthetic: flat for long window, then noisy → ratio > 1
8. **Vol contraction scenario** — synthetic: noisy then flat → ratio < 1
9. **Normal multi-bar path**

### Test vectors

```csharp
// Constant prices → ratio = 1 (guard)
var flat = Enumerable.Repeat(100.0, 100).ToArray();
var flatRatio = new YangZhangVolRatio(flat, flat, flat, flat, shortWindow: 20, longWindow: 60).Compute();
flatRatio.ShouldAllBe(v => Math.Abs(v - 1.0) < 1e-9);

// Commit Python reference values for a known vol-expansion scenario.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder YangZhangVolRatio(double[] open, double[] high, double[] low, double[] close,
    int shortWindow = 20, int longWindow = 60,
    Action<Indicators.YangZhangVolRatio>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot. Often displayed on log-scale Y (values span a wide range). Typical reference lines callers add: `AxHLine(1.0)` (neutral), `AxHLine(2.0)` (significant expansion).

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/SupertrendTests.cs`
- `Tst/MatPlotLibNet/Indicators/CgOscillatorTests.cs`
- `Tst/MatPlotLibNet/Indicators/InverseFisherTransformTests.cs`
- `Tst/MatPlotLibNet/Indicators/YangZhangVolRatioTests.cs`

Four test files. Supertrend has the most branches (direction flip logic); Inverse Fisher is the simplest.

---

## Coverage verification

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass on all four classes + `SupertrendResult`.

**Budget:** comparable to Tier 3a. Supertrend is the most complex (iterative band recurrence + direction tracking); the others are straightforward.

---

## PR checklist

- [ ] 4 indicator classes + 1 result record under `Src/MatPlotLibNet/Indicators/`
- [ ] 4 AxesBuilder shortcuts (inserted alphabetically)
- [ ] 4 test files
- [ ] Python reference snippets for Supertrend and CG Oscillator
- [ ] YangZhang indicator from Tier 1a reused (not duplicated) — verify by grep
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] Changelog entry under `v1.9.0`: "Added Supertrend (Seban 2008), CG Oscillator (Ehlers 2002), Inverse Fisher Transform (Ehlers 2004), Yang-Zhang Vol Ratio"
- [ ] Wiki updated

## What's NOT in this PR

- Streaming variants — all four are streaming-capable but defer to a later streaming tier
- Multi-asset Supertrend ensemble — user-composable from individual Supertrends

---

## Motivation / why these four together

Mixed-theme tier — three of these are **highly requested retail/classic tooling** (Supertrend, CG, Inverse Fisher) and one is a **regime-detection primitive** (YZ Vol Ratio). Supertrend alone is worth the PR — it's the single most-requested indicator absent from .NET libraries per every feature survey.

Inverse Fisher is subtle but strategic: once users have it, they'll compose it with existing indicators (Inverse Fisher of RSI, of stochastic, of CCI) — multiplying the effective indicator count without adding new per-indicator code.

After 3b lands, Tier 3c (Advanced + Cross-Asset: Ehlers iTrend, Decycler, SuperSmoother public, Transfer Entropy) wraps v1.9.0.
