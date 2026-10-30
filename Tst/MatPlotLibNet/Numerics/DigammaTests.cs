// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The digamma function, ψ(x) = d/dx ln Γ(x). The Kraskov estimator of mutual information reads it at whole numbers only,
/// where it is a harmonic sum and needs no logarithm, so its result does not depend on the platform's libm.</summary>
public sealed class DigammaTests
{
    [Theory]
    [InlineData(1.0, -0.5772156649)]
    [InlineData(2.0, 0.4227843351)]
    [InlineData(3.0, 0.9227843351)]
    [InlineData(0.5, -1.9635100260)]
    [InlineData(5.0, 1.5061176684)]
    [InlineData(10.0, 2.2517525891)]
    public void TheDigamma_MatchesTheTabulatedValues(double x, double expected) =>
        Assert.Equal(expected, Gamma.Digamma(x), 1e-9);

    /// <summary>At a whole number ψ(n + 1) - ψ(n) = 1 / n, to the last bits: it is a sum of exactly those terms.</summary>
    [Fact]
    public void AtWholeNumbers_TheStepIsTheReciprocal_ToTheLastBits()
    {
        for (int n = 1; n <= 1000; n++)
            Assert.Equal(1.0 / n, Gamma.Digamma(n + 1) - Gamma.Digamma(n), 1e-13);
    }

    /// <summary>Between whole numbers the recurrence ψ(x + 1) = ψ(x) + 1 / x holds, which ties the series used for large x to the
    /// shift used for small x.</summary>
    [Theory]
    [InlineData(0.3)]
    [InlineData(2.7)]
    [InlineData(9.99)]
    [InlineData(15.2)]
    [InlineData(250.5)]
    public void BetweenWholeNumbers_TheRecurrenceHolds(double x) =>
        Assert.Equal(1.0 / x, Gamma.Digamma(x + 1) - Gamma.Digamma(x), 1e-11);

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-2.5)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void AtZeroBelowOrNotANumber_ThereIsNoValue(double x) =>
        Assert.True(double.IsNaN(Gamma.Digamma(x)));

    [Fact]
    public void AtInfinity_ItGrowsWithoutBound() =>
        Assert.True(double.IsPositiveInfinity(Gamma.Digamma(double.PositiveInfinity)));

    /// <summary>A whole number too large for the harmonic sum to be worth summing goes through the series, and agrees with it.</summary>
    [Fact]
    public void ForAVeryLargeWholeNumber_TheSeriesAgreesWithTheLogarithmToItsPrecision() =>
        Assert.Equal(Math.Log(1_000_000.0) - 1 / 2_000_000.0, Gamma.Digamma(1_000_000.0), 1e-9);
}
