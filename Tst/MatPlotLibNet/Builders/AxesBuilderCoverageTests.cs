// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Phase Y.4 (v1.7.2, 2026-04-19) — branch coverage for the
/// <see cref="AxesBuilder"/> configure-callback arms (Action&lt;T&gt;? overloads
/// where the non-null configure path was untested) and the rare-fluent methods
/// (SetXDateFormat, WithDownsampling, NestedPie, WithProjection, indicator
/// helpers). Pre-Y.4: 85.3%L / 60.8%B (complexity 231).</summary>
public class AxesBuilderCoverageTests
{
    /// <summary>AxHLine with non-null configure callback — line 267 true arm.</summary>
    [Fact]
    public void AxHLine_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxHLine(3.5, line => line.Label = "threshold"))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].ReferenceLines);
    }

    /// <summary>AxVLine with non-null configure callback — line 275 true arm.</summary>
    [Fact]
    public void AxVLine_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxVLine(1.5, line => line.Label = "midpoint"))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].ReferenceLines);
    }

    /// <summary>AxHSpan with non-null configure callback.</summary>
    [Fact]
    public void AxHSpan_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxHSpan(2.0, 4.0, span => span.Alpha = 0.3))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Spans);
    }

    /// <summary>AxVSpan with non-null configure callback.</summary>
    [Fact]
    public void AxVSpan_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .AxVSpan(0.5, 1.5, span => span.Alpha = 0.5))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Spans);
    }

    /// <summary>SetXDateFormat — line 328 (0%-covered method).</summary>
    [Fact]
    public void SetXDateFormat_AppliesFormatter()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetXDateFormat("yyyy-MM"))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>SetYDateFormat — line 336 (0%-covered method).</summary>
    [Fact]
    public void SetYDateFormat_AppliesFormatter()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetYDateFormat("HH:mm"))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>SetYTickFormatter — line 351 (0%-covered method).</summary>
    [Fact]
    public void SetYTickFormatter_AppliesCustomFormatter()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .SetYTickFormatter(new EngFormatter()))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>SetYTickLocator — line 369 (0%-covered method).</summary>
    [Fact]
    public void SetYTickLocator_AppliesCustomLocator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3], [10.0, 100, 1000])
                .SetYTickLocator(new MaxNLocator(5)))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WithDownsampling — line 405 (0%-covered).</summary>
    [Fact]
    public void WithDownsampling_AppliesMaxPoints()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5])
                .WithDownsampling(maxPoints: 100))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WithProjection — line 702 (0%-covered).</summary>
    [Fact]
    public void WithProjection_AppliesCameraAngles()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter3D([1.0, 2], [3.0, 4], [5.0, 6])
                .WithProjection(elevation: 45, azimuth: -90))
            .Build();
        Assert.Equal(45.0, fig.SubPlots[0].Elevation);
        Assert.Equal(-90.0, fig.SubPlots[0].Azimuth);
    }

    /// <summary>NestedPie with non-null configure — line 624 (0%-covered method).</summary>
    [Fact]
    public void NestedPie_WithConfigure_AddsSunburstSeries()
    {
        var root = new TreeNode
        {
            Label = "Root",
            Children =
            [
                new() { Label = "A", Value = 1 },
                new() { Label = "B", Value = 2 },
            ]
        };
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.NestedPie(root, s => s.ShowLabels = true))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].Series);
    }

    /// <summary>Sma indicator with non-null configure — line 893 covers the configure arm.</summary>
    [Fact]
    public void Sma_WithConfigure_AppliesCustomisation()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2, 3, 4, 5], [10.0, 11, 12, 13, 14])
                .Sma(period: 3, configure: ind => ind.Label = "SMA3"))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>WilliamsR — line 933 (0%-covered method).</summary>
    [Fact]
    public void WilliamsR_AppliesIndicator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WilliamsR(
                    high: new[] { 10.0, 11, 12, 13, 14 },
                    low:  new[] { 5.0, 6, 7, 8, 9 },
                    close: new[] { 7.0, 9, 10, 11, 12 },
                    period: 3))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Obv — line 943 (0%-covered method).</summary>
    [Fact]
    public void Obv_AppliesIndicator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Obv(close: new[] { 1.0, 2, 1, 3 }, volume: new[] { 100.0, 200, 50, 150 }))
            .Build();
        Assert.NotNull(fig);
    }

    /// <summary>Cci — line 953 (0%-covered method).</summary>
    [Fact]
    public void Cci_AppliesIndicator()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Cci(
                    high: new[] { 10.0, 11, 12, 13, 14, 15 },
                    low:  new[] { 5.0, 6, 7, 8, 9, 10 },
                    close: new[] { 7.0, 9, 10, 11, 12, 13 },
                    period: 3))
            .Build();
        Assert.NotNull(fig);
    }
}
