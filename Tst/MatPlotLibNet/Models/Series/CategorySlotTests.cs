// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The one rule for where a category, and a bar standing in it, sits on a category axis: category k owns the slot
/// [k, k + 1] and is centred at k + 0.5. The bar renderer, a bar series' data range, the category labels, a candlestick's
/// body and the indicator shortcuts all read it, and so does a verb that marks a bar (Pareto's running share), so a mark
/// and its bar cannot drift apart.</summary>
public class CategorySlotTests
{
    /// <summary>Category k owns [k, k + 1], centred halfway: categories 0 to n - 1 lie side by side from 0 to n.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    public void CategoryK_OwnsTheSlotFromKToKPlusOne_CentredHalfway(int k)
    {
        var slot = CategorySlot.Of(k);

        Assert.Equal(k, slot.Left);
        Assert.Equal(k + 0.5, slot.Centre);
        Assert.Equal(k + 1, slot.Right);
    }

    /// <summary>The shift from an index to its centre is half a slot; the indicator shortcuts move their points by it.</summary>
    [Fact]
    public void TheCentreLiesHalfASlotPastTheIndex()
    {
        Assert.Equal(0.5, CategorySlot.CentreOffset);
        Assert.Equal(CategorySlot.CentreOffset, CategorySlot.Of(0).Centre);
    }

    /// <summary>A bar on a numeric axis stands around its x, half its width to each side.</summary>
    [Fact]
    public void ABarAroundANumericX_ReachesHalfItsWidthToEachSide()
    {
        var bar = CategorySlot.Around(10, 0.8);

        Assert.Equal(9.6, bar.Left, 12);
        Assert.Equal(10, bar.Centre);
        Assert.Equal(10.4, bar.Right, 12);
    }

    /// <summary>A centred bar stands on its slot's centre exactly: that centre is where a mark on the bar goes.</summary>
    [Fact]
    public void ACentredBar_StandsOnItsSlotsCentre()
    {
        var bar = CategorySlot.Of(2).Bar(0.8, 0, BarAlignment.Center);

        Assert.Equal(2.5, bar.Centre);
        Assert.Equal(2.1, bar.Left, 12);
        Assert.Equal(2.9, bar.Right, 12);
    }

    /// <summary>The bars of a group step aside for each other by their offsets: three groups of 0.7 / 3 wide sit side by side
    /// inside one slot, touching, the middle one on the slot's centre.</summary>
    [Fact]
    public void TheBarsOfAGroup_SitSideBySideInsideTheirSlot()
    {
        const double barWidth = 0.7 / 3;
        var bars = Enumerable.Range(0, 3)
            .Select(g => CategorySlot.Of(4).Bar(barWidth, (g - 1) * barWidth, BarAlignment.Center))
            .ToArray();

        Assert.Equal(4.5, bars[1].Centre);
        Assert.Equal(bars[0].Right, bars[1].Left, 12);
        Assert.Equal(bars[1].Right, bars[2].Left, 12);
        Assert.Equal(4.15, bars[0].Left, 12);
        Assert.Equal(4.85, bars[2].Right, 12);
    }

    /// <summary>An edge-aligned bar starts at its slot's left edge, moved by its offset, and is centred half its width in.</summary>
    [Theory]
    [InlineData(0.0, 1.0, 1.4, 1.8)]
    [InlineData(0.2, 1.2, 1.6, 2.0)]
    public void AnEdgeAlignedBar_StartsAtItsSlotsLeftEdge(double offset, double left, double centre, double right)
    {
        var bar = CategorySlot.Of(1).Bar(0.8, offset, BarAlignment.Edge);

        Assert.Equal(left, bar.Left, 12);
        Assert.Equal(centre, bar.Centre, 12);
        Assert.Equal(right, bar.Right, 12);
    }
}
