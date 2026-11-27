// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The windows a series of values is read through: the mean of each full window, the lowest and the highest value of
/// each window (a partial one at the start), and the population standard deviation of each full window about its mean. The
/// indicators and the relative rotation graph read them; these tests read them on their own.</summary>
public sealed class RollingWindowExtensionsTests
{
    [Fact]
    public void TheMean_IsTakenOverEveryFullWindow()
    {
        double[] x = [10.0, 20.0, 30.0, 40.0, 50.0];

        Assert.Equal([20.0, 30.0, 40.0], x.RollingMean(3));
    }

    [Fact]
    public void AWindowAsLongAsTheValues_HasOneMean_AndALongerOneNone()
    {
        double[] x = [10.0, 20.0, 30.0];

        Assert.Equal([20.0], x.RollingMean(3));
        Assert.Empty(x.RollingMean(4));
    }

    public static TheoryData<Action<double[], double[]>, double[], double[]> TheWindowsOfThree => new()
    {
        { (x, dst) => x.RollingMin(3, dst), [5.0, 3.0, 4.0, 1.0, 6.0, 6.0, 2.0], [5.0, 3.0, 3.0, 1.0, 1.0, 1.0, 2.0] },
        { (x, dst) => x.RollingMax(3, dst), [1.0, 5.0, 3.0, 4.0, 2.0, 2.0, 7.0], [1.0, 5.0, 5.0, 5.0, 4.0, 4.0, 7.0] },
    };

    [Theory]
    [MemberData(nameof(TheWindowsOfThree))]
    public void TheLowestAndTheHighest_AreThatOfEachWindow_APartialOneAtTheStart(Action<double[], double[]> roll, double[] x, double[] expected)
    {
        var dst = new double[x.Length];
        roll(x, dst);
        Assert.Equal(expected, dst);
    }

    [Fact]
    public void TheLowestAndTheHighest_OfAWindowOfOne_AreTheValuesThemselves()
    {
        double[] x = [3.0, -1.0, 8.0];
        var low = new double[3];
        var high = new double[3];

        x.RollingMin(1, low);
        x.RollingMax(1, high);

        Assert.Equal(x, low);
        Assert.Equal(x, high);
    }

    /// <summary>The mean of every window and the mean a streaming average says are one number: <c>RollingMean</c> sums the first window in order
    /// and then adds the value that comes in less the value that goes out, which is what <see cref="RollingMeanStep"/> does value by
    /// value, so the batch indicators and their streaming forms agree to the last bit and not to a tolerance.</summary>
    [Theory]
    [InlineData(1, 40)]
    [InlineData(2, 40)]
    [InlineData(3, 3)]
    [InlineData(14, 200)]
    [InlineData(20, 1000)]
    [InlineData(50, 51)]
    public void TheMean_IsBitForBitTheFoldOfTheStreamingStep(int period, int length)
    {
        var random = new Random(period * 1000 + length);
        double[] x = [.. Enumerable.Range(0, length).Select(i => (random.NextDouble() - 0.4) * Math.Pow(10, i % 7) + random.NextDouble() * 1e-9)];

        double[] batch = x.RollingMean(period);

        var step = new RollingMeanStep(period);
        var streamed = new List<double>();
        for (int i = 0; i < x.Length; i++)
        {
            double mean = step.Next(x[i]);
            if (i >= period - 1) streamed.Add(mean);
        }

        Assert.Equal(streamed.Count, batch.Length);
        for (int i = 0; i < batch.Length; i++)
        {
            Assert.Equal(BitConverter.DoubleToInt64Bits(streamed[i]), BitConverter.DoubleToInt64Bits(batch[i]));
        }
    }

    [Fact]
    public void TheStandardDeviation_IsThePopulationOneAboutEachWindowsMean()
    {
        double[] x = [10.0, 10.0, 20.0, 20.0, 10.0];
        var means = x.RollingMean(4);
        var dst = new double[means.Length];

        x.RollingStdDev(4, means, dst);

        Assert.Equal(5.0, dst[0], 1e-10);
        Assert.Equal(Math.Sqrt(((10 - 15.0) * (10 - 15.0) + (20 - 15.0) * (20 - 15.0) * 2 + (10 - 15.0) * (10 - 15.0)) / 4), dst[1], 1e-10);
    }
}
