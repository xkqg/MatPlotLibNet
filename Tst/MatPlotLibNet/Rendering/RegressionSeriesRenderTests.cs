// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="RegressionSeries"/> rendering.</summary>
public class RegressionSeriesRenderTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Y = [2.1, 3.9, 6.2, 7.8, 10.1];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // SVG contains a polyline element (the regression line).
        new DrawnToken("Regression_SvgContainsPolyline", () => TestFigures.OfOneVerb(ax => ax.Regression(X, Y)).ToSvg(), "<polyline"),
        // ShowConfidence=true produces a polygon (confidence band).
        new DrawnToken("Regression_ShowConfidence_ContainsPolygon", () => TestFigures.OfOneVerb(ax => ax.Regression(X, Y, s => s.ShowConfidence = true)).ToSvg(), "<polygon"),
        // ShowConfidence=false omits the confidence polygon.
        new DrawnToken("Regression_NoConfidence_NoPolygon", () => TestFigures.OfOneVerb(ax => ax.Regression(X, Y, s => s.ShowConfidence = false)).ToSvg()) { Absent = ["<polygon"] },
        // Degree-2 polynomial fit renders without error.
        new DrawnToken("Regression_Degree2_RendersWithoutError", () => TestFigures.OfOneVerb(ax => ax.Regression(X, Y, s => s.Degree = 2)).ToSvg(), "<polyline"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
