// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;

namespace MatPlotLibNet.Tests.Indicators.Ehlers;

/// <summary>Verifies <see cref="SuperSmoother"/>. Two-pole Butterworth low-pass.
/// First 2 bars equal the input; third bar onward follows the recurrence.</summary>
public class SuperSmootherTests
{
    [Fact]
    public void Apply_EmptyInput_ReturnsEmpty()
    {
        var result = ReadOnlySpan<double>.Empty.SuperSmooth(period: 10);
        Assert.Empty(result);
    }

    [Fact]
    public void Apply_PeriodBelowTwo_Throws()
    {
        double[] signal = [1, 2, 3, 4];
        Assert.Throws<ArgumentException>(() => signal.AsSpan().SuperSmooth(period: 1));
        Assert.Throws<ArgumentException>(() => signal.AsSpan().SuperSmooth(period: 0));
    }

    [Fact]
    public void Apply_SingleBar_EqualsInput()
    {
        double[] signal = [42.0];
        var r = signal.AsSpan().SuperSmooth(period: 5);
        Assert.Single(r);
        Assert.Equal(42.0, r[0], precision: 12);
    }

    [Fact]
    public void Apply_TwoBars_EqualInput()
    {
        double[] signal = [10.0, 20.0];
        var r = signal.AsSpan().SuperSmooth(period: 5);
        Assert.Equal(10.0, r[0], precision: 12);
        Assert.Equal(20.0, r[1], precision: 12);
    }

    [Fact]
    public void Apply_ConstantSignal_SmoothedEqualsConstant()
    {
        double[] signal = Enumerable.Repeat(50.0, 100).ToArray();
        var r = signal.AsSpan().SuperSmooth(period: 10);
        Assert.All(r, v =>
        {
            Assert.False(double.IsNaN(v));
            Assert.Equal(50.0, v, precision: 10);
        });
    }

    [Fact]
    public void Apply_StepInput_ConvergesToNewLevel()
    {
        // Step input: first 50 bars at 0, next 50 at 100. Settled tail should approach 100.
        var signal = new double[100];
        for (int i = 50; i < 100; i++) signal[i] = 100;
        var r = signal.AsSpan().SuperSmooth(period: 10);
        Assert.True(Math.Abs(r[^1] - 100) < 0.1, $"tail = {r[^1]}, expected ~100");
    }

    [Fact]
    public void Apply_HighFrequencyNoise_AttenuatedAtLongPeriod()
    {
        // Alternating ±1 at the Nyquist is dramatically attenuated by a period=20 LP.
        var signal = new double[200];
        for (int i = 0; i < 200; i++) signal[i] = (i % 2 == 0) ? 1 : -1;
        var r = signal.AsSpan().SuperSmooth(period: 20);
        // Settled tail amplitude should be much less than input amplitude (1.0).
        double tailMax = 0;
        for (int i = 100; i < 200; i++) tailMax = Math.Max(tailMax, Math.Abs(r[i]));
        Assert.True(tailMax < 0.3, $"expected strong attenuation, got tail amplitude {tailMax}");
    }

    [Fact]
    public void Apply_OutputLengthMatchesInput()
    {
        var signal = Enumerable.Range(0, 30).Select(i => (double)i).ToArray();
        var r = signal.AsSpan().SuperSmooth(period: 10);
        Assert.Equal(30, r.Length);
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }
}
