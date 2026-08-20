> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 2c (Ehlers Cycle Family)

Third PR of Tier 2 ("Regime & Cycles" release). Scope: **four Ehlers-canon cycle indicators**. This tier is the other half of the "Cycles" story — MAMA/FAMA from Tier 1d gave us adaptive moving averages; this tier adds the cycle-mode detectors, band-pass filters, and adaptive oscillators that trade on cycle turning points.

**Target:** merge into `main` for v1.8.0, after Tier 2b (Entropy & Wavelet).

**Coverage gate:** ≥90% line AND ≥90% branch per public class. `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** same as Tier 1 — validate at the boundary, explicit math branches for known degeneracies, no blanket guards in compute loops. See `indicator-tier-1d.md` for the full policy.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Ehlers Cyber Cycle** | Adaptive / Cycle | Close + alpha | Cycle oscillator series | Separate subplot |
| 2 | **Ehlers Roofing Filter** | Adaptive / Band-pass | Close + HP period + LP period | Detrended + denoised series | Separate subplot |
| 3 | **Ehlers Sinewave** | Adaptive / Cycle | Close | `(sineWave, leadSine)` pair + cycle/trend flag | Separate subplot |
| 4 | **Ehlers Adaptive Stochastic** | Adaptive / Oscillator | H, L, C | Stochastic %K tuned to dominant cycle | Separate subplot |

All four inherit `PriceIndicator<TResult>` (1, 2, 3) or `CandleIndicator<TResult>` (4). Three of the four benefit from **shared DSP infrastructure** — see next section.

---

## Shared DSP infrastructure

Three helpers unlock this tier and benefit other indicators too. **Extract from Tier 1d's MAMA/FAMA** into their own files before starting Tier 2c. If MAMA/FAMA shipped with these inlined, refactor first — the tests will stay green across the extraction.

**Folder:** `Src/MatPlotLibNet/Indicators/Ehlers/`

### `HilbertDiscriminator`

The MAMA/FAMA Hilbert transform + homodyne-discriminator pipeline, lifted into a reusable internal class.

```csharp
internal static class HilbertDiscriminator
{
    /// <summary>
    /// Runs the full Hilbert + homodyne pipeline. Returns per-bar arrays of
    /// (period, phase, inPhase, quadrature) — the four quantities Ehlers indicators
    /// need for cycle analysis. Length matches input; first 6 bars are warmup (values = 0).
    /// </summary>
    public static (double[] Period, double[] Phase, double[] I1, double[] Q1) Compute(ReadOnlySpan<double> price);
}
```

### `SuperSmoother`

Two-pole Butterworth low-pass filter, Ehlers 2013.

```csharp
internal static class SuperSmoother
{
    /// <summary>
    /// Two-pole Butterworth low-pass with cutoff period `period`. Returns smoothed series
    /// of the same length; first 2 bars equal the input.
    /// </summary>
    public static double[] Apply(ReadOnlySpan<double> signal, int period);
}
```

Recurrence:
```
a1 = exp(-1.414·π / period)
b1 = 2·a1·cos(1.414·180° / period)
c2 = b1
c3 = -a1²
c1 = 1 − c2 − c3
SS_t = c1·(x_t + x_{t-1})/2  +  c2·SS_{t-1}  +  c3·SS_{t-2}
```

### `HighPassFilter`

One-pole high-pass filter, Ehlers.

```csharp
internal static class HighPassFilter
{
    /// <summary>
    /// One-pole high-pass with cutoff period `period`. Removes frequencies below the cutoff.
    /// </summary>
    public static double[] Apply(ReadOnlySpan<double> signal, int period);
}
```

Recurrence:
```
α = (cos(0.707·360°/period) + sin(0.707·360°/period) − 1) / cos(0.707·360°/period)
HP_t = (1 − α/2)² · (x_t − 2·x_{t-1} + x_{t-2})  +  2·(1-α)·HP_{t-1}  −  (1-α)²·HP_{t-2}
```

All three helpers get their own test files under `Tst/MatPlotLibNet/Indicators/Ehlers/` and must cover the recurrence termination / first-N-warmup-bars branches separately from consumer indicator tests. Use `[InternalsVisibleTo]`.

---

## 1. Ehlers Cyber Cycle

Ehlers' pure-cycle component extraction — a second-order IIR filter that emphasizes the dominant cycle frequency while removing trend. Used to identify cycle turning points (zero crossings = cycle mid-line, local maxima = cycle tops).

### Formula

Given close prices `p_t` and smoothing `alpha` (typical 0.07 for ~15-bar cycles, 0.2 for ~7-bar):

```
# Pre-smooth the input
Smooth_t = (p_t + 2·p_{t-1} + 2·p_{t-2} + p_{t-3}) / 6

# Cyber Cycle recurrence
CC_t = (1 − 0.5α)² · (Smooth_t − 2·Smooth_{t-1} + Smooth_{t-2})
     + 2·(1 − α)·CC_{t-1}
     − (1 − α)²·CC_{t-2}
```

Initial conditions: `CC[0] = CC[1] = CC[2] = 0`. First meaningful output at index 3.

**Source:** Ehlers, J. F. (2002). *Cyber Cycle Indicator*. Stocks & Commodities magazine (later ch. 6 of *Cybernetic Analysis for Stocks and Futures* 2004).

### Signature

```csharp
public sealed class CyberCycle : PriceIndicator<SignalResult>
{
    private readonly double _alpha;

    public CyberCycle(double[] prices, double alpha = 0.07) : base(prices)
    {
        if (alpha <= 0 || alpha >= 1) throw new ArgumentException("alpha in (0, 1)", nameof(alpha));
        _alpha = alpha;
        Label = $"CC({alpha:0.00})";
    }

    public override SignalResult Compute() { /* returns double[prices.Length - 3] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 3);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length < 4** → empty (need prev-3 for Smooth)
3. **Length == 4** → boundary, single output
4. **`alpha <= 0`** → throw
5. **`alpha >= 1`** → throw
6. **Constant prices** — Smooth = constant, CC_t = 0 throughout (no cyclical content)
7. **Pure sinusoid at the target period** — CC amplitude should be large (peaks near ±magnitude of input)
8. **Normal multi-bar path**

### Test vectors

```csharp
// Constant → all zeros
var flat = new CyberCycle(Enumerable.Repeat(100.0, 30).ToArray()).Compute();
flat.ShouldAllBe(v => Math.Abs(v) < 1e-9);

// Python reference for regression:
//   def cyber_cycle(p, alpha=0.07):
//       s = [0]*3 + [(p[i] + 2*p[i-1] + 2*p[i-2] + p[i-3])/6 for i in range(3, len(p))]
//       cc = [0, 0, 0]
//       for i in range(3, len(p)):
//           v = ((1-0.5*alpha)**2 * (s[i] - 2*s[i-1] + s[i-2])
//                + 2*(1-alpha)*cc[i-1] - (1-alpha)**2*cc[i-2])
//           cc.append(v)
//       return cc[3:]
// Commit ≥3 expected values with a fixed-seed sinusoidal input.

// Param validation
Assert.Throws<ArgumentException>(() => new CyberCycle([1.0, 2.0, 3.0, 4.0], alpha: 0));
Assert.Throws<ArgumentException>(() => new CyberCycle([1.0, 2.0, 3.0, 4.0], alpha: 1.5));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder CyberCycle(double[] prices, double alpha = 0.07,
    Action<Indicators.CyberCycle>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot centered on 0. No hard Y-range — the amplitude depends on the underlying signal's volatility. Callers often add a zero-line.

---

## 2. Ehlers Roofing Filter

A band-pass filter: **high-pass** removes long-term trend, **SuperSmoother low-pass** removes short-term noise. What's left is the middle-frequency cyclical component that Ehlers argued is what traders should actually care about.

### Formula

```
hp = HighPassFilter.Apply(prices, hpPeriod)   # default hpPeriod = 48
roofing = SuperSmoother.Apply(hp, lpPeriod)    # default lpPeriod = 10
```

Output = `roofing` series, same length as input.

**Source:** Ehlers, J. F. (2014). *Predictive and Successful Indicators*. Stocks & Commodities magazine. Roofing Filter chapter.

### Signature

```csharp
public sealed class RoofingFilter : PriceIndicator<SignalResult>
{
    private readonly int _hpPeriod;
    private readonly int _lpPeriod;

    public RoofingFilter(double[] prices, int hpPeriod = 48, int lpPeriod = 10) : base(prices)
    {
        if (hpPeriod < 4) throw new ArgumentException("hpPeriod >= 4", nameof(hpPeriod));
        if (lpPeriod < 2) throw new ArgumentException("lpPeriod >= 2", nameof(lpPeriod));
        if (lpPeriod >= hpPeriod) throw new ArgumentException("lpPeriod < hpPeriod", nameof(lpPeriod));
        _hpPeriod = hpPeriod;
        _lpPeriod = lpPeriod;
        Label = $"Roof({hpPeriod}/{lpPeriod})";
    }

    public override SignalResult Compute() { /* returns double[prices.Length] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 2);
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **Length < 3** → empty (filters need 2 prev values)
3. **Length == 3** → boundary
4. **`hpPeriod < 4`** → throw
5. **`lpPeriod < 2`** → throw
6. **`lpPeriod >= hpPeriod`** → throw (band-pass would be degenerate)
7. **Constant input** — trend removed, noise removed → output ≈ 0
8. **Pure high-frequency noise** — high-pass passes it, low-pass removes it → small output amplitude
9. **Pure low-frequency trend** — high-pass removes it → output near 0
10. **Target-frequency signal** (period between lpPeriod and hpPeriod) — output amplitude large

### Test vectors

```csharp
// Constant → near-zero output (transient settling only)
var flat = new RoofingFilter(Enumerable.Repeat(100.0, 100).ToArray()).Compute();
flat[^10..].ShouldAllBe(v => Math.Abs(v) < 1e-6);  // settled tail near zero

// Param validation
Assert.Throws<ArgumentException>(() => new RoofingFilter([1.0, 2.0, 3.0, 4.0], hpPeriod: 3));
Assert.Throws<ArgumentException>(() => new RoofingFilter([1.0, 2.0, 3.0, 4.0], lpPeriod: 1));
Assert.Throws<ArgumentException>(() => new RoofingFilter([1.0, 2.0, 3.0, 4.0], hpPeriod: 10, lpPeriod: 10));

// Python reference for known input/output:
//   use signal.filtfilt with the same butter coefs, or implement the recurrence verbatim
// Commit ≥3 expected values from a hand-constructed sinusoidal mixture.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder RoofingFilter(double[] prices, int hpPeriod = 48, int lpPeriod = 10,
    Action<Indicators.RoofingFilter>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, auto Y-range, zero-line reference. Often shown above `CyberCycle` for a full cycle-analysis stack.

---

## 3. Ehlers Sinewave

Extracts the **phase** of the dominant cycle via Hilbert transform, then draws `sin(phase)` (main line) and `sin(phase + 45°)` (lead line). Lead-over-main crossover = cyclic buy signal. Also provides a **cycle/trend flag** — Ehlers' discriminator for "is the market cyclical enough right now to trade this signal?"

### Formula

```
# Use shared HilbertDiscriminator from MAMA/FAMA refactor
(period, phase, I1, Q1) = HilbertDiscriminator.Compute(prices)

# Sine wave + lead wave
sineWave_t   = sin(phase_t)
leadSine_t   = sin(phase_t + 45°)

# Cycle/trend mode: market is cyclic if phase change per bar is roughly consistent
# Ehlers heuristic: abs(phase rate of change − 360/period) < tolerance
isCyclic_t = abs(Δphase_t − 360°/period_t) < tolerance   // typical tolerance: 50°
```

Output = three aligned arrays: `(sineWave, leadSine, isCyclic)`. Length = prices.Length; first 6 bars are warmup (zeros / false).

**Source:** Ehlers, J. F. (2002). *Mesa and Trading Market Cycles*, Wiley, ch. 9. Updated in *Cybernetic Analysis for Stocks and Futures* (2004).

### Output record

```csharp
public readonly record struct SineWaveResult(
    double[] SineWave,
    double[] LeadSine,
    bool[] IsCyclic) : IIndicatorResult;
```

Commit to `Src/MatPlotLibNet/Indicators/SineWaveResult.cs`.

### Signature

```csharp
public sealed class EhlersSineWave : PriceIndicator<SineWaveResult>
{
    public EhlersSineWave(double[] prices) : base(prices)
    {
        Label = "SineWave";
    }

    public override SineWaveResult Compute() { /* uses HilbertDiscriminator */ }

    public override void Apply(Axes axes)
    {
        var result = Compute();
        // Render: sineWave as main line, leadSine as alt-color lead line,
        // dotted/muted where IsCyclic == false to indicate trend-mode regions
        PlotSignal(axes, result.SineWave, warmup: 6, label: "SineWave");
        PlotSignal(axes, result.LeadSine, warmup: 6, label: "LeadSine", color: Colors.Tab10Orange);
        axes.YAxis.Min = -1.2;
        axes.YAxis.Max = 1.2;
    }
}
```

### Branches to cover (≥90/90)

1. **Empty input** → all arrays empty
2. **Length < 7** → empty (Hilbert needs 6 warmup)
3. **Length == 7** → boundary, 1-element output
4. **Flat prices** — period stays at clamp bound, phase 0, sineWave/leadSine both constant 0; `isCyclic = false` throughout (no phase change)
5. **Pure sinusoid at period 20** — `isCyclic = true` after warmup, sineWave roughly tracks input phase
6. **Trend-mode mixed with cycle-mode** — `isCyclic` toggles; verify both branches exercised
7. **Q1 ≈ 0** (atan2 degenerate) — phase still well-defined via `Math.Atan2`
8. **Normal multi-bar path**

### Test vectors

```csharp
// Flat → sineWave = 0, leadSine = 0, isCyclic = false everywhere
var flatPrices = Enumerable.Repeat(100.0, 30).ToArray();
var flat = new EhlersSineWave(flatPrices).Compute();
flat.SineWave[7..].ShouldAllBe(v => Math.Abs(v) < 1e-6);
flat.IsCyclic[7..].ShouldAllBe(v => v == false);

// Sinusoid at known period — sineWave amplitude near 1 post-warmup
var sine = new double[100];
for (int i = 0; i < 100; i++) sine[i] = 100 + 10 * Math.Sin(2 * Math.PI * i / 20);
var sineResult = new EhlersSineWave(sine).Compute();
sineResult.SineWave[50..].Max(Math.Abs).ShouldBeGreaterThan(0.9);  // oscillating near ±1
```

### AxesBuilder shortcut

```csharp
public AxesBuilder EhlersSineWave(double[] prices,
    Action<Indicators.EhlersSineWave>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[-1.2, 1.2]`. Two lines (sineWave + leadSine). Optional dot overlay on x-axis where `isCyclic == true`.

---

## 4. Ehlers Adaptive Stochastic

Classic Stochastic %K, but the lookback period is driven by the **dominant cycle period** from the Hilbert discriminator. Adapts to whatever cycle length currently dominates the series — tighter in fast markets, wider in slow markets.

### Formula

```
# Dominant cycle period from Hilbert
(period, _, _, _) = HilbertDiscriminator.Compute((H + L + C) / 3)

# Stochastic %K with adaptive lookback
cycleBars = max(6, min(50, (int)round(period / 2)))   # half the dominant cycle

%K_t = 100 · (C_t − lowest_low(cycleBars)) / (highest_high(cycleBars) − lowest_low(cycleBars))

# Optional 3-period SuperSmoother to reduce jitter
AdaptiveStoch_t = SuperSmoother.Apply(%K, 3)
```

**Source:** Ehlers, J. F. (2013). *Cycle Analytics for Traders*, Wiley, ch. 12.

### Signature

```csharp
public sealed class AdaptiveStochastic : CandleIndicator<SignalResult>
{
    private readonly int _smoothingPeriod;

    public AdaptiveStochastic(double[] high, double[] low, double[] close, int smoothingPeriod = 3)
        : base([], high, low, close, [])
    {
        if (smoothingPeriod < 1) throw new ArgumentException("smoothingPeriod >= 1", nameof(smoothingPeriod));
        _smoothingPeriod = smoothingPeriod;
        Label = $"AdaptStoch({smoothingPeriod})";
    }

    public override SignalResult Compute() { /* returns double[BarCount - 6], 0-100 scale */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 6);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **HLC length mismatch** → throw (base class check)
3. **BarCount < 7** → empty
4. **`smoothingPeriod < 1`** → throw
5. **H < L** → throw (data corruption check)
6. **Non-positive price** — not strictly required for stochastic (it's a range ratio), but document expectation
7. **Flat H == L** (zero range) — division by zero guard → output 50 (neutral)
8. **Constant prices** → range zero → output 50 throughout
9. **Cycle clamp hit at lower bound** (period < 12) → cycleBars clamped to 6
10. **Cycle clamp hit at upper bound** (period > 100) → cycleBars clamped to 50
11. **Normal multi-bar path**

### Test vectors

```csharp
// Flat → output 50 (neutral guard)
var flat = Enumerable.Repeat(100.0, 30).ToArray();
var flatStoch = new AdaptiveStochastic(flat, flat, flat).Compute();
flatStoch.ShouldAllBe(v => Math.Abs(v - 50.0) < 0.5);

// Monotonic rise (range > 0 always) — stoch should be near 100 at top, near 0 at recovery periods
// Commit Python reference-derived expected values.

// Param validation
Assert.Throws<ArgumentException>(() => new AdaptiveStochastic([1.0, 2.0], [1.0, 2.0], [1.0, 2.0], smoothingPeriod: 0));
Assert.Throws<ArgumentException>(() =>
    new AdaptiveStochastic([100, 99], [102, 101], [101, 100]).Compute());  // H < L
```

### AxesBuilder shortcut

```csharp
public AxesBuilder AdaptiveStochastic(double[] high, double[] low, double[] close,
    int smoothingPeriod = 3,
    Action<Indicators.AdaptiveStochastic>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[0, 100]`. Typical reference lines callers add: `AxHLine(80)` (overbought), `AxHLine(20)` (oversold) — do NOT hardcode.

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/Ehlers/HilbertDiscriminatorTests.cs` (from MAMA/FAMA refactor if not already present)
- `Tst/MatPlotLibNet/Indicators/Ehlers/SuperSmootherTests.cs`
- `Tst/MatPlotLibNet/Indicators/Ehlers/HighPassFilterTests.cs`
- `Tst/MatPlotLibNet/Indicators/CyberCycleTests.cs`
- `Tst/MatPlotLibNet/Indicators/RoofingFilterTests.cs`
- `Tst/MatPlotLibNet/Indicators/EhlersSineWaveTests.cs`
- `Tst/MatPlotLibNet/Indicators/AdaptiveStochasticTests.cs`

Seven test files total. DSP helpers are internal — use `[InternalsVisibleTo]`.

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass on all seven classes / helpers + the four indicator classes.

**Budget warning:** this is the most DSP-heavy tier. Expect ~2× the time of Tier 2a. Roofing Filter is tricky to get right on test vectors — the filter's transient behavior means the first N bars aren't representative; tests should assert on the settled portion (bars `2·hpPeriod` onward).

---

## PR checklist

- [ ] 4 indicator classes + 1 result record (`SineWaveResult`) under `Src/MatPlotLibNet/Indicators/`
- [ ] 3 DSP helpers under `Src/MatPlotLibNet/Indicators/Ehlers/` (HilbertDiscriminator extracted from Tier 1d MAMA/FAMA if inlined there; SuperSmoother + HighPassFilter new)
- [ ] 4 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 7 test files total (3 for helpers, 4 for indicators)
- [ ] Python reference snippets + expected values for Roofing Filter and Sinewave in particular
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] If MAMA/FAMA (Tier 1d) had Hilbert plumbing inlined, confirm those tests still pass after extraction
- [ ] Changelog entry under `v1.8.0`: "Added Ehlers Cyber Cycle (2002), Roofing Filter (2014), Sinewave indicator (2002), Adaptive Stochastic (2013) — with shared Hilbert + SuperSmoother + HighPass DSP infrastructure"
- [ ] Wiki page updated with Ehlers cycle-family usage examples

## What's NOT in this PR

- **Ehlers iTrend** — Tier 3 candidate, simpler adaptive MA variant
- **Ehlers Super Smoother** exposed as a standalone public indicator — it's currently internal since both Roofing Filter and AdaptiveStochastic consume it. Public exposure can be a follow-up if users ask.
- **Ehlers Decycler** — Tier 3 candidate
- **Streaming variants** — all four are naturally streaming (O(1) per bar via the same IIR recurrences), defer to Tier 2e.

---

## Motivation / why these four together

Ehlers' cycle-family indicators are **the** go-to primitives for cycle-based trading strategies in the quant literature. No .NET library ships them; every serious Ehlers user currently has to pay for MetaTrader add-ons, buy them from NinjaTrader's library, or port from TradingView PineScript.

Shipping all four in v1.8.0 closes the "adaptive cycle analysis" gap and gives MatPlotLibNet the **complete Ehlers corpus** when combined with Tier 1d's MAMA/FAMA and Laguerre RSI — 7 Ehlers indicators in one library.

After 2c, Tier 2d (Elder Force, Aroon, RVI classics) wraps the v1.8.0 "Regime & Cycles" release. Then all of v1.8.0 ships as a coherent unit: regime detection + entropy/wavelet + Ehlers cycle family + classic momentum.
