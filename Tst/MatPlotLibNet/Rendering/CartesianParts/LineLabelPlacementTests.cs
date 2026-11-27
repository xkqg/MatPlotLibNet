// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Where the label of a drawn line stands and the font it is drawn in (<see cref="LineLabelPlacement"/>): the tick font in the
/// line's colour, 2 px off the line, above the right end of a horizontal line, beside the top of a vertical one, inside the top of a
/// band, or at the end of a trendline that is drawn. The values are the ones each part drew with before.</summary>
public class LineLabelPlacementTests
{
    [Fact]
    public void AboveTheEndOfAHorizontalLine_TheLabelEndsAtTheEnd_TwoPixelsUp_OrInsetFromIt()
    {
        var across = new LineSegment(new Point(10, 50), new Point(310, 50));

        Assert.Equal(new TextPlacement(new Point(310, 48), TextAlignment.Right), LineLabelPlacement.AboveEnd(across));
        Assert.Equal(new TextPlacement(new Point(308, 48), TextAlignment.Right), LineLabelPlacement.AboveEnd(across, LineLabelPlacement.Gap));
    }

    /// <summary>A label beside the top of a vertical line hangs from the plot's top (its box's ascent below it, the box's rule), starting
    /// two pixels right of the line.</summary>
    [Fact]
    public void BesideTheTopOfAVerticalLine_TheLabelStartsTwoPixelsRight_HungFromThePlotsTop()
    {
        Assert.Equal(new TextPlacement(new Point(102, 20), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top }, LineLabelPlacement.BesideTop(100, 20));
    }

    [Fact]
    public void InsideTheTopOfABand_TheLabelHangsTwoPixelsInside_AtTheLeftOrCentred()
    {
        Assert.Equal(new TextPlacement(new Point(12, 22), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top }, LineLabelPlacement.InsideTopLeft(10, 20));
        Assert.Equal(new TextPlacement(new Point(150, 22), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top }, LineLabelPlacement.InsideTopCentre(150, 20));
    }

    [Fact]
    public void AtTheEndThatIsDrawn_TheLabelStartsThere()
    {
        Assert.Equal(new TextPlacement(new Point(7, 9), TextAlignment.Left), LineLabelPlacement.AtEnd(new Point(7, 9)));
    }

    /// <summary>A line's label is drawn in the theme's tick font in the colour of its line, under every theme: the font is the theme's
    /// one role for it, which no caller chooses.</summary>
    [Theory]
    [MemberData(nameof(ThemeCases.DefaultDarkAndMatplotlib), MemberType = typeof(ThemeCases))]
    public void ALineLabel_IsDrawnInTheTickFont_InTheColourOfItsLine(string name)
    {
        var theme = name.AsTheme();
        var ctx = new RecordingRenderContext();

        ctx.DrawLineLabel("level", new TextPlacement(new Point(1, 2), TextAlignment.Right), theme, Colors.Red);

        var call = ctx.Of<DrawTextCall>().Single();
        Assert.Equal("level", call.Text);
        Assert.Equal(new Point(1, 2), call.Position);
        Assert.Equal(theme.TickFont() with { Color = Colors.Red }, call.Font);
        Assert.Equal(theme.LineLabelFont(Colors.Red), call.Font);
        Assert.Equal(TextAlignment.Right, call.Alignment);
    }
}
