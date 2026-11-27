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

    /// <summary>Row k of n counted from the top stands in the slot n - 1 - k: the first row is the highest slot of an axis that
    /// counts upward, the last row the lowest. The ridgeline, the dot plot, the risk table, a heatmap's rows and a calendar's
    /// weekdays all stand a row by this one rule.</summary>
    [Theory]
    [InlineData(0, 4, 3)]
    [InlineData(1, 4, 2)]
    [InlineData(3, 4, 0)]
    [InlineData(0, 1, 0)]
    public void RowK_OfN_CountedFromTheTop_StandsInSlotNMinusOneMinusK(int row, int count, int slot)
    {
        Assert.Equal(CategorySlot.Of(slot), CategorySlot.FromTop(row, count));
        Assert.Equal(slot + 0.5, CategorySlot.FromTop(row, count).Centre);
    }

    /// <summary>The centres of the first n slots, in the order of the slots: where the marks of n categories stand, each on
    /// the centre of its own slot.</summary>
    [Theory]
    [InlineData(1, new[] { 0.5 })]
    [InlineData(3, new[] { 0.5, 1.5, 2.5 })]
    public void TheCentres_AreTheMiddlesOfTheFirstNSlots(int count, double[] expected) =>
        Assert.Equal(expected, CategorySlot.Centres(count));

    /// <summary>No categories have no centres.</summary>
    [Fact]
    public void NoCategories_HaveNoCentres() => Assert.Empty(CategorySlot.Centres(0));

    /// <summary>A slot is as wide as it runs from its left edge to its right: one for a whole slot, the bar's width for a bar
    /// that stands in it or around a number.</summary>
    [Fact]
    public void ASlotsWidth_IsTheDistanceFromItsLeftEdgeToItsRight()
    {
        Assert.Equal(1.0, CategorySlot.Of(7).Width);
        Assert.Equal(0.8, CategorySlot.Around(10, 0.8).Width, 12);
        Assert.Equal(0.6, CategorySlot.Of(2).Bar(0.6, 0.1, BarAlignment.Edge).Width, 12);
    }

    /// <summary>The centres of the first rows counted from the top: the top row stands in the highest slot, so the centres run
    /// downward.</summary>
    [Theory]
    [InlineData(3, 5, new[] { 4.5, 3.5, 2.5 })]
    [InlineData(4, 4, new[] { 3.5, 2.5, 1.5, 0.5 })]
    [InlineData(1, 1, new[] { 0.5 })]
    public void TheCentresFromTheTop_AreTheCentresOfTheFirstRows_HighestFirst(int count, int rows, double[] expected) =>
        Assert.Equal(expected, CategorySlot.CentresFromTop(count, rows));

    /// <summary>No rows counted have no centres.</summary>
    [Fact]
    public void NoRowsCounted_HaveNoCentresFromTheTop() => Assert.Empty(CategorySlot.CentresFromTop(0, 3));
}
