// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="StripplotSeries"/> rendering.</summary>
public class StripplotSeriesRenderTests
{
    private static readonly double[][] Datasets = [[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Stripplot_SvgContainsCircles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Stripplot(Datasets))
            .ToSvg(), "<circle"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
