// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;

namespace MatPlotLibNet.Tests.Indicators.Ehlers;

/// <summary>Verifies <see cref="HighPassFilter"/>. One-pole high-pass per Ehlers.</summary>
public class HighPassFilterTests
{
    [Fact]
    public void Apply_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(ReadOnlySpan<double>.Empty.HighPass(period: 10));
    }

    [Fact]
    public void Apply_PeriodBelowTwo_Throws()
    {
        double[] signal = [1, 2, 3, 4];
        Assert.Throws<ArgumentException>(() => signal.AsSpan().HighPass(period: 1));
        Assert.Throws<ArgumentException>(() => signal.AsSpan().HighPass(period: 0));
    }

    [Fact]
    public void Apply_SingleBar_ReturnsZero()
    {
        // Canonical HP initialization is zero — no recurrence runs yet.
        double[] single = [42.0];
        var r = single.AsSpan().HighPass(period: 5);
        Assert.Single(r);
        Assert.Equal(0.0, r[0], precision: 12);
    }

    [Fact]
    public void Apply_TwoBars_StartsAtZeroFirstThenInputTypical()
    {
        // First two bars get no filter output by convention (recurrence needs x_{t-2}).
        // The implementation should keep them finite — assert only that.
        double[] two = [1.0, 2.0];
        var r = two.AsSpan().HighPass(period: 5);
        Assert.Equal(2, r.Length);
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }

    [Fact]
    public void Apply_ConstantSignal_SettledTailNearZero()
    {
        // HP rejects DC. After the transient, output is ~0.
        var signal = Enumerable.Repeat(50.0, 200).ToArray();
        var r = signal.AsSpan().HighPass(period: 10);
        // Assert the settled portion (last 50 bars) is near zero.
        for (int i = 150; i < 200; i++)
            Assert.True(Math.Abs(r[i]) < 1e-6, $"bar {i}: {r[i]}");
    }

    [Fact]
    public void Apply_LinearTrend_SettledTailNearZero()
    {
        // Linear trend = DC + ramp; HP removes DC component. Settled portion should
        // oscillate near zero (the ramp has all its energy well below the cutoff).
        var signal = new double[300];
        for (int i = 0; i < 300; i++) signal[i] = 100 + 0.1 * i;
        var r = signal.AsSpan().HighPass(period: 20);
        // Tail amplitude should be very small compared to signal scale.
        double tailMax = 0;
        for (int i = 200; i < 300; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax < 1.0, $"tail max = {tailMax}; HP should strongly attenuate slow trend");
    }

    [Fact]
    public void Apply_HighFrequencyNoise_PassedThrough()
    {
        // Alternating ±1 is far above the cutoff — passes through with some gain/phase shift.
        var signal = new double[200];
        for (int i = 0; i < 200; i++) signal[i] = (i % 2 == 0) ? 1 : -1;
        var r = signal.AsSpan().HighPass(period: 20);
        // Settled tail amplitude should be non-trivial.
        double tailMax = 0;
        for (int i = 100; i < 200; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax > 0.5, $"tail max = {tailMax}; HF signal should pass through");
    }

    [Fact]
    public void Apply_OutputLengthMatchesInput()
    {
        var signal = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var r = signal.AsSpan().HighPass(period: 10);
        Assert.Equal(50, r.Length);
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }
}
