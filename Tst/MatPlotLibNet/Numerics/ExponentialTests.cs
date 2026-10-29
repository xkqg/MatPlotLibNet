// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The exponential distribution's quantile, -ln(1 - p), against scipy (<c>expon.ppf</c>). For a small p the logarithm of
/// 1 - p loses every digit of p (1 - 1e-9 is stored with seven correct digits of 1e-9), so the quantile is worked out with an
/// accurate ln(1 + x), which <see cref="double.LogP1"/> is not.</summary>
public class ExponentialTests
{
    [Theory]
    [InlineData(1e-09, 1.0000000005000001e-09)]
    [InlineData(1e-05, 1.0000050000333337e-05)]
    [InlineData(0.1, 0.10536051565782631)]
    [InlineData(0.5, 0.6931471805599453)]
    [InlineData(0.9, 2.302585092994046)]
    [InlineData(0.999999, 13.815510557935518)]
    public void TheQuantile_MatchesScipy_ToTwelveDigits(double p, double expected)
    {
        double actual = Exponential.Quantile(p);
        Assert.True(Math.Abs(actual - expected) <= 1e-12 * expected, $"expected {expected:R}, got {actual:R}");
    }

    [Fact]
    public void ForATinyProbability_NoDigitIsLost()
    {
        // -ln(1 - 1e-9) = 1e-9 + 5e-19 + ...: the naive form is right to 7 digits only.
        double actual = Exponential.Quantile(1e-9);
        Assert.True(Math.Abs(actual - 1.0000000005e-9) <= 1e-12 * 1e-9, $"got {actual:R}");
    }

    [Fact]
    public void BelowTheSpacingOfOne_TheQuantileIsTheProbabilityItself()
    {
        // 1 - 1e-20 is stored as exactly 1, and -ln(1 - p) is p to every digit a double holds for p below 1e-16.
        Assert.Equal(1e-20, Exponential.Quantile(1e-20));
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(-1.0, 0.0)]
    [InlineData(1.0, double.PositiveInfinity)]
    [InlineData(2.0, double.PositiveInfinity)]
    public void AtTheEnds_TheQuantileSaturates(double p, double expected) => Assert.Equal(expected, Exponential.Quantile(p));

    [Fact]
    public void AMissingProbability_IsNaN() => Assert.True(double.IsNaN(Exponential.Quantile(double.NaN)));
}
