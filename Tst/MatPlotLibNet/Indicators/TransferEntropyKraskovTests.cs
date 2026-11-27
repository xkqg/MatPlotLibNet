// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Transfer entropy by the Kraskov estimator: a k-nearest-neighbour estimate that needs no bins, the default for
/// <see cref="TransferEntropyOptions"/>. The analytic cases are the ones with a known answer: <c>Y(t+1) = c·X(t) + ε</c> carries
/// <c>½·ln(1 + c²/σ²)</c> nats from X to Y and nothing back, and a common driver carries information to both that conditioning on
/// it removes. The estimate can be below 0 for independent series, and is not clamped.</summary>
public sealed class TransferEntropyKraskovTests
{
    /// <summary>n values from the generator, one array after another.</summary>
    private static double[] Normals(SeededNormals rng, int n)
    {
        var values = new double[n];
        for (int t = 0; t < n; t++) values[t] = rng.Normal();
        return values;
    }

    /// <summary>X standard normal; Y(t+1) = c·X(t) + noise of standard deviation sigma.</summary>
    private static (double[] X, double[] Y) Coupled(int n, double c, double sigma, ulong seed)
    {
        var rng = new SeededNormals(seed);
        var x = Normals(rng, n);
        var noise = Normals(rng, n);
        var y = new double[n];
        y[0] = noise[0];
        for (int t = 0; t + 1 < n; t++) y[t + 1] = c * x[t] + sigma * noise[t + 1];
        return (x, y);
    }

    private static double Te(double[] source, double[] target, TransferEntropyOptions options) =>
        new TransferEntropy(source, target, options).Compute().Values[0];

    [Fact]
    public void TheDefaultEstimator_IsKraskov_WithFourNeighbours_AndTheDefaultsOfTheBinnedOne()
    {
        var options = new TransferEntropyOptions();

        Assert.Equal(TransferEntropyEstimator.Kraskov, options.Estimator);
        Assert.Equal(4, options.Neighbours);
        Assert.Equal(8, options.Bins);
        Assert.Equal(1, options.Lag);
        Assert.Null(options.Condition);
    }

    /// <summary>c = 1 and sigma = 1: TE(X to Y) = ½·ln 2 = 0.3466 and TE(Y to X) = 0.</summary>
    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void TheGaussianCouplingCase_IsHalfLnTwo_AndZeroTheOtherWay(ulong seed)
    {
        var (x, y) = Coupled(700, c: 1, sigma: 1, seed);
        var options = new TransferEntropyOptions(Neighbours: 4);

        double forward = Te(x, y, options);
        double backward = Te(y, x, options);

        Assert.InRange(forward, 0.3466 - 0.06, 0.3466 + 0.06);
        Assert.InRange(Math.Abs(backward), 0, 0.03);
    }

    /// <summary>X = C + e1 and Y(t+1) = C + e2: the common driver C makes X look like it leads Y, TE = ½·ln(4/3) = 0.1438; given C,
    /// X adds nothing.</summary>
    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void ACommonDriver_FoolsTheUnconditionalEstimate_AndTheConditionalOneSeesThroughIt(ulong seed)
    {
        const int n = 700;
        var rng = new SeededNormals(seed);
        var driver = Normals(rng, n);
        var noiseOfX = Normals(rng, n);
        var noiseOfY = Normals(rng, n);
        var x = new double[n];
        var y = new double[n];
        for (int t = 0; t < n; t++) x[t] = driver[t] + noiseOfX[t];
        y[0] = noiseOfY[0];
        for (int t = 0; t + 1 < n; t++) y[t + 1] = driver[t] + noiseOfY[t + 1];

        double unconditional = Te(x, y, new TransferEntropyOptions(Neighbours: 4));
        double conditional = Te(x, y, new TransferEntropyOptions(Neighbours: 4, Condition: driver));

        Assert.InRange(unconditional, 0.1438 - 0.06, 0.1438 + 0.06);
        Assert.InRange(Math.Abs(conditional), 0, 0.06);
    }

    /// <summary>Scaling and shifting either series changes no distance order, because every variable is standardised first.</summary>
    [Fact]
    public void AffineChangesOfTheSeries_ChangeNothing()
    {
        var (x, y) = Coupled(300, c: 1, sigma: 1, seed: 5);
        var options = new TransferEntropyOptions(Neighbours: 4);

        double plain = Te(x, y, options);
        double changed = Te([.. x.Select(v => 3.5 * v + 12)], [.. y.Select(v => 0.02 * v - 4)], options);

        Assert.Equal(plain, changed, 9);
    }

    /// <summary>A series that never moves has nothing to give or to take: the entropy is 0, not NaN, and nothing throws.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AConstantSeries_GivesZero_NotNaN(bool sourceIsConstant)
    {
        var (x, y) = Coupled(200, c: 1, sigma: 1, seed: 9);
        double[] constant = [.. Enumerable.Repeat(3.25, 200)];

        double te = sourceIsConstant ? Te(constant, y, new TransferEntropyOptions()) : Te(x, constant, new TransferEntropyOptions());

        Assert.Equal(0, te, 12);
    }

    /// <summary>Independent series give an estimate around 0, and it is allowed to be below it: clamping at 0 would bias the mean up.</summary>
    [Fact]
    public void ForIndependentSeries_TheEstimateIsAroundZero_AndMayBeNegative()
    {
        int below = 0;
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var rng = new SeededNormals(seed);
            double[] a = [.. Enumerable.Range(0, 150).Select(_ => rng.Normal())];
            double[] b = [.. Enumerable.Range(0, 150).Select(_ => rng.Normal())];
            double te = Te(a, b, new TransferEntropyOptions());
            Assert.InRange(te, -0.15, 0.15);
            if (te < 0) below++;
        }

        Assert.InRange(below, 1, 11);
    }

    /// <summary>Ties among samples (tick-quantised returns repeat exactly) are broken by a jitter of 1e-10 keyed by the index, so a
    /// series that repeats values gives a number, the same one every time.</summary>
    [Fact]
    public void TiedSamples_GiveANumber_TheSameEveryTime()
    {
        double[] x = [.. Enumerable.Range(0, 120).Select(i => (double)(i * 7 % 5))];
        double[] y = [.. Enumerable.Range(0, 120).Select(i => (double)(i * 3 % 4))];

        double first = Te(x, y, new TransferEntropyOptions());
        double second = Te(x, y, new TransferEntropyOptions());

        first.AssertFinite();
        Assert.Equal(first, second);
    }

    /// <summary>The older constructor is the binned estimator and gives what it always gave.</summary>
    [Fact]
    public void TheBinnedEstimatorOfTheOptions_IsTheOlderConstructor()
    {
        var (x, y) = Coupled(400, c: 1, sigma: 1, seed: 4);

        double older = new TransferEntropy(x, y, bins: 6, lag: 2).Compute().Values[0];
        double options = new TransferEntropy(x, y, new TransferEntropyOptions(TransferEntropyEstimator.Binned, Bins: 6, Lag: 2)).Compute().Values[0];

        Assert.Equal(older, options);
    }

    [Fact]
    public void ALag_ReadsTheSourceThatManyBarsBack()
    {
        // Y(t+2) = X(t): with lag 2 the source is read two bars back from the future of the target, so the flow is seen at lag 2.
        var rng = new SeededNormals(11);
        const int n = 600;
        var x = new double[n];
        var y = new double[n];
        for (int t = 0; t < n; t++) x[t] = rng.Normal();
        y[0] = rng.Normal();
        y[1] = rng.Normal();
        for (int t = 0; t + 2 < n; t++) y[t + 2] = x[t] + 0.5 * rng.Normal();

        double atLagTwo = Te(x, y, new TransferEntropyOptions(Lag: 2));
        double atLagOne = Te(x, y, new TransferEntropyOptions(Lag: 1));

        Assert.True(atLagTwo > atLagOne + 0.3, $"lag 2 gives {atLagTwo}, lag 1 gives {atLagOne}");
    }

    [Fact]
    public void TheLabel_NamesTheEstimator()
    {
        var (x, y) = Coupled(50, 1, 1, 1);

        Assert.Equal("TE(k=4,lag=1)", new TransferEntropy(x, y, new TransferEntropyOptions()).Label);
        Assert.Equal("TE(bins=6,lag=2)", new TransferEntropy(x, y, new TransferEntropyOptions(TransferEntropyEstimator.Binned, Bins: 6, Lag: 2)).Label);
    }
}
