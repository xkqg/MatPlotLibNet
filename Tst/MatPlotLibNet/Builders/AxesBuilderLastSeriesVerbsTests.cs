// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The verbs that change the series added last: its point budget and its normaliser (its colour map, by name or by map,
/// is <c>ColormappableTests</c>').</summary>
public class AxesBuilderLastSeriesVerbsTests
{
    [Fact]
    public void WithDownsampling_GivesTheSeriesOfTheAxesThePointBudget()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3, 4, 5], [1.0, 2, 3, 4, 5]).WithDownsampling(maxPoints: 100))
            .Build().SubPlots[0];

        Assert.Equal(100, Assert.IsAssignableFrom<XYSeries>(Assert.Single(axes.Series)).MaxDisplayPoints);
    }

    [Fact]
    public void WithNormalizer_SetsTheNormalizerOfTheLastHeatmap()
    {
        var norm = new LogNormalizer();

        var heatmap = (HeatmapSeries)Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithNormalizer(norm))
            .Build().SubPlots[0].Series[0];

        Assert.Same(norm, heatmap.Normalizer);
    }
}
