// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="TransferEntropy"/>. Covers all 9 branches in
/// docs/contrib/indicator-tier-3c.md §4.</summary>
public class TransferEntropyTests
{
    // ── Branch 1 — Null inputs → throw ──
    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TransferEntropy(null!, [1.0, 2, 3]));
    }

    [Fact]
    public void Constructor_NullTarget_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TransferEntropy([1.0, 2, 3], null!));
    }

    // ── Branch 2 — Length mismatch → throw ──
    [Fact]
    public void Constructor_LengthMismatch_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TransferEntropy([1.0, 2, 3], [1.0, 2]));
    }

    // ── Branch 3 — Length < lag + 2 → throw ──
    [Fact]
    public void Constructor_LengthBelowLagPlusTwo_Throws()
    {
        // lag=1 → need at least 3 aligned samples. 2 is insufficient.
        Assert.Throws<ArgumentException>(() =>
            new TransferEntropy([1.0, 2], [1.0, 2], bins: 8, lag: 1));
    }

    // ── Branch 4 — bins < 2 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Constructor_BinsBelowTwo_Throws(int bins)
    {
        Assert.Throws<ArgumentException>(() =>
            new TransferEntropy([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5], bins));
    }

    // ── Branch 5 — lag < 1 → throw ──
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_LagBelowOne_Throws(int lag)
    {
        Assert.Throws<ArgumentException>(() =>
            new TransferEntropy([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5], bins: 4, lag: lag));
    }

    // ── Branch 6 — Identical series (X == Y) → TE is non-negative and bounded ──
    [Fact]
    public void Compute_IdenticalSeries_ReturnsNonNegative()
    {
        var rng = new Random(42);
        var x = Enumerable.Range(0, 500).Select(_ => rng.NextDouble()).ToArray();
        var te = new TransferEntropy(x, x, bins: 4, lag: 1).Compute().Values;
        Assert.Single(te);
        Assert.False(double.IsNaN(te[0]));
        Assert.True(te[0] >= 0, $"TE must be non-negative, got {te[0]}");
    }

    // ── Branch 7 — Independent series → TE ≈ 0 ──
    [Fact]
    public void Compute_IndependentSeries_TeNearZero()
    {
        var rngX = new Random(42);
        var rngY = new Random(7919); // different seed
        int n = 2000;
        var x = Enumerable.Range(0, n).Select(_ => rngX.NextDouble()).ToArray();
        var y = Enumerable.Range(0, n).Select(_ => rngY.NextDouble()).ToArray();
        var te = new TransferEntropy(x, y, bins: 4, lag: 1).Compute().Values;
        // Histogram-based TE on finite samples has a small positive bias even for
        // independent streams; values below ~0.05 nats at n=2000, bins=4 are typical.
        Assert.True(te[0] < 0.1, $"independent series → TE should be small, got {te[0]}");
    }

    // ── Branch 8 — Pure causation (Y_t = f(X_{t-1})) → TE(X→Y) > TE(Y→X) ──
    [Fact]
    public void Compute_CausalSeries_ForwardTeExceedsReverse()
    {
        int n = 2000;
        var rng = new Random(11);
        var x = Enumerable.Range(0, n).Select(_ => rng.NextDouble()).ToArray();
        var y = new double[n];
        y[0] = rng.NextDouble();
        for (int i = 1; i < n; i++) y[i] = x[i - 1];                    // pure causation

        var forward = new TransferEntropy(x, y, bins: 4, lag: 1).Compute().Values;
        var reverse = new TransferEntropy(y, x, bins: 4, lag: 1).Compute().Values;

        Assert.True(forward[0] > reverse[0],
            $"TE(X→Y)={forward[0]} must exceed TE(Y→X)={reverse[0]} when Y strictly follows X");
        Assert.True(forward[0] > 0.2,
            $"forward TE on pure causation should be substantially positive, got {forward[0]}");
    }

    // ── Branch 9 — All values in one bin (range collapse) → TE = 0 ──
    [Fact]
    public void Compute_AllValuesInOneBin_ReturnsZero()
    {
        // Two constant series — every sample lands in the same bin; no information content.
        var constant = Enumerable.Repeat(42.0, 100).ToArray();
        var te = new TransferEntropy(constant, constant, bins: 8, lag: 1).Compute().Values;
        Assert.Single(te);
        Assert.Equal(0.0, te[0], precision: 10);
    }

    // ── Compute returns scalar (single-element array) ──
    [Fact]
    public void Compute_ReturnsSingleElementArray()
    {
        var x = Enumerable.Range(0, 200).Select(i => i * 0.01).ToArray();
        var y = Enumerable.Range(0, 200).Select(i => i * 0.01 + 0.5).ToArray();
        var te = new TransferEntropy(x, y).Compute().Values;
        Assert.Single(te);
    }

    // ── Apply — scalar TE doesn't emit a line series (per the plan) ──
    [Fact]
    public void Apply_NoDefaultSeries()
    {
        var axes = new Axes();
        var x = Enumerable.Range(0, 200).Select(i => i * 0.01).ToArray();
        new TransferEntropy(x, x).Apply(axes);
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void DefaultLabel_HasBinsAndLag()
    {
        var te = new TransferEntropy([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5]);
        Assert.Equal("TE(bins=8,lag=1)", te.Label);
    }

    [Fact]
    public void InheritsIndicator()
    {
        var te = new TransferEntropy([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5]);
        Assert.IsAssignableFrom<Indicator<SignalResult>>(te);
        Assert.IsAssignableFrom<IIndicator>(te);
    }
}
