// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>Direct unit tests for <see cref="CartesianBreakMarkersPart"/>: where the mark of each axis break stands, what it
/// draws (the gap erased in the axes background, then the zigzag or straight strokes in the foreground colour), which breaks
/// draw nothing, and the strokes of <see cref="CartesianBreakMarkersPart.DrawBreakSegments"/> for each style and orientation.</summary>
public class CartesianBreakMarkersPartTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly Theme DefaultTheme = Theme.Default;

    /// <summary>The transform the renderer builds for an X range 0..150 and a Y range 0..75 with the given breaks: the
    /// compressed range spread over the plot, the full range remapped per point.</summary>
    private static DataTransform Tf(IReadOnlyList<AxisBreak> xBreaks, IReadOnlyList<AxisBreak> yBreaks)
    {
        var cx = xBreaks.Count > 0 ? new AxisBreakMap(xBreaks, 0, 150).CompressedRange : new(0, 150);
        var cy = yBreaks.Count > 0 ? new AxisBreakMap(yBreaks, 0, 75).CompressedRange : new(0, 75);
        return new DataTransform(cx.Min, cx.Max, cy.Min, cy.Max, Plot, xBreaks, yBreaks, 0, 150, 0, 75);
    }

    private static CartesianBreakMarkersPart Build(Axes axes, RecordingRenderContext ctx) =>
        CartesianParts.PartFactory.BreakMarkers(axes, Plot, ctx, DefaultTheme, Tf(axes.XBreaks, axes.YBreaks));

    private static RecordingRenderContext Drawn(Axes axes) => CartesianParts.PartFactory.Drawn(ctx => Build(axes, ctx));

    [Fact]
    public void Render_XBreak_ErasesAnEightPixelGapOverTheWholeHeight_AtTheBreakStart()
    {
        var axes = new Axes();
        axes.AddXBreak(80, 120);
        var ctx = new RecordingRenderContext();
        var part = Build(axes, ctx);
        part.Render();

        var gap = ctx.Of<DrawRectangleCall>().Single().Rect;
        double at = Tf(axes.XBreaks, axes.YBreaks).DataToPixel(80, 0).X;
        Assert.Equal(at - 4, gap.X, 9);
        Assert.Equal(8, gap.Width, 9);
        Assert.Equal(Plot.Y, gap.Y);
        Assert.Equal(Plot.Height, gap.Height);
        Assert.Equal(DefaultTheme.AxesBackground, ctx.Of<DrawRectangleCall>()[0].Fill);
        Assert.All(ctx.Of<DrawLineCall>(), l => Assert.Equal(DefaultTheme.ForegroundText, l.Color));
    }

    [Fact]
    public void Render_YBreak_ErasesAnEightPixelGapOverTheWholeWidth_AtTheBreakStart()
    {
        var axes = new Axes();
        axes.AddYBreak(20, 40);
        var ctx = Drawn(axes);

        var gap = ctx.Of<DrawRectangleCall>().Single().Rect;
        double at = Tf(axes.XBreaks, axes.YBreaks).DataToPixel(0, 20).Y;
        Assert.Equal(at - 4, gap.Y, 9);
        Assert.Equal(8, gap.Height, 9);
        Assert.Equal(Plot.X, gap.X);
        Assert.Equal(Plot.Width, gap.Width);
        Assert.Equal(2, ctx.Count<DrawLineCall>());
    }

    [Fact]
    public void Render_XBreaksBeforeYBreaks_EachInTheOrderGiven()
    {
        var axes = new Axes();
        axes.AddYBreak(20, 40, BreakStyle.Straight);
        axes.AddXBreak(80, 120, BreakStyle.Straight);
        var ctx = Drawn(axes);

        var rects = ctx.Rects().ToArray();
        Assert.Equal(2, rects.Length);
        Assert.Equal(Plot.Height, rects[0].Height);   // the X break's gap runs top to bottom
        Assert.Equal(Plot.Width, rects[1].Width);     // the Y break's gap runs left to right
    }

    [Fact]
    public void Render_StyleNone_DrawsNothing()
    {
        var axes = new Axes();
        axes.AddXBreak(80, 120, BreakStyle.None);
        axes.AddYBreak(20, 40, BreakStyle.None);
        var ctx = Drawn(axes);
        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_ABreakStartingInsideAnother_DrawsNoMark()
    {
        var axes = new Axes();
        axes.AddXBreak(20, 90);
        axes.AddXBreak(40, 60);   // starts inside the first: no place on the axis
        axes.AddYBreak(10, 50);
        axes.AddYBreak(20, 30);
        var ctx = Drawn(axes);
        Assert.Equal(2, ctx.Count<DrawRectangleCall>());
    }

    public static TheoryData<Action<Axes>, double[], bool> TwoBreaks => new()
    {
        { axes => { axes.AddXBreak(30, 50); axes.AddXBreak(80, 120); }, [30, 80], true },
        { axes => { axes.AddYBreak(10, 20); axes.AddYBreak(40, 60); }, [10, 40], false },
    };

    [Theory]
    [MemberData(nameof(TwoBreaks))]
    public void Render_TwoBreaks_EachMarkStandsWhereTheTransformPutsTheBreakStart(Action<Axes> addTwoBreaks, double[] starts, bool alongX)
    {
        var axes = new Axes();
        addTwoBreaks(axes);
        var ctx = Drawn(axes);

        var tf = Tf(axes.XBreaks, axes.YBreaks);
        var gaps = ctx.Rects().ToArray();
        Assert.Equal(2, gaps.Length);
        for (int i = 0; i < 2; i++)
        {
            var start = alongX ? tf.DataToPixel(starts[i], 0).X : tf.DataToPixel(0, starts[i]).Y;
            Assert.Equal(start, (alongX ? gaps[i].X : gaps[i].Y) + 4, 9); // the second X mark was once the pixel of 60: the earlier gap taken off twice
        }
    }

    /// <summary>The transform the renderer builds for a log X axis over 1..10⁶ (0..6 in log space) and a linear Y over 0..75.</summary>
    private static DataTransform LogTf(IReadOnlyList<AxisBreak> xBreaks)
    {
        var c = new AxisBreakMap(xBreaks, 1, 1e6).CompressedRange;
        return new DataTransform(Math.Log10(c.Min), Math.Log10(c.Max), 0, 75, Plot, xBreaks, null, 1, 1e6, 0, 75,
            AxisScale.Log, AxisScale.Linear, 1.0, 1.0);
    }

    [Fact]
    public void Render_TwoXBreaksOnALogAxis_EachMarkStandsWhereTheTransformPutsTheBreakStart()
    {
        var axes = new Axes();
        axes.AddXBreak(2, 3);
        axes.AddXBreak(10, 100);
        var tf = LogTf(axes.XBreaks);
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.BreakMarkers(axes, Plot, c, DefaultTheme, tf));

        var gaps = ctx.Rects().ToArray();
        Assert.Equal(2, gaps.Length);
        Assert.Equal(tf.DataToPixel(2, 0).X, gaps[0].X + 4, 9);
        Assert.Equal(tf.DataToPixel(10, 0).X, gaps[1].X + 4, 9);
    }

    [Fact]
    public void Render_ABreakStartingWhereALogAxisHasNoPlace_DrawsNoMark()
    {
        var axes = new Axes();
        axes.AddXBreak(0, 0.5);   // log has no place for 0: the mark used to be drawn at x = NaN
        var ctx = CartesianParts.PartFactory.Drawn(c => CartesianParts.PartFactory.BreakMarkers(axes, Plot, c, DefaultTheme, LogTf(axes.XBreaks)));

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void Render_ABreakStartingBeyondThePlot_DrawsNoMark()
    {
        var axes = new Axes();
        axes.AddXBreak(200, 300);   // the X range is 0..150: the mark used to stand 130 px right of the plot
        axes.AddYBreak(-20, -10);   // the Y range is 0..75: below the plot
        var ctx = Drawn(axes);

        Assert.Empty(ctx.Calls);
    }

    // ── DrawBreakSegments ────────────────────────────────────────────────────

    private static CartesianBreakMarkersPart Segments(RecordingRenderContext ctx) => Build(new Axes(), ctx);

    /// <summary>An X break's zigzag is one zigzag of two strokes, as a Y break's is: it used to write the same two strokes
    /// twice (the third and fourth ran from <c>crossEnd − 0.6·span</c>, which is <c>crossStart + 0.4·span</c>).</summary>
    [Fact]
    public void DrawBreakSegments_Horizontal_Zigzag_DrawsTwoLines_TheVerticalZigzagTurned()
    {
        var ctx = new RecordingRenderContext();
        Segments(ctx).DrawBreakSegments(
            Orientation.Horizontal,
            crossStart: 0, crossEnd: 300,
            perpPos: 200, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: BreakStyle.Zigzag);

        var lines = ctx.Of<DrawLineCall>();
        Assert.Equal(2, lines.Count);
        Assert.Equal(new Point(194, 120), lines[0].P1);
        Assert.Equal(new Point(200, 150), lines[0].P2);
        Assert.Equal(new Point(200, 150), lines[1].P1);
        Assert.Equal(new Point(206, 180), lines[1].P2);
    }

    [Theory]
    [InlineData(Orientation.Vertical, 400, 150, BreakStyle.Zigzag, 2)] // DrawBreakSegments_Vertical_Zigzag_DrawsTwoLines
    [InlineData(Orientation.Horizontal, 300, 200, BreakStyle.Straight, 2)] // DrawBreakSegments_Horizontal_Straight_DrawsTwoLines
    [InlineData(Orientation.Vertical, 400, 150, BreakStyle.Straight, 2)] // DrawBreakSegments_Vertical_Straight_DrawsTwoLines
    [InlineData(Orientation.Horizontal, 300, 200, BreakStyle.None, 0)] // DrawBreakSegments_None_DrawsNothing
    public void DrawBreakSegments_DrawsTheLinesItsStyleCallsFor(Orientation orientation, int crossEnd, int perpPos, BreakStyle style, int lines)
    {
        var ctx = new RecordingRenderContext();
        Segments(ctx).DrawBreakSegments(
            orientation,
            crossStart: 0, crossEnd: crossEnd,
            perpPos: perpPos, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: style);

        Assert.Equal(lines, ctx.Count<DrawLineCall>());
    }
}
