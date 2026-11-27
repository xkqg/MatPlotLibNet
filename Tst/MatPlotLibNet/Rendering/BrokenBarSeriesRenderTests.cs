// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="BrokenBarSeries"/> rendering.</summary>
public class BrokenBarSeriesRenderTests
{
    private static readonly BarRange[][] Ranges = [[new(1.0, 2.0), new(4.0, 1.5)], [new(0.5, 3.0)]];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("BrokenBar_SvgContainsRectangles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.BrokenBarH(Ranges))
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
