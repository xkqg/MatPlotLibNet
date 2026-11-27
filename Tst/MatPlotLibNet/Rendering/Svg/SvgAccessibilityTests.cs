// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG semantic-structure accessibility (role, title, desc, aria-label).</summary>
public class SvgAccessibilityTests
{
    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("SvgRoot_HasRoleImgAttribute", () => Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg(), "role=\"img\""),
        new DrawnToken("WithAltText_EmitsTitleElement", () => Plt.Create()
            .WithAltText("Sales over time")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<title", "Sales over time"),
        new DrawnToken("WithDescription_EmitsDescElement", () => Plt.Create()
            .WithDescription("Monthly revenue 2025")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<desc", "Monthly revenue 2025"),
        new DrawnToken("WithoutAltText_UsesFigureTitleAsFallback", () => Plt.Create()
            .WithTitle("My Chart")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "<title", "My Chart"),
        new DrawnToken("AltText_SpecialChars_AreXmlEscaped", () => Plt.Create()
            .WithAltText("Revenue <Q1> & profit")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "&lt;Q1&gt;", "&amp;"),
        new DrawnToken("SvgRoot_HasAriaLabelledby_WhenAltTextSet", () => Plt.Create()
            .WithAltText("Test chart")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "aria-labelledby="),
        new DrawnToken("SvgRoot_HasAriaDescribedby_WhenDescriptionSet", () => Plt.Create()
            .WithDescription("Detailed description")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg(), "aria-describedby="),
        new DrawnToken("LegendGroup_HasAriaLabel", () => Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Revenue")
            .ToSvg(), "aria-label=\"Chart legend\""),
        new DrawnToken("SeriesGroup_HasAriaLabel_WhenLabeled", () => Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Revenue")
            .ToSvg(), "aria-label=\"Revenue\""),
        // Legend item group should carry aria-label from series label
        new DrawnToken("LegendItemGroup_HasAriaLabel", () => Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Revenue")
            .ToSvg(), "aria-label=\"Revenue\""),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void EscapeForXml_Helper_MatchesOriginal()
    {
        Assert.Equal("a &amp; b", "a & b".EscapeForXml());
        Assert.Equal("&lt;b&gt;", "<b>".EscapeForXml());
        Assert.Equal("plain", "plain".EscapeForXml());
    }
}
