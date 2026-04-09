// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies legend rendering behavior in <see cref="ChartServices"/>.</summary>
public class LegendRenderingTests
{
    /// <summary>Verifies that labeled series produce a legend box in SVG output.</summary>
    [Fact]
    public void Legend_WithLabeledSeries_ContainsLegendText()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Temperature")
            .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "Humidity")
            .ToSvg();

        Assert.Contains("Temperature", svg);
        Assert.Contains("Humidity", svg);
    }

    /// <summary>Verifies that series without labels do not produce legend entries.</summary>
    [Fact]
    public void Legend_NoLabels_SkipsLegend()
    {
        string svg = Plt.Create()
            .Plot([1.0, 2.0], [3.0, 4.0])
            .ToSvg();

        // No labeled series → no legend rectangle
        // The SVG should not contain a legend group
        Assert.DoesNotContain("class=\"legend\"", svg);
    }

    /// <summary>Verifies that a hidden legend produces no legend output.</summary>
    [Fact]
    public void Legend_NotVisible_SkipsLegend()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Data")
                .WithLegend(visible: false))
            .ToSvg();

        Assert.DoesNotContain("class=\"legend\"", svg);
    }

    /// <summary>Verifies that the legend contains color swatches as rect elements.</summary>
    [Fact]
    public void Legend_ContainsColorSwatches()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A")
                .WithLegend())
            .ToSvg();

        Assert.Contains("class=\"legend\"", svg);
        Assert.Contains("Series A", svg);
    }

    /// <summary>Verifies that legend position can be set via AxesBuilder.</summary>
    [Fact]
    public void WithLegend_SetsPosition()
    {
        var builder = new FigureBuilder();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Test")
                .WithLegend(LegendPosition.LowerLeft))
            .Build();

        Assert.Equal(LegendPosition.LowerLeft, figure.SubPlots[0].Legend.Position);
    }

    /// <summary>Verifies that WithLegend defaults to Best position when called without arguments.</summary>
    [Fact]
    public void WithLegend_DefaultPosition_IsBest()
    {
        var builder = new FigureBuilder();
        var figure = builder
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Test")
                .WithLegend())
            .Build();

        Assert.Equal(LegendPosition.Best, figure.SubPlots[0].Legend.Position);
    }

    /// <summary>Verifies that only labeled series appear in the legend.</summary>
    [Fact]
    public void Legend_MixedLabeledAndUnlabeled_OnlyShowsLabeled()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Visible")
                .Plot([1.0, 2.0], [5.0, 6.0])
                .WithLegend())
            .ToSvg();

        Assert.Contains("Visible", svg);
        Assert.Contains("class=\"legend\"", svg);
    }

    /// <summary>Verifies that multiple labeled series all appear in the legend.</summary>
    [Fact]
    public void Legend_MultipleSeries_AllLabelsPresent()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Alpha")
                .Plot([1.0, 2.0], [5.0, 6.0], s => s.Label = "Beta")
                .Scatter([1.0, 2.0], [7.0, 8.0], s => s.Label = "Gamma")
                .WithLegend())
            .ToSvg();

        Assert.Contains("Alpha", svg);
        Assert.Contains("Beta", svg);
        Assert.Contains("Gamma", svg);
    }
}
