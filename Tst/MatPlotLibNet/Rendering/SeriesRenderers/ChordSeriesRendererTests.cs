// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A chord diagram is drawn as a ring of arcs with the ribbons inside it: each arc a closed path on the ring, each
/// ribbon a closed path that leaves the ring, bends through the centre and arrives at the other group, filled in the colour of
/// the group that sends more and a little transparent so the ones behind show. The names stand outside the ring, turned to read
/// outward and never upside down.</summary>
public class ChordSeriesRendererTests
{
    private static readonly Rect Box = new(100, 50, 400, 400);
    private static readonly Point Centre = new(Box.X + Box.Width / 2, Box.Y + Box.Height / 2);

    private static readonly double[,] Flows =
    {
        { 0, 5, 2 },
        { 3, 0, 0 },
        { 0, 4, 6 },
    };

    private static Axes ChordAxes(out ChordSeries series, string[]? names = null)
    {
        var axes = new Axes();
        series = axes.Chord(names ?? ["North", "South", "East"], Flows);
        return axes;
    }

    private static RecordingRenderContext Draw(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        AxesRenderer.Create(axes, Box, ctx, Theme.Default).Render();
        return ctx;
    }

    private static PathSegment[][] Paths(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawPath").Select(c => (PathSegment[])((dynamic)c.Args!).segments)];

    private static Color?[] Fills(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawPath").Select(c => (Color?)((dynamic)c.Args!).fill)];

    private static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    [Fact]
    public void ThereIsOnePathForEachArc_ThenOneForEachRibbon_TheLargestFirst()
    {
        var ctx = Draw(ChordAxes(out _));

        // Arcs: 3. Ribbons: (0,1) of 5 + 3, (0,2) of 2, (2,1) of 4, (2,2) of 6: four, largest first by size: 8, 6, 4, 2.
        var fills = Fills(ctx);
        Assert.Equal(3 + 4, fills.Length);
        Assert.Equal(3, fills.Take(3).Distinct().Count());   // the three groups wear three colours
    }

    [Fact]
    public void TheRibbons_WearTheColourOfTheirSource_ABitTransparent()
    {
        var ctx = Draw(ChordAxes(out var series));
        var fills = Fills(ctx);
        var groupColours = fills.Take(3).Select(c => c!.Value).ToArray();

        // The largest ribbon is the pair 0-1 (8), sent mostly by group 0: its colour is group 0's, at the opacity of the series.
        var ribbon = fills[3]!.Value;
        Assert.Equal(groupColours[0].WithAlpha(0), ribbon.WithAlpha(0));
        Assert.Equal((byte)Math.Round(series.RibbonAlpha * 255), ribbon.A);
        // The arcs are solid.
        Assert.All(groupColours, c => Assert.Equal(255, c.A));
    }

    [Fact]
    public void AnArcStartsAtTwelveOClock_OnTheRim_AndTheRingIsCentredInThePlot()
    {
        var ctx = Draw(ChordAxes(out _));

        var first = Paths(ctx)[0];
        var start = Assert.IsType<MoveToSegment>(first[0]).Point;
        var arc = Assert.IsType<ArcSegment>(first[1]);

        Assert.Equal(Centre.X, start.X, 6);          // straight up from the centre
        Assert.True(start.Y < Centre.Y);
        double rim = Centre.Y - start.Y;
        Assert.True(rim > 0.7 * Box.Width / 2 && rim < Box.Width / 2);   // the names have room outside it
        Assert.Equal(Centre, arc.Center);
        Assert.Equal(rim, arc.RadiusX, 6);
        Assert.Equal(-90.0, arc.StartAngle, 6);      // 12 o'clock is -90 degrees on a screen whose Y points down
    }

    [Fact]
    public void ARibbon_LeavesTheRing_BendsThroughTheCentre_AndArrivesAtTheOtherGroup()
    {
        var ctx = Draw(ChordAxes(out _));

        var ribbon = Paths(ctx)[3];   // the largest: groups 0 and 1

        // Two sides, each a cubic that is a quadratic through the centre raised exactly: the control points lie two thirds of
        // the way from the end of the side toward the centre.
        var curves = ribbon.OfType<BezierSegment>().ToArray();
        Assert.Equal(2, curves.Length);
        var moveTo = Assert.IsType<MoveToSegment>(ribbon[0]).Point;
        Assert.Equal(moveTo.X + 2.0 / 3 * (Centre.X - moveTo.X), curves[1].Control2.X, 6);
        Assert.Equal(moveTo.Y + 2.0 / 3 * (Centre.Y - moveTo.Y), curves[1].Control2.Y, 6);
        Assert.IsType<CloseSegment>(ribbon[^1]);
    }

    [Fact]
    public void ASelfLoop_IsOneCurveBackToItsOwnStart()
    {
        var ctx = Draw(ChordAxes(out _));

        // The ribbons by size: 8 (groups 0 and 1), 6 (the loop of group 2), 4 (2 to 1), 2 (0 to 2). The loop is the second, so
        // the fifth path after the three arcs' paths.
        var loop = Paths(ctx)[4];

        var start = Assert.IsType<MoveToSegment>(loop[0]).Point;
        Assert.Single(loop.OfType<ArcSegment>());       // its end on the ring
        var back = Assert.Single(loop.OfType<BezierSegment>());   // the one curve through the centre, back to the start
        Assert.Equal(start.X, back.End.X, 6);
        Assert.Equal(start.Y, back.End.Y, 6);
        Assert.IsType<CloseSegment>(loop[^1]);
    }

    [Fact]
    public void TheNames_StandOutsideTheRing_TurnedToReadOutward()
    {
        var ctx = Draw(ChordAxes(out _));

        // The names on the ring come first; the legend, which has an entry for each group, is drawn after them.
        var labels = ctx.OfKind("DrawText").Take(3).Select(c => (dynamic)c.Args!).ToArray();

        Assert.Equal(["North", "South", "East"], labels.Select(l => (string)l.text));
        var rim = Centre.Y - ((MoveToSegment)Paths(ctx)[0][0]).Point.Y;
        foreach (var label in labels)
        {
            Assert.True(Distance((Point)label.position, Centre) > rim, "a name stands past the rim");
            Assert.InRange((double)label.rotation, -90.0 - 1e-9, 90.0 + 1e-9);
        }
    }

    [Fact]
    public void AnExplicitColour_IsWornByItsGroup_AndByTheRibbonsItSends()
    {
        var axes = ChordAxes(out var series);
        series.Colors = [Color.FromHex("#C03030")];

        var fills = Fills(Draw(axes));

        Assert.Equal(Color.FromHex("#C03030"), fills[0]);
        Assert.Equal(Color.FromHex("#C03030").WithAlpha((byte)Math.Round(series.RibbonAlpha * 255)), fills[3]);
    }

    [Fact]
    public void APlotTooSmallToHoldTheRing_DrawsNothing()
    {
        var ctx = new RecordingRenderContext();

        AxesRenderer.Create(ChordAxes(out _), new Rect(0, 0, 4, 4), ctx, Theme.Default).Render();

        Assert.Equal(0, ctx.CountOf("DrawPath"));
    }

    [Fact]
    public void AChordFigure_RendersToSvg()
    {
        var figure = new Figure { Width = 500, Height = 500 };
        figure.AddSubPlot(1, 1, 1).Chord(["North", "South", "East"], Flows);

        string svg = figure.ToSvg();

        Assert.Contains("North", svg, StringComparison.Ordinal);
        Assert.Contains("<path", svg, StringComparison.Ordinal);
    }
}
