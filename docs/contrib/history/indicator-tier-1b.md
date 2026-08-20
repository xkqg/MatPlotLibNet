> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 1b (Regime + Statistical, close-only)

Follows the pattern established by Tier 1a (see `indicator-tier-1a.md`). Scope: **two indicators** in the regime-detection + long-memory-statistical space. Both operate on close prices only. Both go in their own PR on top of Tier 1a.

**Target:** merge into `main` for the v1.8.0 release, after Tier 1a.

**Coverage gate:** ≥90% line AND ≥90% branch per public class, enforced per `docs/COVERAGE.md`. Follow the branch enumeration under each indicator — `pwsh tools/coverage/run.ps1 -Strict` must pass locally before PR.

**NaN / ±∞ policy:** validate structural + semantic inputs at the boundary (throw on bad config / data), handle known math degeneracies explicitly via enumerated branches, but **do not** add per-element `IsNaN` / `IsInfinity` guards in compute loops. Sanitization belongs at the rendering boundary. See `indicator-tier-1d.md` for the full policy — same rules apply to every tier.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **CUSUM Filter** | Regime | Close + threshold | 3-value signal + S_pos, S_neg series | Separate subplot |
| 2 | **Fractional Differentiation (FFD)** | Statistical | Close + d + tolerance | Stationary series | Separate subplot |

Both inherit `PriceIndicator<SignalResult>`. Match the existing pattern in [`Src/MatPlotLibNet/Indicators/Rsi.cs`](../../Src/MatPlotLibNet/Indicators/Rsi.cs).

---

## 1. CUSUM Filter

Classic statistical process control applied to financial returns. Detects structural breaks — moments when the cumulative drift of returns exceeds a threshold, signalling a regime shift. Zero lookahead, O(1) per bar.

### Formula

For each bar, given log-returns `y_t = ln(p_t / p_{t-1})`:

```
S_pos_t = max(0, S_pos_{t-1} + y_t - θ)
S_neg_t = min(0, S_neg_{t-1} + y_t + θ)
```

Where `θ` is a drift-control parameter (small, typically `0` or a tiny expected-return offset).

Emit a break event when either boundary is breached:

```
signal_t = +1   if  S_pos_t  >  h        (and reset S_pos_t = 0)
         = -1   if  S_neg_t  <  -h       (and reset S_neg_t = 0)
         = 0    otherwise
```

Where `h` is the detection threshold. Typical `h` for crypto 1h close returns: **0.02–0.05** (2–5% cumulative drift).

**Source:** Page, E. S. (1954). *Continuous Inspection Schemes*. Biometrika, 41(1/2), 100–115. Applied to financial ML by Lopez de Prado (2018), *Advances in Financial Machine Learning*, ch. 2.

### Output

`SignalResult` carries three aligned arrays:
- `signal` — the `{-1, 0, +1}` events series (primary; hooked into `Apply` as a stem plot)
- `sPos` — the accumulating positive CUSUM series (diagnostic, optional overlay)
- `sNeg` — the accumulating negative CUSUM series (diagnostic, optional overlay)

Use a new nested result record to keep all three available:

```csharp
public readonly record struct CusumResult(double[] Signal, double[] SPos, double[] SNeg) : IIndicatorResult;
```

Put it in `Src/MatPlotLibNet/Indicators/CusumResult.cs` (match the existing result-record pattern from `AdxResult.cs`, `MacdResult.cs`).

### Signature

```csharp
public sealed class Cusum : PriceIndicator<CusumResult>
{
    public Cusum(double[] prices, double threshold, double drift = 0.0) : base(prices)
    {
        _threshold = threshold;
        _drift = drift;
        Label = $"CUSUM(h={threshold:0.##})";
    }

    public override CusumResult Compute() { /* returns triple of double[n-1] */ }

    public override void Apply(Axes axes)
    {
        var result = Compute();
        // Stem plot of signal events on its own zero-line
        // Alternatively: PlotSignal for sPos/sNeg and markers at non-zero signal
    }
}
```

**Note on warmup:** First valid output is at index 1 (need prev-close for the log-return). Output length = `prices.Length - 1`.

### Branches to cover (≥90/90)

1. **Empty input** — `prices.Length == 0` → all three arrays empty
2. **Length == 1** — `prices.Length < 2` → all three arrays empty (need at least one return)
3. **Length == 2** — boundary, one output row
4. **Non-positive price** — `prices[t] <= 0` on any bar → **throw `ArgumentException`** (log undefined). Guard in constructor.
5. **Threshold <= 0** — throw `ArgumentException("threshold must be positive")`
6. **Flat prices** — all equal → all returns zero, `S_pos` and `S_neg` stay at 0, `signal` all zeros, no NaN
7. **Monotonic rise above threshold** — triggers `+1` signal, verify reset to 0 after
8. **Monotonic fall below -threshold** — triggers `-1` signal, verify reset to 0 after
9. **Drift compensation** — positive drift reduces S_pos; verify `y_t = θ` for all t produces `signal = 0`
10. **Both sides same bar** (pathological: large gap, would normally trigger only one side) — only one of `signal = +1` or `-1` per bar; `+1` wins (S_pos checked first)
11. **Normal multi-bar path**

### Test vectors

```csharp
// Flat prices: [100, 100, 100, 100, 100], threshold=0.01 → all signals=0
var flat = new Cusum([100, 100, 100, 100, 100], threshold: 0.01).Compute();
flat.Signal.ShouldAllBe(v => v == 0);
flat.SPos.ShouldAllBe(v => v == 0);
flat.SNeg.ShouldAllBe(v => v == 0);

// Monotonic rise — cumulative log-return reaches threshold:
// prices [100, 101, 102, 103] → log-returns ~[0.00995, 0.00985, 0.00976]
// Cumulative S_pos: 0.00995 → 0.01980 → 0.02956
// threshold=0.015 → signal at bar 2 (when S_pos first exceeds 0.015); reset to 0; bar 3 continues
// Verify signal[0]=0, signal[1]=+1 (first breach), signal[2]=0 (after reset, below h again)

// Negative threshold → ArgumentException
Assert.Throws<ArgumentException>(() => new Cusum([100, 101], threshold: -0.01));

// Non-positive price → ArgumentException
Assert.Throws<ArgumentException>(() => new Cusum([100, 0, 101], threshold: 0.01));

// Verify drift offset:
// With drift θ = log(1.01), a 1% per-bar rise produces y_t - θ = 0 → S_pos stays flat
var drifted = new Cusum([100, 101, 102.01, 103.03], threshold: 0.01, drift: Math.Log(1.01)).Compute();
drifted.Signal.ShouldAllBe(v => v == 0);  // no breach, drift absorbs the trend
```

### AxesBuilder shortcut

```csharp
/// <summary>Adds a CUSUM filter panel indicator.</summary>
public AxesBuilder Cusum(double[] prices, double threshold, double drift = 0.0,
                         Action<Indicators.Cusum>? configure = null)
{
    var indicator = new Indicators.Cusum(prices, threshold, drift);
    if (IsBarSlotContext()) indicator.Offset = 0.5;
    configure?.Invoke(indicator);
    indicator.Apply(_axes);
    return this;
}
```

### Panel placement

Separate subplot. Render as **stem plot** (`.Stem()`) with Y-range `[-1.1, +1.1]` so signal bars are clearly visible. Optional overlay of `S_pos` / `S_neg` lines if caller wants the continuous view (tested via `Apply` behavior).

---

## 2. Fractional Differentiation (FFD)

Lopez de Prado's fixed-width fractional differentiation. Preserves long-memory patterns (unlike integer differencing, which destroys them) while achieving stationarity (verified by ADF test at typical `d ≈ 0.4` for financial time series). Foundational for ML on financial data because it turns non-stationary price series into stationary features without losing the signal.

### Formula

Given a price series `p_t` and fractional order `d ∈ (0, 1)`:

```
X_t = Σ_{k=0}^{m} w_k · p_{t-k}
```

Where the weights satisfy the recurrence:

```
w_0 = 1
w_k = -w_{k-1} · (d − k + 1) / k,   for k ≥ 1
```

Truncate the weight series at width `m` where `|w_m| < tolerance` (typical `tolerance = 1e-3` gives `m ≈ 30–60` for `d ≈ 0.4`).

Output length = `prices.Length - m` (first `m` bars have no full window).

**Source:** Lopez de Prado, M. (2018). *Advances in Financial Machine Learning*, Wiley, §5.5 *Fractional Differentiation*.

### Signature

```csharp
public sealed class FractionalDifferentiation : PriceIndicator<SignalResult>
{
    private readonly double _d;
    private readonly double _tolerance;
    private readonly double[] _weights;  // computed once in ctor

    public FractionalDifferentiation(double[] prices, double d = 0.4, double tolerance = 1e-3)
        : base(prices)
    {
        if (d <= 0 || d >= 1) throw new ArgumentException("d must be in (0, 1)", nameof(d));
        if (tolerance <= 0) throw new ArgumentException("tolerance must be positive", nameof(tolerance));
        _d = d;
        _tolerance = tolerance;
        _weights = ComputeWeights(d, tolerance);
        Label = $"FFD(d={d:0.00})";
    }

    public override SignalResult Compute() { /* returns double[n - weights.Length + 1] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _weights.Length - 1);

    internal static double[] ComputeWeights(double d, double tolerance) { /* see branches */ }
}
```

### Branches to cover (≥90/90)

Two classes need coverage: `FractionalDifferentiation` and the `ComputeWeights` static helper. Expose `ComputeWeights` as `internal` and use `[InternalsVisibleTo]` on the test project (already configured in MatPlotLibNet per the Tier 1a PR).

**`FractionalDifferentiation`:**
1. **`d <= 0`** → throw
2. **`d >= 1`** → throw
3. **`tolerance <= 0`** → throw
4. **Empty input** — `prices.Length == 0` → empty output
5. **Length < weights.Length** — no full window → empty output
6. **Length == weights.Length** — boundary, single output
7. **Normal path** — multi-bar output
8. **Apply path** — verify `PlotSignal` warmup = weights.Length - 1

**`ComputeWeights`:**
1. **`d = 0.0` and `d = 1.0` boundaries** — already rejected by caller, but test `ComputeWeights` directly throws if passed (defensive, belt-and-braces)
2. **Very small `d` (0.01)** — weights decay very slowly → large `m`; verify truncation still terminates (hard cap at e.g. 10_000 iterations → throw if exceeded)
3. **Near-integer `d` (0.99)** — weights decay very fast → small `m` (typically m < 10)
4. **Tolerance boundary** — verify last kept weight satisfies `|w_m| >= tolerance` AND `|w_{m+1}| < tolerance`
5. **First two weights** — `w_0 = 1`, `w_1 = -d`
6. **Weight signs alternate** — for `d ∈ (0,1)`, successive weights alternate sign after `w_1`

### Test vectors

```csharp
// Weight recurrence sanity:
// d=0.5, w_0=1, w_1 = -0.5, w_2 = w_1 * (-(0.5 - 1)/2) = -0.5 * 0.25 = -0.125
// Wait — recheck recurrence: w_k = -w_{k-1} * (d - k + 1) / k
// w_1 = -w_0 * (0.5 - 1 + 1)/1 = -1 * 0.5 = -0.5
// w_2 = -w_1 * (0.5 - 2 + 1)/2 = -(-0.5) * (-0.5)/2 = -0.5 * 0.5 / 2 = wait let me redo
// Actually the cleaner form: w_k = w_{k-1} * (k - 1 - d) / k
//   w_0 = 1
//   w_1 = 1 * (0 - 0.5)/1 = -0.5
//   w_2 = -0.5 * (1 - 0.5)/2 = -0.5 * 0.25 = -0.125
//   w_3 = -0.125 * (2 - 0.5)/3 = -0.125 * 0.5 = -0.0625
// CONFIRM via reference Python:
//   import numpy as np
//   def ffd_weights(d, tol):
//       w = [1.0]; k = 1
//       while True:
//           wk = -w[-1] * (d - k + 1) / k
//           if abs(wk) < tol: break
//           w.append(wk); k += 1
//       return np.array(w)
// Commit the reference snippet as a comment + expected first 5 weights to 10 decimals

var w = FractionalDifferentiation.ComputeWeights(0.5, 1e-9);
w[0].ShouldBe(1.0, 1e-12);
w[1].ShouldBe(-0.5, 1e-12);
w[2].ShouldBe(-0.125, 1e-12);
w[3].ShouldBe(-0.0625, 1e-12);

// Constant prices produce constant output (roughly — not zero, because FFD weights don't sum to zero for d < 1)
// [100, 100, 100, ..., 100] * sum(weights) = constant series
// With d=0.5 and tol=1e-3, sum(weights) ≈ 0.324... (verify via Python)
// FFD output will be flat at price * sum_of_weights — verify no NaN, constant to 1e-10

// d out of range → throw
Assert.Throws<ArgumentException>(() => new FractionalDifferentiation([100, 101, 102], d: 0.0));
Assert.Throws<ArgumentException>(() => new FractionalDifferentiation([100, 101, 102], d: 1.0));
Assert.Throws<ArgumentException>(() => new FractionalDifferentiation([100, 101, 102], d: -0.5));
Assert.Throws<ArgumentException>(() => new FractionalDifferentiation([100, 101, 102], tolerance: 0));

// Regression test: known BTC 1h close sample, d=0.4, tol=1e-4
// Compute expected output via reference Python, commit ≥3 expected values to 8 decimals
// (store as static readonly array in test class with Python-snippet comment)
```

### AxesBuilder shortcut

```csharp
/// <summary>Adds a fractional-differentiation panel indicator.</summary>
public AxesBuilder Ffd(double[] prices, double d = 0.4, double tolerance = 1e-3,
                       Action<Indicators.FractionalDifferentiation>? configure = null)
{
    var indicator = new Indicators.FractionalDifferentiation(prices, d, tolerance);
    if (IsBarSlotContext()) indicator.Offset = 0.5;
    configure?.Invoke(indicator);
    indicator.Apply(_axes);
    return this;
}
```

### Panel placement

Separate subplot. Y-axis auto (the FFD output is a stationary price-like series, no fixed bounds). Caller typically places it directly below the price chart so visual alignment of bars is preserved.

**Performance note:** weights are computed once in constructor, then reused per `Compute()` call. The convolution is O(n·m) where n=bars, m=weight-count. For typical `d ≈ 0.4, tol = 1e-3`: m ≈ 40–60. For 10k-bar series: ~500k multiplications. Well within budget, no SIMD needed, but leave a comment: `// TODO: if m > 500, switch to FFT-based convolution for O(n log n)`.

---

## Test file structure

Same as Tier 1a — one test file per indicator:
- `Tst/MatPlotLibNet/Indicators/CusumTests.cs`
- `Tst/MatPlotLibNet/Indicators/FractionalDifferentiationTests.cs`

xUnit v3 `[Theory]` + `[InlineData]` for simple cases, `[MemberData]` for multi-bar regression vectors. Name rows after the branch they hit (`Compute_FlatPrices_SignalAllZero`, `Ctor_DOutOfRange_Throws`, `ComputeWeights_FirstTwoWeights_MatchClosedForm`, etc.).

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `Cusum`: ≥90/90
- `CusumResult` (record struct): exempted if trivially covered by `Cusum.Compute()` tests (no manual logic)
- `FractionalDifferentiation`: ≥90/90
- Static `ComputeWeights` helper: ≥90/90 (exposed via `InternalsVisibleTo`)

If anything drops, add a Theory row — don't add threshold exemptions.

---

## PR checklist

- [ ] 2 indicator classes under `Src/MatPlotLibNet/Indicators/` (+ 1 result record for CUSUM)
- [ ] 2 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 2 test files under `Tst/MatPlotLibNet/Indicators/`, covering all branches listed above
- [ ] Reference vectors for non-trivial outputs committed as static arrays with Python-snippet comment
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] No `thresholds.json` changes
- [ ] Changelog entry under `v1.8.0` section: "Added CUSUM filter (Page 1954 / Lopez de Prado 2018), Fractional Differentiation (Lopez de Prado 2018)"
- [ ] Wiki `Indicators.md` (or Tier 1a's new page) updated with a usage snippet for each

## What's NOT in this PR

Tier 1c (microstructure: Amihud, Corwin-Schultz, VPIN, Roll Spread), Tier 1d (Ehlers adaptive), streaming variants. CUSUM has a natural streaming variant (O(1) per bar, perfect fit); FFD does not (fixed-width convolution, tight coupling to the entire window). Both deferred to Tier 1e.

---

## Motivation / why these two next

- **CUSUM** is the single most foundational regime-break detector in financial ML. Lopez de Prado's *Advances in Financial Machine Learning* uses it as a sampling filter (event-driven bars) — zero-lookahead, O(1), no tuning beyond one threshold.
- **Fractional Differentiation** is the "how do you ML on prices without destroying the signal" answer. Everyone who ships RL / supervised-ML on crypto eventually rediscovers they need it. Having it built-in means no one has to re-derive the weight recurrence from the book.

Both are absent from every mainstream .NET financial library. Shipping them in v1.8.0 makes MatPlotLibNet the first .NET lib to carry Lopez de Prado primitives out of the box.
