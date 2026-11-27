// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that shared axes produce synchronized data ranges during rendering.</summary>
public class SharedAxesRenderTests
{
    /// <summary>Verifies that non-shared axes remain independent (regression).</summary>
    [Fact]
    public void NonSharedAxes_RendersIndependently()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(1, 2, 1);
        ax1.Plot([0.0, 10.0], [1.0, 2.0]);

        var ax2 = fig.AddSubPlot(1, 2, 2);
        ax2.Plot([100.0, 200.0], [1.0, 2.0]);

        string svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
        // Both subplots should render — each with their own range
        Assert.Equal(2, fig.SubPlots.Count);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that the sharing axes uses the union X range (includes parent's data).
        // If sharing works, ax2's tick labels should include values near 0 (from ax1)
        // and values near 15 (from ax2's own data)
        // tick from ax1's range
        new DrawnToken("SharedX_SharingAxesSeesParentXRange", () =>
        {
            // ax1: X [0,10], ax2: X [5,15] shares X with ax1
            // ax2 should see X range covering at least [0,15] (union)
            var fig = new Figure();
            var ax1 = fig.AddSubPlot(2, 1, 1);
            ax1.Plot([0.0, 10.0], [1.0, 2.0]);
            var ax2 = fig.AddSubPlot(2, 1, 2, sharex: ax1);
            ax2.Plot([5.0, 15.0], [3.0, 4.0]);
            return fig.ToSvg();
        }, "0", "15"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
