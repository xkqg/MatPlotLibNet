// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one rule that places an axis title beside the tick labels of its edge, on all four edges, and the room it takes
/// (<see cref="TitleClearance"/>): made for an edge by <see cref="TitleClearance.Beside"/>, which reads the edge's terms.</summary>
public class TitleClearanceTests
{
    /// <summary>Beside an edge, a clearance takes the ink of a title that faces that edge's labels and the place a title stands there
    /// without labels: 35 px below a bottom edge, 45 px beside a left or a right one, none above a top edge.</summary>
    [Theory]
    [InlineData("Bottom", 35.0)]
    [InlineData("Left", 45.0)]
    [InlineData("Right", 45.0)]
    [InlineData("Top", null)]
    public void BesideAnEdge_TheClearanceTakesItsInkAndItsPlaceWithoutLabels(string name, double? withoutLabels)
    {
        var edge = EdgeTerms.Of(Enum.Parse<TickSide>(name));

        var clearance = TitleClearance.Beside(edge, 27, 4, 10);

        Assert.Equal(new TitleClearance(27, 4, 10, edge.Ink), clearance with { WithoutTickLabels = null });
        Assert.Equal(withoutLabels, clearance.WithoutTickLabels);
        Assert.Equal(withoutLabels, edge.TitleWithoutTickLabels);
    }

    /// <summary>Past a band, a title's room is the band, the gap, its pad and its height, less the part of its box that dips past its
    /// placing line into the gap; at the place without labels it is that place less the ink that faces the labels, then its pad and its
    /// height: on every edge the room reaches the far side of the title drawn at its offset (its box's edge facing the labels stands at its
    /// offset less the facing ink and the dip). Above a top edge the title stands on its baseline, as matplotlib's top X label does
    /// (<c>va='baseline'</c>), so its descent (0.2 of its size, 2 px here) dips into the gap.</summary>
    [Theory]
    [InlineData("Bottom", 27.0, 27 + 6 + 3 + 12)]
    [InlineData("Bottom", 0.0, 35 - 8 + 3 + 12)]
    [InlineData("Top", 27.0, 27 + 6 + 3 + 12 - 2)]
    [InlineData("Top", 0.0, 0 + 6 + 3 + 12 - 2)]
    [InlineData("Left", 0.0, 45 - 0 + 3 + 12)]
    public void TheRoomOfATitle_ReachesItsFarSide_PastTheBandOrAtThePlaceWithoutLabels(string name, double band, double room)
    {
        var clearance = TitleClearance.Beside(EdgeTerms.Of(Enum.Parse<TickSide>(name)), band, 6, 10);

        Assert.Equal(room, clearance.TitleExtent(12, 3), 9);
        Assert.Equal(clearance.TitleOffset() - (clearance.Ink.Facing + clearance.Ink.Dip) * 10 + 3 + 12, clearance.TitleExtent(12, 3), 9);
    }

    /// <summary>matplotlib sets the top X label on its baseline (<c>lib/matplotlib/axis.py:2600</c>, <c>{'top': 'baseline'}</c>) at
    /// <c>labelpad</c> above the box of the top tick labels (<c>axis.py:2629</c>, <c>bbox.y1 + labelpad</c>): the baseline stands the gap
    /// past the band, not the gap and the descent.</summary>
    [Fact]
    public void AboveATopEdge_TheTitlesBaseline_StandsTheGapPastTheBand()
    {
        var clearance = TitleClearance.Beside(EdgeTerms.Top, 27, 6, 10);

        Assert.Equal(27 + 6, clearance.TitleOffset(), 9);
    }

    /// <summary>The gap a horizontal title keeps from the tick labels under a theme that names none is 6 px.</summary>
    [Fact]
    public void TheGapOfAHorizontalTitle_UnderTheLibrarysThemes_IsSixPixels()
    {
        Assert.Equal(6, TitleClearance.XLabelGap);
        Assert.Equal(TitleClearance.XLabelGap, Theme.Default.XLabelGapUnder);
    }
}
