// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG rich-tooltip script injection and behavior.</summary>
public class SvgRichTooltipTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that enabling rich tooltips injects a script element.
        new DrawnToken("WithRichTooltips_InjectsScriptElement", () => Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<script"),
        // Verifies that the rich-tooltip script creates a floating div element.
        new DrawnToken("RichTooltipScript_CreatesDiv", () => Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "createElement('div')"),
        // Verifies that the rich-tooltip script reads title element text content.
        new DrawnToken("RichTooltipScript_ReadsTitleText", () => Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "textContent"),
        // Verifies that the rich-tooltip script hides on mouseout.
        new DrawnToken("RichTooltipScript_HidesOnMouseout", () => Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "mouseout"),
        // Verifies that WithRichTooltips(false) disables the feature.
        new DrawnToken("WithRichTooltipsFalse_NoScript", () => Plt.Create()
            .WithRichTooltips(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg()) { Absent = ["<script"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
