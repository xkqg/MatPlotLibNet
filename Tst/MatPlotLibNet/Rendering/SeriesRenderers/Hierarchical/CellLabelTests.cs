// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers.Hierarchical;

/// <summary>Where the label of a cell of a hierarchy (a treemap tile, an icicle cell) stands in it: one <see cref="CellLabel"/>, inset
/// four pixels from the left with its baseline two below the font's size, and room for the text the width less the inset on each
/// side. The numbers were typed at each treemap and icicle site; the value gives every site the bits it typed.</summary>
public class CellLabelTests
{
    private static readonly Rect Cell = new(100.3, 50.7, 240.9, 80.1);
    private static readonly CellLabel Label = CellLabel.Standard;

    [Fact]
    public void TheStandardLabel_IsInsetFourPixelsWithItsBaselineTwoBelowTheFontSize()
    {
        Assert.Equal(4, Label.Pad);
        Assert.Equal(2, Label.BaselineGap);
    }

    /// <summary>The name hangs the gap below the cell's top (its box's ascent under that point), a pad from the left.</summary>
    [Fact]
    public void TheName_HangsTheGapBelowTheTop_AtThePadFromTheLeft()
    {
        Assert.Equal(new TextPlacement(new Point(Cell.X + 4, Cell.Y + 2), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top }, Label.Name(Cell));
    }

    [Fact]
    public void TheRoom_IsTheWidthLessThePadOnEachSide()
    {
        Assert.Equal(Cell.Width - 8, Label.RoomIn(Cell));
    }

    [Fact]
    public void ACellIsTallEnough_WhenItHoldsTheFontSizeAndThePadAboveAndBelow()
    {
        Assert.Equal(12 + 2 * 4, Label.HeightFor(12));
    }

    /// <summary>The measure hangs a pad under the name's baseline, at the pad from the left.</summary>
    [Fact]
    public void TheMeasure_HangsAPadUnderTheNamesBaseline()
    {
        Assert.Equal(new TextPlacement(new Point(Cell.X + 4, 90 + 4), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top }, Label.MeasureUnder(Cell, 90));
    }

    [Fact]
    public void TheMeasure_NeedsTheGapAboveTheBottomOfTheCell()
    {
        Assert.True(Label.MeasureFits(Cell, Cell.Y + Cell.Height - 2));
        Assert.False(Label.MeasureFits(Cell, Cell.Y + Cell.Height - 1.5));
    }

    [Fact]
    public void AHeaderStrip_PutsTheNameAtTheLeftAndTheMeasureAtTheRightAPadAboveItsBottom()
    {
        Assert.Equal(new Point(Cell.X + 4, Cell.Y + 18 - 4), Label.HeaderName(Cell, 18));
        Assert.Equal(new Point(Cell.X + Cell.Width - 4, Cell.Y + 18 - 4), Label.HeaderMeasure(Cell, 18));
    }

    [Fact]
    public void ANameAndAMeasureShareAHeader_WhenTheyAndAPadOnEachSideOfBothFitTheCell()
    {
        var cell = new Rect(10, 20, 200, 50);

        Assert.True(Label.HeaderPairFits(100, 84, cell));
        Assert.False(Label.HeaderPairFits(100, 84.5, cell));
    }
}
