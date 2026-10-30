// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Transfer entropy over time: with a <see cref="TransferEntropyOptions.Window"/> the indicator gives one value for each bar,
/// the entropy of the last <c>Window</c> bars, stamped at the right edge of the window (the bar it is known at, never earlier), and
/// NaN until the first window is full. Only the Kraskov estimate is rolled: the binned estimate is biased upward on a window.</summary>
public sealed class TransferEntropyRollingTests
{
    private sealed class Lcg(ulong seed)
    {
        private ulong _state = seed;

        public double Uniform()
        {
            _state = unchecked(_state * 6364136223846793005UL + 1442695040888963407UL);
            return (_state >> 11) / (double)(1UL << 53);
        }

        public double Normal()
        {
            double sum = 0;
            for (int i = 0; i < 12; i++) sum += Uniform();
            return sum - 6;
        }
    }

    /// <summary>X standard normal and Y(t+1) = c(t)·X(t) + noise, with c = 0 before the bar <paramref name="switchOn"/> and 1 from it.</summary>
    private static (double[] X, double[] Y) Switching(int n, int switchOn, ulong seed)
    {
        var rng = new Lcg(seed);
        var x = new double[n];
        var noise = new double[n];
        for (int t = 0; t < n; t++) x[t] = rng.Normal();
        for (int t = 0; t < n; t++) noise[t] = rng.Normal();
        var y = new double[n];
        y[0] = noise[0];
        for (int t = 0; t + 1 < n; t++) y[t + 1] = (t >= switchOn ? 1 : 0) * x[t] + noise[t + 1];
        return (x, y);
    }

    /// <summary>The coupling is off for the first half and on for the second: the rolling entropy sits near 0 and then rises to the
    /// ½·ln 2 = 0.3466 of a coupling of 1 against noise of 1, once the window is mostly in the second half.</summary>
    [Fact]
    public void TheRollingEntropy_FollowsACouplingThatSwitchesOn()
    {
        var (x, y) = Switching(1000, switchOn: 500, seed: 7);

        double[] te = new TransferEntropy(x, y, new TransferEntropyOptions(Neighbours: 4, Window: 250, Stride: 10)).Compute();

        Assert.Equal(1000, te.Length);
        Assert.All(te[..249], v => Assert.True(double.IsNaN(v)));
        for (int end = 249; end <= 499; end += 10) Assert.InRange(Math.Abs(te[end]), 0, 0.08);
        for (int end = 749; end <= 899; end += 10) Assert.InRange(te[end], 0.20, 0.45);
    }

    /// <summary>A bar that comes later changes no value that came before it: a value is the entropy of a window that ends at its bar.</summary>
    [Fact]
    public void AValue_NeverDependsOnABarAfterIt()
    {
        var (x, y) = Switching(300, switchOn: 150, seed: 3);
        var options = new TransferEntropyOptions(Neighbours: 3, Window: 60, Stride: 5);

        double[] shorter = new TransferEntropy(x[..220], y[..220], options).Compute();
        double[] longer = new TransferEntropy(x, y, options).Compute();

        for (int i = 0; i < shorter.Length; i++)
            Assert.Equal(shorter[i], longer[i]);   // NaN equals NaN here; every other value is the same to the last bit
    }

    /// <summary>The stride picks which windows are worked out, counted from the first full one; the bars between are NaN.</summary>
    [Fact]
    public void TheStride_WorksOutEveryNthWindow_AndLeavesTheBarsBetweenNaN()
    {
        var (x, y) = Switching(120, switchOn: 60, seed: 5);
        const int window = 40;

        double[] every = new TransferEntropy(x, y, new TransferEntropyOptions(Neighbours: 3, Window: window)).Compute();
        double[] third = new TransferEntropy(x, y, new TransferEntropyOptions(Neighbours: 3, Window: window, Stride: 3)).Compute();

        for (int i = 0; i < 120; i++)
        {
            bool worked = i >= window - 1 && (i - (window - 1)) % 3 == 0;
            if (worked) Assert.Equal(every[i], third[i]);
            else Assert.True(double.IsNaN(third[i]), $"bar {i} is not a stride bar");
        }
    }

    [Fact]
    public void ASeriesShorterThanTheWindow_HasNoValueYet_NotAnError()
    {
        double[] te = new TransferEntropy(new double[30], new double[30], new TransferEntropyOptions(Window: 50)).Compute();

        Assert.Equal(30, te.Length);
        Assert.All(te, v => Assert.True(double.IsNaN(v)));
    }

    [Fact]
    public void WithoutAWindow_TheResultIsTheSingleValueOfTheWholeSeries()
    {
        var (x, y) = Switching(200, switchOn: 0, seed: 2);

        Assert.Single(new TransferEntropy(x, y, new TransferEntropyOptions()).Compute().Values);
    }

    /// <summary>A window that cannot hold a neighbour for every sample is refused by name: it needs lag + neighbours + 1 bars.</summary>
    [Theory]
    [InlineData(4, 1, 5)]
    [InlineData(4, 1, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(3, 2, 5)]
    public void AWindowTooShortForTheNeighbours_IsRefusedByName(int neighbours, int lag, int window)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new TransferEntropy(new double[100], new double[100], new TransferEntropyOptions(Neighbours: neighbours, Lag: lag, Window: window)));

        Assert.Equal("Window", ex.ParamName);
    }

    [Fact]
    public void TheSmallestWindowThatHoldsTheNeighbours_IsAccepted() =>
        Assert.Equal(20, new TransferEntropy(new double[20], new double[20], new TransferEntropyOptions(Neighbours: 4, Window: 6)).Compute().Values.Length);

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void AStrideBelowOne_IsRefusedByName(int stride) =>
        Assert.Equal("Stride", Assert.Throws<ArgumentException>(() =>
            new TransferEntropy(new double[50], new double[50], new TransferEntropyOptions(Window: 20, Stride: stride))).ParamName);

    [Fact]
    public void AWindowWithTheBinnedEstimator_IsRefusedByName() =>
        Assert.Equal("Window", Assert.Throws<ArgumentException>(() =>
            new TransferEntropy(new double[50], new double[50], new TransferEntropyOptions(TransferEntropyEstimator.Binned, Window: 20))).ParamName);

    [Fact]
    public void AStrideWithoutAWindow_IsRefusedByName() =>
        Assert.Equal("Stride", Assert.Throws<ArgumentException>(() =>
            new TransferEntropy(new double[50], new double[50], new TransferEntropyOptions(Stride: 5))).ParamName);

    [Fact]
    public void TheLabel_NamesTheWindow()
    {
        Assert.Equal("TE(k=4,lag=1,W=250)",
            new TransferEntropy(new double[300], new double[300], new TransferEntropyOptions(Window: 250)).Label);
    }

    /// <summary>A rolling entropy is a line; the scalar is not drawn.</summary>
    [Fact]
    public void Apply_DrawsTheRollingEntropyAsALine_AndTheScalarNot()
    {
        var (x, y) = Switching(120, switchOn: 0, seed: 6);
        var rolling = new Axes();
        var scalar = new Axes();

        new TransferEntropy(x, y, new TransferEntropyOptions(Neighbours: 3, Window: 40, Stride: 4)).Apply(rolling);
        new TransferEntropy(x, y, new TransferEntropyOptions(Neighbours: 3)).Apply(scalar);

        Assert.Single(rolling.Series);
        Assert.Empty(scalar.Series);
    }

    /// <summary>The line is drawn through the windows that were worked out, at the bar each ends at: with a stride the bars between are
    /// not values, and a line broken at every one of them would draw nothing at all.</summary>
    [Fact]
    public void Apply_DrawsALineThroughTheWorkedOutWindows_AtTheBarsTheyEndAt()
    {
        var (x, y) = Switching(120, switchOn: 0, seed: 6);
        var axes = new Axes();

        new TransferEntropy(x, y, new TransferEntropyOptions(Neighbours: 3, Window: 40, Stride: 4)).Apply(axes);

        var line = Assert.IsType<LineSeries>(Assert.Single(axes.Series));
        double[] ends = [.. Enumerable.Range(0, 21).Select(k => 39.0 + 4 * k)];   // 39, 43, ..., 119
        Assert.Equal(ends, line.XData);
        Assert.All(line.YData, v => Assert.False(double.IsNaN(v)));
    }

    /// <summary>The coupling that copies the source into the target one bar later: the binned entropy is ln 2 for (0,0,0,1,0,1,1,1)
    /// repeated, and 0 the other way.</summary>
    [Fact]
    public void TheBinnedEstimator_FindsTheCopyCase_ToTheAnalyticValue()
    {
        const int repeats = 100;
        double[] pattern = [0, 0, 0, 1, 0, 1, 1, 1];
        double[] x = [.. Enumerable.Range(0, pattern.Length * repeats).Select(i => pattern[i % pattern.Length])];
        var y = new double[x.Length];
        y[0] = 1;
        for (int t = 1; t < x.Length; t++) y[t] = x[t - 1];
        var binned = new TransferEntropyOptions(TransferEntropyEstimator.Binned, Bins: 2, Lag: 1);

        double forward = new TransferEntropy(x, y, binned).Compute().Values[0];
        double backward = new TransferEntropy(y, x, binned).Compute().Values[0];

        Assert.InRange(Math.Abs(forward - Math.Log(2)), 0, 1e-5);
        Assert.InRange(backward, 0, 1e-4);
    }
}
