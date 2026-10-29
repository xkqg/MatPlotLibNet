// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The gamma function's logarithm (Lanczos) and the regularised upper incomplete gamma function, against scipy
/// (<c>gammaln</c>, <c>gammaincc</c>): the pieces the chi-square distribution is built from.</summary>
public class GammaTests
{
    private static void Close(double expected, double actual, double relative)
    {
        double scale = Math.Max(1e-300, Math.Abs(expected));
        Assert.True(Math.Abs(actual - expected) <= relative * scale + 1e-300, $"expected {expected:R}, got {actual:R}");
    }

    [Theory]
    [InlineData(0.5, 0.5723649429247)]
    [InlineData(1.0, 0.0)]
    [InlineData(1.5, -0.12078223763524526)]
    [InlineData(2.0, 0.0)]
    [InlineData(2.5, 0.2846828704729192)]
    [InlineData(10.0, 12.801827480081469)]
    [InlineData(100.5, 361.43554046777757)]
    [InlineData(0.01, 4.599479878042022)]
    [InlineData(1e-05, 11.512919692895826)]
    [InlineData(100000.0, 1051287.7089736569)]
    public void TheLogGamma_MatchesScipy(double x, double expected) => Close(expected, Gamma.LogGamma(x), 1e-13);

    [Theory]
    [InlineData(0.5, 0.1, 0.6547208460185768)]
    [InlineData(0.5, 1.0, 0.15729920705028105)]
    [InlineData(0.5, 5.0, 0.001565402258002549)]
    [InlineData(0.5, 30.0, 9.485737571073857e-15)]
    [InlineData(1.0, 0.1, 0.9048374180359595)]
    [InlineData(1.0, 1.0, 0.36787944117144245)]
    [InlineData(1.0, 5.0, 0.006737946999085468)]
    [InlineData(1.0, 30.0, 9.357622968840163e-14)]
    [InlineData(2.5, 0.1, 0.9991138612111875)]
    [InlineData(2.5, 1.0, 0.8491450360846096)]
    [InlineData(2.5, 5.0, 0.07523524614651217)]
    [InlineData(2.5, 30.0, 1.2154569777183007e-11)]
    [InlineData(10.0, 0.1, 1.0)]
    [InlineData(10.0, 1.0, 0.9999998885745217)]
    [InlineData(10.0, 5.0, 0.9681719426937951)]
    [InlineData(10.0, 30.0, 7.121750862815593e-06)]
    public void TheRegularisedUpperIncomplete_MatchesScipy(double a, double x, double expected) =>
        Close(expected, Gamma.RegularisedUpper(a, x), 1e-12);

    [Fact]
    public void TheUpperIncomplete_RunsFromOneAtZeroToZeroAtInfinity()
    {
        Assert.Equal(1, Gamma.RegularisedUpper(2.5, 0));
        Assert.Equal(0, Gamma.RegularisedUpper(2.5, double.PositiveInfinity));
        Assert.True(double.IsNaN(Gamma.RegularisedUpper(2.5, double.NaN)));
        Assert.True(double.IsNaN(Gamma.RegularisedUpper(0, 1)));
        Assert.True(double.IsNaN(Gamma.RegularisedUpper(-1, 1)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    public void TheLogGamma_OfANonPositiveNumber_IsNaN(double x) => Assert.True(double.IsNaN(Gamma.LogGamma(x)));
}
