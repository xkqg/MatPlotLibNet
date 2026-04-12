// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>Verifies that SVG data attributes (data-series-index, data-legend-index) are emitted
/// when any Figure-level interactivity flag is enabled.</summary>
public class SvgDataAttributeTests
{
    /// <summary>Verifies that enabling legend-toggle causes data-series-index to appear in SVG.</summary>
    [Fact]
    public void EnableLegendToggle_EmitsDataSeriesIndex()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("data-series-index", svg);
    }

    /// <summary>Verifies that enabling highlight causes data-series-index to appear in SVG.</summary>
    [Fact]
    public void EnableHighlight_EmitsDataSeriesIndex()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("data-series-index", svg);
    }

    /// <summary>Verifies that enabling selection causes data-series-index to appear in SVG.</summary>
    [Fact]
    public void EnableSelection_EmitsDataSeriesIndex()
    {
        string svg = Plt.Create()
            .WithSelection()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("data-series-index", svg);
    }

    /// <summary>Verifies that enabling rich tooltips causes data-series-index to appear in SVG.</summary>
    [Fact]
    public void EnableRichTooltips_EmitsDataSeriesIndex()
    {
        string svg = Plt.Create()
            .WithRichTooltips()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.Contains("data-series-index", svg);
    }

    /// <summary>Verifies that without any interactivity flag, no data-series-index attribute is emitted.</summary>
    [Fact]
    public void NoInteractivityFlags_NoDataSeriesIndex()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        Assert.DoesNotContain("data-series-index", svg);
    }

    /// <summary>Verifies that data-series-index values are sequential starting from 0.</summary>
    [Fact]
    public void MultiSeries_DataSeriesIndexIsSequential()
    {
        string svg = Plt.Create()
            .WithHighlight()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .Plot([1.0, 2.0], [5.0, 6.0])
            .ToSvg();

        Assert.Contains("data-series-index=\"0\"", svg);
        Assert.Contains("data-series-index=\"1\"", svg);
    }

    /// <summary>Verifies that labeled series produce data-legend-index attributes when interactivity is enabled.</summary>
    [Fact]
    public void LabeledSeries_WithInteractivity_EmitsDataLegendIndex()
    {
        string svg = Plt.Create()
            .WithLegendToggle()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
            .ToSvg();

        Assert.Contains("data-legend-index", svg);
    }

    /// <summary>Verifies that without interactivity, labeled series do not produce data-legend-index.</summary>
    [Fact]
    public void LabeledSeries_NoInteractivity_NoDataLegendIndex()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
            .ToSvg();

        Assert.DoesNotContain("data-legend-index", svg);
    }
}
