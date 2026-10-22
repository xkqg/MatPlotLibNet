// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The outline of a violin's body: how wide it is at each height. The renderer draws it and the series ranges over
/// it, so the width is decided here once.</summary>
public class ViolinBodyExtensionsTests
{
    [Fact]
    public void TheOutline_StandsAtTwentyOneHeights_FromTheLowestValueToTheHighest()
    {
        var outline = new[] { 1.0, 2, 3, 4, 5 }.Outline(0.5);

        Assert.Equal(ViolinBodyExtensions.Intervals + 1, outline.Length);
        Assert.Equal(21, outline.Length);
        Assert.Equal(1, outline[0].Y);
        Assert.Equal(5, outline[^1].Y, 12);
        Assert.Equal(1.2, outline[1].Y, 12);
    }

    /// <summary>The width at a height is the share of the sample within a twentieth of the extent either side, times the
    /// width: with 1..5 (extent 4, window 0.2 either side) at most one observation is in a window, 0.2 of the sample.</summary>
    [Fact]
    public void TheHalfWidth_IsTheShareOfTheSampleInTheWindow_TimesTheWidth()
    {
        var outline = new[] { 1.0, 2, 3, 4, 5 }.Outline(0.5);

        Assert.Equal(0.1, outline[0].HalfWidth, 12);   // 1 is in the window around 1
        Assert.Equal(0, outline[2].HalfWidth);         // nothing lies within 0.2 of 1.4
        Assert.Equal(0.1, outline.Max(slice => slice.HalfWidth), 12);
    }

    [Fact]
    public void TheHalfWidth_ScalesWithTheWidth()
    {
        var narrow = new[] { 1.0, 2, 3, 4, 5 }.Outline(0.5);
        var wide = new[] { 1.0, 2, 3, 4, 5 }.Outline(2);

        Assert.Equal(narrow.Select(s => 4 * s.HalfWidth), wide.Select(s => s.HalfWidth));
    }

    /// <summary>A sample whose values are all equal has no extent: it is given one of 1, so the outline runs from the value
    /// up to the value plus 1, and the body is as wide as it can be (the whole width) where the observations are.</summary>
    [Fact]
    public void ASampleOfEqualValues_IsGivenAnExtentOfOne()
    {
        var outline = new[] { 5.0, 5, 5, 5 }.Outline(0.5);

        Assert.Equal(5, outline[0].Y);
        Assert.Equal(6, outline[^1].Y, 12);
        Assert.Equal(0.5, outline[0].HalfWidth, 12);
        Assert.Equal(0.5, outline[1].HalfWidth, 12);
        Assert.Equal(0, outline[2].HalfWidth);
    }

    [Fact]
    public void TwoClumps_MakeTwoBulges_AndNothingBetween()
    {
        var outline = new[] { 0.0, 0, 0, 10, 10, 10 }.Outline(1);

        Assert.Equal(0.5, outline[0].HalfWidth, 12);
        Assert.Equal(0.5, outline[^1].HalfWidth, 12);
        Assert.Equal(0, outline[10].HalfWidth);
    }
}
