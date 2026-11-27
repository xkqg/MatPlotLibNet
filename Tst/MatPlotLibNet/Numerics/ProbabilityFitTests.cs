// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How a sorted sample is fitted to a reference distribution for a probability plot: Filliben's plotting positions, the
/// reference's quantile at each, and the least-squares line through the points (scipy's <c>probplot</c>).</summary>
public class ProbabilityFitTests
{
    [Fact]
    public void FillibenPositions_OfTwo_AreSymmetricAroundOneHalf()
    {
        double last = Math.Pow(0.5, 1.0 / 2);

        Assert.Equal([1 - last, last], ProbabilityFit.FillibenPositions(2));
    }

    [Fact]
    public void FillibenPositions_OfFive_UseTheMedianOfTheExtremesAndTheFormulaBetween()
    {
        double last = Math.Pow(0.5, 1.0 / 5);

        Assert.Equal(
            [1 - last, (2 - 0.3175) / (5 + 0.365), (3 - 0.3175) / (5 + 0.365), (4 - 0.3175) / (5 + 0.365), last],
            ProbabilityFit.FillibenPositions(5));
    }

    [Fact]
    public void TheTheoreticalQuantiles_AreTheReferencesQuantilesAtThePositions()
    {
        double[] sample = [1.0, 2.0, 4.0, 8.0];

        var fit = QQReference.Normal.FitTo(sample);

        Assert.Equal(ProbabilityFit.FillibenPositions(4), fit.Positions);
        Assert.Equal(fit.Positions.Select(StandardNormal.Quantile), fit.Theoretical);
    }

    [Fact]
    public void TheLine_IsTheLeastSquaresFit_EvaluatedAtTheFirstAndTheLastQuantile()
    {
        // Against the uniform reference the quantile is the position itself, and these are equally spaced positions on a line.
        var fit = QQReference.Uniform.FitTo([1.0, 2.0, 3.0]);

        Assert.Equal(1.0, fit.StartY, 9);
        Assert.Equal(3.0, fit.EndY, 9);
    }

    /// <summary>A reference with no finite quantile at a position broke the contract of its quantile function: refused through the
    /// one owner of a function's result (R52), naming the reference, the position's index and the value; no line is fitted.</summary>
    [Theory]
    [InlineData(double.PositiveInfinity, "The quantile function of the reference 'capped' gave Infinity for sample 4; every value is a finite number.")]
    [InlineData(double.NaN, "The quantile function of the reference 'capped' gave NaN for sample 4; every value is a finite number.")]
    public void AReferenceWithNoFiniteQuantileAtAPosition_IsRefusedAsTheFunctionsFault(double quantile, string message)
    {
        var reference = QQReference.Of("capped", p => p > 0.8 ? quantile : p);

        var error = Assert.Throws<InvalidOperationException>(() => reference.FitTo([1.0, 2.0, 3.0, 4.0, 5.0]));

        Assert.Equal(message, error.Message);
    }
}
