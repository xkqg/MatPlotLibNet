// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase 7 of the v1.7.2 plan — opacity / transition tokens are configurable
/// via <c>FigureBuilder.WithInteractionTheme</c>. Defaults match v1.7.1 hard-coded
/// values (zero-config callers see no behaviour change); custom values are emitted as
/// <c>data-mpl-*</c> attributes on the SVG so embedded scripts can read them.</summary>
public class InteractionThemeTests
{
    [Fact]
    public void DefaultTheme_DoesNotEmitDataMplAttributesAsSvgAttribute()
    {
        var svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        // The script body MENTIONS data-mpl-highlight-opacity (in a comment + getAttribute call),
        // but the SVG opening tag must NOT carry it as an attribute when default theme is used.
        Assert.Null(svg.Elements("svg")[0]["data-mpl-highlight-opacity"]);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("CustomTheme_EmitsHighlightOpacityAttribute", () => Plt.Create()
            .WithHighlight()
            .WithInteractionTheme(new InteractionTheme(HighlightOpacity: 0.15))
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "data-mpl-highlight-opacity=\"0.15\""),
        new DrawnToken("HighlightScript_ReadsThemableOpacity", () => Plt.Create().WithHighlight().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "data-mpl-highlight-opacity"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Phase 7 of v1.7.2 — every non-default token must round-trip through the
    /// SVG opening tag as a <c>data-mpl-*</c> attribute. Theory covers the five remaining
    /// branches in <c>SvgTransform.BuildSvgDocument</c> (HighlightOpacity is already covered
    /// by <see cref="CustomTheme_EmitsHighlightOpacityAttribute"/>).</summary>
    [Theory]
    [MemberData(nameof(NonDefaultTokenCases))]
    public void CustomTheme_EmitsEachTokenAttribute_WhenNonDefault(string attributeName, InteractionTheme theme, string expectedValueFragment)
    {
        var svg = Plt.Create()
            .WithInteractionTheme(theme)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();
        Assert.Equal(expectedValueFragment, svg.Elements("svg")[0][attributeName]);
    }

    public static IEnumerable<object[]> NonDefaultTokenCases() =>
    [
        ["data-mpl-sankey-link-opacity",   new InteractionTheme(SankeyDimLinkOpacity: 0.5),  "0.5"],
        ["data-mpl-sankey-node-opacity",   new InteractionTheme(SankeyDimNodeOpacity: 0.5),  "0.5"],
        ["data-mpl-treemap-transition-ms", new InteractionTheme(TreemapTransitionMs: 600),   "600"],
        ["data-mpl-tooltip-offset-x",      new InteractionTheme(TooltipOffsetX: 20),         "20"],
        ["data-mpl-tooltip-offset-y",      new InteractionTheme(TooltipOffsetY: -10),        "-10"],
    ];
}
