// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="StandardNormal.Quantile"/>, the one inverse of the standard normal CDF.
/// Every expected value below was computed to 50 digits with mpmath (the exact decimal value of the double
/// <c>p</c> given) and cross-checked against scipy's <c>ndtri</c>; they are compared by tolerance, never as stored
/// bytes, because the arithmetic runs through <c>Math.Sqrt</c> and <c>Math.Log</c>.</summary>
public class StandardNormalTests
{
    /// <summary>Absolute tolerance of every comparison: AS241 is good to about 1e-16, so 1e-12 leaves four digits of margin.</summary>
    private const double Tolerance = 1e-12;

    // --- AS241 region 1: the centre, |p - 0.5| &lt;= 0.425 ---

    /// <summary>The centre of the distribution: the rational function of q = p - 0.5 (AS241 PPND16, first branch).</summary>
    [Theory]
    [InlineData(0.5, 0.0)]
    [InlineData(0.5000000000000001, 2.782916424671767e-16)]
    [InlineData(0.75, 0.6744897501960817)]
    [InlineData(0.9, 1.2815515655446006)]
    [InlineData(0.95, 1.6448536269514722)]
    [InlineData(0.8413447460685429, 1.0)]
    [InlineData(0.3, -0.5244005127080408)]
    [InlineData(0.125, -1.150349380376008)]
    [InlineData(0.1, -1.2815515655446004)]
    public void Quantile_CentralRegion_MatchesTheExactValue(double p, double expected) =>
        Assert.Equal(expected, StandardNormal.Quantile(p), Tolerance);

    // --- AS241 region 2: the tail with r = sqrt(-ln(min(p, 1 - p))) &lt;= 5 ---

    /// <summary>The nearer tails, below and above the median: the second rational function. 0.975 gives the familiar 1.96.</summary>
    [Theory]
    [InlineData(0.975, 1.959963984540054)]
    [InlineData(0.99, 2.3263478740408408)]
    [InlineData(0.995, 2.5758293035489004)]
    [InlineData(0.9999, 3.719016485455708)]
    [InlineData(0.0001, -3.7190164854556805)]
    [InlineData(1e-10, -6.361340902404056)]
    [InlineData(0.9999990463256836, 4.763001034267814)]   // 1 - 2^-20, exact in binary
    [InlineData(0.9999999998835847, 6.337957754553789)]   // 1 - 2^-33, exact in binary
    public void Quantile_NearTail_MatchesTheExactValue(double p, double expected) =>
        Assert.Equal(expected, StandardNormal.Quantile(p), Tolerance);

    // --- AS241 region 3: the far tail, r &gt; 5 ---

    /// <summary>The far tails, down to the smallest positive double (the denormal 5e-324) and up to the largest double below one:
    /// the third rational function.</summary>
    [Theory]
    [InlineData(1e-12, -7.034483825301132)]
    [InlineData(1e-15, -7.941345326170997)]
    [InlineData(1e-20, -9.262340089798408)]
    [InlineData(1e-100, -21.273453560965324)]
    [InlineData(1e-300, -37.047096299361199)]
    [InlineData(5e-324, -38.46740561714435)]
    [InlineData(0.9999999999990905, 7.047700256664409)]   // 1 - 2^-40, exact in binary
    [InlineData(0.9999999999999999, 8.209536151601387)]   // 1 - 2^-53, the largest double below 1
    public void Quantile_FarTail_MatchesTheExactValue(double p, double expected) =>
        Assert.Equal(expected, StandardNormal.Quantile(p), Tolerance);

    // --- the seams between the regions ---

    /// <summary>On both sides of each seam the function is the same curve: p = 0.075 / 0.925 is where |q| = 0.425 (centre to
    /// near tail) and p = exp(-25) is where r = 5 (near tail to far tail). A coefficient typed wrong shows as a step here.</summary>
    [Theory]
    [InlineData(0.0749, -1.4402382675279637)]
    [InlineData(0.075, -1.4395314709384559)]
    [InlineData(0.0751, -1.43882539275254)]
    [InlineData(0.9249, 1.4388253927525404)]
    [InlineData(0.925, 1.4395314709384562)]
    [InlineData(0.9251, 1.4402382675279639)]
    [InlineData(1.3887929977020156e-11, -6.657904790517103)]   // exp(-25) times (1 - 1e-6): r just above 5
    [InlineData(1.3887943864964021e-11, -6.657904643501104)]   // exp(-25)
    [InlineData(1.3887957752907884e-11, -6.657904496485248)]   // exp(-25) times (1 + 1e-6): r just below 5
    public void Quantile_AtTheSeamsBetweenRegions_IsContinuous(double p, double expected) =>
        Assert.Equal(expected, StandardNormal.Quantile(p), Tolerance);

    /// <summary>Each tail function is exact (about 1e-16) well beyond the point where the next one takes over, so a seam moved by
    /// a few percent changes an answer by 1e-12 at most: p = 1e-7 (r = 4.01) is such a point, where the far-tail function
    /// would answer 1.2e-12 away from the exact value. The tolerance of 1e-13 is a hundred times the error that
    /// <c>Math.Log</c> and <c>Math.Sqrt</c> may add on any platform, and ten times below that disagreement.</summary>
    [Fact]
    public void Quantile_WhereTheTailFunctionsOverlap_IsExactToThirteenDigits() =>
        Assert.Equal(-5.199337582192817, StandardNormal.Quantile(1e-7), 1e-13);

    // --- symmetry ---

    /// <summary>Quantile(p) = -Quantile(1 - p). The p values are powers of two (and 3/8), so 1 - p is exact in binary and the
    /// two sides reach the same r: the sign is the only difference. Covers all three regions on both sides of the median.</summary>
    [Theory]
    [InlineData(0.25)]
    [InlineData(0.375)]
    [InlineData(0.0625)]                 // near tail
    [InlineData(0.0009765625)]           // 2^-10, near tail
    [InlineData(9.5367431640625e-07)]    // 2^-20, near tail
    [InlineData(9.094947017729282e-13)]  // 2^-40, far tail
    [InlineData(2.220446049250313e-16)]  // 2^-52, far tail
    public void Quantile_IsOdd_AroundTheMedian(double p) =>
        Assert.Equal(-StandardNormal.Quantile(1.0 - p), StandardNormal.Quantile(p), Tolerance);

    /// <summary>The quantile function never decreases as p grows, across the seams and into both tails.</summary>
    [Fact]
    public void Quantile_IsStrictlyIncreasing_AcrossTheWholeRange()
    {
        double[] ps = [1e-300, 1e-100, 1e-20, 1e-12, 1.4e-11, 1e-10, 1e-4, 0.0749, 0.0751, 0.3, 0.5, 0.7, 0.9249, 0.9251, 0.9999, 0.9999999998835847, 0.9999999999999999];
        double previous = double.NegativeInfinity;
        foreach (double p in ps)
        {
            double z = StandardNormal.Quantile(p);
            Assert.True(z > previous, $"Quantile({p:R}) = {z:R} does not exceed the quantile before it ({previous:R})");
            previous = z;
        }
    }

    // --- the edges of the domain ---

    /// <summary>A probability of zero or less has no finite quantile: the answer is minus infinity (a saturating function, as the
    /// confidence band of <see cref="LeastSquares"/> has always read it: a level far below zero gives an infinite margin).</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(-1e300)]
    [InlineData(double.NegativeInfinity)]
    public void Quantile_ZeroOrBelow_IsNegativeInfinity(double p) =>
        Assert.True(double.IsNegativeInfinity(StandardNormal.Quantile(p)));

    /// <summary>A probability of one or more has no finite quantile: the answer is plus infinity.</summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(1e300)]
    [InlineData(double.PositiveInfinity)]
    public void Quantile_OneOrAbove_IsPositiveInfinity(double p) =>
        Assert.True(double.IsPositiveInfinity(StandardNormal.Quantile(p)));

    /// <summary>A missing probability gives a missing quantile; it neither throws nor becomes an infinity.</summary>
    [Fact]
    public void Quantile_NaN_IsNaN() =>
        Assert.True(double.IsNaN(StandardNormal.Quantile(double.NaN)));
}
