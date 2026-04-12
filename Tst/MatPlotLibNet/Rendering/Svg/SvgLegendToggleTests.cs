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
}
