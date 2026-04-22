// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Wavelet;

namespace MatPlotLibNet.Tests.Indicators.Wavelet;

/// <summary>Verifies <see cref="HaarDwt"/>. Covers all 7 branches enumerated in
/// docs/contrib/indicator-tier-2b.md §shared-infrastructure.</summary>
public class HaarDwtTests
{
    // ── Branch 1 — Empty signal → empty details + empty approx ──
    [Fact]
    public void Decompose_EmptySignal_ReturnsEmpty()
    {
        var (details, approx) = HaarDwt.Decompose([], levels: 2);
        Assert.Empty(details);
        Assert.Empty(approx);
    }

    // ── Branch 2 — levels < 1 throws ──
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Decompose_NonPositiveLevels_Throws(int levels)
    {
        double[] signal = [1.0, 2.0, 3.0, 4.0];
        Assert.Throws<ArgumentException>(() => HaarDwt.Decompose(signal, levels));
    }

    // ── Branch 3 — signal length not power of 2 throws ──
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Decompose_NonPowerOfTwoLength_Throws(int length)
    {
        double[] signal = Enumerable.Range(0, length).Select(i => (double)i).ToArray();
        Assert.Throws<ArgumentException>(() => HaarDwt.Decompose(signal, levels: 1));
    }

    // ── Branch 4 — signal length < 2^levels throws ──
    [Fact]
    public void Decompose_LengthBelowRequired_Throws()
    {
        // levels = 3 → need 2^3 = 8 samples. Provide only 4.
        double[] signal = [1.0, 2, 3, 4];
        Assert.Throws<ArgumentException>(() => HaarDwt.Decompose(signal, levels: 3));
    }

    // ── Branch 5 — Single-level (levels == 1) — known reference ──
    //   signal = [1, 2, 3, 4]
    //   approx[i] = (x[2i] + x[2i+1])/√2 → [(1+2)/√2, (3+4)/√2] = [3/√2, 7/√2] ≈ [2.12132, 4.94975]
    //   detail[i] = (x[2i] - x[2i+1])/√2 → [-1/√2, -1/√2] ≈ [-0.70711, -0.70711]
    [Fact]
    public void Decompose_SingleLevel_MatchesReference()
    {
        double[] signal = [1.0, 2, 3, 4];
        var (details, approx) = HaarDwt.Decompose(signal, levels: 1);
        Assert.Single(details);
        Assert.Equal(2, details[0].Length);
        Assert.Equal(2, approx.Length);
        double s = 1.0 / Math.Sqrt(2);
        Assert.Equal(3 * s, approx[0], precision: 10);
        Assert.Equal(7 * s, approx[1], precision: 10);
        Assert.Equal(-s, details[0][0], precision: 10);
        Assert.Equal(-s, details[0][1], precision: 10);
    }

    // ── Branch 6 — Multi-level recursion — signal length 8, levels 3 ──
    //   Constant [c,c,c,c,c,c,c,c]: after 3 levels, approx = [c·√8] = [c·2√2].
    [Fact]
    public void Decompose_MultiLevel_ConstantSignal_FinalApproxMatchesRootN()
    {
        double c = 5.0;
        double[] signal = Enumerable.Repeat(c, 8).ToArray();
        var (details, approx) = HaarDwt.Decompose(signal, levels: 3);
        Assert.Equal(3, details.Length);
        Assert.Single(approx);
        Assert.Equal(c * Math.Sqrt(8), approx[0], precision: 10);
    }

    [Fact]
    public void Decompose_MultiLevel_DetailLengthsHalveEachLevel()
    {
        double[] signal = Enumerable.Range(0, 16).Select(i => (double)i).ToArray();
        var (details, approx) = HaarDwt.Decompose(signal, levels: 3);
        Assert.Equal(3, details.Length);
        Assert.Equal(8, details[0].Length); // 16 → 8
        Assert.Equal(4, details[1].Length); // 8 → 4
        Assert.Equal(2, details[2].Length); // 4 → 2
        Assert.Equal(2, approx.Length);     // final approx
    }

    // ── Branch 7 — Constant signal — all details zero, approx ≠ 0 ──
    [Fact]
    public void Decompose_ConstantSignal_AllDetailsZero()
    {
        double c = 7.0;
        double[] signal = Enumerable.Repeat(c, 16).ToArray();
        var (details, approx) = HaarDwt.Decompose(signal, levels: 4);
        foreach (var d in details)
            Assert.All(d, v => Assert.Equal(0.0, v, precision: 12));
        Assert.All(approx, v => Assert.False(double.IsNaN(v)));
    }

    // ── Energy conservation — sum of detail + approx energies == sum of signal squared ──
    [Fact]
    public void Decompose_EnergyConservation()
    {
        double[] signal = [3.0, 1.0, -4.0, 1.0, 5.0, 9.0, -2.0, 6.0];
        var (details, approx) = HaarDwt.Decompose(signal, levels: 3);
        double sigEnergy = signal.Select(x => x * x).Sum();
        double dwtEnergy = 0;
        foreach (var d in details) foreach (var v in d) dwtEnergy += v * v;
        foreach (var v in approx) dwtEnergy += v * v;
        Assert.Equal(sigEnergy, dwtEnergy, precision: 10);
    }

    // ── EnergyPerLevel ──

    [Fact]
    public void EnergyPerLevel_ConstantSignal_AllDetailsZero_ApproxHoldsTotal()
    {
        double c = 3.0;
        double[] signal = Enumerable.Repeat(c, 8).ToArray();
        var energy = HaarDwt.EnergyPerLevel(signal, levels: 3);
        Assert.Equal(4, energy.Length); // levels + 1
        for (int k = 0; k < 3; k++) Assert.Equal(0.0, energy[k], precision: 12);
        // Approx energy = signal total energy = 8 × 9 = 72.
        Assert.Equal(72.0, energy[3], precision: 10);
    }

    [Fact]
    public void EnergyPerLevel_Empty_ReturnsEmpty()
    {
        var energy = HaarDwt.EnergyPerLevel([], levels: 3);
        Assert.Empty(energy);
    }

    // Alternating ±1 → all energy at the finest detail level (level 0 of 3):
    //   N=8 signal: 8 pairs of (+1, -1) in pairs → level-1 detail energy dominant.
    [Fact]
    public void EnergyPerLevel_AlternatingSignal_EnergyConcentratedAtLevel0()
    {
        double[] alt = [1, -1, 1, -1, 1, -1, 1, -1];
        var energy = HaarDwt.EnergyPerLevel(alt, levels: 3);
        double total = energy.Sum();
        Assert.True(energy[0] / total > 0.95, $"expected ≥95% energy at level 0, got {energy[0] / total:P2}");
    }
}
