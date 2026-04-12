// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.DataFrame;

/// <summary>Verifies <see cref="DataFrameNumericsExtensions"/> extension methods.</summary>
public class DataFrameNumericsExtensionsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Creates a DataFrame with X and Y double columns.</summary>
    private static Microsoft.Data.Analysis.DataFrame MakeXYDf(double[] xs, double[] ys)
    {
        return new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("x", xs),
            new PrimitiveDataFrameColumn<double>("y", ys));
    }

    // ── PolyFit ───────────────────────────────────────────────────────────────

    [Fact]
    public void PolyFit_Degree1_ReturnsLinearCoefficients()
    {
        // y = 2x + 1  →  coefficients should be [1, 2]
        double[] xs = [0, 1, 2, 3, 4, 5];
        double[] ys = [1, 3, 5, 7, 9, 11];
        var df = MakeXYDf(xs, ys);

        double[] coeffs = df.PolyFit("x", "y", degree: 1);

        Assert.Equal(2, coeffs.Length);
        Assert.Equal(1.0, coeffs[0], precision: 6); // intercept
        Assert.Equal(2.0, coeffs[1], precision: 6); // slope
    }

    [Fact]
    public void PolyFit_Degree0_ReturnsConstant()
    {
        // y = 5 everywhere
        double[] xs = [1, 2, 3, 4];
        double[] ys = [5, 5, 5, 5];
        var df = MakeXYDf(xs, ys);

        double[] coeffs = df.PolyFit("x", "y", degree: 0);

        Assert.Single(coeffs);
        Assert.Equal(5.0, coeffs[0], precision: 6);
    }

    [Fact]
    public void PolyFit_UnknownColumn_Throws_ArgumentException()
    {
        var df = MakeXYDf([1, 2, 3], [1, 4, 9]);
        var ex = Assert.Throws<ArgumentException>(() => df.PolyFit("missing", "y", 1));
        Assert.Contains("missing", ex.Message);
    }

    // ── PolyEval ──────────────────────────────────────────────────────────────

    [Fact]
    public void PolyEval_UsesColumnAsX_ReturnsSameLengthAsColumn()
    {
        double[] xs = [0, 1, 2, 3, 4];
        double[] ys = [1, 3, 5, 7, 9];
        var df = MakeXYDf(xs, ys);

        double[] coeffs = df.PolyFit("x", "y", degree: 1);
        double[] evaluated = df.PolyEval("x", coeffs);

        Assert.Equal(5, evaluated.Length);
    }

    [Fact]
    public void PolyEval_LinearCoefficients_MatchExpected()
    {
        // coefficients [1, 2] means y = 1 + 2x
        double[] coeffs = [1.0, 2.0];
        var df = MakeXYDf([0, 1, 2], [0, 0, 0]); // y values irrelevant for eval

        double[] evaluated = df.PolyEval("x", coeffs);

        Assert.Equal(1.0, evaluated[0], precision: 10); // 1 + 2*0
        Assert.Equal(3.0, evaluated[1], precision: 10); // 1 + 2*1
        Assert.Equal(5.0, evaluated[2], precision: 10); // 1 + 2*2
    }

    // ── ConfidenceBand ────────────────────────────────────────────────────────

    [Fact]
    public void ConfidenceBand_ReturnsUpperAndLower_SameLengthAsEvalX()
    {
        double[] xs = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        double[] ys = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];
        var df = MakeXYDf(xs, ys);

        double[] coeffs = df.PolyFit("x", "y", degree: 1);
        double[] evalX  = [0.5, 1.5, 2.5, 3.5, 4.5];
        ConfidenceBand band = df.ConfidenceBand("x", "y", coeffs, evalX);

        Assert.Equal(5, band.Upper.Length);
        Assert.Equal(5, band.Lower.Length);
    }

    [Fact]
    public void ConfidenceBand_UpperAlwaysAtLeastLower()
    {
        double[] xs = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
        double[] ys = xs.Select(x => 2 * x + 1 + (x % 3 - 1) * 0.5).ToArray(); // slight noise
        var df = MakeXYDf(xs, ys);

        double[] coeffs = df.PolyFit("x", "y", degree: 1);
        double[] evalX  = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        ConfidenceBand band = df.ConfidenceBand("x", "y", coeffs, evalX);

        for (int i = 0; i < evalX.Length; i++)
            Assert.True(band.Upper[i] >= band.Lower[i],
                $"Upper[{i}]={band.Upper[i]} < Lower[{i}]={band.Lower[i]}");
    }

    [Fact]
    public void ConfidenceBand_UnknownColumn_Throws_ArgumentException()
    {
        var df = MakeXYDf([1, 2, 3], [1, 4, 9]);
        double[] coeffs = [0, 1];
        var ex = Assert.Throws<ArgumentException>(() =>
            df.ConfidenceBand("bad_x", "y", coeffs, [1.0, 2.0]));
        Assert.Contains("bad_x", ex.Message);
    }
}
