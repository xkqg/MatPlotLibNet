// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies SVG legend-toggle script injection and behavior.</summary>
public class SvgLegendToggleTests
{
    /// <summary>Verifies that enabling legend-toggle injects a script element.</summary>
    [Fact]
    public void WithLegendToggle_InjectsScriptElement()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("<script", svg);
    }

    /// <summary>Verifies that the legend-toggle script queries elements by data-legend-index.</summary>
    [Fact]
    public void LegendToggleScript_QueriesDataLegendIndex()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("data-legend-index", svg);
    }

    /// <summary>Verifies that the legend-toggle script queries elements by data-series-index.</summary>
    [Fact]
    public void LegendToggleScript_QueriesDataSeriesIndex()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("data-series-index", svg);
    }

    /// <summary>Verifies that the legend-toggle script uses a click event handler.</summary>
    [Fact]
    public void LegendToggleScript_UsesClickEvent()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("click", svg);
    }

    /// <summary>Verifies that without WithLegendToggle(), the legend-toggle script is not present.</summary>
    [Fact]
    public void WithoutLegendToggle_NoLegendToggleScript()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        // No interactivity at all — no script
        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Verifies that WithLegendToggle(false) disables the feature.</summary>
    [Fact]
    public void WithLegendToggleFalse_NoScript()
    {
        string svg = Plt.Create()
            .WithLegendToggle(false)
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("<script", svg);
    }

    /// <summary>Phase S regression (2026-04-19): the frame <c>&lt;rect&gt;</c> must be a CHILD of
    /// <c>&lt;g class="legend"&gt;</c>, not a sibling. Pre-fix the renderer emitted the rect
    /// BEFORE opening the group; <see cref="MatPlotLibNet.Rendering.Svg.SvgLegendDragScript"/>
    /// applies <c>transform="translate(dx,dy)"</c> to the group, so a stranded sibling rect
    /// stayed put while items moved (reported by user mid-drag, fixed in Phase S commit
    /// 93c30e3, AxesRenderer.cs). This test pins the markup contract so a future renderer
    /// refactor cannot re-strand the frame.</summary>
    [Fact]
    public void LegendFrameRect_IsInside_LegendGroup()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A")
            .ToSvg();

        // Locate the legend group's opening tag and its matching close. The frame <rect>
        // is the FIRST element emitted inside; data-legend-index entries follow.
        var groupOpen = svg.IndexOf("class=\"legend\"", StringComparison.Ordinal);
        Assert.True(groupOpen >= 0, "Legend group not found in output.");
        var groupEnd = svg.IndexOf("</g>", groupOpen, StringComparison.Ordinal);
        Assert.True(groupEnd > groupOpen, "Legend group closing tag not found.");

        var groupBody = svg.Substring(groupOpen, groupEnd - groupOpen);
        Assert.Contains("<rect", groupBody);
        // And the rect must precede the first legend-item entry — confirms it is the frame
        // background, not a swatch belonging to an entry.
        var firstRect = groupBody.IndexOf("<rect", StringComparison.Ordinal);
        var firstItem = groupBody.IndexOf("data-legend-index", StringComparison.Ordinal);
        Assert.True(firstItem < 0 || firstRect < firstItem,
            $"Frame rect must precede the first legend item, got rect at {firstRect}, item at {firstItem}.");
    }
}
