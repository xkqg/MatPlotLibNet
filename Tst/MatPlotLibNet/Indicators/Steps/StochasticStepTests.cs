// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Tests.Indicators.Steps;

/// <summary>The stochastic oscillator one bar at a time: the raw %K of the last <c>k</c> bars once, a mean of it for the smoothed
/// %K, and a mean of that for %D. The batch <c>Stochastic</c> folds it over its bars and <c>StreamingStochastic</c> takes a bar at
/// a time, so the formula exists here only.</summary>
public sealed class StochasticStepTests
{
    private static (double[] High, double[] Low, double[] Close) Bars(int count)
    {
        var random = new Random(11);
        var high = new double[count];
        var low = new double[count];
        var close = new double[count];
        double previous = 100;
        for (int i = 0; i < count; i++)
        {
            double middle = previous = Math.Round(previous + (random.NextDouble() - 0.5) * 6, 2);
            high[i] = middle + 0.5 + Math.Round(random.NextDouble(), 2);
            low[i] = middle - 0.5 - Math.Round(random.NextDouble(), 2);
            close[i] = Math.Round(low[i] + (high[i] - low[i]) * random.NextDouble(), 2);
        }
        return (high, low, close);
    }

    private static double RawK(double[] high, double[] low, double[] close, int i, int k)
    {
        double highest = high[(i - k + 1)..(i + 1)].Max();
        double lowest = low[(i - k + 1)..(i + 1)].Min();
        double range = highest - lowest;
        return range > 0 ? (close[i] - lowest) / range * 100 : 50;
    }

    [Fact]
    public void BeforeKBars_BothLinesSayNaN_AndTheNextBarGivesTheRawK()
    {
        var (high, low, close) = Bars(30);
        var step = new StochasticStep(kPeriod: 5, smoothK: 1, dPeriod: 3);

        for (int i = 0; i < 4; i++)
        {
            var point = step.Next(high[i], low[i], close[i]);
            Assert.True(double.IsNaN(point.K) && double.IsNaN(point.D), $"bar {i}");
        }

        Assert.Equal(RawK(high, low, close, 4, 5), step.Next(high[4], low[4], close[4]).K, 12);
    }

    [Fact]
    public void WithoutSmoothing_KIsTheRawK_AndDIsItsMeanOverTheLastDValues()
    {
        var (high, low, close) = Bars(60);
        var step = new StochasticStep(kPeriod: 14, smoothK: 1, dPeriod: 3);

        for (int i = 0; i < 60; i++)
        {
            var point = step.Next(high[i], low[i], close[i]);
            if (i < 13) continue;
            Assert.Equal(RawK(high, low, close, i, 14), point.K);
            if (i < 15) { Assert.True(double.IsNaN(point.D), $"bar {i}"); continue; }
            double mean = (RawK(high, low, close, i, 14) + RawK(high, low, close, i - 1, 14) + RawK(high, low, close, i - 2, 14)) / 3;
            Assert.Equal(mean, point.D, 9);
        }
    }

    [Fact]
    public void WithSmoothing_KIsTheMeanOfTheLastSmoothKRawValues_AndComesSmoothKMinusOneBarsLater()
    {
        var (high, low, close) = Bars(60);
        var step = new StochasticStep(kPeriod: 14, smoothK: 3, dPeriod: 3);

        for (int i = 0; i < 60; i++)
        {
            var point = step.Next(high[i], low[i], close[i]);
            if (i < 15) { Assert.True(double.IsNaN(point.K), $"bar {i}"); continue; }
            double mean = (RawK(high, low, close, i, 14) + RawK(high, low, close, i - 1, 14) + RawK(high, low, close, i - 2, 14)) / 3;
            Assert.Equal(mean, point.K, 9);
            if (i < 17) Assert.True(double.IsNaN(point.D), $"bar {i}");
            else point.D.AssertFinite($"bar {i}");
        }
    }

    [Fact]
    public void AWindowWithNoRange_IsFiftyPercent()
    {
        var step = new StochasticStep(kPeriod: 2, smoothK: 1, dPeriod: 1);

        step.Next(10, 10, 10);
        var point = step.Next(10, 10, 10);

        Assert.Equal(50.0, point.K);
    }

    [Fact]
    public void ACloseAtTheTopOfTheRange_IsOneHundred_AndAtTheBottomZero()
    {
        var up = new StochasticStep(kPeriod: 2, smoothK: 1, dPeriod: 1);
        up.Next(12, 8, 10);
        Assert.Equal(100.0, up.Next(14, 9, 14).K);

        var down = new StochasticStep(kPeriod: 2, smoothK: 1, dPeriod: 1);
        down.Next(12, 8, 10);
        Assert.Equal(0.0, down.Next(14, 6, 6).K);
    }
}
