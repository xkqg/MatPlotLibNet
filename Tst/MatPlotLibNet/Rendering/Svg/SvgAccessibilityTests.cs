// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG semantic-structure accessibility (role, title, desc, aria-label).</summary>
public class SvgAccessibilityTests
{
    [Fact]
    public void SvgRoot_HasRoleImgAttribute()
    {
        var svg = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();
        Assert.Contains("role=\"img\"", svg);
    }

    [Fact]
    public void WithAltText_EmitsTitleElement()
    {
        var svg = Plt.Create()
            .WithAltText("Sales over time")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<title", svg);
        Assert.Contains("Sales over time", svg);
    }

    [Fact]
    public void WithDescription_EmitsDescElement()
    {
        var svg = Plt.Create()
            .WithDescription("Monthly revenue 2025")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<desc", svg);
        Assert.Contains("Monthly revenue 2025", svg);
    }

    [Fact]
    public void WithoutAltText_UsesFigureTitleAsFallback()
    {
        var svg = Plt.Create()
            .WithTitle("My Chart")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<title", svg);
        Assert.Contains("My Chart", svg);
    }

    [Fact]
    public void WithoutAnyTitle_StillHasRoleImg()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("role=\"img\"", svg);
    }

    [Fact]
    public void AltText_SpecialChars_AreXmlEscaped()
    {
        var svg = Plt.Create()
            .WithAltText("Revenue <Q1> & profit")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("&lt;Q1&gt;", svg);
        Assert.Contains("&amp;", svg);
    }

    [Fact]
    public void SvgRoot_HasAriaLabelledby_WhenAltTextSet()
    {
        var svg = Plt.Create()
            .WithAltText("Test chart")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("aria-labelledby=", svg);
    }

    [Fact]
    public void SvgRoot_HasAriaDescribedby_WhenDescriptionSet()
    {
        var svg = Plt.Create()
            .WithDescription("Detailed description")
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("aria-describedby=", svg);
    }

    [Fact]
    public void LegendGroup_HasAriaLabel()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Revenue")
            .ToSvg();

        Assert.Contains("aria-label=\"Chart legend\"", svg);
    }

    [Fact]
    public void SeriesGroup_HasAriaLabel_WhenLabeled()
    {
        var svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Revenue")
            .ToSvg();

        Assert.Contains("aria-label=\"Revenue\"", svg);
    }

    [Fact]
    public void LegendItemGroup_HasAriaLabel()
    {
        var svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Revenue")
            .ToSvg();

        // Legend item group should carry aria-label from series label
        Assert.Contains("aria-label=\"Revenue\"", svg);
    }

    [Fact]
    public void EscapeForXml_Helper_MatchesOriginal()
    {
        Assert.Equal("a &amp; b", "a & b".EscapeForXml());
        Assert.Equal("&lt;b&gt;", "<b>".EscapeForXml());
        Assert.Equal("plain", "plain".EscapeForXml());
    }
}
