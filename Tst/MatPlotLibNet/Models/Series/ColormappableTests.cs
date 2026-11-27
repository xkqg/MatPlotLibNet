// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies that <see cref="Builders.AxesBuilder"/> routes a colour map and a normalizer to the series that take one
/// (<see cref="IColormappable"/>, <see cref="INormalizable"/>) and passes over the rest. A colour map or a normalizer a series is
/// given is held by the drawn-property round trips; how a normalizer maps a value, by the normalizer tests.</summary>
public class ColormappableTests
{
    [Fact]
    public void AxesBuilder_WithColorMap_AppliesTo_ScatterSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter([1.0, 2.0], [3.0, 4.0])
                .WithColorMap(ColorMaps.Viridis))
            .Build();

        var scatter = (ScatterSeries)figure.SubPlots[0].Series[0];
        Assert.Same(ColorMaps.Viridis, scatter.ColorMap);
    }

    [Fact]
    public void AxesBuilder_WithColorMap_AppliesTo_SurfaceSeries()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Surface([0.0, 1.0], [0.0, 1.0], new double[2, 2])
                .WithColorMap(ColorMaps.Plasma))
            .Build();

        var surface = (SurfaceSeries)figure.SubPlots[0].Series[0];
        Assert.Same(ColorMaps.Plasma, surface.ColorMap);
    }

    [Fact]
    public void AxesBuilder_WithNormalizer_IgnoresNonNormalizableSeries()
    {
        // Should not throw even though LineSeries is not INormalizable
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0], [2.0])
                .WithNormalizer(LinearNormalizer.Instance))
            .Build();

        Assert.Single(figure.SubPlots[0].Series);
    }

    [Fact]
    public void AxesBuilder_WithColorMap_ByName_AppliesColormap()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } })
                .WithColorMap("plasma"))
            .Build();

        var hs = (HeatmapSeries)figure.SubPlots[0].Series[0];
        Assert.Equal("plasma", hs.ColorMap?.Name);
    }
}
