// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one tick drawing, <see cref="AxisTickExtensions"/>, tested at its own granularity: every edge, every
/// direction, every label arm and every <see cref="TickConfig"/> setting it honours. The primary axes, their mirrored
/// side and both secondary axes draw through it.</summary>
public class AxisTickExtensionsTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);

    // Maps x 0..2 onto 0..400 and y 0..2 onto 300..0, so ticks [0, 1, 2] sit at x 0/200/400 and y 300/150/0.
    private static DataTransform Tf() => new(0, 2, 0, 2, Plot);

    private const double Len = 5.0, Half = 0.5, Pad = 2.0, Size = 10.0, Width = 1.25;
    private static readonly double[] Xs = [0, 200, 400];
    private static readonly double[] Ys = [300, 150, 0];

    private static readonly TickConfig Config = new() { Length = Len, Pad = Pad, Width = Width };

    private static TickDrawContext Draw(TickSide side, double edge, ITickFormatter? formatter = null,
        double rotation = 0, TextAlignment alignment = TextAlignment.Center, double half = Half) => new()
    {
        Side = side,
        AxisEdge = edge,
        SpineHalf = half,
        BaseFont = new Font { Size = Size },
        Formatter = formatter,
        UniformFormat = v => v.ToString("G", CultureInfo.InvariantCulture),
        Foreground = Colors.Black,
        LabelRotation = rotation,
        Alignment = alignment,
    };

    private static RecordingRenderContext Render(TickConfig config, TickDrawContext draw, out double measured)
    {
        var ctx = new RecordingRenderContext();
        measured = ctx.RenderAxisTicks([0.0, 1.0, 2.0], fixedCoord: 0.0, Tf(), config, draw);
        return ctx;
    }

    private static RecordingRenderContext Render(TickConfig config, TickDrawContext draw) => Render(config, draw, out _);

    private readonly record struct Segment(Point P1, Point P2);

    private static Segment Mark(RecordingRenderContext ctx, int i)
    {
        dynamic a = ctx.OfKind("DrawLine")[i].Args!;
        return new Segment((Point)a.p1, (Point)a.p2);
    }

    private static dynamic Label(RecordingRenderContext ctx, int i) => ctx.OfKind("DrawText")[i].Args!;

    // ── Marks: one rule for every edge and direction ─────────────────────────
    // "from"/"to" are offsets from the edge along the growing pixel coordinate; Out always points away from the plot
    // and its inner end reaches half a spine into it; In points into the plot and its outer end reaches half a spine
    // beyond the edge.

    [Theory]
    [InlineData(false, TickDirection.Out, -Half, Len)]
    [InlineData(false, TickDirection.In, -Len, Half)]
    [InlineData(false, TickDirection.InOut, -Len, Len)]
    [InlineData(true, TickDirection.Out, -Len, Half)]
    [InlineData(true, TickDirection.In, -Half, Len)]
    [InlineData(true, TickDirection.InOut, -Len, Len)]
    public void RenderAxisTicks_HorizontalEdge_MarkRunsFromTheEdgeByDirection(bool farSide, TickDirection direction,
        double from, double to)
    {
        // farSide: the top edge; otherwise the bottom one.
        const double edge = 100;
        var ctx = Render(Config with { Direction = direction }, Draw(farSide ? TickSide.Top : TickSide.Bottom, edge));

        Assert.Equal(3, ctx.CountOf("DrawLine"));
        for (int i = 0; i < 3; i++)
            Assert.Equal(new Segment(new Point(Xs[i], edge + from), new Point(Xs[i], edge + to)), Mark(ctx, i));
    }

    [Theory]
    [InlineData(false, TickDirection.Out, -Len, Half)]
    [InlineData(false, TickDirection.In, -Half, Len)]
    [InlineData(false, TickDirection.InOut, -Len, Len)]
    [InlineData(true, TickDirection.Out, -Half, Len)]
    [InlineData(true, TickDirection.In, -Len, Half)]
    [InlineData(true, TickDirection.InOut, -Len, Len)]
    public void RenderAxisTicks_VerticalEdge_MarkRunsFromTheEdgeByDirection(bool farSide, TickDirection direction,
        double from, double to)
    {
        // farSide: the right edge; otherwise the left one.
        const double edge = 50;
        var ctx = Render(Config with { Direction = direction }, Draw(farSide ? TickSide.Right : TickSide.Left, edge));

        Assert.Equal(3, ctx.CountOf("DrawLine"));
        for (int i = 0; i < 3; i++)
            Assert.Equal(new Segment(new Point(edge + from, Ys[i]), new Point(edge + to, Ys[i])), Mark(ctx, i));
    }

    [Fact]
    public void RenderAxisTicks_MarkWearsTheConfigWidth_andTheForegroundWhenTheConfigSetsNoColour()
    {
        var ctx = Render(Config, Draw(TickSide.Bottom, 300));

        Assert.All(ctx.OfKind("DrawLine"), c =>
        {
            dynamic a = c.Args!;
            Assert.Equal(Colors.Black, (Color)a.color);
            Assert.Equal(Width, (double)a.thickness);
        });
    }

    [Fact]
    public void RenderAxisTicks_ConfigColour_WinsOverTheForeground()
    {
        var ctx = Render(Config with { Color = Colors.Red }, Draw(TickSide.Left, 0));

        Assert.All(ctx.OfKind("DrawLine"), c => Assert.Equal(Colors.Red, (Color)((dynamic)c.Args!).color));
    }

    [Fact]
    public void RenderAxisTicks_MarkShorterThanATenthOfAPixel_IsNotDrawn_butItsLabelIs()
    {
        var ctx = Render(Config with { Length = 0 }, Draw(TickSide.Bottom, 300, half: 0));

        Assert.Equal(0, ctx.CountOf("DrawLine"));
        Assert.Equal(3, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void RenderAxisTicks_NotVisible_DrawsNothing_andMeasuresZero()
    {
        var ctx = Render(Config with { Visible = false }, Draw(TickSide.Left, 0), out var measured);

        Assert.Empty(ctx.Calls);
        Assert.Equal(0, measured);
    }

    // ── Labels: where each edge puts them ────────────────────────────────────

    [Fact]
    public void RenderAxisTicks_Bottom_LabelSitsBelowTheMarkByLengthPadAndFontSize()
    {
        var ctx = Render(Config, Draw(TickSide.Bottom, 300, rotation: 30, alignment: TextAlignment.Right));

        for (int i = 0; i < 3; i++) Assert.Equal(new Point(Xs[i], 300 + Len + Pad + Size), (Point)Label(ctx, i).position);
        Assert.Equal(30.0, (double)Label(ctx, 1).rotation);
        Assert.Equal(TextAlignment.Right, (TextAlignment)Label(ctx, 1).alignment);
    }

    [Fact]
    public void RenderAxisTicks_Top_LabelSitsAboveTheMarkByLengthAndPad()
    {
        var ctx = Render(Config, Draw(TickSide.Top, 40));

        for (int i = 0; i < 3; i++) Assert.Equal(new Point(Xs[i], 40 - Len - Pad), (Point)Label(ctx, i).position);
    }

    [Fact]
    public void RenderAxisTicks_Left_LabelSitsBeforeTheEdge()
    {
        var ctx = Render(Config, Draw(TickSide.Left, 50, alignment: TextAlignment.Right));

        for (int i = 0; i < 3; i++) Assert.Equal(new Point(50 - Len - Pad, Ys[i] + 4), (Point)Label(ctx, i).position);
        Assert.Equal(TextAlignment.Right, (TextAlignment)Label(ctx, 0).alignment);
    }

    [Fact]
    public void RenderAxisTicks_Right_LabelSitsPastTheEdge()
    {
        var ctx = Render(Config, Draw(TickSide.Right, 450, alignment: TextAlignment.Left));

        for (int i = 0; i < 3; i++) Assert.Equal(new Point(450 + Len + Pad, Ys[i] + 4), (Point)Label(ctx, i).position);
    }

    [Fact]
    public void RenderAxisTicks_LabelFont_TakesTheConfigSizeAndColour()
    {
        var ctx = Render(Config with { LabelSize = 14, LabelColor = Colors.Blue }, Draw(TickSide.Bottom, 300));

        Assert.All(ctx.OfKind("DrawText"), c =>
        {
            var font = (Font)((dynamic)c.Args!).font;
            Assert.Equal(14.0, font.Size);
            Assert.Equal(Colors.Blue, font.Color);
        });
        // The label offset follows the label font, not the base font.
        Assert.Equal(300 + Len + Pad + 14, ((Point)Label(ctx, 0).position).Y);
    }

    [Fact]
    public void RenderAxisTicks_Formatter_WinsOverTheUniformFormat()
    {
        var withFormatter = Render(Config, Draw(TickSide.Bottom, 300, formatter: new PercentFormatter(100)));
        var without = Render(Config, Draw(TickSide.Bottom, 300));

        Assert.All(withFormatter.OfKind("DrawText"), c => Assert.Contains('%', (string)((dynamic)c.Args!).text));
        Assert.Equal(["0", "1", "2"], without.OfKind("DrawText").Select(c => (string)((dynamic)c.Args!).text));
    }

    // ── What it returns: the measured label extent the axis title clears ─────

    [Fact]
    public void RenderAxisTicks_HorizontalEdge_ReturnsTheTallestLabel()
    {
        // RecordingRenderContext.MeasureText returns (text.Length * font.Size * 0.6, font.Size).
        Render(Config with { LabelSize = 12 }, Draw(TickSide.Bottom, 300), out var measured);

        Assert.Equal(12.0, measured, 6);
    }

    [Fact]
    public void RenderAxisTicks_VerticalEdge_ReturnsTheWidestLabel()
    {
        Render(Config, Draw(TickSide.Right, 400, formatter: new LengthByValue()), out var measured);

        // Labels "", "x", "xx": the widest is 2 characters.
        Assert.Equal(2 * Size * 0.6, measured, 6);
    }

    /// <summary>Formats tick value n as n copies of "x", so each label has a known, different width.</summary>
    private sealed class LengthByValue : ITickFormatter
    {
        public string Format(double value) => new('x', (int)value);
    }

    // ── A turned label reaches as far as it is drawn ──────────────────────────
    // RecordingRenderContext measures a label (characters * 0.6 * size) wide and (size) high, so its descent below the
    // baseline, the height less the size, is 0: an upright label reaches its height, a turned one the anchor's depth
    // (the size), the turned width times the sine and the descent times the cosine. Labels "", "x", "xx" (LengthByValue):
    // the widest is 12 px.

    private const double Widest = 2 * Size * 0.6;

    [Theory]
    [InlineData(false, TextAlignment.Right, 30.0, Size + Widest * 0.5)]
    [InlineData(false, TextAlignment.Right, 90.0, Size + Widest)]
    [InlineData(false, TextAlignment.Center, 90.0, Size + Widest / 2)]
    [InlineData(false, TextAlignment.Left, -90.0, Size + Widest)]
    [InlineData(true, TextAlignment.Left, 90.0, Widest)]
    public void RenderAxisTicks_TurnedLabelOnAHorizontalEdge_ReturnsHowFarTheTurnedLabelReaches(bool top,
        TextAlignment alignment, double rotation, double expected)
    {
        Render(Config, Draw(top ? TickSide.Top : TickSide.Bottom, top ? 0 : 300, formatter: new LengthByValue(),
            rotation: rotation, alignment: alignment), out var measured);

        Assert.Equal(expected, measured, 9);
    }

    [Fact]
    public void RenderAxisTicks_TurnedTopLabel_ReachesItsTurnedWidthAndItsTurnedHeight()
    {
        // Anchored at its start and turned 30 degrees, a top label rises by its width times the sine and its height above
        // the baseline (the size) times the cosine; the band keeps the descent it has always kept below the baseline (0
        // here).
        Render(Config, Draw(TickSide.Top, 0, formatter: new LengthByValue(), rotation: 30, alignment: TextAlignment.Left),
            out var measured);

        Assert.Equal(Widest * 0.5 + Size * Math.Sqrt(3) / 2, measured, 9);
    }

    /// <summary>The identity the turned extent reduces to: upright, a label on a top or bottom edge reaches exactly its
    /// height, whatever its width and alignment, so a figure whose labels are not turned is laid out to the bit as before.</summary>
    [Theory]
    [InlineData(false, TextAlignment.Center, 12.0, 14.4)]
    [InlineData(false, TextAlignment.Right, 12.0, 14.4)]
    [InlineData(true, TextAlignment.Center, 12.0, 14.4)]
    [InlineData(true, TextAlignment.Left, 9.7, 11.64)]
    [InlineData(false, TextAlignment.Left, 23.3, 27.96)]
    public void LabelExtent_Upright_IsExactlyTheLabelHeight(bool top, TextAlignment alignment, double size, double height)
    {
        foreach (double width in new[] { 0.0, 7.3, 61.25, 333.3 })
        {
            var draw = Draw(top ? TickSide.Top : TickSide.Bottom, 0, alignment: alignment);

            Assert.Equal(height, draw.LabelExtent(new Size(width, height), size));
        }
    }

    /// <summary>On a left or right edge a label reaches the horizontal extent of its turned box: its width upright, and, turned
    /// about its anchor, the width times the cosine plus what the font size (the letters above the baseline) or the descent (below
    /// it) adds times the sine. A label on a left edge ends at its anchor and one on a right edge starts at it.</summary>
    [Theory]
    [InlineData(false, 0.0, 42.5)]
    [InlineData(false, 30.0, 42.5 * 0.8660254037844386 + 12 * 0.5)]
    [InlineData(false, 45.0, (42.5 + 12) * 0.7071067811865476)]
    [InlineData(false, 90.0, 12.0)]
    [InlineData(true, 0.0, 42.5)]
    [InlineData(true, 30.0, 42.5 * 0.8660254037844386 + 2.4 * 0.5)]
    [InlineData(true, 45.0, (42.5 + 2.4) * 0.7071067811865476)]
    [InlineData(true, 90.0, 2.4)]
    public void LabelExtent_OnAVerticalEdge_IsTheHorizontalExtentOfTheTurnedBox(bool right, double rotation, double expected)
    {
        var draw = Draw(right ? TickSide.Right : TickSide.Left, 0, rotation: rotation,
            alignment: right ? TextAlignment.Left : TextAlignment.Right);

        Assert.Equal(expected, draw.LabelExtent(new Size(42.5, 14.4), 12), 9);
    }

    /// <summary>How far a bottom label stands before and after its anchor along the edge, turned: a label that ends at its
    /// anchor and is turned up reaches back by its width times the cosine and the font size times the sine.</summary>
    [Fact]
    public void Reach_OfATurnedBottomLabel_SaysHowFarItStandsBeforeAndAfterItsAnchor()
    {
        var draw = Draw(TickSide.Bottom, 0, rotation: 30, alignment: TextAlignment.Right);

        var reach = draw.Reach(new Size(60, 14.4), 12);

        Assert.Equal(60 * 0.8660254037844386 + 12 * 0.5, reach.Before, 9);
        Assert.Equal(2.4 * 0.5, reach.After, 9);
        Assert.Equal(draw.LabelExtent(new Size(60, 14.4), 12), reach.Away, 9);
    }

    [Fact]
    public void Reach_OfAnUprightCentredLabel_IsHalfItsWidthEitherWay()
    {
        var draw = Draw(TickSide.Bottom, 0, rotation: 0, alignment: TextAlignment.Center);

        var reach = draw.Reach(new Size(60, 14.4), 12);

        Assert.Equal(30, reach.Before, 9);
        Assert.Equal(30, reach.After, 9);
        Assert.Equal(14.4, reach.Away, 9);
    }

    [Theory]
    [InlineData(30.0, 12 + 60 * 0.5 + 2.4 * 0.8660254037844386)]
    [InlineData(45.0, 12 + (60 + 2.4) * 0.7071067811865476)]
    [InlineData(90.0, 12 + 60.0)]
    public void LabelExtent_TurnedBottomLabel_IsTheAnchorDepthTheTurnedWidthAndTheTurnedDescent(double rotation, double expected)
    {
        // A 60 px wide label of size 12 measured 14.4 high: the anchor stands 12 below the reach line, the descent is 2.4.
        var draw = Draw(TickSide.Bottom, 0, rotation: rotation, alignment: TextAlignment.Right);

        Assert.Equal(expected, draw.LabelExtent(new Size(60, 14.4), 12), 9);
    }

    [Theory]
    [InlineData(false, 30.0, TextAlignment.Right)]
    [InlineData(false, 90.0, TextAlignment.Right)]
    [InlineData(true, 30.0, TextAlignment.Left)]
    [InlineData(false, 0.0, TextAlignment.Center)]
    public void MeasureAxisTicks_IsWhatRenderAxisTicksReturns_TurnedOrNot(bool top, double rotation, TextAlignment alignment)
    {
        var draw = Draw(top ? TickSide.Top : TickSide.Bottom, 0, formatter: new LengthByValue(), rotation: rotation,
            alignment: alignment);
        double measured = new RecordingRenderContext().MeasureAxisTicks([0.0, 1.0, 2.0], Config, draw);

        Render(Config, draw, out var drawn);

        Assert.Equal(drawn, measured);
    }

    // ── The TickConfig rules the primary and secondary axes share ────────────

    [Fact]
    public void LabelFont_WithoutOverrides_IsTheBaseFont()
    {
        var baseFont = new Font { Size = 9, Color = Colors.Gray };

        Assert.Equal(baseFont, new TickConfig().LabelFont(baseFont));
    }

    /// <summary>A Y title clears the mark, the pad and the widest tick label, then the container's gap, then the part of
    /// its own ink that faces them: nothing on the left (its baseline faces them), the body of its text on the right (0.6
    /// of its size), so the blank gap between the two inks is the same on both sides. A 4 px mark, a 3 px pad, a 20 px
    /// label, a 4 px gap and a 10 px title.</summary>
    [Theory]
    [InlineData("Left", 4 + 3 + 20 + 4 + 0)]
    [InlineData("Right", 4 + 3 + 20 + 4 + 6)]
    public void AYTitle_ClearsMarkPadWidestLabel_TheGapAndItsFacingInk(string side, double offset)
    {
        var axis = new Axis { Label = "Y", MajorTicks = new TickConfig { Length = 4, Pad = 3 } };

        var extent = axis.YTitleExtent(new YAxisTitleInput(axis.MajorTicks.TickLabelBand(20), Enum.Parse<AxisSide>(side), 4,
            new Font { Size = 10 }));

        Assert.Equal(offset, extent.TitleOffset, 9);
    }

    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    public void AYTitle_WithNoTickLabelDrawn_IsTheFixed45(string side)
    {
        var axis = new Axis { Label = "Y" };

        var extent = axis.YTitleExtent(new YAxisTitleInput(axis.MajorTicks.TickLabelBand(0), Enum.Parse<AxisSide>(side), 4,
            new Font { Size = 10 }));

        Assert.Equal(45, extent.TitleOffset, 9);
    }

    /// <summary>The one facing-edge rule, on every edge: the title's anchor stands past a 27 px tick label band by the gap
    /// and the part of the title's ink that faces the labels, so that ink edge stands exactly the gap past the band. A Y
    /// title is drawn turned a quarter (<c>rotate(-90)</c>) about its anchor with no <c>dy</c>, so its ink spans
    /// [x - 0.8 em, x + 0.2 em]; the ink it is read to face the labels with is the baseline left of a left edge and 0.6 em
    /// above it right of a right edge. Above a top edge a horizontal title's descent (0.2 em) faces them, below a bottom
    /// edge its ascent (0.8 em). A 10 px title and a 4 px gap: 27 + 4 + 0 = 31 left, 27 + 4 + 6 = 37 right, 27 + 4 + 2 = 33
    /// above a top edge and 27 + 4 + 8 = 39 below a bottom one.</summary>
    [Theory]
    [InlineData("Left", 31.0, 0.8)]
    [InlineData("Top", 33.0, 1.0)]
    [InlineData("Right", 37.0, 0.8)]
    [InlineData("Bottom", 39.0, 1.0)]
    public void TitleOffset_PutsTheFacingInkEdgeTheGapPastTheTickLabelBand_OnEveryEdge(string edge, double offset, double reach)
    {
        var side = Enum.Parse<TickSide>(edge);
        var ink = FacingInk.Of(side);

        double placed = new TitleClearance(TickLabelBand: 27, Gap: 4, LabelSize: 10, ink).TitleOffset();

        Assert.Equal(offset, placed, 9);
        // The ink edge facing the labels, measured from the axis edge, is the band plus the gap: the anchor less the
        // facing ink, whichever way the title faces.
        Assert.Equal(27 + 4, placed - ink.Facing * 10, 9);
        // A horizontal title's two ink edges together span its font size (ascent 0.8 em plus descent 0.2 em); a turned
        // title's facing ink is the part the reader sees, so its edges span the 0.8 em its outward ink and facing ink make.
        Assert.Equal(reach, ink.Facing + ink.Outward, 12);
    }

    /// <summary>An edge with no tick label drawn keeps its fixed offset when it has one (the 45 px a Y title has always
    /// stood at, the 35 px below a bottom edge); an edge without one places the title the gap and the facing ink past the
    /// edge itself, as the top edge always has.</summary>
    [Fact]
    public void TitleOffset_WithNoTickLabelDrawn_IsTheEdgesFixedOffset_OrTheGapPastTheEdge()
    {
        var left = new TitleClearance(0, 4, 10, FacingInk.Of(TickSide.Left)) { WithoutTickLabels = 45 };
        var top = new TitleClearance(0, 6, 10, FacingInk.Of(TickSide.Top));

        Assert.Equal(45, left.TitleOffset(), 9);
        Assert.Equal(6 + 2, top.TitleOffset(), 9);
        // A drawn band wins over the fixed offset.
        Assert.Equal(27 + 4 + 0, (left with { TickLabelBand = 27 }).TitleOffset(), 9);
    }

    /// <summary>A context that carries the labels' sizes, one per tick (measured once where they decided the rotation), is
    /// measured and drawn by those sizes; sizes that do not match the ticks are not read, and the labels are measured.</summary>
    [Fact]
    public void LabelSizes_OnePerTick_AreReadInsteadOfMeasuring_OtherwiseTheLabelsAreMeasured()
    {
        double[] ticks = [0.0, 1.0, 2.0];
        var carried = Draw(TickSide.Left, 0) with { LabelSizes = [new Size(7, 9), new Size(41, 9), new Size(3, 9)] };
        var mismatched = carried with { LabelSizes = [new Size(41, 9)] };
        var ctx = new RecordingRenderContext();

        Assert.Equal(41, ctx.MeasureAxisTicks(ticks, Config, carried), 9);
        Assert.Equal(41, ctx.RenderAxisTicks(ticks, 0.0, Tf(), Config, carried), 9);
        // Measured: "0", "1", "2" at 10 px are each 6 px wide in the recording context.
        Assert.Equal(6, ctx.MeasureAxisTicks(ticks, Config, mismatched), 9);
    }

    /// <summary>The horizontal axis titles' offsets, as plain numbers at two title sizes: a 4 px mark, a 3 px pad and a 20 px
    /// tall tick label make a 27 px band; the title's baseline stands a 6 px gap past it plus the part of the title that
    /// faces the labels (a fifth of its size above a top edge, four fifths below a bottom one), or the fixed 35 px below a
    /// bottom edge, when no label is drawn there. Written as numbers, not as the rule, so a change to the rule that
    /// unifies the title offsets cannot move where an X axis title stands without this going red.</summary>
    [Theory]
    [InlineData(13.0, 35.6, 43.4, 8.6)]
    [InlineData(18.0, 36.6, 47.4, 9.6)]
    public void TopAndBottomTitleOffsets_KeepTodaysValues(double titleSize, double top, double bottom, double topWithNoLabels)
    {
        var config = new TickConfig { Length = 4, Pad = 3 };

        Assert.Equal(top, config.TopAxisLabelOffset(20, titleSize), 9);
        Assert.Equal(bottom, config.BottomAxisLabelOffset(20, titleSize), 9);
        Assert.Equal(topWithNoLabels, config.TopAxisLabelOffset(0, titleSize), 9);
        Assert.Equal(35, config.BottomAxisLabelOffset(0, titleSize), 9);
    }
}
