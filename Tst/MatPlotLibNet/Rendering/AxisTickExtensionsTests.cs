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

    [Theory]
    [InlineData(false, 30.0)]
    [InlineData(true, 90.0)]
    public void LabelExtent_OnAVerticalEdge_IsTheLabelWidth(bool right, double rotation)
    {
        var draw = Draw(right ? TickSide.Right : TickSide.Left, 0, rotation: rotation, alignment: TextAlignment.Right);

        Assert.Equal(42.5, draw.LabelExtent(new Size(42.5, 14.4), 12));
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

    [Fact]
    public void AxisLabelOffset_ClearsMarkPadWidestLabelAndAGap()
    {
        var config = new TickConfig { Length = 4, Pad = 3 };

        Assert.Equal(4 + 3 + 20 + 12, config.AxisLabelOffset(20));
    }

    [Fact]
    public void AxisLabelOffset_WithNoTickLabelDrawn_IsTheFixed45()
    {
        Assert.Equal(45, new TickConfig().AxisLabelOffset(0));
    }
}
