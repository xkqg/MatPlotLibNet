// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A broken axis under a theme whose subplots stand a share of a row apart (the matplotlib themes: <c>figure.subplot.hspace</c>
/// and <c>wspace</c> 0.2) is drawn as matplotlib's broken-axis recipe draws it (gallery <c>broken_axis</c>, read with python, matplotlib
/// 3.11.2): the parts are axes of their own, stacked as a GridSpec stacks rows, so the room between two parts is <c>share * L / (n + share
/// * (n - 1))</c> of the plot's length L for n parts, and each part keeps the length its range takes, one data unit the same length on
/// either side (printed for limits 85..125 over 0..25, height_ratios [40, 25], 462 px: gap 42.0, parts 258.46 and 161.54); each part has
/// its own ticks (printed: 85, 90 ... 125 and 0, 5 ... 25 under both styles), its own spines, which stop where it stops, and nothing of the
/// axes is drawn in the room, the figure's ground showing; the recipe's mark is a diagonal at each corner of the cut,
/// <c>d = .015</c> of the axes each way, in the axes' edge colour and <c>lines.linewidth</c>. The library's own themes, which stand no
/// subplots a share apart, keep a break as one axis with the stretch cut to nothing.</summary>
public sealed class BrokenAxisPartsTests
{
    /// <summary>The default style's plot of an 800 x 600 figure: 0.125, 0.1, 0.12 and 0.11 of it are margins.</summary>
    private static readonly Rect Plot = new(100, 72, 620, 462);

    /// <summary>The check's data, 0 to 18 and 100 to 118, shown from 0 to 125 with 25 to 85 cut out.</summary>
    private static Axes Broken(BreakStyle style = BreakStyle.Straight)
    {
        var axes = new Axes();
        double[] x = [.. Enumerable.Range(0, 20).Select(i => (double)i)];
        axes.Plot(x, [.. x.Select(v => v < 10 ? v * 2 : v * 2 + 80)]);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 125;
        axes.AddYBreak(25, 85, style);
        return axes;
    }

    private static (RecordingRenderContext Ctx, DataTransform Transform) Render(Axes axes, Theme theme)
    {
        var ctx = new RecordingRenderContext();
        var renderer = new CartesianAxesRenderer(axes, Plot, ctx, theme);
        renderer.Render();
        return (ctx, renderer.DrawnTransform!);
    }

    private static double Y(DataTransform transform, double value) => transform.DataToPixel(0, value).Y;

    // ── The map ──────────────────────────────────────────────────────────────

    [Fact]
    public void AMap_GivenAShare_LeavesRoomBetweenTheParts_AShareOfTheirMeanSpan()
    {
        var map = new AxisBreakMap([new AxisBreak(25, 85)], 0, 125, null, spacing: 0.2);

        // The parts span 25 and 40; the room is 0.2 of their mean, 6.5.
        Assert.Equal(25, map.Remap(25), 12);
        Assert.Equal(31.5, map.Remap(85), 12);
        Assert.Equal(71.5, map.Remap(125), 12);
        Assert.Equal(new MinMaxRange(0, 71.5), map.CompressedRange);
        Assert.Equal([new Range1D(25, 85)], map.Rooms.ToArray());
        Assert.Equal([new Range1D(0, 25), new Range1D(85, 125)], map.Parts.ToArray());
        Assert.True(map.IsDrawnInParts);
    }

    [Fact]
    public void AMap_OfThreeParts_LeavesTheSameRoomBetweenEachTwo()
    {
        var map = new AxisBreakMap([new AxisBreak(60, 70), new AxisBreak(20, 30)], 0, 100, null, spacing: 0.2);

        double room = 0.2 * 80 / 3;
        Assert.Equal(room, map.Remap(30) - map.Remap(20), 12);
        Assert.Equal(room, map.Remap(70) - map.Remap(60), 12);
        Assert.Equal(80 + 2 * room, map.CompressedRange.Max - map.CompressedRange.Min, 12);
    }

    [Fact]
    public void AMap_WithoutAShare_OrWithABreakPastAnEnd_LeavesNoRoom()
    {
        var plain = new AxisBreakMap([new AxisBreak(25, 85)], 0, 125);
        var pastTheEnd = new AxisBreakMap([new AxisBreak(25, 85)], 0, 30, null, spacing: 0.2);

        Assert.Equal(plain.Remap(25), plain.Remap(85), 12);
        // An axis with no room between parts has no rooms and no parts: the one rule of being drawn in parts.
        Assert.All([plain, pastTheEnd], map => Assert.True(!map.IsDrawnInParts && map.Rooms.IsEmpty && map.Parts.IsEmpty));
        Assert.Equal(new MinMaxRange(0, 25), pastTheEnd.CompressedRange);
    }

    [Fact]
    public void AMap_WithRoom_ReadsEveryPlacedValueBack()
    {
        var map = new AxisBreakMap([new AxisBreak(25, 85)], 0, 125, null, spacing: 0.2);

        foreach (double value in new[] { 0.0, 10, 25, 85, 100, 125 })
            Assert.Equal(value, map.Inverse(map.Remap(value)), 9);
    }

    [Fact]
    public void AMap_ReadsAPlaceInTheSpaceOfTheParts_AndBack()
    {
        var map = new AxisBreakMap([new AxisBreak(25, 85)], 0, 125, null, spacing: 0.2);

        Assert.Equal(6.5, map.Room, 12);
        Assert.Equal(25, map.WithoutRooms(25), 12);
        Assert.Equal(25, map.WithoutRooms(28), 12);     // inside the room: where the break is cut
        Assert.Equal(25, map.WithoutRooms(31.5), 12);
        Assert.Equal(33.5, map.WithoutRooms(40), 12);
        Assert.Equal(10, map.InverseWithoutRooms(10), 12);
        Assert.Equal(93.5, map.InverseWithoutRooms(33.5), 12);
        Assert.Equal(map.Inverse(40), map.InverseWithoutRooms(map.WithoutRooms(40)), 12);
    }

    [Fact]
    public void AMapWithNoRoom_ReadsTheDrawnSpaceAsItIs()
    {
        var map = new AxisBreakMap([new AxisBreak(25, 85)], 0, 125);

        Assert.Equal(0, map.Room);
        Assert.Equal(40, map.WithoutRooms(40), 12);
        Assert.Equal(map.Inverse(40), map.InverseWithoutRooms(40), 12);
    }

    [Theory]
    [InlineData(0.5, 100.0)]
    [InlineData(2.0, 10.0)]
    public void UnderTheMatplotlibThemes_AWheelZoom_KeepsTheValueUnderThePointerUnderIt(double scale, double value)
    {
        var axes = Broken();
        var (_, transform) = Render(axes, Theme.MatplotlibV2);
        var pointer = new Point(Plot.X + 200, Y(transform, value));

        var zoomed = transform.ZoomedAbout(pointer, scale);
        axes.XAxis.Min = zoomed.XMin;
        axes.XAxis.Max = zoomed.XMax;
        axes.YAxis.Min = zoomed.YMin;
        axes.YAxis.Max = zoomed.YMax;
        var (_, after) = Render(axes, Theme.MatplotlibV2);

        Assert.Equal(pointer.Y, Y(after, value), 6);
        Assert.Equal(transform.PixelToData(pointer).X, after.PixelToData(pointer).X, 9);
        Assert.Equal(42.0, Y(after, 25) - Y(after, 85), 6);   // the break stays in view, and its room keeps the GridSpec gap
    }

    // ── The theme ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_ThePartsStandTheGridSpecGapApart_EachAtTheLengthItsRangeTakes(string themeId)
    {
        var (_, transform) = Render(Broken(), themeId.AsTheme());

        Assert.Equal(42.0, Y(transform, 25) - Y(transform, 85), 9);
        Assert.Equal(258.46153846153845, Y(transform, 85) - Y(transform, 125), 9);
        Assert.Equal(161.53846153846155, Y(transform, 0) - Y(transform, 25), 9);
    }

    [Fact]
    public void UnderTheLibrarysTheme_TheStretchIsCutToNothing()
    {
        var (_, transform) = Render(Broken(), Theme.Default);

        Assert.Equal(Y(transform, 25), Y(transform, 85), 9);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_EachPartTicksItsOwnRange(string themeId)
    {
        var theme = themeId.AsTheme();
        var axes = Broken();
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), theme);
        var range = placement.ComputeDataRanges();

        var ticks = placement.PlacePrimary(range, placement.PrimaryView(range)).Ticks;

        Assert.Equal([0.0, 5, 10, 15, 20, 25, 85, 90, 95, 100, 105, 110, 115, 120, 125], ticks);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_TheSpinesAlongTheBrokenAxis_StopWhereEachPartStops(string themeId)
    {
        var (ctx, transform) = Render(Broken(), themeId.AsTheme());

        var vertical = SpineLines(ctx).Where(l => l.From.X == l.To.X).ToList();
        Assert.Equal(4, vertical.Count);   // the left and the right spine, each in two
        foreach (double x in new[] { Plot.X, Plot.Right })
        {
            var pieces = vertical.Where(l => l.From.X == x).Select(l => (Top: Math.Min(l.From.Y, l.To.Y), Bottom: Math.Max(l.From.Y, l.To.Y)))
                .OrderBy(p => p.Top).ToList();
            Assert.Equal(Plot.Y, pieces[0].Top, 9);
            Assert.Equal(Y(transform, 85), pieces[0].Bottom, 9);
            Assert.Equal(Y(transform, 25), pieces[1].Top, 9);
            Assert.Equal(Plot.Bottom, pieces[1].Bottom, 9);
        }
    }

    [Fact]
    public void UnderTheLibrarysTheme_EachSpineIsOneLine()
    {
        var (ctx, _) = Render(Broken(), Theme.Default);

        Assert.Equal(4, SpineLines(ctx).Count);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_TheRoomBetweenTheParts_IsTheFiguresGround(string themeId)
    {
        var theme = themeId.AsTheme();
        var (ctx, transform) = Render(Broken(), theme);

        var room = new Rect(Plot.X, Y(transform, 85), Plot.Width, Y(transform, 25) - Y(transform, 85));
        Assert.Contains(ctx.Of<DrawRectangleCall>(),
            a => a.Fill == theme.Background && (a.Rect).IsCloseTo(room, 1e-6));
    }

    [Theory]
    [InlineData("classic", 1.0)]
    [InlineData("v2", 1.5)]
    public void UnderTheMatplotlibThemes_AStraightBreak_DrawsTheRecipesDiagonalAtEachCornerOfTheCut(string themeId, double linePoints)
    {
        var theme = themeId.AsTheme();
        var (ctx, transform) = Render(Broken(BreakStyle.Straight), theme);

        double width = linePoints * 100 / 72;   // lines.linewidth at 100 dpi
        var marks = Marks(ctx, width);
        Assert.Equal(4, marks.Count);
        Assert.All(marks, m => Assert.Equal(Colors.Black, m.Color));

        // d = .015 of the axes: across, of the plot's width; along, of the part's own height. Each a "/" (rising to the right).
        double dx = 0.015 * Plot.Width;
        double topPart = Y(transform, 85) - Plot.Y, bottomPart = Plot.Bottom - Y(transform, 25);
        foreach (double x in new[] { Plot.X, Plot.Right })
        {
            Assert.Contains(marks, m => m.Low.IsCloseTo(new Point(x - dx, Y(transform, 85) + 0.015 * topPart), 1e-6)
                && m.High.IsCloseTo(new Point(x + dx, Y(transform, 85) - 0.015 * topPart), 1e-6));
            Assert.Contains(marks, m => m.Low.IsCloseTo(new Point(x - dx, Y(transform, 25) + 0.015 * bottomPart), 1e-6)
                && m.High.IsCloseTo(new Point(x + dx, Y(transform, 25) - 0.015 * bottomPart), 1e-6));
        }
    }

    [Fact]
    public void UnderTheMatplotlibThemes_AZigzagBreak_DrawsAZigzagOfThreeStrokesAtEachCorner()
    {
        var (ctx, _) = Render(Broken(BreakStyle.Zigzag), Theme.MatplotlibV2);

        Assert.Equal(12, Marks(ctx, 1.5 * 100 / 72).Count);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_ABreakStyledNone_LeavesTheRoom_AndDrawsNoMark()
    {
        var theme = Theme.MatplotlibV2;
        var (ctx, transform) = Render(Broken(BreakStyle.None), theme);

        Assert.Empty(Marks(ctx, 1.5 * 100 / 72));
        Assert.Equal(42.0, Y(transform, 25) - Y(transform, 85), 9);
    }

    [Fact]
    public void AnXBreak_UnderTheMatplotlibThemes_StandsItsPartsAColumnsGapApart()
    {
        var axes = new Axes();
        axes.Plot([0.0, 100], [0.0, 1]);
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 100;
        axes.AddXBreak(40, 60);
        var (_, transform) = Render(axes, Theme.MatplotlibV2);

        // Two columns of 40 sharing 620 px with wspace 0.2: a gap of 0.2 * 620 / 2.2.
        Assert.Equal(0.2 * 620 / 2.2, transform.DataToPixel(60, 0).X - transform.DataToPixel(40, 0).X, 9);
    }

    /// <summary>The lines drawn inside a spine's group.</summary>
    private static List<(Point From, Point To)> SpineLines(RecordingRenderContext ctx)
    {
        var lines = new List<(Point, Point)>();
        bool inSpine = false;
        foreach (var call in ctx.Calls)
        {
            if (call is BeginGroupCall group) inSpine = group.CssClass == "spine";
            else if (call is EndGroupCall) inSpine = false;
            else if (inSpine && call is DrawLineCall line) lines.Add((line.P1, line.P2));
        }

        return lines;
    }

    /// <summary>The strokes of the break's marks: single lines outside a spine's group at the width a mark is drawn with, each from its
    /// left end.</summary>
    private static List<(Point Low, Point High, Color Color)> Marks(RecordingRenderContext ctx, double width)
    {
        var marks = new List<(Point, Point, Color)>();
        bool inSpine = false;
        foreach (var call in ctx.Calls)
        {
            if (call is BeginGroupCall group) inSpine = group.CssClass == "spine";
            else if (call is EndGroupCall) inSpine = false;
            else if (!inSpine && call is DrawLineCall line && Math.Abs(line.Thickness - width) < 1e-9)
            {
                var (p1, p2) = (line.P1, line.P2);
                marks.Add(p1.X <= p2.X ? (p1, p2, line.Color) : (p2, p1, line.Color));
            }
        }

        return marks;
    }
}
