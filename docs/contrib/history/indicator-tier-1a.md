> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 1a (Volatility + Regime, OHLC-only)

Scope: add **three indicators** that ship with every trading platform but are absent from v1.7.x. All are pure OHLC (no volume, no tick data), all are ≤60 lines of implementation, all land ≥90/90 line+branch coverage with the test vectors in this document.

**Target:** merge into `main` for the v1.8.0 release. Single PR.

**Coverage gate:** enforced per `docs/COVERAGE.md`. Follow the branch enumeration under each indicator — every listed branch must be hit by a Theory row, or coverage fails. Run `pwsh tools/coverage/run.ps1 -Strict` locally to verify before pushing.

**NaN / ±∞ policy:** validate structural + semantic inputs at the boundary (throw on bad config / data), handle known math degeneracies explicitly via enumerated branches, but **do not** add per-element `IsNaN` / `IsInfinity` guards in compute loops. Sanitization belongs at the rendering boundary. See `indicator-tier-1d.md` for the full policy — same rules apply to every tier.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Garman-Klass Volatility** | Volatility | OHLC + window | σ series | Separate subplot |
| 2 | **Yang-Zhang Volatility** | Volatility | OHLC + window | σ series | Separate subplot |
| 3 | **Kaufman Efficiency Ratio** | Regime | Close + window | 0–1 signal | Separate subplot |

All three inherit `CandleIndicator<SignalResult>` (GK / YZ) or `PriceIndicator<SignalResult>` (ER). Match the existing pattern in [`Src/MatPlotLibNet/Indicators/Atr.cs`](../../Src/MatPlotLibNet/Indicators/Atr.cs) and [`Rsi.cs`](../../Src/MatPlotLibNet/Indicators/Rsi.cs).

---

## Pattern (reference implementation)

Every new indicator lands as **four things**:

1. **Pure class** in `Src/MatPlotLibNet/Indicators/<Name>.cs` — `Compute()` returns `SignalResult`, `Apply(Axes)` draws via `PlotSignal(axes, Compute(), warmup)`
2. **AxesBuilder extension** in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` alongside existing `Sma/Rsi/Atr` shortcuts
3. **Tests** in `Tst/MatPlotLibNet/Indicators/<Name>Tests.cs` — xUnit v3 Theory with canonical vectors + branch-boundary rows
4. **Thresholds baseline** auto-regenerates via `pwsh tools/coverage/run.ps1 -SetBaseline` after green CI

**No streaming variant for Tier 1a.** GK/YZ/ER are window-based and benefit from batch SIMD rather than O(1) ring-buffer append. Streaming ports deferred to Tier 1e.

---

## 1. Garman-Klass Volatility

Classic OHLC volatility estimator — 7.4× more efficient than close-to-close. Zero assumptions about drift.

### Formula

For each bar inside a rolling window of length `N`:

```
σ²_GK(bar) = 0.5 · (ln(H/L))²  −  (2·ln(2) − 1) · (ln(C/O))²
```

Output = `√(mean(σ²_GK over window))`, annualized **not applied** here (leave that to downstream — consistent with `Atr`).

**Source:** Garman, M. B., & Klass, M. J. (1980). *On the Estimation of Security Price Volatilities from Historical Data*. Journal of Business, 53(1), 67–78.

### Signature

```csharp
public sealed class GarmanKlass : CandleIndicator<SignalResult>
{
    public GarmanKlass(double[] open, double[] high, double[] low, double[] close, int period = 20)
        : base(open, high, low, close, []) { /* Label = $"GK({period})" */ }

    public override SignalResult Compute() { /* returns double[n - period + 1] */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), Period - 1);
}
```

### Branches to cover (≥90/90)

1. **Empty input** — `BarCount == 0` → `Array.Empty<double>()`
2. **Length < period** — `BarCount < period` → `Array.Empty<double>()`
3. **Length == period** — boundary, returns length-1 array
4. **Non-positive price** — `H <= 0 || L <= 0 || O <= 0 || C <= 0` on any bar in window → **throw `ArgumentException`** (log undefined). Add explicit guard in constructor over full arrays (not per-bar during compute) so the error is loud, not silent.
5. **H == L** (zero range) — `ln(H/L) == 0` contributes 0, must not produce NaN
6. **O == C** (doji) — `ln(C/O) == 0` contributes 0
7. **All-flat window** (all bars have H=L=O=C) — returns 0.0 values, no NaN
8. **Normal path** — single window output
9. **Rolling path** — multiple window outputs (length ≥ 2)

### Test vectors (canonical)

```csharp
// Constant price (H=L=O=C=100, 25 bars, period=20) → all zeros
// Hand-computed: σ²_GK = 0.5·0² − 0.4427·0² = 0
AssertSequenceEqual(new double[6] { 0, 0, 0, 0, 0, 0 }, gk.Compute(), tol: 1e-12);

// Known single-window vector (period=5, bars = [O,H,L,C]):
// Bar 1: 100, 105, 99,  102
// Bar 2: 102, 104, 100, 103
// Bar 3: 103, 106, 102, 105
// Bar 4: 105, 108, 104, 107
// Bar 5: 107, 109, 106, 108
// Expected √(mean σ²_GK) ≈ 0.020186 (verify with reference Python:
//   np.sqrt(np.mean(0.5*np.log(h/l)**2 - (2*np.log(2)-1)*np.log(c/o)**2)))
Assert.Equal(0.020186, gk.Compute()[0], precision: 5);
```

### AxesBuilder shortcut

```csharp
/// <summary>Adds a Garman-Klass volatility panel indicator.</summary>
public AxesBuilder GarmanKlass(double[] open, double[] high, double[] low, double[] close,
                               int period = 20, Action<Indicators.GarmanKlass>? configure = null)
{
    var indicator = new Indicators.GarmanKlass(open, high, low, close, period);
    if (IsBarSlotContext()) indicator.Offset = 0.5;
    configure?.Invoke(indicator);
    indicator.Apply(_axes);
    return this;
}
```

### Panel placement

Separate subplot below price. Y-axis auto. Label `GK({period})`.

---

## 2. Yang-Zhang Volatility

Best-in-class OHLC estimator — 14× more efficient than close-to-close, handles overnight gaps. Combines three variance components: overnight jump, open-to-close drift, and Rogers-Satchell intraday.

### Formula

For window of `N` bars:

```
σ²_O (overnight)   = variance of ln(O_t / C_{t-1})                      over window
σ²_C (open-close)  = variance of ln(C_t / O_t)                          over window
σ²_RS (Rogers-Satchell, per bar, then averaged):
  σ²_RS(bar) = ln(H/C)·ln(H/O) + ln(L/C)·ln(L/O)

k = 0.34 / (1.34 + (N+1)/(N-1))

σ²_YZ = σ²_O + k · σ²_C + (1 − k) · σ²_RS
```

Output = `√σ²_YZ` per window (same length semantics as GK).

**Source:** Yang, D., & Zhang, Q. (2000). *Drift-Independent Volatility Estimation Based on High, Low, Open, and Close Prices*. Journal of Business, 73(3), 477–491.

### Signature

```csharp
public sealed class YangZhang : CandleIndicator<SignalResult>
{
    public YangZhang(double[] open, double[] high, double[] low, double[] close, int period = 20)
        : base(open, high, low, close, []) { /* Label = $"YZ({period})" */ }

    public override SignalResult Compute() { /* returns double[n - period] — needs prevClose, so first valid bar is index `period` */ }
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), Period);
}
```

**Note on warmup:** YZ needs `C_{t-1}` for the overnight term → output length is `BarCount - period` (not `BarCount - period + 1`). First valid bar index is `period`.

### Branches to cover (≥90/90)

1. **Empty input** — `BarCount == 0` → empty
2. **Length <= period** — `BarCount <= period` → empty (strictly less-than-or-equal, unlike GK)
3. **Length == period + 1** — boundary, returns length-1 array
4. **Non-positive price** — throw (same guard as GK)
5. **All-flat window** — all components zero, output 0.0, no NaN
6. **Single-bar period** — `period == 1` must still compute `k` correctly (edge case: `(N-1) == 0` in `k` formula → need `period >= 2` precondition → throw `ArgumentException` if period < 2)
7. **Normal multi-window path**
8. **k calculation boundary** — `period = 2`: k = 0.34 / (1.34 + 3) = 0.0784…

### Test vectors

```csharp
// Constant price across 25 bars, period=20 → all zeros
AssertSequenceEqual(Enumerable.Repeat(0.0, 5).ToArray(), yz.Compute(), tol: 1e-12);

// Known vector — use Python reference:
// import numpy as np
// def yz(o,h,l,c,n):
//     lnoc = np.log(o[1:]/c[:-1])
//     lnco = np.log(c/o)
//     rs = np.log(h/c)*np.log(h/o) + np.log(l/c)*np.log(l/o)
//     sigO = lnoc.var(ddof=1); sigC = lnco.var(ddof=1); sigRS = rs.mean()
//     k = 0.34/(1.34+(n+1)/(n-1))
//     return np.sqrt(sigO + k*sigC + (1-k)*sigRS)
// Precompute one vector with period=5 and commit the expected value to 8 decimals.

// Period < 2 → ArgumentException
Assert.Throws<ArgumentException>(() => new YangZhang(o, h, l, c, period: 1));
```

### AxesBuilder shortcut

Same structure as `GarmanKlass` above — copy-paste, rename.

### Panel placement

Separate subplot. Often shown alongside GK for comparison (GK overstates vol during drift, YZ corrects it).

---

## 3. Kaufman Efficiency Ratio

Clean 0–1 trend-vs-noise signal. Near 1 = strong trend. Near 0 = choppy range.

### Formula

For each bar at position `t` with window `N`:

```
change    = |C_t − C_{t-N}|
volatility = Σ |C_i − C_{i-1}|  for i in (t-N+1 .. t)

ER(t) = change / volatility    (0 if volatility == 0)
```

**Source:** Kaufman, P. J. (1995). *Smarter Trading*. McGraw-Hill. Also basis of Kaufman Adaptive Moving Average (KAMA).

### Signature

```csharp
public sealed class KaufmanEfficiencyRatio : PriceIndicator<SignalResult>
{
    public KaufmanEfficiencyRatio(double[] prices, int period = 10)
        : base(prices) { /* Label = $"ER({period})" */ }

    public override SignalResult Compute() { /* returns double[n - period] */ }
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), Period);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

### Branches to cover (≥90/90)

1. **Empty input** — `prices.Length == 0` → empty
2. **Length <= period** — boundary → empty
3. **Length == period + 1** — returns length-1 array
4. **Flat prices** (all equal) — both `change` and `volatility` are 0 → **explicit `volatility == 0 ? 0 : change/volatility` guard** (division-by-zero path)
5. **Monotonic rising** — `ER → 1.0`
6. **Zigzag** (|ΔC| constant, net 0) — `ER → 0.0`
7. **Normal path** — multi-bar output
8. **Period < 1** — throw `ArgumentException`

### Test vectors

```csharp
// Flat prices: [100, 100, 100, 100, 100, 100], period=5 → [0.0]
Assert.Equal(new[] { 0.0 }, new KaufmanEfficiencyRatio([100,100,100,100,100,100], 5).Compute());

// Monotonic: [100, 101, 102, 103, 104, 105], period=5 → [1.0]
// change = |105-100| = 5; volatility = 1+1+1+1+1 = 5; ER = 1.0
Assert.Equal(1.0, new KaufmanEfficiencyRatio([100,101,102,103,104,105], 5).Compute()[0], precision: 10);

// Zigzag: [100, 101, 100, 101, 100, 101], period=5 → [|101-100| / (1+1+1+1+1)] = 0.2
Assert.Equal(0.2, new KaufmanEfficiencyRatio([100,101,100,101,100,101], 5).Compute()[0], precision: 10);

// Known vector for regression test — period=10, real BTC close sample.
// Compute in Python via talib or manual reference, commit ≥3 expected values.
```

### AxesBuilder shortcut

Same pattern as `Rsi` (panel indicator) — copy-paste, rename, note the `Y.Min/Max = 0/1` in `Apply()` so the panel is bounded.

### Panel placement

Separate subplot. Y-range `[0, 1]`. Typical reference lines: `AxHLine(0.3)` (choppy threshold), `AxHLine(0.6)` (trending threshold) — do **not** hard-code these in the indicator; let callers add via `.AxHLine(...)`.

---

## Test file structure

One test file per indicator: `Tst/MatPlotLibNet/Indicators/GarmanKlassTests.cs`, etc. Match existing convention — see `Tst/MatPlotLibNet/Indicators/IndicatorTests.cs` and `AdvancedIndicatorTests.cs` for style.

Use xUnit v3 `[Theory]` with `[InlineData]` rows. Each branch from the enumeration above needs at least one dedicated row. Name rows descriptively (e.g., `Compute_FlatWindow_ReturnsZero`, `Compute_EmptyInput_ReturnsEmpty`).

For multi-bar reference vectors, use `[MemberData]` with the reference vector as a static array — avoids inline-data size limits.

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass with:
- `GarmanKlass`: ≥90% line, ≥90% branch
- `YangZhang`:   ≥90% line, ≥90% branch
- `KaufmanEfficiencyRatio`: ≥90% line, ≥90% branch

If any drops, add a Theory row hitting the uncovered branch — **do not add a threshold exemption**. Regeneration of baseline only happens after merge via CI release flow.

---

## PR checklist

- [ ] 3 indicator classes under `Src/MatPlotLibNet/Indicators/`
- [ ] 3 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically alongside existing indicator shortcuts)
- [ ] 3 test files under `Tst/MatPlotLibNet/Indicators/`, one per indicator, covering all branches listed above
- [ ] Reference vectors for non-trivial outputs committed as static arrays with Python-snippet comment showing how they were generated
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] No `thresholds.json` changes
- [ ] Changelog entry under `v1.8.0` section: "Added Garman-Klass volatility, Yang-Zhang volatility, Kaufman Efficiency Ratio"
- [ ] Wiki `Chart-Types.md` or a new `Indicators.md` page updated with usage snippet for each

## What's NOT in this PR

Tier 1b (CUSUM / Fractional Differentiation), Tier 1c (Amihud / Corwin-Schultz / VPIN / Roll Spread), Tier 1d (MAMA-FAMA / Laguerre RSI), streaming variants. Each is a follow-up PR once this pattern merges.

---

## Motivation / why these three first

These are the three most-requested absent indicators in retail/quant trading:
- **GK & YZ** replace naive close-to-close volatility in every production risk model. Most quant books start here.
- **Kaufman ER** is the gate to KAMA (Kaufman Adaptive MA) and a clean signal for regime-aware strategies. Dirt-simple math, huge downstream utility.

All three are pure functions of OHLC — no volume, no ticks, no external data. Shortest path to "finance-credible" for v1.8.0.
