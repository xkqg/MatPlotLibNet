// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>What an axes draws below its bottom spine is read from the bands its renderer measured, by one rule both layout
/// paths reserve: whether anything is drawn there, and the room the bottom edge needs for it.</summary>
public sealed class EdgeBandsTests
{
    [Fact]
    public void NothingMeasuredBelow_DrawsNothingBelow()
    {
        Assert.False(new EdgeBands(Top: 4, Right: 5, Left: 6, Bottom: 0).DrawsBelow);
    }

    [Fact]
    public void TickLabelsOrAnAxisTitle_AreDrawnBelow()
    {
        Assert.True(new EdgeBands(0, 0, 0, Bottom: 12).DrawsBelow);
        Assert.True(new EdgeBands(0, 0, 0, Bottom: 0, BottomTitle: 20).DrawsBelow);
    }

    [Fact]
    public void TheBottomNeed_IsTheTitlesFarSide_WhenATitleIsDrawn_AndThePad()
    {
        Assert.Equal(30 + ConstrainedLayoutEngine.PadBottom, new EdgeBands(0, 0, 0, Bottom: 12, BottomTitle: 30).BottomNeed(ConstrainedLayoutEngine.PadBottom));
    }

    [Fact]
    public void TheBottomNeed_IsTheTickLabelBand_WithoutATitle_AndThePad()
    {
        Assert.Equal(12 + ConstrainedLayoutEngine.PadBottom, new EdgeBands(0, 0, 0, Bottom: 12).BottomNeed(ConstrainedLayoutEngine.PadBottom));
    }

    /// <summary>The pad holds what is drawn off the edge: nothing drawn below the bottom spine needs nothing there, not even the pad.</summary>
    [Fact]
    public void TheBottomNeed_OfBandsThatDrawNothingBelow_IsNothing_NotThePad()
    {
        Assert.Equal(0, new EdgeBands(Top: 4, Right: 5, Left: 6, Bottom: 0).BottomNeed(15));
    }

    [Fact]
    public void TheBottomExtent_IsTheTitlesFarSide_ElseTheTickLabelBand_ElseNothing()
    {
        Assert.Equal(30, new EdgeBands(0, 0, 0, Bottom: 12, BottomTitle: 30).BottomExtent);
        Assert.Equal(12, new EdgeBands(0, 0, 0, Bottom: 12).BottomExtent);
        Assert.Equal(0, new EdgeBands(0, 0, 0, Bottom: 0).BottomExtent);
    }
}
