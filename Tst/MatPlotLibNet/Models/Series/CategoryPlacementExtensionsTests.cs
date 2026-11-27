// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Which axis a series' categories stand along: upright bars along X, bars that lie down along Y. A horizontal chart is an
/// upright one with its axes swapped.</summary>
public class CategoryPlacementExtensionsTests
{
    private static ICategoryLabeled Series(string kind) => kind switch
    {
        "bar" => new BarSeries(["a"], [1.0]),
        "barh" => new BarSeries(["a"], [1.0]) { Orientation = BarOrientation.Horizontal },
        "count" => new CountSeries(["a"]),
        "counth" => new CountSeries(["a"]) { Orientation = BarOrientation.Horizontal },
        _ => new CandlestickSeries([1.0], [2.0], [0.5], [1.5]) { DateLabels = ["d"] },
    };

    /// <summary>Bars that lie down name their categories along Y; upright bars and every other category-labelled series (a
    /// candlestick's dates) along X.</summary>
    [Theory]
    [InlineData("bar", false)]
    [InlineData("barh", true)]
    [InlineData("count", false)]
    [InlineData("counth", true)]
    [InlineData("candlestick", false)]
    public void NamesCategoriesAlongY_OnlyForBarsThatLieDown(string kind, bool alongY)
    {
        Assert.Equal(alongY, Series(kind).NamesCategoriesAlongY());
    }
}
