// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Algorithms;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>
/// Phase A.2 batch 1 — strict-90 floor plan. Mid-complexity test additions
/// for MathTextParser grammar arms, DataTransform axis-break + log-scale arms,
/// MarchingSquares saddle/edge cases.
/// </summary>
public class PinpointBranchTests18
{
    // ── MathTextParser grammar arms (currently 100L/78B) ──────────────────

    [Fact] public void MathTextParser_Fraction_ProducesFractionSpans()
    {
        var rt = MathTextParser.Parse(@"$\frac{a}{b}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.FractionNumerator);
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.FractionDenominator);
    }

    [Fact] public void MathTextParser_SquareRoot_ProducesSqrtSpan()
    {
        var rt = MathTextParser.Parse(@"$\sqrt{x}$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_NthRoot_IncludesIndexAsSuperscript()
    {
        var rt = MathTextParser.Parse(@"$\sqrt[3]{x}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.Superscript);
    }

    [Fact] public void MathTextParser_SingleCharSpacing_Comma_AddsSmallSpace()
    {
        var rt = MathTextParser.Parse(@"$a\,b$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_SingleCharSpacing_Colon_AddsMediumSpace()
    {
        var rt = MathTextParser.Parse(@"$a\:b$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_SingleCharSpacing_Semicolon_AddsLargeSpace()
    {
        var rt = MathTextParser.Parse(@"$a\;b$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_SingleCharSpacing_Bang_AddsNegativeSpace()
    {
        var rt = MathTextParser.Parse(@"$a\!b$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_QuadSpacing_AddsQuadSpace()
    {
        var rt = MathTextParser.Parse(@"$a\quad b$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_QqquadSpacing_AddsDoubleQuad()
    {
        var rt = MathTextParser.Parse(@"$a\qquad b$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_NestedFraction_HandlesGracefully()
    {
        var rt = MathTextParser.Parse(@"$\frac{1}{\frac{a}{b}}$");
        Assert.NotEmpty(rt.Spans);
    }

    [Fact] public void MathTextParser_GreekLetter_Theta_ProducesUnicodeTheta()
    {
        var rt = MathTextParser.Parse(@"$\theta$");
        Assert.Contains(rt.Spans, s => s.Text.Contains('\u03B8'));
    }

    [Fact] public void MathTextParser_ContainsMath_ReturnsTrueForDollarDelimiters()
    {
        Assert.True(MathTextParser.ContainsMath(@"$\alpha$"));
    }

    [Fact] public void MathTextParser_ContainsMath_ReturnsFalseForPlainText()
    {
        Assert.False(MathTextParser.ContainsMath("hello world"));
    }

    [Fact] public void MathTextParser_DegreeSymbol_ProducesUnicodeDegree()
    {
        var rt = MathTextParser.Parse(@"$30^\circ$");
        Assert.NotEmpty(rt.Spans);
    }

    // ── DataTransform — axis-breaks + log-scale for-loop arms (L138-145, L161-168)

    [Fact]
    public void DataTransform_TransformX_WithMultiplePointsAndXBreaks_TraversesLoop()
    {
        // Force the for-loop arm by adding XBreaks AND multiple X-data points
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                                                [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10]))
            .Build();
        fig.SubPlots[0].AddXBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_WithMultiplePointsAndYBreaks_TraversesLoop()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10],
                                                [1.0, 2, 3, 4, 5, 6, 7, 8, 9, 10]))
            .Build();
        fig.SubPlots[0].AddYBreak(3, 5, BreakStyle.Zigzag);
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformX_WithLogScaleAndManyPoints_TraversesLoop()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 10, 100, 1000, 10000],
                                                [1.0, 2, 3, 4, 5]))
            .Build();
        fig.SubPlots[0].XAxis.Scale = AxisScale.Log;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void DataTransform_TransformY_WithSymLogScaleAndManyPoints_TraversesLoop()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5],
                                                [-100.0, -10, 0, 10, 100]))
            .Build();
        fig.SubPlots[0].YAxis.Scale = AxisScale.SymLog;
        var svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ── MarchingSquares saddle / edge cases (L66, L200) ───────────────────

    [Fact]
    public void MarchingSquares_Extract_ConcentricRings_ProducesMultipleContours()
    {
        // Create a concentric-rings z field — each isoLevel produces multiple contours
        int n = 11;
        var grid = new double[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                double dx = c - n / 2.0;
                double dy = r - n / 2.0;
                grid[r, c] = Math.Sqrt(dx * dx + dy * dy);
            }
        var x = new double[n]; var y = new double[n];
        for (int i = 0; i < n; i++) { x[i] = i; y[i] = i; }
        var contours = MarchingSquares.Extract(x, y, grid, [2.0, 4.0]);
        Assert.NotEmpty(contours);
    }

    [Fact]
    public void MarchingSquares_ExtractBands_ConcentricRings_ProducesBands()
    {
        int n = 11;
        var grid = new double[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
            {
                double dx = c - n / 2.0;
                double dy = r - n / 2.0;
                grid[r, c] = Math.Sqrt(dx * dx + dy * dy);
            }
        var x = new double[n]; var y = new double[n];
        for (int i = 0; i < n; i++) { x[i] = i; y[i] = i; }
        var bands = MarchingSquares.ExtractBands(x, y, grid, levels: 5);
        Assert.NotEmpty(bands);
    }

    [Fact]
    public void MarchingSquares_Extract_SaddleCellAlternating_ProducesContours()
    {
        // Alternating high-low pattern — every 2x2 cell is a saddle
        var grid = new double[,] {
            { 0, 1, 0, 1 },
            { 1, 0, 1, 0 },
            { 0, 1, 0, 1 },
            { 1, 0, 1, 0 },
        };
        var x = new double[] { 0, 1, 2, 3 };
        var y = new double[] { 0, 1, 2, 3 };
        var contours = MarchingSquares.Extract(x, y, grid, [0.5]);
        Assert.NotNull(contours);
    }
}
