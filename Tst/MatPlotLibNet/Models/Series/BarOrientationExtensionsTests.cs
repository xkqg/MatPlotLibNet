// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one rule for which axis bars stand their categories along: upright bars along X, bars that lie down along
/// Y. A horizontal chart is an upright one with its axes swapped.</summary>
public class BarOrientationExtensionsTests
{
    [Theory]
    [InlineData(BarOrientation.Vertical, false)]
    [InlineData(BarOrientation.Horizontal, true)]
    public void CategoriesAlongY_OnlyForBarsThatLieDown(BarOrientation orientation, bool alongY)
    {
        Assert.Equal(alongY, orientation.CategoriesAlongY());
    }

    private static readonly DataRangeContribution Upright = new(1, 2, 3, 4,
        StickyXMin: 5, StickyXMax: 6, StickyYMin: 7, StickyYMax: 8, StickyZMin: 9, StickyZMax: 10, ZMin: 11, ZMax: 12);

    [Fact]
    public void OnAxes_UprightBars_KeepTheRangeAsMeasured()
    {
        Assert.Equal(Upright, Upright.OnAxes(BarOrientation.Vertical));
    }

    [Fact]
    public void OnAxes_BarsThatLieDown_SwapXAndY_TheirStickyEdgesWithThem_AndKeepZ()
    {
        var lying = Upright.OnAxes(BarOrientation.Horizontal);

        Assert.Equal(new DataRangeContribution(3, 4, 1, 2,
            StickyXMin: 7, StickyXMax: 8, StickyYMin: 5, StickyYMax: 6, StickyZMin: 9, StickyZMax: 10, ZMin: 11, ZMax: 12), lying);
    }

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
