// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG highlight-on-hover script injection and behavior.</summary>
public class SvgHighlightTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // Verifies that enabling highlight injects a script element.
        new DrawnToken("WithHighlight_InjectsScriptElement", () => Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<script"),
        // Verifies that the highlight script uses mouseenter to trigger dimming.
        new DrawnToken("HighlightScript_UsesMouseenterEvent", () => Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "mouseenter"),
        // Verifies that the highlight script manipulates opacity.
        new DrawnToken("HighlightScript_ManipulatesOpacity", () => Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "opacity"),
        // Verifies that the highlight script restores sibling opacity on mouseleave.
        new DrawnToken("HighlightScript_RestoresOnMouseleave", () => Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "mouseleave"),
        // Verifies that the highlight script dims siblings to 0.3 opacity.
        new DrawnToken("HighlightScript_DimsSiblingToThirtyPercent", () => Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "0.3"),
        // Verifies that WithHighlight(false) disables the feature.
        new DrawnToken("WithHighlightFalse_NoScript", () => Plt.Create()
            .WithHighlight(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg()) { Absent = ["<script"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
