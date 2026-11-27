// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="TableSeries"/> rendering.</summary>
public class TableSeriesRenderTests
{
    private static readonly string[][] Data = [["Alice", "90"], ["Bob", "85"], ["Carol", "92"]];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Table_SvgContainsRectangles", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Table(Data))
            .ToSvg(), "<rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
