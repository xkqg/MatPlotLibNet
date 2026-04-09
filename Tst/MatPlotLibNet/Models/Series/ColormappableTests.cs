// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies that <see cref="IColormappable"/> and <see cref="INormalizable"/> are properly
/// implemented by the expected series types, and that <see cref="Builders.AxesBuilder"/> routes
/// colormap/normalizer assignment polymorphically.</summary>
public class ColormappableTests
{
    // --- IColormappable ---

    [Fact]
    public void IColormappable_HeatmapSeries()
    {
        IColormappable c = new HeatmapSeries(new double[2, 2]);
        c.ColorMap = ColorMaps.Viridis;
        Assert.Same(ColorMaps.Viridis, c.ColorMap);
    }

    [Fact]
    public void IColormappable_ImageSeries()
    {
        IColormappable c = new ImageSeries(new double[2, 2]);
        c.ColorMap = ColorMaps.Plasma;
        Assert.Same(ColorMaps.Plasma, c.ColorMap);
    }

    [Fact]
    public void IColormappable_Histogram2DSeries()
    {
        IColormappable c = new Histogram2DSeries([1.0, 2.0], [3.0, 4.0]);
        c.ColorMap = ColorMaps.Inferno;
        Assert.Same(ColorMaps.Inferno, c.ColorMap);
    }

    [Fact]
    public void IColormappable_ContourSeries()
    {
        IColormappable c = new ContourSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2]);
        c.ColorMap = ColorMaps.Magma;
        Assert.Same(ColorMaps.Magma, c.ColorMap);
    }

    [Fact]
    public void IColormappable_SurfaceSeries()
    {
        IColormappable c = new SurfaceSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2]);
        c.ColorMap = ColorMaps.Blues;
        Assert.Same(ColorMaps.Blues, c.ColorMap);
    }

    [Fact]
    public void IColormappable_ScatterSeries()
    {
        IColormappable c = new ScatterSeries([1.0], [2.0]);
        c.ColorMap = ColorMaps.Reds;
        Assert.Same(ColorMaps.Reds, c.ColorMap);
    }

    [Fact]
    public void IColormappable_TreemapSeries()
    {
        var root = new TreeNode { Label = "root", Value = 1.0 };
        IColormappable c = new TreemapSeries(root);
        c.ColorMap = ColorMaps.Viridis;
        Assert.Same(ColorMaps.Viridis, c.ColorMap);
    }

    // --- INormalizable ---

    [Fact]
    public void INormalizable_HeatmapSeries()
    {
        INormalizable n = new HeatmapSeries(new double[2, 2]);
        n.Normalizer = LinearNormalizer.Instance;
        Assert.Same(LinearNormalizer.Instance, n.Normalizer);
    }

    [Fact]
    public void INormalizable_ImageSeries()
    {
        INormalizable n = new ImageSeries(new double[2, 2]);
        n.Normalizer = new LogNormalizer();
        Assert.IsType<LogNormalizer>(n.Normalizer);
    }

    [Fact]
    public void INormalizable_Histogram2DSeries()
    {
        INormalizable n = new Histogram2DSeries([1.0], [1.0]);
        n.Normalizer = new TwoSlopeNormalizer(0.5);
        Assert.IsType<TwoSlopeNormalizer>(n.Normalizer);
    }

    [Fact]
    public void ScatterSeries_IsColormappableButNotNormalizable()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.IsAssignableFrom<IColormappable>(series);
        Assert.False(typeof(INormalizable).IsAssignableFrom(typeof(ScatterSeries)));
    }

    [Fact]
    public void SurfaceSeries_IsColormappableButNotNormalizable()
    {
        var series = new SurfaceSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2]);
        Assert.IsAssignableFrom<IColormappable>(series);
        Assert.False(typeof(INormalizable).IsAssignableFrom(typeof(SurfaceSeries)));
    }

    // --- AxesBuilder polymorphic routing ---

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
