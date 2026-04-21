// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="LeastSquares"/> polynomial fit, evaluation, and confidence band math.</summary>
public class LeastSquaresTests
{
    // --- PolyFit ---

    /// <summary>Degree-1 fit on a perfect line returns exact slope and intercept.</summary>
    [Fact]
    public void PolyFit_Degree1_PerfectLine_ReturnsExactCoefficients()
    {
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] y = [1.0, 3.0, 5.0, 7.0, 9.0]; // y = 1 + 2x
        double[] coeff = LeastSquares.PolyFit(x, y, degree: 1);
        Assert.Equal(2, coeff.Length); // [a0, a1]
        Assert.Equal(1.0, coeff[0], precision: 8); // intercept
        Assert.Equal(2.0, coeff[1], precision: 8); // slope
    }

    /// <summary>Degree-2 fit on a perfect parabola returns exact coefficients.</summary>
    [Fact]
    public void PolyFit_Degree2_PerfectParabola_ReturnsExactCoefficients()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] y = [4.0, 1.0, 0.0, 1.0, 4.0]; // y = x^2
        double[] coeff = LeastSquares.PolyFit(x, y, degree: 2);
        Assert.Equal(3, coeff.Length);
        Assert.Equal(0.0, coeff[0], precision: 8); // constant
        Assert.Equal(0.0, coeff[1], precision: 8); // x term
        Assert.Equal(1.0, coeff[2], precision: 8); // x^2 term
    }

    /// <summary>PolyFit with degree 0 returns the mean of y.</summary>
    [Fact]
    public void PolyFit_Degree0_ReturnsMean()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] y = [2.0, 4.0, 6.0];
        double[] coeff = LeastSquares.PolyFit(x, y, degree: 0);
        Assert.Single(coeff);
        Assert.Equal(4.0, coeff[0], precision: 8); // mean(y) = 4
    }

    /// <summary>PolyFit with degree > 10 throws ArgumentOutOfRangeException.</summary>
    [Fact]
    public void PolyFit_DegreeAbove10_ThrowsArgumentOutOfRange()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] y = [1.0, 2.0, 3.0];
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.PolyFit(x, y, degree: 11));
    }

    /// <summary>PolyFit with empty input throws ArgumentException.</summary>
    [Fact]
    public void PolyFit_EmptyInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => LeastSquares.PolyFit([], [], degree: 1));
    }

    // --- PolyEval ---

    /// <summary>PolyEval evaluates the polynomial correctly at multiple points.</summary>
    [Fact]
    public void PolyEval_EvaluatesCorrectly()
    {
        double[] coeff = [1.0, 2.0]; // y = 1 + 2x
        double[] xs = [0.0, 1.0, 2.0, 3.0];
        double[] ys = LeastSquares.PolyEval(coeff, xs);
        Assert.Equal([1.0, 3.0, 5.0, 7.0], ys.Select(v => Math.Round(v, 8)).ToArray());
    }

    /// <summary>PolyEval with degree-2 polynomial evaluates correctly.</summary>
    [Fact]
    public void PolyEval_Degree2_EvaluatesCorrectly()
    {
        double[] coeff = [0.0, 0.0, 1.0]; // y = x^2
        double[] xs = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] ys = LeastSquares.PolyEval(coeff, xs);
        Assert.Equal([4.0, 1.0, 0.0, 1.0, 4.0], ys.Select(v => Math.Round(v, 8)).ToArray());
    }

    // --- ConfidenceBand ---

    /// <summary>Confidence band is narrower at the mean X than at the extremes.</summary>
    [Fact]
    public void ConfidenceBand_NarrowsAtMeanX()
    {
        double[] x = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
        double[] y = x.Select(xi => 2.0 * xi + 1.0 + (xi % 2 == 0 ? 0.3 : -0.3)).ToArray();
        double[] coeff = LeastSquares.PolyFit(x, y, degree: 1);
        double[] evalX = [0.0, 9.5, 19.0]; // left, mean, right
        double[] yHat = LeastSquares.PolyEval(coeff, evalX);
        var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, evalX);
        double widthLeft = upper[0] - lower[0];
        double widthMean = upper[1] - lower[1];
        double widthRight = upper[2] - lower[2];
        Assert.True(widthMean < widthLeft, $"Band at mean ({widthMean}) should be narrower than at left ({widthLeft})");
        Assert.True(widthMean < widthRight, $"Band at mean ({widthMean}) should be narrower than at right ({widthRight})");
    }

    /// <summary>Confidence band upper > lower for all evaluation points.</summary>
    [Fact]
    public void ConfidenceBand_UpperGreaterThanLower()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0, 5.0];
        double[] y = [2.1, 3.9, 6.2, 7.8, 10.1];
        double[] coeff = LeastSquares.PolyFit(x, y, degree: 1);
        double[] evalX = [1.0, 3.0, 5.0];
        var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, evalX);
        for (int i = 0; i < evalX.Length; i++)
            Assert.True(upper[i] > lower[i]);
    }

    /// <summary>Confidence band with level=0.99 is wider than level=0.95.</summary>
    [Fact]
    public void ConfidenceBand_HigherLevel_IsWider()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];
        double[] y = [2.1, 3.9, 6.0, 8.2, 10.1, 11.9, 14.2, 16.0];
        double[] coeff = LeastSquares.PolyFit(x, y, degree: 1);
        double[] evalX = [4.5];
        var (u95, l95) = LeastSquares.ConfidenceBand(x, y, coeff, evalX, level: 0.95);
        var (u99, l99) = LeastSquares.ConfidenceBand(x, y, coeff, evalX, level: 0.99);
        Assert.True(u99[0] - l99[0] > u95[0] - l95[0]);
    }

    // ── Phase J coverage additions ────────────────────────────────────────────

    /// <summary>L24 short-circuit — degree &lt; 0 fires the TRUE arm of the OR guard independently.</summary>
    [Fact]
    public void PolyFit_DegreeNegative_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LeastSquares.PolyFit([1.0, 2.0], [1.0, 2.0], -1));
    }

    /// <summary>SolveLinear L153 TRUE (near-zero diagonal → skip) and L167 FALSE (zero result).
    /// All identical x-values produce a rank-1 Vandermonde matrix; after elimination the second
    /// diagonal entry is 0, triggering both guards.</summary>
    [Fact]
    public void PolyFit_DegenerateX_AllSame_ReturnsWithoutCrashing()
    {
        double[] x = [1.0, 1.0, 1.0, 1.0];
        double[] y = [1.0, 2.0, 3.0, 4.0];
        double[] coeff = LeastSquares.PolyFit(x, y, 1);
        Assert.Equal(2, coeff.Length);
    }

    /// <summary>InvertSymmetric L189 TRUE (near-zero diagonal → skip). ConfidenceBand internally
    /// builds the same singular XtX, so its inversion hits the near-zero guard.</summary>
    [Fact]
    public void ConfidenceBand_DegenerateX_SingularXtX_DoesNotThrow()
    {
        double[] x = [1.0, 1.0, 1.0, 1.0];
        double[] y = [1.0, 2.0, 3.0, 4.0];
        double[] coeff = LeastSquares.PolyFit(x, y, 1);
        var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, [1.0]);
        Assert.Single(upper);
    }

    /// <summary>StudentTQuantile L212 TRUE — dof ≥ 120 → NormalQuantile path.
    /// n=122, degree=0 → m=1, dof=max(122−1,1)=121.</summary>
    [Fact]
    public void ConfidenceBand_Dof121_UsesNormalQuantile_ProducesFiniteBand()
    {
        double[] x = Enumerable.Range(0, 122).Select(i => (double)i).ToArray();
        double[] y = x.Select((xi, i) => 2.0 * xi + 1.0 + (i % 2 == 0 ? 0.3 : -0.3)).ToArray();
        double[] coeff = LeastSquares.PolyFit(x, y, 0);
        var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, [61.0]);
        Assert.True(double.IsFinite(upper[0]) && upper[0] > lower[0]);
    }

    /// <summary>NormalQuantile L251 TRUE — p &lt; 0.5 → recursive call with (1−p).
    /// level=−0.2 → p=(1+(−0.2))/2=0.4 &lt; 0.5; dof=121 routes to NormalQuantile.</summary>
    [Fact]
    public void ConfidenceBand_NegativeLevel_NormalQuantileRecurses_DoesNotThrow()
    {
        double[] x = Enumerable.Range(0, 122).Select(i => (double)i).ToArray();
        double[] y = x.Select((xi, i) => 2.0 * xi + 1.0 + (i % 2 == 0 ? 0.3 : -0.3)).ToArray();
        double[] coeff = LeastSquares.PolyFit(x, y, 0);
        var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, [61.0], level: -0.2);
        Assert.True(double.IsFinite(upper[0]) && double.IsFinite(lower[0]));
    }

    /// <summary>NormalQuantile L250 TRUE — p ≥ 1 → +Infinity.
    /// level=1.1 → p=1.05 ≥ 1; margin=+∞ → upper is +∞.</summary>
    [Fact]
    public void ConfidenceBand_LevelAbove1_NormalQuantileReturnsPositiveInfinity()
    {
        double[] x = Enumerable.Range(0, 122).Select(i => (double)i).ToArray();
        double[] y = x.Select((xi, i) => 2.0 * xi + 1.0 + (i % 2 == 0 ? 0.3 : -0.3)).ToArray();
        double[] coeff = LeastSquares.PolyFit(x, y, 0);
        var (upper, _) = LeastSquares.ConfidenceBand(x, y, coeff, [61.0], level: 1.1);
        Assert.True(double.IsPositiveInfinity(upper[0]));
    }

    /// <summary>NormalQuantile L249 TRUE — p ≤ 0 → −Infinity.
    /// level=−1.2 → p=−0.1 ≤ 0; margin=−∞ → upper is −∞.</summary>
    [Fact]
    public void ConfidenceBand_LevelBelow_Minus1_NormalQuantileReturnsNegativeInfinity()
    {
        double[] x = Enumerable.Range(0, 122).Select(i => (double)i).ToArray();
        double[] y = x.Select((xi, i) => 2.0 * xi + 1.0 + (i % 2 == 0 ? 0.3 : -0.3)).ToArray();
        double[] coeff = LeastSquares.PolyFit(x, y, 0);
        var (upper, _) = LeastSquares.ConfidenceBand(x, y, coeff, [61.0], level: -1.2);
        Assert.True(double.IsNegativeInfinity(upper[0]));
    }

    /// <summary>StudentTQuantile lookup table — one Theory case per return branch.
    /// Each (n, level) pair targets a specific dof range via degree-1 fit (dof = n−2).
    /// Alternating ±0.5 noise ensures s² &gt; 0 so the band is non-degenerate.</summary>
    [Theory]
    // 99% CI (p=0.995 ≥ 0.99) — drives every dof sub-branch
    [InlineData(3,  0.99)]   // dof=1  → 63.657
    [InlineData(4,  0.99)]   // dof=2  → 9.925
    [InlineData(5,  0.99)]   // dof=3  → 5.841
    [InlineData(6,  0.99)]   // dof=4  → 4.604
    [InlineData(7,  0.99)]   // dof=5  → 4.032
    [InlineData(10, 0.99)]   // dof=8  → 3.355
    [InlineData(12, 0.99)]   // dof=10 → 3.169
    [InlineData(17, 0.99)]   // dof=15 → 2.947
    [InlineData(22, 0.99)]   // dof=20 → 2.845
    [InlineData(32, 0.99)]   // dof=30 → 2.750
    [InlineData(62, 0.99)]   // dof=60 → 2.660
    // 95% CI — drives remaining uncovered dof sub-branches
    [InlineData(3,  0.95)]   // dof=1  → 12.706
    [InlineData(4,  0.95)]   // dof=2  → 4.303
    [InlineData(6,  0.95)]   // dof=4  → 2.776
    [InlineData(7,  0.95)]   // dof=5  → 2.571
    [InlineData(10, 0.95)]   // dof=8  → 2.306
    [InlineData(12, 0.95)]   // dof=10 → 2.228
    [InlineData(22, 0.95)]   // dof=20 → 2.086
    [InlineData(32, 0.95)]   // dof=30 → 2.042
    [InlineData(62, 0.95)]   // dof=60 → 2.000
    public void ConfidenceBand_StudentTQuantile_AllLookupBranches_ProduceValidBands(int n, double level)
    {
        double[] x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        double[] y = x.Select((xi, i) => 2.0 * xi + 1.0 + (i % 2 == 0 ? 0.5 : -0.5)).ToArray();
        double[] coeff = LeastSquares.PolyFit(x, y, 1);
        double mid = (x[0] + x[^1]) / 2.0;
        var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, [mid], level);
        Assert.True(upper[0] > lower[0]);
    }
}
