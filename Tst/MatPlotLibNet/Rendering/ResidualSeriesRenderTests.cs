// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="ResidualSeries"/> rendering.</summary>
public class ResidualSeriesRenderTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Y = [2.1, 3.9, 6.1, 7.9, 10.1];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Residual_SvgContainsCircles", () => TestFigures.OfOneVerb(ax => ax.Residplot(X, Y)).ToSvg(), "<circle"),
        new DrawnToken("Residual_ShowZeroLine_ContainsLine", () => TestFigures.OfOneVerb(ax => ax.Residplot(X, Y, s => s.ShowZeroLine = true)).ToSvg(), "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
