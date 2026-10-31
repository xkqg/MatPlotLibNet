// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Correlation"/>: the cross-correlation of two signals as matplotlib's <c>Axes.xcorr</c> works it
/// out (<c>np.correlate(x, y, "full")</c>, so the value at lag k is the sum over n of x[n + k] · y[n]), for the lags
/// −MaxLags .. MaxLags, divided by √(x·x) · √(y·y) unless the options ask for the raw sums, after the detrend. The vectors
/// marked "matplotlib" were printed by matplotlib 3.11.2 (numpy 2) and are compared by tolerance, never by stored bytes.</summary>
public class CorrelationTests
{
    private const double Tolerance = 1e-12;

    private const double Gap = double.NaN;

    /// <summary>Two signals of eight integers that share no obvious structure, so no vector below is a symmetry in disguise.</summary>
    private static readonly double[] X = [3, -1, 4, 1, -5, 9, 2, -6];

    private static readonly double[] Y = [2, 7, 1, -8, 2, 8, 1, -8];

    private static CorrelationOptions Lags(int maxLags) => new() { MaxLags = maxLags };

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual, double tolerance = Tolerance)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            if (double.IsNaN(expected[i]))
            {
                Assert.True(double.IsNaN(actual[i]), $"[{i}]: expected NaN, got {actual[i]:R}");
            }
            else
            {
                Assert.True(Math.Abs(expected[i] - actual[i]) <= tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
            }
        }
    }

    // ---- matplotlib's vectors ----------------------------------------------------------------------------------------------

    [Fact]
    public void TheAutocorrelation_OfOneTwoThree_IsMatplotlibs()
    {
        // matplotlib: acorr([1, 2, 3], maxlags=1) = [0.571429, 1, 0.571429]; the full sums are [3, 8, 14, 8, 3].
        Correlogram correlogram = Correlation.Auto([1, 2, 3], Lags(1));

        AssertClose([-1, 0, 1], correlogram.Lags);
        AssertClose([8 / 14.0, 1, 8 / 14.0], correlogram.Values);
    }

    [Fact]
    public void ThePositiveLag_IsXDelayedAgainstY()
    {
        // matplotlib: xcorr([0, 1], [1, 0], maxlags=1) = [0, 0, 1] at the lags -1, 0, 1: x is y one sample later, so the peak is at +1.
        Correlogram correlogram = Correlation.Cross([0, 1], [1, 0], Lags(1));

        AssertClose([-1, 0, 1], correlogram.Lags);
        AssertClose([0, 0, 1], correlogram.Values);
    }

    [Fact]
    public void ASignalDelayedByTwo_PeaksAtMinusTwo_AndTheOtherWayRoundAtPlusTwo()
    {
        // y is x two samples later. matplotlib: xcorr(x, y, maxlags=3) peaks at lag -2; xcorr(y, x, maxlags=3) at +2.
        double[] x = [0, 0, 1, 3, 2, 0, 0, 0];
        double[] y = [0, 0, 0, 0, 1, 3, 2, 0];

        AssertClose([9 / 14.0, 1, 9 / 14.0, 2 / 14.0, 0, 0, 0], Correlation.Cross(x, y, Lags(3)).Values);
        AssertClose([0, 0, 0, 2 / 14.0, 9 / 14.0, 1, 9 / 14.0], Correlation.Cross(y, x, Lags(3)).Values);
    }

    [Fact]
    public void TheCrossCorrelation_OfTwoSignals_IsMatplotlibs()
    {
        Correlogram correlogram = Correlation.Cross(X, Y, Lags(3));

        AssertClose([-3, -2, -1, 0, 1, 2, 3], correlogram.Lags);
        AssertClose(
            [0.22554746258196273, -0.2399441091297476, -0.2735362844079122, 0.5134803935376598, 0.4558938073465204, -0.5086815113550649, -0.24954187349493748],
            correlogram.Values);
    }

    [Fact]
    public void TheMostLagsThereAre_AreAllButTheLast()
    {
        // maxlags = n - 1 is the longest matplotlib accepts: every lag with an overlap.
        Correlogram correlogram = Correlation.Cross(X, Y, Lags(7));

        Assert.Equal(15, correlogram.Lags.Length);
        AssertClose(
        [
            -0.11517317238227884, 0.05278770400854447, -0.04318993964335457, -0.02879329309556971, 0.22554746258196273,
            -0.2399441091297476, -0.2735362844079122, 0.5134803935376598, 0.4558938073465204, -0.5086815113550649,
            -0.24954187349493748, 0.49428486480728, 0.12477093674746874, -0.18235752293860816, -0.05758658619113942,
        ], correlogram.Values);
    }

    [Fact]
    public void TheAutocorrelation_IsOneAtLagZero_AndSymmetric()
    {
        Correlogram correlogram = Correlation.Auto(X, Lags(3));

        AssertClose([0.4393063583815029, -0.3699421965317919, -0.27167630057803466, 1, -0.27167630057803466, -0.3699421965317919, 0.4393063583815029], correlogram.Values);
        Assert.Equal(1.0, correlogram.Values[3], 12);
    }

    [Fact]
    public void TheShortestSignalThatCanBeCorrelated_IsTwoSamples_WithOneLag()
    {
        // matplotlib: acorr([1, 2], maxlags=1) = [0.4, 1, 0.4].
        AssertClose([0.4, 1, 0.4], Correlation.Auto([1, 2], Lags(1)).Values);
    }

    // ---- the raw sums ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Unnormalised_IsTheSumOfProducts_AsNumpyCorrelateGivesIt()
    {
        var options = new CorrelationOptions { MaxLags = 3, Unnormalised = true };

        AssertClose([47, -50, -57, 107, 95, -106, -52], Correlation.Cross(X, Y, options).Values);
        AssertClose([8, 14, 8], Correlation.Auto([1, 2, 3], new CorrelationOptions { MaxLags = 1, Unnormalised = true }).Values);
        AssertClose([11, 20, 30, 20, 11], Correlation.Auto([1, 2, 3, 4], new CorrelationOptions { MaxLags = 2, Unnormalised = true }).Values);
    }

    [Fact]
    public void Unnormalised_OfASignalWithNoEnergy_IsZero_NotAGap()
    {
        var options = new CorrelationOptions { MaxLags = 1, Unnormalised = true };

        AssertClose([0, 0, 0], Correlation.Cross([0, 0, 0], [1, 2, 3], options).Values);
    }

    // ---- the detrend -------------------------------------------------------------------------------------------------------

    [Fact]
    public void ADetrendOfTheMean_IsAppliedToBothSignals_BeforeTheSums()
    {
        // matplotlib: xcorr(x, y, detrend=mlab.detrend_mean, maxlags=3).
        var options = new CorrelationOptions { MaxLags = 3, Detrend = SpectralDetrend.Mean };

        AssertClose(
            [0.25990223898537623, -0.246304043212272, -0.31429502207779314, 0.5045929369364325, 0.4177027707252411, -0.572046134048215, -0.2625143217892494],
            Correlation.Cross(X, Y, options).Values);
        // [1, 2, 3] less its mean is [-1, 0, 1]: matplotlib gives [-0.5, 0, 1, 0, -0.5].
        AssertClose([-0.5, 0, 1, 0, -0.5], Correlation.Auto([1, 2, 3], new CorrelationOptions { MaxLags = 2, Detrend = SpectralDetrend.Mean }).Values);
    }

    [Fact]
    public void ALinearDetrend_IsAppliedToBothSignals_BeforeTheSums()
    {
        // matplotlib: xcorr(x, y, detrend=mlab.detrend_linear, maxlags=3).
        var options = new CorrelationOptions { MaxLags = 3, Detrend = SpectralDetrend.Linear };

        AssertClose(
            [0.23181971048069597, -0.18272162518379417, -0.3097736279039415, 0.46528401837538447, 0.4770840048207807, -0.6451275980846917, -0.33208533282044],
            Correlation.Cross(X, Y, options).Values);
    }

    [Fact]
    public void ADetrendOfTheCallersOwn_IsUsed()
    {
        // The first sample taken off every sample: [1, 2, 3] becomes [0, 1, 2], whose raw sums at lags -1, 0, 1 are 2, 5, 2.
        var options = new CorrelationOptions
        {
            MaxLags = 1,
            Unnormalised = true,
            Detrend = SpectralDetrend.Of(signal => [.. signal.Select(value => value - signal[0])]),
        };

        AssertClose([2, 5, 2], Correlation.Auto([1, 2, 3], options).Values);
    }

    [Fact]
    public void ASignalThatIsNothingButItsTrend_HasNoEnergyLeft_AndIsAGap()
    {
        // [1, 2, 3] is a straight line: matplotlib gives nan (0 / 0) at every lag, with a divide warning.
        var linear = new CorrelationOptions { MaxLags = 1, Detrend = SpectralDetrend.Linear };
        var mean = new CorrelationOptions { MaxLags = 1, Detrend = SpectralDetrend.Mean };

        AssertClose([Gap, Gap, Gap], Correlation.Auto([1, 2, 3], linear).Values);
        AssertClose([Gap, Gap, Gap], Correlation.Auto([2, 2, 2], mean).Values);
    }

    [Fact]
    public void TheCallersArrays_AreNotChanged()
    {
        double[] x = [.. X], y = [.. Y];

        Correlation.Cross(x, y, new CorrelationOptions { MaxLags = 3, Detrend = SpectralDetrend.Mean });
        Correlation.Cross(x, y, Lags(3));

        Assert.Equal(X, x);
        Assert.Equal(Y, y);
    }

    // ---- a signal with no energy, a missing sample -------------------------------------------------------------------------

    [Fact]
    public void ASignalOfZeros_HasNoCorrelation_ItIsAGap_NotAZero()
    {
        // matplotlib: xcorr([0, 0, 0], [1, 2, 3], maxlags=1) = [nan, nan, nan].
        Correlogram correlogram = Correlation.Cross([0, 0, 0], [1, 2, 3], Lags(1));

        AssertClose([-1, 0, 1], correlogram.Lags);
        AssertClose([Gap, Gap, Gap], correlogram.Values);
    }

    [Fact]
    public void ATinySignal_IsStillNormalised_BecauseTheRootsAreTakenOneByOne()
    {
        // Each energy is 1.4e-199, a double; the product of the two, 2e-398, is below the smallest one and would be 0.
        double[] tiny = [1e-100, 2e-100, 3e-100];

        AssertClose([8 / 14.0, 1, 8 / 14.0], Correlation.Auto(tiny, Lags(1)).Values);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AMissingSampleInX_MakesEveryLagMissing_TheLagsAreStillGiven(double sample)
    {
        Correlogram correlogram = Correlation.Cross([1, sample, 3, 4], [4, 3, 2, 1], Lags(2));

        AssertClose([-2, -1, 0, 1, 2], correlogram.Lags);
        Assert.All(correlogram.Values, value => Assert.True(double.IsNaN(value)));
    }

    [Fact]
    public void AMissingSampleInY_MakesEveryLagMissing_AlsoWithoutNormalising()
    {
        var options = new CorrelationOptions { MaxLags = 2, Unnormalised = true };

        Correlogram correlogram = Correlation.Cross([1, 2, 3, 4], [4, double.NaN, 2, 1], options);

        Assert.Equal(5, correlogram.Values.Length);
        Assert.All(correlogram.Values, value => Assert.True(double.IsNaN(value)));
    }

    [Fact]
    public void AMissingSample_NeverReachesTheDetrend()
    {
        // The caller's own detrend is not asked about a signal that cannot be correlated; it would have to refuse a NaN.
        var options = new CorrelationOptions { MaxLags = 1, Detrend = SpectralDetrend.Of(_ => throw new InvalidOperationException("asked")) };

        Correlogram correlogram = Correlation.Auto([1, double.NaN, 3], options);

        Assert.All(correlogram.Values, value => Assert.True(double.IsNaN(value)));
    }

    // ---- what it refuses ---------------------------------------------------------------------------------------------------

    [Fact]
    public void ANullSignal_IsRefusedByItsName()
    {
        Assert.Equal("signal", Assert.Throws<ArgumentNullException>(() => Correlation.Auto(null!, Lags(1))).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Correlation.Cross(null!, [1.0, 2.0], Lags(1))).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Correlation.Cross([1.0, 2.0], null!, Lags(1))).ParamName);
    }

    [Fact]
    public void AnEmptySignal_IsRefusedByItsName()
    {
        Assert.Equal("signal", Assert.Throws<ArgumentException>(() => Correlation.Auto([], Lags(1))).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentException>(() => Correlation.Cross([], [], Lags(1))).ParamName);
    }

    [Fact]
    public void SignalsOfOtherLengths_AreRefused_ByTheNameOfY_AndTheMessageNamesBothCounts()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Correlation.Cross([1, 2, 3], [1, 2], Lags(1)));

        Assert.Equal("y", thrown.ParamName);
        Assert.Contains("3", thrown.Message);
        Assert.Contains("2", thrown.Message);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    public void MoreLagsThanTheSignalHasSamplesToSpare_IsRefused_ByTheNameOfTheOptions(int maxLags)
    {
        // matplotlib: "maxlags must be None or strictly positive < 3".
        var thrown = Assert.Throws<ArgumentException>(() => Correlation.Auto([1, 2, 3], Lags(maxLags)));

        Assert.Equal("options", thrown.ParamName);
        Assert.Contains("3", thrown.Message);
    }

    [Fact]
    public void TheDefaultOptions_NeedElevenSamples_AsMatplotlibsDefaultMaxlagsOfTenDoes()
    {
        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Correlation.Auto(new double[10], default)).ParamName);
        Assert.Equal(21, Correlation.Auto([.. Enumerable.Range(1, 11).Select(i => (double)i)], default).Lags.Length);
    }

    [Fact]
    public void OneSample_CannotBeCorrelated_ForNoLagFitsInIt()
    {
        // The fewest lags there are is one (matplotlib refuses maxlags below 1), and one lag needs two samples.
        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Correlation.Auto([5], Lags(1))).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Correlation.Auto([5], default)).ParamName);
    }
}
