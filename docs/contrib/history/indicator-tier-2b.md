> **HISTORY (moved 2026-08-20).** A completed release build-plan: the version it targets has shipped (the repo is
> at 1.14.2) and nothing in the code, the docs site or another document references this file. Kept for provenance —
> the class and test names in it are from the plan, not from today's source.

# v1.8.0 Indicator Pack — Tier 2b (Entropy & Wavelet)

Second PR of Tier 2 ("Regime & Cycles" release). Scope: **three statistical indicators** that quantify signal complexity from different angles — Shannon entropy of ordinal patterns, and multi-resolution wavelet analysis. All three operate on close-only price series.

**Target:** merge into `main` for v1.8.0, after Tier 2a (Regime Detection) lands.

**Coverage gate:** ≥90% line AND ≥90% branch per public class, enforced per `docs/COVERAGE.md`. `pwsh tools/coverage/run.ps1 -Strict` must pass before PR.

**NaN / ±∞ policy:** same as Tier 1 — validate at the boundary, explicit math branches for known degeneracies, no blanket `IsNaN` / `IsInfinity` guards in compute loops. See `indicator-tier-1d.md` for the full policy.

---

## Indicators to add

| # | Name | Category | Inputs | Output | Panel |
|---|---|---|---|---|---|
| 1 | **Permutation Entropy** | Statistical / Complexity | Close + order + rolling window | Normalized entropy [0, 1] | Separate subplot |
| 2 | **Wavelet Energy Ratio** | Statistical / Multi-resolution | Close + window (power of 2) + level | Energy ratio at target level | Separate subplot |
| 3 | **Wavelet Entropy** | Statistical / Multi-resolution | Close + window (power of 2) | Shannon entropy of band energies | Separate subplot |

All three inherit `PriceIndicator<SignalResult>`. Wavelet Energy/Entropy share a **Haar DWT helper** — see the shared-infrastructure section below.

---

## Shared infrastructure — Haar DWT helper

Both wavelet indicators use the same **discrete Haar wavelet transform**. Add once, reuse twice.

**File:** `Src/MatPlotLibNet/Indicators/Wavelet/HaarDwt.cs`

```csharp
namespace MatPlotLibNet.Indicators.Wavelet;

/// <summary>
/// Discrete Haar wavelet transform. Single pass produces approximation + detail coefficients.
/// Multi-level decomposition recurses on the approximation.
/// </summary>
internal static class HaarDwt
{
    /// <summary>
    /// Compute L-level DWT of a signal. Input length must be a power of 2 and ≥ 2^L.
    /// Returns: details[0..L-1] where details[k] is the length-(N >> (k+1)) detail array at level k+1,
    /// plus approx = the final approximation array of length N >> L.
    /// </summary>
    public static (double[][] Details, double[] Approx) Decompose(ReadOnlySpan<double> signal, int levels);

    /// <summary>
    /// Compute energy at each level: energy[k] = sum(details[k]^2).
    /// Includes a final "approximation energy" at index `levels` for the smoothest band.
    /// Returns array of length (levels + 1).
    /// </summary>
    public static double[] EnergyPerLevel(ReadOnlySpan<double> signal, int levels);
}
```

### Haar formula

At each level, for signal `x` of even length `N`:

```
approx_i  = (x[2i] + x[2i+1]) / √2
detail_i  = (x[2i] − x[2i+1]) / √2     for i in [0, N/2)
```

Recurse on `approx` for `L` levels total. Energy at level `k` = `Σ detail_k[i]²`.

### Branches the helper must cover (≥90/90)

1. **Empty signal** → empty details + empty approx
2. **`levels < 1`** → throw
3. **Signal length not power of 2** → throw
4. **Signal length < 2^levels** → throw (not enough bars to reach target depth)
5. **Single-level path** (`levels == 1`)
6. **Multi-level path** (`levels >= 2`) — recursion termination
7. **Constant signal** — all details zero, approx = `sqrt(N) × value`

Tests live in `Tst/MatPlotLibNet/Indicators/Wavelet/HaarDwtTests.cs`. Use `[InternalsVisibleTo]` for internal access from the test project (already configured).

---

## 1. Permutation Entropy

**Bandt & Pompe's** complexity measure. For each length-`order` window of the signal, rank the values → one of `order!` ordinal permutations. Shannon entropy of the permutation frequency distribution quantifies how predictable the series is. Pure trend → 1 permutation dominates → low entropy. Pure noise → uniform distribution → max entropy.

Used to detect regime transitions: entropy shifts between "structured" and "stochastic" phases reveal changing market dynamics without assuming stationarity.

### Formula

Given signal `x`, order `d` (typical 3–7), delay `τ=1`, rolling window `W`:

```
For each bar t in [W-1, len(x)):
    # Extract window
    w = x[t - W + 1 : t + 1]

    # Count permutation frequencies
    counts = zeros(d!)
    for i in [0, W - d):
        sub = w[i : i + d]
        perm = argsort(sub)              # ranks → permutation index (0..d!-1)
        counts[perm] += 1

    # Shannon entropy, normalized to [0, 1]
    total = sum(counts)                   # = W - d + 1
    H = -Σ (counts[k] / total) × log(counts[k] / total)   for counts[k] > 0
    output[t] = H / log(d!)
```

**Source:** Bandt, C. & Pompe, B. (2002). *Permutation Entropy: A Natural Complexity Measure for Time Series*. Physical Review Letters, 88(17), 174102.

### Signature

```csharp
public sealed class PermutationEntropy : PriceIndicator<SignalResult>
{
    private readonly int _order;
    private readonly int _window;

    public PermutationEntropy(double[] prices, int order = 4, int window = 100) : base(prices)
    {
        if (order < 2 || order > 7) throw new ArgumentException("order in [2, 7]", nameof(order));
        if (window < order + 1) throw new ArgumentException("window must exceed order", nameof(window));
        _order = order;
        _window = window;
        Label = $"PE(d={order},W={window})";
    }

    public override SignalResult Compute() { /* returns double[prices.Length - window + 1] */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _window - 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

**Warmup:** `window - 1` bars. Output length = `prices.Length - window + 1`.

### Implementation tip — permutation index

For small `order` (up to 7), encode an ordinal permutation as a single int: compute the factorial-base representation of the argsort. This lets you use a fixed-size `int[order!]` counts array. `order=4` → 24 slots; `order=7` → 5040 slots. All fit easily.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **`prices.Length < window`** → empty output
3. **`prices.Length == window`** → boundary, one output
4. **`order < 2`** → throw
5. **`order > 7`** → throw (factorial explosion; cap at 7)
6. **`window <= order`** → throw
7. **Constant signal** — all sub-windows identical → ties in argsort → handled by stable sort convention; entropy should be 0 (single permutation dominates)
8. **Strictly monotonic rise** — every subwindow ranks `[0,1,2,...,d-1]` → entropy 0
9. **Perfect noise** (use `Random(42)` seed) — entropy should approach 1.0 for large enough window
10. **Normal multi-window path**

### Test vectors

```csharp
// Monotonic rising → entropy = 0
var rising = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
var risingPE = new PermutationEntropy(rising, order: 3, window: 20).Compute();
risingPE.ShouldAllBe(v => v == 0);

// Constant prices → entropy = 0 (all windows produce identical tied-rank permutation)
var flat = Enumerable.Repeat(100.0, 50).ToArray();
var flatPE = new PermutationEntropy(flat, order: 3, window: 20).Compute();
flatPE.ShouldAllBe(v => v == 0);

// Python reference for regression test:
//   from pyentrp import entropy as ent
//   pe = ent.permutation_entropy(prices, order=3, delay=1, normalize=True)
// Commit ≥3 expected values with fixed-seed random walk.

// Parameter validation
Assert.Throws<ArgumentException>(() => new PermutationEntropy([1.0, 2.0], order: 1));
Assert.Throws<ArgumentException>(() => new PermutationEntropy([1.0, 2.0], order: 8));
Assert.Throws<ArgumentException>(() => new PermutationEntropy([1.0, 2.0], order: 3, window: 3));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder PermutationEntropy(double[] prices, int order = 4, int window = 100,
    Action<Indicators.PermutationEntropy>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[0, 1]`. Typical reference lines callers add: `AxHLine(0.5)` (transitional), `AxHLine(0.85)` (chaotic).

---

## 2. Wavelet Energy Ratio

Decomposes the signal via multi-level Haar DWT into frequency bands, computes the **energy at each band**, outputs the **ratio of energy at a target level to total energy**. Reveals which time-scale of variation currently dominates — short-term noise vs medium-term swings vs long-term trend.

High `EnergyRatio(level=0)` = high-frequency (noise) dominates. High `EnergyRatio(level=L-1)` = long-term structure dominates.

### Formula

Given signal `x`, rolling window `W` (must be power of 2), target `level` (0-indexed into the L decomposition levels):

```
For each bar t in [W-1, len(x)):
    w = x[t - W + 1 : t + 1]
    energy = HaarDwt.EnergyPerLevel(w, levels=log2(W))   # array of length (levels + 1)
    total = sum(energy)
    output[t] = total > 0 ? energy[level] / total : 0
```

**Source:** Rosso, O. A. et al. (2001). *Wavelet entropy: a new tool for analysis of short duration brain electrical signals*. Journal of Neuroscience Methods, 105(1), 65–75. (Foundational paper; applied to finance in Mallat 2009 *A Wavelet Tour of Signal Processing*.)

### Signature

```csharp
public sealed class WaveletEnergyRatio : PriceIndicator<SignalResult>
{
    private readonly int _window;
    private readonly int _level;
    private readonly int _levels;

    public WaveletEnergyRatio(double[] prices, int window = 64, int level = 0) : base(prices)
    {
        if (window < 4 || (window & (window - 1)) != 0)
            throw new ArgumentException("window must be a power of 2, ≥ 4", nameof(window));
        _levels = (int)Math.Log2(window);
        if (level < 0 || level >= _levels)
            throw new ArgumentException($"level must be in [0, {_levels})", nameof(level));
        _window = window;
        _level = level;
        Label = $"WER(W={window},L={level})";
    }

    public override SignalResult Compute() { /* returns double[prices.Length - window + 1] */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _window - 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

**Warmup:** `window - 1` bars. Output length = `prices.Length - window + 1`.

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **`window` not power of 2** → throw
3. **`window < 4`** → throw
4. **`level < 0`** → throw
5. **`level >= log2(window)`** → throw
6. **`prices.Length < window`** → empty
7. **`prices.Length == window`** → boundary
8. **Constant signal** — all detail energies = 0, approximation energy = `window × value²` → `level < levels` → `0 / total = 0`; `level == levels` would be the approximation but our API caps `level < levels` → always 0 for constant signal
9. **Pure high-frequency signal** (alternating `±1`) — all energy concentrated at level 0 (highest detail) → `WER(0) ≈ 1.0`, `WER(k>0) ≈ 0`
10. **Normal multi-window path**

### Test vectors

```csharp
// Constant → all 0 (no detail energy at any level)
var flat = new WaveletEnergyRatio(Enumerable.Repeat(100.0, 100).ToArray(), window: 64, level: 0).Compute();
flat.ShouldAllBe(v => v == 0);

// Alternating ±1 signal → all energy at level 0
var alt = new double[100];
for (int i = 0; i < 100; i++) alt[i] = (i % 2 == 0) ? 1.0 : -1.0;
var altWer = new WaveletEnergyRatio(alt, window: 64, level: 0).Compute();
altWer[^1].ShouldBeGreaterThan(0.95);  // ~all energy at highest detail level

// Same signal at level 1 → should be ~0
var altWer1 = new WaveletEnergyRatio(alt, window: 64, level: 1).Compute();
altWer1[^1].ShouldBeLessThan(0.05);

// Python reference:
//   import pywt, numpy as np
//   def wer(x, wavelet='haar', level=None):
//       coeffs = pywt.wavedec(x, wavelet, level=level)
//       E = np.array([np.sum(c**2) for c in coeffs])
//       return E[1:] / E.sum()   # details only, approx stripped
// Commit ≥3 expected values from a known synthetic signal.

// Parameter validation
Assert.Throws<ArgumentException>(() => new WaveletEnergyRatio([1.0, 2.0], window: 3));  // not power of 2
Assert.Throws<ArgumentException>(() => new WaveletEnergyRatio([1.0, 2.0], window: 2));  // < 4
Assert.Throws<ArgumentException>(() => new WaveletEnergyRatio([1.0, 2.0], window: 64, level: 6));  // level >= log2(64)=6
Assert.Throws<ArgumentException>(() => new WaveletEnergyRatio([1.0, 2.0], window: 64, level: -1));
```

### AxesBuilder shortcut

```csharp
public AxesBuilder WaveletEnergyRatio(double[] prices, int window = 64, int level = 0,
    Action<Indicators.WaveletEnergyRatio>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[0, 1]`. Callers often stack multiple `WaveletEnergyRatio` instances (one per level) on the same axes for a spectrogram-like view of energy migration across scales over time.

---

## 3. Wavelet Entropy

Shannon entropy of the wavelet energy **distribution**. When one band dominates → low entropy (structured signal). When energy is spread across all bands → high entropy (complex / noisy signal).

This is the natural companion to `WaveletEnergyRatio`: the ratio tells you *where* the energy is, entropy tells you *how concentrated* it is.

### Formula

Given signal `x`, rolling window `W` (power of 2):

```
For each bar t:
    w = x[t - W + 1 : t + 1]
    energy = HaarDwt.EnergyPerLevel(w, levels=log2(W))   # length = levels + 1
    total = sum(energy)
    if total == 0: output[t] = 0 ; continue
    probs = energy / total
    H = -Σ probs[k] × log(probs[k])   for probs[k] > 0
    output[t] = H / log(levels + 1)   # normalize to [0, 1]
```

**Source:** Same as Energy Ratio — Rosso et al. (2001), Mallat (2009).

### Signature

```csharp
public sealed class WaveletEntropy : PriceIndicator<SignalResult>
{
    private readonly int _window;
    private readonly int _levels;

    public WaveletEntropy(double[] prices, int window = 64) : base(prices)
    {
        if (window < 4 || (window & (window - 1)) != 0)
            throw new ArgumentException("window must be a power of 2, ≥ 4", nameof(window));
        _window = window;
        _levels = (int)Math.Log2(window);
        Label = $"WEnt(W={window})";
    }

    public override SignalResult Compute() { /* returns double[prices.Length - window + 1] */ }

    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: _window - 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
```

### Branches to cover (≥90/90)

1. **Empty input** → empty output
2. **`window` not power of 2** → throw
3. **`window < 4`** → throw
4. **`prices.Length < window`** → empty
5. **`prices.Length == window`** → boundary
6. **Total energy == 0** (constant signal) → output 0 (explicit guard)
7. **Single band dominates** (alternating ±1) → entropy near 0
8. **Uniform energy across bands** (synthetic: sum of sinusoids at each scale) → entropy near 1
9. **Normal multi-window path**

### Test vectors

```csharp
// Constant → entropy = 0 (total energy = 0 guard path)
var flat = new WaveletEntropy(Enumerable.Repeat(100.0, 100).ToArray()).Compute();
flat.ShouldAllBe(v => v == 0);

// Alternating ±1 → one band dominates → entropy near 0
var alt = new double[100];
for (int i = 0; i < 100; i++) alt[i] = (i % 2 == 0) ? 1.0 : -1.0;
var altEnt = new WaveletEntropy(alt, window: 64).Compute();
altEnt[^1].ShouldBeLessThan(0.1);

// Python reference:
//   import pywt, numpy as np
//   def w_ent(x, wavelet='haar', level=None):
//       coeffs = pywt.wavedec(x, wavelet, level=level)
//       E = np.array([np.sum(c**2) for c in coeffs])
//       p = E / E.sum()
//       return -np.sum(p[p>0] * np.log(p[p>0])) / np.log(len(p))
// Commit ≥3 expected values.
```

### AxesBuilder shortcut

```csharp
public AxesBuilder WaveletEntropy(double[] prices, int window = 64,
    Action<Indicators.WaveletEntropy>? configure = null) { /* template */ }
```

### Panel placement

Separate subplot, Y-range `[0, 1]`. Pairs well above `WaveletEnergyRatio` — one shows concentration, the other shows location of the energy.

---

## Test file structure

- `Tst/MatPlotLibNet/Indicators/PermutationEntropyTests.cs`
- `Tst/MatPlotLibNet/Indicators/WaveletEnergyRatioTests.cs`
- `Tst/MatPlotLibNet/Indicators/WaveletEntropyTests.cs`
- `Tst/MatPlotLibNet/Indicators/Wavelet/HaarDwtTests.cs` (for the shared helper)

The Haar DWT tests are critical — both wavelet indicators depend on correctness there. Cover the recursion termination, the power-of-2 boundary, and the constant-signal case separately from the two consumer tests.

---

## Coverage verification before PR

```pwsh
pwsh tools/coverage/run.ps1 -Strict
```

Must pass:
- `PermutationEntropy`: ≥90/90
- `WaveletEnergyRatio`: ≥90/90
- `WaveletEntropy`: ≥90/90
- `HaarDwt` (internal): ≥90/90

**Budget warning:** Permutation Entropy is the easiest of the three. Both Wavelet indicators share the Haar DWT helper, so getting that right once pays off twice. Total effort similar to Tier 2a (Turbulence was the hard one; here HaarDwt is the equivalent central piece of work).

---

## PR checklist

- [ ] 3 indicator classes + 1 Haar DWT helper under `Src/MatPlotLibNet/Indicators/` (Haar in `Indicators/Wavelet/` subfolder)
- [ ] 3 AxesBuilder shortcuts in `Src/MatPlotLibNet/Builders/AxesBuilder.cs` (inserted alphabetically)
- [ ] 4 test files under `Tst/MatPlotLibNet/Indicators/`
- [ ] Python reference snippets using `pyentrp` and `pywt` committed with expected values
- [ ] `pwsh tools/coverage/run.ps1 -Strict` passes locally
- [ ] No `thresholds.json` changes
- [ ] Changelog entry under `v1.8.0`: "Added Permutation Entropy (Bandt & Pompe 2002), Wavelet Energy Ratio (Rosso et al. 2001), Wavelet Entropy (Rosso et al. 2001) — with shared Haar DWT infrastructure"
- [ ] Wiki page updated

## What's NOT in this PR

- **Daubechies wavelets** (db2, db4, db8) — Haar only for v1.8.0. Daubechies adds filter-coefficient tables + slightly more complex recurrence; deferred to v1.10+.
- **Continuous Wavelet Transform (CWT)** — CWT is O(N²) per bar and produces 2D output; out of scope for a 1D indicator panel. Deferred entirely.
- **Multi-channel wavelets** — input is single price series; multichannel would need the `MultivariateIndicator` base from Tier 2a.
- **Streaming variants** — Permutation Entropy has a natural streaming form (ring-buffer of permutation counts); wavelet ones don't because the DWT requires full-window input. Defer streaming PE to Tier 2e.

---

## Motivation / why these three together

**Entropy + wavelet is the "signal complexity" column** that complements Tier 2a's "regime detection" column. Together they give a researcher the ability to ask:

- *"Is the market currently predictable or chaotic?"* → Permutation Entropy
- *"What time scale is driving today's moves?"* → Wavelet Energy Ratio
- *"Is the energy concentrated in one scale or spread across many?"* → Wavelet Entropy

All three have wide academic use in financial time-series analysis but zero mainstream .NET library coverage. Shipping in v1.8.0 puts MatPlotLibNet in the same league as Python's `pywt` + `pyentrp` stack for this work — with no Python dependency.

After 2b, Tier 2c (Ehlers cycle family) is next. Then 2d (Elder Force, Aroon, RVI classics) — a quick wrap-up.
