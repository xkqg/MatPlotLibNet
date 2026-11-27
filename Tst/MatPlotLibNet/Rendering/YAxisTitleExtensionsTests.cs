// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one band rule of a Y axis slot (<see cref="YAxisTitleExtensions"/>), tested at its own granularity: where
/// the title stands past the tick label band and how far the slot reaches, on both sides, with and without a title, a pad
/// or drawn tick labels. A 10 px title, a 30 px tick label band and a 4 px gap throughout.</summary>
public class YAxisTitleExtensionsTests
{
    private static readonly Font LabelFont = new() { Size = 10 };

    private const string Title = "Rate";

    private static Axis Titled(TextStyle? style = null) => new() { Label = Title, LabelStyle = style };

    /// <summary>The input of a Y title's band is made in one place for the drawing and the layout: the band given, the side, the
    /// container's gap under the theme (its own when it was given one, the theme's label pad else) and the theme's label font.</summary>
    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    public void TheInputOfAYTitlesBand_IsTheBandTheSideTheContainersGapAndTheThemesLabelFont(string side)
    {
        var container = new Axes().YAxes;
        var axisSide = Enum.Parse<AxisSide>(side);

        Assert.Equal(new YAxisTitleExtensions.YAxisTitleInput(27, axisSide, container.TitleGapUnder(Theme.MatplotlibV2), Theme.MatplotlibV2.LabelFont()),
            container.TitleInputUnder(Theme.MatplotlibV2, axisSide, 27));
        container.TitleGap = 9;
        Assert.Equal(9, container.TitleInputUnder(Theme.Default, axisSide, 27).TitleGap);
    }

    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    public void YTitleExtent_WithoutATitle_IsTheTickLabelBandAlone(string side)
    {
        var extent = new Axis().YTitleExtent(null, new YAxisTitleExtensions.YAxisTitleInput(30, Enum.Parse<AxisSide>(side), 4, LabelFont));

        Assert.Equal(new YAxisTitleExtent(0, 30), extent);
    }

    /// <summary>Left of a left edge the title's baseline faces the labels: its anchor stands 30 + 4 + 0 = 34 px out, and the
    /// slot reaches its ascent past that, 34 + 8 = 42.</summary>
    [Fact]
    public void YTitleExtent_Left_PutsTheBaselineTheGapPastTheBand_AndReachesTheAscentPastTheAnchor()
    {
        var extent = Titled().YTitleExtent(Title, new YAxisTitleExtensions.YAxisTitleInput(30, AxisSide.Left, 4, LabelFont));

        Assert.Equal(34, extent.TitleOffset, 9);
        Assert.Equal(42, extent.Band, 9);
    }

    /// <summary>Right of a right edge the body of the title's text (0.6 of its size) faces the labels: its anchor stands
    /// 30 + 4 + 6 = 40 px out, and the slot reaches its descent past that, 40 + 2 = 42, the same band as on the left.</summary>
    [Fact]
    public void YTitleExtent_Right_PutsTheTextBodyTheGapPastTheBand_AndReachesTheDescentPastTheAnchor()
    {
        var extent = Titled().YTitleExtent(Title, new YAxisTitleExtensions.YAxisTitleInput(30, AxisSide.Right, 4, LabelFont));

        Assert.Equal(40, extent.TitleOffset, 9);
        Assert.Equal(42, extent.Band, 9);
    }

    /// <summary>The title's own pad moves it out on top of the gap, and the slot reaches that much further; the title is
    /// measured in its own style's size (here 20 px: ascent 16, descent 4, text body 12), and the band is the same on both
    /// sides.</summary>
    [Theory]
    [InlineData("Left", 30 + 4 + 0 + 3, 30 + 4 + 3 + 16)]
    [InlineData("Right", 30 + 4 + 12 + 3, 30 + 4 + 3 + 16)]
    public void YTitleExtent_ThePadAndTheStylesSize_MoveTheTitleAndTheBand(string side, double offset, double band)
    {
        var extent = Titled(new TextStyle { Pad = 3, FontSize = 20 })
            .YTitleExtent(Title, new YAxisTitleExtensions.YAxisTitleInput(30, Enum.Parse<AxisSide>(side), 4, LabelFont));

        Assert.Equal(offset, extent.TitleOffset, 9);
        Assert.Equal(band, extent.Band, 9);
    }

    /// <summary>With no tick label drawn the title keeps the fixed 45 px it has always stood at (plus its pad), and the slot
    /// reaches its outward ink past it.</summary>
    [Theory]
    [InlineData("Left", 45 + 2 + 8)]
    [InlineData("Right", 45 + 2 + 2)]
    public void YTitleExtent_WithNoTickLabelDrawn_KeepsTheFixedOffset(string side, double band)
    {
        var extent = Titled(new TextStyle { Pad = 2 }).YTitleExtent(Title, new YAxisTitleExtensions.YAxisTitleInput(0, Enum.Parse<AxisSide>(side), 4, LabelFont));

        Assert.Equal(45 + 2, extent.TitleOffset, 9);
        Assert.Equal(band, extent.Band, 9);
    }
}
