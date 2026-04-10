// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG rich-tooltip script injection and behavior.</summary>
public class SvgRichTooltipTests
{
    /// <summary>Verifies that enabling rich tooltips injects a script element.</summary>
    [Fact]
    public void WithRichTooltips_InjectsScriptElement()
    {
        string svg = Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<script", svg);
    }

    /// <summary>Verifies that the rich-tooltip script creates a floating div element.</summary>
    [Fact]
    public void RichTooltipScript_CreatesDiv()
    {
        string svg = Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("createElement('div')", svg);
    }

    /// <summary>Verifies that the rich-tooltip script reads title element text content.</summary>
    [Fact]
    public void RichTooltipScript_ReadsTitleText()
    {
        string svg = Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("textContent", svg);
    }

    /// <summary>Verifies that the rich-tooltip script hides on mouseout.</summary>
    [Fact]
    public void RichTooltipScript_HidesOnMouseout()
    {
        string svg = Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("mouseout", svg);
    }

    /// <summary>Verifies that without WithRichTooltips(), no rich-tooltip script is present.</summary>
    [Fact]
    public void WithoutRichTooltips_NoScript()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that WithRichTooltips(false) disables the feature.</summary>
    [Fact]
    public void WithRichTooltipsFalse_NoScript()
    {
        string svg = Plt.Create()
            .WithRichTooltips(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }
}
