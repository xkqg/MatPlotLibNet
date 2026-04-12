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
}
