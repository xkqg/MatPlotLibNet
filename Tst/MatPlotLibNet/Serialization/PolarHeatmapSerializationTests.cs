// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Verifies JSON serialization round-trip for <see cref="PolarHeatmapSeries"/>.</summary>
public class PolarHeatmapSerializationTests
{
    private static double[,] MakeData(int thetaBins = 4, int rBins = 3)
    {
        var d = new double[thetaBins, rBins];
        for (int t = 0; t < thetaBins; t++)
            for (int r = 0; r < rBins; r++)
                d[t, r] = t * rBins + r + 1.0;
        return d;
    }

    [Fact]
    public void RoundTrip_PreservesType()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(), 4, 3))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"polarheatmap\"", json);

        var restored = new ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<PolarHeatmapSeries>().FirstOrDefault();
        Assert.NotNull(series);
    }

    [Fact]
    public void RoundTrip_PreservesBinCounts()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(6, 5), 6, 5))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<PolarHeatmapSeries>().First();
        Assert.Equal(6, series.ThetaBins);
        Assert.Equal(5, series.RBins);
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        var data = MakeData(4, 3);
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(data, 4, 3))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<PolarHeatmapSeries>().First();
        Assert.Equal(data[2, 1], series.Data[2, 1], 1e-9);
    }

    [Fact]
    public void RoundTrip_PreservesColorMap()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarHeatmap(MakeData(), 4, 3,
                s => s.ColorMap = ColorMapRegistry.Get("inferno")))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var series = restored.SubPlots[0].Series.OfType<PolarHeatmapSeries>().First();
        Assert.Equal("inferno", series.ColorMap?.Name);
    }
}
