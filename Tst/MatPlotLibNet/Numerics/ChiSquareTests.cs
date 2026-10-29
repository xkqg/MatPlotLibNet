// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The chi-square distribution's survival function (the p-value of a statistic) against scipy (<c>chi2.sf</c>),
/// to 1e-12 relative, 1 and 2 degrees of freedom among the rows: with 2 it is exp(-x / 2).</summary>
public class ChiSquareTests
{
    [Theory]
    [InlineData(0.01, 1, 0.920344325445942)]
    [InlineData(1.0, 1, 0.31731050786291115)]
    [InlineData(3.841458820694124, 1, 0.04999999999999989)]
    [InlineData(6.6348966010212145, 1, 0.009999999999999972)]
    [InlineData(16.79, 1, 4.1752751389197874e-05)]
    [InlineData(50.0, 1, 1.537459794428033e-12)]
    [InlineData(0.01, 2, 0.9950124791926823)]
    [InlineData(1.0, 2, 0.6065306597126334)]
    [InlineData(3.841458820694124, 2, 0.1465000644860843)]
    [InlineData(6.6348966010212145, 2, 0.03624520071516962)]
    [InlineData(16.79, 2, 0.00022599447633189242)]
    [InlineData(50.0, 2, 1.3887943864964e-11)]
    [InlineData(0.01, 3, 0.9997348349413444)]
    [InlineData(1.0, 3, 0.8012519569012009)]
    [InlineData(3.841458820694124, 3, 0.27910046378359765)]
    [InlineData(6.6348966010212145, 3, 0.0844916596210414)]
    [InlineData(16.79, 3, 0.0007806145860246613)]
    [InlineData(50.0, 3, 7.989179244951495e-11)]
    [InlineData(0.01, 4, 0.9999875415886458)]
    [InlineData(1.0, 4, 0.9097959895689501)]
    [InlineData(3.841458820694124, 4, 0.42788704696224755)]
    [InlineData(6.6348966010212145, 4, 0.15648678022937496)]
    [InlineData(16.79, 4, 0.0021232181051381296)]
    [InlineData(50.0, 4, 3.610865404890647e-10)]
    public void TheSurvival_MatchesScipy(double x, int degreesOfFreedom, double expected)
    {
        double actual = ChiSquare.Survival(x, degreesOfFreedom);
        Assert.True(Math.Abs(actual - expected) <= 1e-12 * Math.Max(1e-300, Math.Abs(expected)) + 1e-300, $"expected {expected:R}, got {actual:R}");
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(7.0)]
    public void TwoDegreesOfFreedom_IsTheExponential(double x) => Assert.Equal(Math.Exp(-x / 2), ChiSquare.Survival(x, 2), 14);

    [Fact]
    public void TheSurvival_OfNothingAndOfEverything()
    {
        Assert.Equal(1, ChiSquare.Survival(0, 3));
        Assert.Equal(1, ChiSquare.Survival(-1, 3));
        Assert.Equal(0, ChiSquare.Survival(double.PositiveInfinity, 3));
        Assert.True(double.IsNaN(ChiSquare.Survival(double.NaN, 3)));
        Assert.True(double.IsNaN(ChiSquare.Survival(1, 0)));
        Assert.True(double.IsNaN(ChiSquare.Survival(1, -2)));
    }
}
