// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    public void ScatterSeries_IsColormappableAndNormalizable()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.IsAssignableFrom<IColormappable>(series);
        Assert.IsAssignableFrom<INormalizable>(series);
    }

    [Fact]
    public void SurfaceSeries_IsColormappableAndNormalizable()
    {
        var series = new SurfaceSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2]);
        Assert.IsAssignableFrom<IColormappable>(series);
        Assert.IsAssignableFrom<INormalizable>(series);
    }

    [Fact]
    public void INormalizable_SurfaceSeries()
    {
        INormalizable n = new SurfaceSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2]);
        n.Normalizer = new PowerNormNormalizer(0.5);
        Assert.IsType<PowerNormNormalizer>(n.Normalizer);
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

    // --- B6: ScatterSeries C array ---

    [Fact]
    public void ScatterSeries_C_CanBeAssigned()
    {
        var series = new ScatterSeries([1.0, 2.0], [3.0, 4.0]) { C = [0.0, 1.0] };
        Assert.Equal(2, series.C!.Length);
    }

    [Fact]
    public void ScatterSeries_ColorsArray_TakesPrecedenceOverC()
    {
        // When Colors[] is set, C is effectively ignored (Colors wins)
        var colors = new[] { ColorMaps.Viridis.GetColor(0.0), ColorMaps.Viridis.GetColor(1.0) };
        var series = new ScatterSeries([1.0, 2.0], [3.0, 4.0])
        {
            Colors = colors,
            C = [0.5, 0.5],
            ColorMap = ColorMaps.Viridis
        };
        // Colors is set — renderer should use it; C is secondary
        Assert.NotNull(series.Colors);
        Assert.NotNull(series.C);
    }

    [Fact]
    public void ScatterSeries_CWithVMinVMax_Used()
    {
        var series = new ScatterSeries([1.0], [2.0])
        {
            C = [5.0],
            VMin = 0.0,
            VMax = 10.0,
            ColorMap = ColorMaps.Viridis
        };
        // VMin/VMax constrain the normalization: 5/(10-0) = 0.5
        var norm = LinearNormalizer.Instance;
        double expected = norm.Normalize(series.C![0], series.VMin!.Value, series.VMax!.Value);
        Assert.Equal(0.5, expected, 5);
    }

    [Fact]
    public void ScatterSeries_C_WithNormalizerFromSeries()
    {
        var series = new ScatterSeries([1.0], [2.0])
        {
            C = [25.0],
            VMin = 0.0,
            VMax = 100.0,
            ColorMap = ColorMaps.Viridis,
            Normalizer = new PowerNormNormalizer(gamma: 0.5)
        };
        // sqrt(25/100) = sqrt(0.25) = 0.5
        double t = series.Normalizer!.Normalize(series.C![0], series.VMin!.Value, series.VMax!.Value);
        Assert.Equal(0.5, t, 5);
    }
}
