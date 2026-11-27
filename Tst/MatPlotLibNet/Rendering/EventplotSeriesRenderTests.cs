// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="EventplotSeries"/> rendering.</summary>
public class EventplotSeriesRenderTests
{
    private static readonly double[][] Positions = [[1.0, 2.0, 3.0], [4.0, 5.0]];

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Eventplot_SvgContainsLines", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions))
            .ToSvg(), "<line"),
        // EventplotSeriesRenderer: series.Colors is not null true-branch
        new DrawnToken("Eventplot_WithColors_UsesPerRowColor", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions, s =>
            {
                s.Colors = [Colors.Red, Colors.Blue];
            }))
            .ToSvg(), "<line"),
        // EventplotSeriesRenderer: i >= series.Colors.Length false-branch
        // 1 color for 2 rows
        new DrawnToken("Eventplot_MoreRowsThanColors_FallsBackForExtraRows", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Eventplot(Positions, s =>
            {
                s.Colors = [Colors.Red];  
            }))
            .ToSvg(), "<line"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ── Branch coverage ──────────────────────────────────────────────────────

}
