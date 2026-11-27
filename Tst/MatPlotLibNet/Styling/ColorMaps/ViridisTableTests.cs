// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Styling.ColorMapNormalizers;

/// <summary>The library's viridis is matplotlib's own table of 256 colours, not an approximation through a few stops. matplotlib
/// 3.11.2, <c>colormaps['viridis']</c> is a <c>ListedColormap</c> of 256 entries, and a chart drawn with it is as far from the
/// reference as its colours are: a map of nine stops puts a mid-range cell up to 17 of 255 away from the colour matplotlib gives its
/// value, which moves the dominant colours of a picture from one histogram bucket to the next, and so does one level: the rounded hex
/// a table of the map prints is one level above the bytes matplotlib's renderers write in most entries (the pictures of the
/// references carry <c>(70, 51, 127)</c> where the rounded entry is <c>(70, 52, 128)</c>, a byte on the other side of a bucket of 16).
/// The values below are <c>colormaps['viridis'](i, bytes=True)</c>, read from the installed matplotlib.</summary>
public class ViridisTableTests
{
    [Theory]
    [InlineData(0, "#440154")]
    [InlineData(32, "#472C7B")]
    [InlineData(64, "#3A528B")]
    [InlineData(96, "#2C728E")]
    [InlineData(128, "#20908C")]
    [InlineData(160, "#28AE7F")]
    [InlineData(192, "#5EC961")]
    [InlineData(224, "#ADDC30")]
    [InlineData(255, "#FDE724")]
    public void AnEntryOfTheTable_IsTheColourMatplotlibGivesIt(int entry, string expected)
    {
        Assert.Equal(Color.FromHex(expected), ColorMaps.Viridis.GetColor(entry / 255.0));
    }

    [Fact]
    public void BetweenTwoEntries_TheColourIsABlendOfThem()
    {
        var first = ColorMaps.Viridis.GetColor(100 / 255.0);
        var second = ColorMaps.Viridis.GetColor(101 / 255.0);
        var between = ColorMaps.Viridis.GetColor(100.5 / 255.0);

        Assert.InRange(between.R, Math.Min(first.R, second.R), Math.Max(first.R, second.R));
        Assert.InRange(between.G, Math.Min(first.G, second.G), Math.Max(first.G, second.G));
        Assert.InRange(between.B, Math.Min(first.B, second.B), Math.Max(first.B, second.B));
    }

    [Fact]
    public void TheReversedMap_IsTheTableTheOtherWayRound()
    {
        var reversed = ColorMapRegistry.Get("viridis_r");
        Assert.NotNull(reversed);

        Assert.Equal(ColorMaps.Viridis.GetColor(0.0), reversed.GetColor(1.0));
        Assert.Equal(ColorMaps.Viridis.GetColor(128 / 255.0), reversed.GetColor(127 / 255.0));
    }
}
