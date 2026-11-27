// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A verb that acts on the series added last finds it by its kind: the last of a kind wherever it stands in the list, or the
/// last series itself when it is of the kind; with none, nothing is found and nothing changes.</summary>
public sealed class AxesLastSeriesExtensionsTests
{
    private static readonly string[] Names = ["a"];

    [Fact]
    public void TheLastSeriesOfAKind_IsTheNewestOfIt_WhateverIsAddedAfter()
    {
        var axes = new Axes();
        axes.Bar(Names, [1.0]);
        var newest = axes.Bar(Names, [2.0]);
        axes.Plot([1.0], [1.0]);

        Assert.Same(newest, axes.LastSeriesOf<BarSeries>());
    }

    [Fact]
    public void WhenNoSeriesIsOfTheKind_NothingIsFound()
    {
        var axes = new Axes();
        axes.Plot([1.0], [1.0]);

        Assert.Null(axes.LastSeriesOf<BarSeries>());
        Assert.Null(new Axes().LastSeriesOf<BarSeries>());
    }

    [Fact]
    public void TheLastSeriesIsTheOneAskedFor_OnlyWhenItIsOfTheKind()
    {
        var axes = new Axes();
        var heatmap = axes.Heatmap(new double[,] { { 1 } });

        Assert.Same(heatmap, axes.LastSeriesIs<IColormappable>());

        axes.Plot([1.0], [1.0]);

        Assert.Null(axes.LastSeriesIs<IColormappable>());
        Assert.Null(new Axes().LastSeriesIs<IColormappable>());
    }
}
