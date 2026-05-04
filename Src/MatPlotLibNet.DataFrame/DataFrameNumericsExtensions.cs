// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.DataFrame;
using MatPlotLibNet.Numerics;
using MsDataFrame = Microsoft.Data.Analysis.DataFrame;

namespace MatPlotLibNet;

/// <summary>
/// Extension methods that apply polynomial regression and confidence-band utilities directly to a
/// <see cref="MsDataFrame"/>, resolving named columns to <see langword="double"/> arrays and delegating
/// to <see cref="LeastSquares"/>.
/// </summary>
/// <example>
/// Fit a linear trend and overlay a 95 % confidence band:
/// <code>
/// // df has columns: "x" (double), "y" (double)
/// double[]       coeffs = df.PolyFit("x", "y", degree: 1);
/// double[]       fitY   = df.PolyEval("x", coeffs);
/// ConfidenceBand band   = df.ConfidenceBand("x", "y", coeffs,
///                             evalX: xValues, level: 0.95);
///
/// string svg = Plt.Create()
///     .AddSubPlot(1, 1, 1, ax =>
///     {
///         ax.Scatter(xValues, yValues, s => s.Label = "Data")
///           .Plot(xValues, fitY, s => s.Label = "Linear fit")
///           .FillBetween(xValues, band.Upper, band.Lower,
///               s => { s.Alpha = 0.2; s.Label = "95 % CI"; });
///     })
///     .WithTitle("Regression with Confidence Band")
///     .ToSvg();
/// </code>
/// Quadratic fit for a curved relationship:
/// <code>
/// double[] c2   = df.PolyFit("x", "y", degree: 2);
/// double[] curveY = df.PolyEval("x", c2);
/// </code>
/// </example>
public static class DataFrameNumericsExtensions
{
    /// <summary>Fits a polynomial of the specified degree to two numeric columns.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="xCol">Name of the numeric X column.</param>
    /// <param name="yCol">Name of the numeric Y column.</param>
    /// <param name="degree">Polynomial degree (0–10). Prefer ≤ 6 for numerical stability.</param>
    /// <returns>Coefficient array [a₀, a₁, …, aₙ] of length <c>degree + 1</c>.</returns>
    public static double[] PolyFit(this MsDataFrame df, string xCol, string yCol, int degree) =>
        LeastSquares.PolyFit(df.DoubleCol(xCol), df.DoubleCol(yCol), degree);

    /// <summary>Evaluates a fitted polynomial at every value in the named X column.</summary>
    /// <param name="df">The source data frame.</param>
    /// <param name="xCol">Name of the numeric X column whose values are the evaluation points.</param>
    /// <param name="coefficients">Polynomial coefficients from <see cref="PolyFit"/>.</param>
    /// <returns>Evaluated Y values — same length as the X column.</returns>
    public static double[] PolyEval(this MsDataFrame df, string xCol, double[] coefficients) =>
        LeastSquares.PolyEval(coefficients, df.DoubleCol(xCol));

    /// <summary>
    /// Computes confidence bands for a fitted polynomial at the specified evaluation X values.
    /// </summary>
    /// <param name="df">The source data frame containing the original fitting data.</param>
    /// <param name="xCol">Name of the original X data column.</param>
    /// <param name="yCol">Name of the original Y data column.</param>
    /// <param name="coefficients">Fitted polynomial coefficients from <see cref="PolyFit"/>.</param>
    /// <param name="evalX">X values at which the confidence band is evaluated.</param>
    /// <param name="level">Confidence level (default 0.95).</param>
    /// <returns>A <see cref="ConfidenceBand"/> with <c>Upper</c> and <c>Lower</c> bound arrays.</returns>
    public static ConfidenceBand ConfidenceBand(
        this MsDataFrame df, string xCol, string yCol,
        double[] coefficients, double[] evalX, double level = 0.95) =>
        LeastSquares.ConfidenceBand(df.DoubleCol(xCol), df.DoubleCol(yCol), coefficients, evalX, level);

}
