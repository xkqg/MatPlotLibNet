// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Serialization;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>Each look says how its value meets the value a caller gave and the one its owner rests at: the given one wins, else the look's,
/// else the resting one.</summary>
public class LookOverTests
{
    [Fact]
    public void TheTickLook_GivenWins_ElseItsOwn_ElseTheRest()
    {
        var none = AxisTickLook.Library with { Direction = null, Length = null, Width = null, Pad = null, MinorWidth = null, MinorPad = null };
        var look = none with { Direction = TickDirection.In, Length = 7, Width = 2, Pad = 3, MinorWidth = 1, MinorPad = 2 };

        Assert.True(none.NamesNothingFor(major: true));
        Assert.True(none.NamesNothingFor(major: false));
        Assert.False(look.NamesNothingFor(major: true));
        Assert.False((none with { MinorPad = 1 }).NamesNothingFor(major: false));
        Assert.Equal(TickDirection.InOut, look.DirectionOver(TickDirection.InOut, TickDirection.Out));
        Assert.Equal(TickDirection.In, look.DirectionOver(null, TickDirection.Out));
        Assert.Equal(TickDirection.Out, none.DirectionOver(null, TickDirection.Out));
        Assert.Equal(9, look.LengthOver(9, true, 4));
        Assert.Equal(7, look.LengthOver(null, true, 4));
        Assert.Equal(4, look.LengthOver(null, false, 4));
        Assert.Equal(2, look.WidthOver(null, true, 0.5));
        Assert.Equal(1, look.WidthOver(null, false, 0.5));
        Assert.Equal(0.5, none.WidthOver(null, false, 0.5));
        Assert.Equal(3, look.PadOver(null, true, 4));
        Assert.Equal(2, look.PadOver(null, false, 4));
        Assert.Equal(3, (look with { MinorPad = null }).PadOver(null, false, 4));
        Assert.Equal(4, none.PadOver(null, false, 4));
        Assert.Equal(6, none.PadOver(6, false, 4));
    }

    [Fact]
    public void TheDotLook_GivenWins_ElseItsOwn_ElseTheRest()
    {
        var look = DotLook.Library with { CapSize = 0.3, Jitter = 0.2, JoinsPoints = true };
        var none = DotLook.Library with { CapSize = null, Jitter = null, JoinsPoints = false };

        Assert.Equal(0.5, look.CapSizeOver(0.5, 0));
        Assert.Equal(0.3, look.CapSizeOver(null, 0));
        Assert.Equal(0, none.CapSizeOver(null, 0));
        Assert.True(look.JoinsPointsOver(null));
        Assert.False(look.JoinsPointsOver(false));
        Assert.Equal(0.2, look.JitterOver(null, 0.1));
        Assert.Equal(0.1, none.JitterOver(null, 0.1));
        Assert.Equal(0.4, none.JitterOver(0.4, 0.1));
    }

    [Fact]
    public void TheFrameAndTextLooks_GivenWins_ElseTheirOwn_ElseTheRest()
    {
        var frame = AxesFrameLook.Library with { SpineWidth = 1.2, AxisZOrder = 0.5 };
        var bare = AxesFrameLook.Library with { SpineWidth = null, AxisZOrder = null };
        var text = TextLook.Library with { LabelPad = 5, TitlePad = 7, TitleWeight = FontWeight.Normal };
        var plain = TextLook.Library with { LabelPad = null, TitlePad = null, TitleWeight = null };

        Assert.Equal(2, frame.SpineWidthOver(2, 0.8));
        Assert.Equal(1.2, frame.SpineWidthOver(null, 0.8));
        Assert.Equal(0.8, bare.SpineWidthOver(null, 0.8));
        Assert.Equal(0.5, frame.AxisZOrderOr(double.NegativeInfinity));
        Assert.Equal(double.NegativeInfinity, bare.AxisZOrderOr(double.NegativeInfinity));
        Assert.Equal(9, text.LabelPadOver(9, 4));
        Assert.Equal(5, text.LabelPadOver(null, 4));
        Assert.Equal(4, plain.LabelPadOver(null, 4));
        Assert.Equal(7, text.TitlePadOr(6));
        Assert.Equal(6, plain.TitlePadOr(6));
        Assert.Equal(FontWeight.Normal, text.DrawnTitleWeight);
        Assert.Equal(FontWeight.Bold, plain.DrawnTitleWeight);
    }

    [Fact]
    public void TheCubeRangeAndTickLabelLooks_TheirOwn_ElseTheRest()
    {
        var wall = new GridStyle { Visible = false };
        Assert.Same(wall, (CubeLook.Library with { WallGrid = wall }).WallGridOr(GridStyle.AtRest));
        Assert.Same(GridStyle.AtRest, (CubeLook.Library with { WallGrid = null }).WallGridOr(GridStyle.AtRest));

        var range = AxisRangeLook.Library with { ScatterMargin = 0.05, SymLogLinThresh = 2 };
        var open = AxisRangeLook.Library with { ScatterMargin = null, SymLogLinThresh = null };
        Assert.Equal(0.05, range.ScatterMarginOr(0));
        Assert.Equal(0, open.ScatterMarginOr(0));
        Assert.Equal(3, range.SymLogLinThreshOver(3, 1));
        Assert.Equal(2, range.SymLogLinThreshOver(null, 1));
        Assert.Equal(1, open.SymLogLinThreshOver(null, 1));

        var thresholds = new LogLabelThresholds(2, 0.5);
        Assert.Equal(thresholds, (TickLabelLook.Library with { LogThresholds = thresholds }).DrawnLogThresholds);
        Assert.Equal(LogLabelThresholds.Default, (TickLabelLook.Library with { LogThresholds = null }).DrawnLogThresholds);
    }

    [Fact]
    public void TheLegendPatchAndPolarLooks_GivenWins_ElseTheirOwn()
    {
        var legend = LegendLook.Library;
        Assert.Equal(0.3, legend.FrameAlphaOver(0.3));
        Assert.Equal(legend.FrameAlpha, legend.FrameAlphaOver(null));
        Assert.Equal(Colors.Red, legend.EdgeColorOver(Colors.Red));
        Assert.Equal(legend.EdgeColor, legend.EdgeColorOver(null));
        Assert.Equal(!legend.FancyBox, legend.FancyBoxOver(!legend.FancyBox));
        Assert.Equal(legend.FancyBox, legend.FancyBoxOver(null));
        Assert.Equal(4, legend.NumPointsOver(4));
        Assert.Equal(legend.NumPoints, legend.NumPointsOver(null));
        Assert.Equal(5, legend.ScatterPointsOver(5));
        Assert.Equal(legend.ScatterPoints, legend.ScatterPointsOver(null));

        Assert.True((PatchLook.Library with { LikeCollections = false }).ContourLinesOver(null));
        Assert.False((PatchLook.Library with { LikeCollections = true }).ContourLinesOver(null));
        Assert.True((PatchLook.Library with { LikeCollections = true }).ContourLinesOver(true));

        Assert.Equal(0.4, PolarLook.Library.BarWidthOver(0.4));
        Assert.Equal(PolarLook.Library.BarWidth, PolarLook.Library.BarWidthOver(null));
    }
}
