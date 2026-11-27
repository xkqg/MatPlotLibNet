// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The range a hexbin's hexagons are coloured over and the range its colour bar covers are one range
/// (<see cref="HexbinSeries.ColourRangeUnder"/>). matplotlib 3.11.2 (printed: 5 points at one place and 7 at another, gridsize 2):
/// <c>hexbin(..., mincnt=1)</c> and <c>mincnt=3</c> autoscale the norm to the counts drawn, (5, 7), and the bar covers (5, 7); with no
/// <c>mincnt</c> every hexagon is drawn and the range is (0, 7); one hexagon of 2 points opens to (1.8, 2.2). The library's own themes keep
/// their rule: half a count under the least count asked for, so it is not the darkest colour of the map.</summary>
public class HexbinColourRangeTests
{
    private static HexbinSeries Hexbin(int minCount) =>
        new([.. Enumerable.Repeat(0.0, 5), .. Enumerable.Repeat(10.0, 7)], [.. Enumerable.Repeat(0.0, 5), .. Enumerable.Repeat(10.0, 7)])
        {
            GridSize = 2,
            MinCount = minCount,
        };

    [Theory]
    [InlineData(1, 5.0, 7.0)]
    [InlineData(3, 5.0, 7.0)]
    [InlineData(0, 0.0, 7.0)]
    public void UnderAMatplotlibTheme_TheCountsAreColouredFromTheLeastDrawn(int minCount, double lo, double hi)
    {
        var range = Hexbin(minCount).ColourRangeUnder(Theme.MatplotlibV2);

        Assert.Equal(new MinMaxRange(lo, hi), range);
    }

    /// <summary>One count drawn is a range of no width, opened one wide as every colour range is (<c>ColorMapping.Opened</c>): its hexagon
    /// takes the map's first colour, as matplotlib 3.11.2 draws <c>hexbin([1, 1], [1, 1], gridsize=2, mincnt=1)</c> with no colour bar
    /// (python: norm (2.0, 2.0), the face colour viridis(0)); only matplotlib's colour bar widens the norm it reads, by a tenth.</summary>
    [Fact]
    public void UnderAMatplotlibTheme_OneCount_IsOpenedOneWide()
    {
        var one = new HexbinSeries([0.0, 0.0], [0.0, 0.0]) { GridSize = 2, MinCount = 1 };

        var range = one.ColourRangeUnder(Theme.MatplotlibV2);

        Assert.Equal(new MinMaxRange(2, 3), range);
    }

    [Fact]
    public void UnderTheLibrarysTheme_TheLeastCountAskedFor_StandsHalfACountUnderTheScale()
    {
        Assert.Equal(new MinMaxRange(2.5, 7), Hexbin(3).ColourRangeUnder(Theme.Default));
        Assert.Equal(new MinMaxRange(0, 7), Hexbin(0).ColourRangeUnder(Theme.Default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void TheColourBar_CoversTheRangeTheHexagonsAreColouredOver_UnderEveryTheme(int minCount)
    {
        foreach (var theme in new[] { Theme.Default, Theme.MatplotlibV2, Theme.MatplotlibClassic })
        {
            var series = Hexbin(minCount);

            Assert.Equal(series.ColourRangeUnder(theme), ((IColorBarDataProvider)series).ColorBarRangeUnder(theme));
        }
    }

}
