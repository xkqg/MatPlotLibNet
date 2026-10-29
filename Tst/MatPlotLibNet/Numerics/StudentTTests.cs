// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Student's t distribution: the cumulative distribution (through the regularised incomplete beta function) and its
/// inverse, the quantile, found by a bracketed root of the first. The reference values are scipy's (<c>t.cdf</c>, <c>t.ppf</c>,
/// scipy 1.18.1), compared to 1e-12 relative in the body of the distribution and 1e-9 in the tails. It replaces a table of
/// five values for ten degrees of freedom that drew a 90 % regression band at the 95 % quantile's width.</summary>
public class StudentTTests
{
    private static void Close(double expected, double actual, double relative)
    {
        double scale = Math.Max(1e-300, Math.Abs(expected));
        Assert.True(Math.Abs(actual - expected) <= relative * scale + 1e-300, $"expected {expected:R}, got {actual:R}");
    }

    [Theory]
    [InlineData(0.6, 1, 0.32491969623290634)]
    [InlineData(0.9, 1, 3.0776835371752544)]
    [InlineData(0.95, 1, 6.313751514675037)]
    [InlineData(0.975, 1, 12.706204736174694)]
    [InlineData(0.995, 1, 63.656741162871526)]
    [InlineData(0.999999, 1, 318309.88617359026)]
    [InlineData(1e-09, 1, -318309886.1837907)]
    [InlineData(0.5, 1, 0.0)]
    [InlineData(0.6, 2, 0.2886751345948128)]
    [InlineData(0.9, 2, 1.8856180831641272)]
    [InlineData(0.95, 2, 2.9199855803537242)]
    [InlineData(0.975, 2, 4.302652729749462)]
    [InlineData(0.995, 2, 9.924843200918287)]
    [InlineData(0.999999, 2, 707.1057205157672)]
    [InlineData(1e-09, 2, -22360.679741456875)]
    [InlineData(0.5, 2, 0.0)]
    [InlineData(0.6, 5, 0.2671808657041451)]
    [InlineData(0.9, 5, 1.4758840488244815)]
    [InlineData(0.95, 5, 2.0150483733330233)]
    [InlineData(0.975, 5, 2.5705818356363146)]
    [InlineData(0.995, 5, 4.032142983555228)]
    [InlineData(0.999999, 5, 24.771029720372493)]
    [InlineData(1e-09, 5, -98.93722464836996)]
    [InlineData(0.5, 5, 0.0)]
    [InlineData(0.6, 10, 0.2601848294920803)]
    [InlineData(0.9, 10, 1.372183641110336)]
    [InlineData(0.95, 10, 1.8124611228116756)]
    [InlineData(0.975, 10, 2.2281388519862744)]
    [InlineData(0.995, 10, 3.16927267261695)]
    [InlineData(0.999999, 10, 9.751995490909856)]
    [InlineData(1e-09, 10, -20.144697766673996)]
    [InlineData(0.5, 10, 0.0)]
    [InlineData(0.6, 30, 0.2556053649519128)]
    [InlineData(0.9, 30, 1.3104150253913955)]
    [InlineData(0.95, 30, 1.697260886593957)]
    [InlineData(0.975, 30, 2.0422724563012378)]
    [InlineData(0.995, 30, 2.7499956535672254)]
    [InlineData(0.999999, 30, 5.871117120408624)]
    [InlineData(1e-09, 30, -8.445862796274243)]
    [InlineData(0.5, 30, 0.0)]
    public void TheQuantile_MatchesScipy(double p, int degreesOfFreedom, double expected)
    {
        double tolerance = p is < 1e-6 or > 1 - 1e-6 ? 1e-9 : 1e-12;
        if (expected == 0) Assert.Equal(0, StudentT.Quantile(p, degreesOfFreedom), 15);
        else Close(expected, StudentT.Quantile(p, degreesOfFreedom), tolerance);
    }

    [Theory]
    [InlineData(-3.0, 1, 0.10241638234956672)]
    [InlineData(-0.5, 1, 0.35241638234956674)]
    [InlineData(0.0, 1, 0.5)]
    [InlineData(0.7, 1, 0.6944001122142147)]
    [InlineData(2.5, 1, 0.8788810584091566)]
    [InlineData(12.0, 1, 0.9735353239404101)]
    [InlineData(-3.0, 2, 0.04773298313335457)]
    [InlineData(-0.5, 2, 0.33333333333333337)]
    [InlineData(0.0, 2, 0.5)]
    [InlineData(0.7, 2, 0.7218034876835673)]
    [InlineData(2.5, 2, 0.9351941398892446)]
    [InlineData(12.0, 2, 0.9965635331614208)]
    [InlineData(-3.0, 5, 0.015049623948731293)]
    [InlineData(-0.5, 5, 0.31914943582046457)]
    [InlineData(0.0, 5, 0.5)]
    [InlineData(0.7, 5, 0.7424255258425918)]
    [InlineData(2.5, 5, 0.9727549503288119)]
    [InlineData(12.0, 5, 0.9999645525374142)]
    [InlineData(-3.0, 10, 0.006671827511284785)]
    [InlineData(-0.5, 10, 0.3139468028714865)]
    [InlineData(0.0, 10, 0.5)]
    [InlineData(0.7, 10, 0.7500562149135578)]
    [InlineData(2.5, 10, 0.9842765778816955)]
    [InlineData(12.0, 10, 0.9999998539295588)]
    [InlineData(-3.0, 30, 0.0026949820328259727)]
    [InlineData(-0.5, 30, 0.3103615024425636)]
    [InlineData(0.0, 30, 0.5)]
    [InlineData(0.7, 30, 0.7553397782501642)]
    [InlineData(2.5, 30, 0.9909421754659666)]
    [InlineData(12.0, 30, 0.999999999999721)]
    public void TheCdf_MatchesScipy(double t, int degreesOfFreedom, double expected) =>
        Close(expected, StudentT.Cdf(t, degreesOfFreedom), 1e-12);

    [Theory]
    [InlineData(0.9, 7)]
    [InlineData(0.55, 3)]
    [InlineData(1e-6, 12)]
    public void TheQuantile_IsTheInverseOfTheCdf(double p, int degreesOfFreedom) =>
        Close(p, StudentT.Cdf(StudentT.Quantile(p, degreesOfFreedom), degreesOfFreedom), 1e-11);

    [Theory]
    [InlineData(0.8, 5)]
    [InlineData(0.99, 2)]
    public void TheQuantile_IsOddAroundTheMedian(double p, int degreesOfFreedom) =>
        Close(StudentT.Quantile(p, degreesOfFreedom), -StudentT.Quantile(1 - p, degreesOfFreedom), 1e-12);

    /// <summary>The nine degrees of freedom the old table had no entry for: exact at 10 (a 95 % band is 2.228, not the 2.262 of 9).</summary>
    [Fact]
    public void ANinetyPercentBand_IsNotTheNinetyFivePercentOnes()
    {
        Close(1.8124611228116756, StudentT.Quantile(0.95, 10), 1e-12);
        Assert.NotEqual(StudentT.Quantile(0.975, 10), StudentT.Quantile(0.95, 10), 3);
    }

    /// <summary>A million degrees of freedom is the normal to about two millionths (scipy: 1.9599663568 against 1.9599639845).</summary>
    [Fact]
    public void ALargeNumberOfDegreesOfFreedom_ApproachesTheNormal()
    {
        double t = StudentT.Quantile(0.975, 1_000_000);

        Close(1.9599663568141068, t, 1e-8);
        Assert.Equal(StandardNormal.Quantile(0.975), t, 4);
    }

    [Theory]
    [InlineData(double.NaN, 5)]
    [InlineData(0.5, double.NaN)]
    public void ANumberThatIsMissing_GivesNaN(double p, double degreesOfFreedom) =>
        Assert.True(double.IsNaN(StudentT.Quantile(p, degreesOfFreedom)));

    [Theory]
    [InlineData(0.0, 5, double.NegativeInfinity)]
    [InlineData(1.0, 5, double.PositiveInfinity)]
    [InlineData(-0.1, 5, double.NegativeInfinity)]
    [InlineData(1.1, 5, double.PositiveInfinity)]
    public void AProbabilityOutsideTheOpenInterval_SaturatesLikeTheNormalQuantile(double p, int degreesOfFreedom, double expected) =>
        Assert.Equal(expected, StudentT.Quantile(p, degreesOfFreedom));

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void NoDegreesOfFreedom_GiveNaN(double degreesOfFreedom) =>
        Assert.True(double.IsNaN(StudentT.Quantile(0.9, degreesOfFreedom)) && double.IsNaN(StudentT.Cdf(1.0, degreesOfFreedom)));

    [Fact]
    public void TheCdf_OfAnInfinity_IsZeroOrOne()
    {
        Assert.Equal(0, StudentT.Cdf(double.NegativeInfinity, 4));
        Assert.Equal(1, StudentT.Cdf(double.PositiveInfinity, 4));
        Assert.True(double.IsNaN(StudentT.Cdf(double.NaN, 4)));
    }
}
