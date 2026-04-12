// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG highlight-on-hover script injection and behavior.</summary>
public class SvgHighlightTests
{
    /// <summary>Verifies that enabling highlight injects a script element.</summary>
    [Fact]
    public void WithHighlight_InjectsScriptElement()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<script", svg);
    }

    /// <summary>Verifies that the highlight script uses mouseenter to trigger dimming.</summary>
    [Fact]
    public void HighlightScript_UsesMouseenterEvent()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("mouseenter", svg);
    }

    /// <summary>Verifies that the highlight script manipulates opacity.</summary>
    [Fact]
    public void HighlightScript_ManipulatesOpacity()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("opacity", svg);
    }

    /// <summary>Verifies that the highlight script restores sibling opacity on mouseleave.</summary>
    [Fact]
    public void HighlightScript_RestoresOnMouseleave()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("mouseleave", svg);
    }

    /// <summary>Verifies that the highlight script dims siblings to 0.3 opacity.</summary>
    [Fact]
    public void HighlightScript_DimsSiblingToThirtyPercent()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("0.3", svg);
    }

    /// <summary>Verifies that without WithHighlight(), no highlight script is present.</summary>
    [Fact]
    public void WithoutHighlight_NoScript()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that WithHighlight(false) disables the feature.</summary>
    [Fact]
    public void WithHighlightFalse_NoScript()
    {
        string svg = Plt.Create()
            .WithHighlight(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }
}
