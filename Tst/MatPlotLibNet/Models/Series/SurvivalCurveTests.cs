// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>What a survival curve says about itself: Greenwood's confidence band around its levels, and the level it is drawn
/// at any time. The curve of the worked example of <see cref="SurvivalGroupExtensionsTests"/> is the reference: at risk
/// [6, 6, 5, 2, 1], events [0, 1, 2, 1, 0], levels [1, 5/6, 1/2, 1/4, 1/4] at the times [0, 1, 3, 5, 6].</summary>
public class SurvivalCurveTests
{
    private const double Z95 = 1.959963984540054;

    private static DistributionAxesExtensions.SurvivalCurve Worked() =>
        new([0, 1, 3, 5, 6], [1, 5.0 / 6, 1.0 / 2, 1.0 / 4, 1.0 / 4], [6, 6, 5, 2, 1], [0, 1, 2, 1, 0]);

    /// <summary>The band is the level plus and minus 1.959963984540054 standard errors, where the variance of a level is its
    /// square times the sum, over the events so far, of d / (n (n - d)): plain Greenwood, one square root, no logarithm.</summary>
    [Fact]
    public void TheBand_IsTheLevelPlusAndMinusZ95StandardErrors_OfGreenwoodsSum()
    {
        var curve = Worked();
        double[] sums =
        [
            0,
            1.0 / (6 * 5),
            1.0 / (6 * 5) + 2.0 / (5 * 3),
            1.0 / (6 * 5) + 2.0 / (5 * 3) + 1.0 / (2 * 1),
            1.0 / (6 * 5) + 2.0 / (5 * 3) + 1.0 / (2 * 1),
        ];

        var band = curve.GreenwoodBand();

        for (int i = 0; i < sums.Length; i++)
        {
            double se = Math.Sqrt(curve.Survival[i] * curve.Survival[i] * sums[i]);
            Assert.Equal(Math.Clamp(curve.Survival[i] - Z95 * se, 0, 1), band.Lower[i], 12);
            Assert.Equal(Math.Clamp(curve.Survival[i] + Z95 * se, 0, 1), band.Upper[i], 12);
        }
    }

    /// <summary>A point without an event adds nothing to the sum, so the band has no width at the seed point: the curve is 1
    /// there and its band is [1, 1].</summary>
    [Fact]
    public void ThePointBeforeTheFirstEvent_HasABandOfNoWidth()
    {
        var band = Worked().GreenwoodBand();

        Assert.Equal(1.0, band.Lower[0]);
        Assert.Equal(1.0, band.Upper[0]);
    }

    /// <summary>A bound stays inside [0, 1]: a wide band around a middling level is cut at 0 below and 1 above.</summary>
    [Fact]
    public void TheBand_IsClampedToZeroAndOne()
    {
        var curve = new DistributionAxesExtensions.SurvivalCurve([0, 1], [1, 0.5], [2, 2], [0, 1]);

        var band = curve.GreenwoodBand();

        Assert.Equal(0.0, band.Lower[1]);
        Assert.Equal(1.0, band.Upper[1]);
    }

    /// <summary>When every subject at risk has the event, n - d is 0 and the term is undefined: the sum stops there, and the
    /// level of 0 already has a band of [0, 0], so nothing is lost.</summary>
    [Fact]
    public void WhenEverySubjectAtRiskHasTheEvent_TheSumStops_AndTheBandIsZero()
    {
        var curve = new DistributionAxesExtensions.SurvivalCurve([0, 2], [1, 0], [3, 3], [0, 3]);

        var band = curve.GreenwoodBand();

        Assert.Equal(0.0, band.Lower[1]);
        Assert.Equal(0.0, band.Upper[1]);
    }

    /// <summary>The curve is read steps-post: before its first point it stands at the first level, and from a point on it
    /// holds that point's level until the next.</summary>
    [Theory]
    [InlineData(-1.0, 1.0)]
    [InlineData(0.0, 1.0)]
    [InlineData(0.5, 1.0)]
    [InlineData(1.0, 5.0 / 6)]
    [InlineData(2.9, 5.0 / 6)]
    [InlineData(3.0, 1.0 / 2)]
    [InlineData(5.5, 1.0 / 4)]
    [InlineData(6.0, 1.0 / 4)]
    [InlineData(99.0, 1.0 / 4)]
    public void TheLevelAt_IsTheLevelOfTheLastPointAtOrBeforeThatTime(double time, double level) =>
        Assert.Equal(level, Worked().LevelAt(time));
}
