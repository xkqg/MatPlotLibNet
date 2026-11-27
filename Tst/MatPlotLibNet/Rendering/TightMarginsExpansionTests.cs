// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// L.11 — when <c>WithTightMargins()</c> is applied, the rendered axis limits
/// should match the data exactly. Pre-fix <c>Range1D.ExpandedToNiceBoundsIfAuto</c>
/// still widened the range outward to the next nice tick boundary, so data
/// <c>[3,4]</c> rendered inside an axis <c>[2,5]</c> — visibly NOT touching the
/// spines. The playground checkbox label literally reads "Tight margins (data
/// touches spines)", so failing to touch them is a contract violation.
/// </summary>
public class TightMarginsExpansionTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // The rendered Y-axis should show 3 at the bottom spine and 4 at the top.
        // We assert the "nice" rounding (2, 5) did NOT happen — ticks must stay
        // inside [3, 4]. Weak but stable probe: '>2<' must NOT appear as a tick
        // label inside the SVG for this dataset.
        // After rendering, Axes.YAxis.Min / .Max may still be null (user-set limits).
        // The rendered axis range is computed on the fly — probe it via the SVG.
        new DrawnToken("WithTightMargins_YAxis_ExactlyMatchesDataRange", () =>
        {
            var figure = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2.0, 3.0], [3.0, 4.0, 3.5])
                    .WithTightMargins())
                .Build();
            var ax0 = figure.SubPlots[0];
            return figure.ToSvg();
        }) { Absent = [">2<", ">5<"] },
        // X data is 1..3, must not widen to 0..4.
        new DrawnToken("WithTightMargins_XAxis_ExactlyMatchesDataRange", () =>
        {
            var figure = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax
                    .Plot([1.0, 2.0, 3.0], [3.0, 4.0, 3.5])
                    .WithTightMargins())
                .Build();
            return figure.ToSvg();
        }) { Absent = [">0<", ">4<"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
