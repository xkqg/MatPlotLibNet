// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Tests.Indicators.Ehlers;

/// <summary>Verifies <see cref="HighPassStep"/>, the one-pole high-pass recursion of Ehlers that the decycler, the roofing filter and
/// their streaming forms fold; the tests fold it over a series themselves, as those indicators do.</summary>
public class HighPassStepTests
{
    private static double[] Fold(double[] signal, int period)
    {
        var step = new HighPassStep(period);
        var result = new double[signal.Length];
        for (int i = 0; i < signal.Length; i++) result[i] = step.Next(signal[i]);
        return result;
    }

    [Fact]
    public void SingleBar_ReturnsZero()
    {
        // Canonical HP initialization is zero — no recurrence runs yet.
        var r = Fold([42.0], period: 5);
        Assert.Single(r);
        Assert.Equal(0.0, r[0], precision: 12);
    }

    [Fact]
    public void TwoBars_StartAtZero_AndStayFinite()
    {
        // The first two bars get no filter output by convention (the recurrence needs x_{t-2}).
        var r = Fold([1.0, 2.0], period: 5);
        Assert.Equal(2, r.Length);
        r.AssertAllFinite();
    }

    [Fact]
    public void ConstantSignal_SettledTailNearZero()
    {
        // HP rejects DC. After the transient, output is ~0.
        var r = Fold(Enumerable.Repeat(50.0, 200).ToArray(), period: 10);
        for (int i = 150; i < 200; i++)
            Assert.True(Math.Abs(r[i]) < 1e-6, $"bar {i}: {r[i]}");
    }

    [Fact]
    public void LinearTrend_SettledTailNearZero()
    {
        // Linear trend = DC + ramp; HP removes DC component. Settled portion should
        // oscillate near zero (the ramp has all its energy well below the cutoff).
        var signal = new double[300];
        for (int i = 0; i < 300; i++) signal[i] = 100 + 0.1 * i;
        var r = Fold(signal, period: 20);
        double tailMax = 0;
        for (int i = 200; i < 300; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax < 1.0, $"tail max = {tailMax}; HP should strongly attenuate slow trend");
    }

    [Fact]
    public void HighFrequencyNoise_PassedThrough()
    {
        // Alternating ±1 is far above the cutoff — passes through with some gain/phase shift.
        var signal = new double[200];
        for (int i = 0; i < 200; i++) signal[i] = (i % 2 == 0) ? 1 : -1;
        var r = Fold(signal, period: 20);
        double tailMax = 0;
        for (int i = 100; i < 200; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax > 0.5, $"tail max = {tailMax}; HF signal should pass through");
    }
}
