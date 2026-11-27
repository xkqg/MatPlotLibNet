// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG tooltip rendering behavior.</summary>
public class SvgTooltipTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that scatter series with tooltips enabled contains title elements.
        new DrawnToken("ScatterSeries_WithTooltips_ContainsTitleElements", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTooltips()
                .Scatter([1.0, 2.0], [3.0, 4.0]))
            .ToSvg(), "<title>"),
        // Verifies that scatter series without tooltips does not contain title elements.
        new DrawnToken("Tooltips_Disabled_NoTitleElements", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter([1.0, 2.0], [3.0, 4.0]))
            .ToSvg()) { Absent = ["<title>"] },
        // Verifies that tooltip text includes the x and y data values for each point.
        new DrawnToken("ScatterSeries_WithTooltips_ContainsDataValues", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTooltips()
                .Scatter([1.5], [3.2]))
            .ToSvg(), "x=1.5", "y=3.2"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
