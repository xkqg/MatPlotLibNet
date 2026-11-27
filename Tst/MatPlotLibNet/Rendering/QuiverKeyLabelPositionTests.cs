// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// A quiver key stands where matplotlib's <c>QuiverKey</c> stands, by the one rule its label position decides: matplotlib 3.11,
/// <c>quiver.QuiverKey</c>: <c>pivot = {'N': 'middle', 'S': 'middle', 'E': 'tip', 'W': 'tail'}</c> (the point <c>X, Y</c> is the
/// middle of the arrow, its head or its tail), <c>halign = {'N': 'center', 'S': 'center', 'E': 'left', 'W': 'right'}</c> and
/// <c>valign = {'N': 'bottom', 'S': 'top', 'E': 'center', 'W': 'center'}</c>, the text put at that point shifted by
/// <c>labelsep</c>, 0.1 inch, in the direction of the position (<c>_text_shift</c>). The plot is 400 by 300 pixels over 0 to 10
/// on both axes, so a unit is 40 pixels: a key of U = 2 at (0.5, 0.5) has an arrow 80 pixels long on the point (200, 150), and
/// 0.1 inch is 10 pixels (a point is 100 / 72 pixels, the rule every length in points follows).
/// </summary>
public class QuiverKeyLabelPositionTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);
    private const double LabelSep = 10;      // 0.1 inch: QuiverKey(labelsep=0.1)
    private static readonly double Size = Theme.Default.DefaultFont.Size;   // the key's label at its resting size: the theme's text

    private static RecordingRenderContext Render(QuiverKeyLabelPosition? position)
    {
        var ctx = new RecordingRenderContext();
        var series = new QuiverKeySeries(0.5, 0.5, 2.0, "key");
        if (position is { } given) series.LabelPosition = given;
        new QuiverKeySeriesRenderer(new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)))
            .Render(series);
        return ctx;
    }

    private static (Point From, Point To) Shaft(RecordingRenderContext ctx)
    {
        var line = ctx.Of<DrawLineCall>()[0];
        return (line.P1, line.P2);
    }

    private static (Point At, TextAlignment Alignment) Label(RecordingRenderContext ctx)
    {
        var text = ctx.Of<DrawTextCall>().Single();
        return (text.Position, text.Alignment);
    }

    [Fact]
    public void TheFirstLabelPosition_IsAbove_MatplotlibsN()
    {
        Assert.Equal(QuiverKeyLabelPosition.Above, default);
    }

    [Fact]
    public void ALabelAbove_StandsTheLabelSepOverTheMiddleOfTheArrow_BottomAndCentred()
    {
        var ctx = Render(null);

        var (from, to) = Shaft(ctx);
        var (at, alignment) = Label(ctx);
        Assert.Equal(new Point(160, 150), from);   // the point (200, 150) is the middle of the arrow
        Assert.Equal(new Point(240, 150), to);
        Assert.Equal(new Point(200, 150 - LabelSep), at);   // the text's bottom is the label sep above it
        Assert.Equal(TextAlignment.Center, alignment);
    }

    [Fact]
    public void ALabelBelow_StandsTheLabelSepUnderTheMiddleOfTheArrow_TopAndCentred()
    {
        var ctx = Render(QuiverKeyLabelPosition.Below);

        var (from, to) = Shaft(ctx);
        var (at, alignment) = Label(ctx);
        Assert.Equal(new Point(160, 150), from);
        Assert.Equal(new Point(240, 150), to);
        Assert.Equal(new Point(200, 150 + LabelSep + Size), at);   // the baseline is a font size under the text's top
        Assert.Equal(TextAlignment.Center, alignment);
    }

    [Fact]
    public void ALabelRight_PutsTheHeadOnThePoint_AndStartsTheLabelTheLabelSepPastIt()
    {
        var ctx = Render(QuiverKeyLabelPosition.Right);

        var (from, to) = Shaft(ctx);
        var (at, alignment) = Label(ctx);
        Assert.Equal(new Point(120, 150), from);   // 'tip': the point is the head of the arrow
        Assert.Equal(new Point(200, 150), to);
        Assert.Equal(200 + LabelSep, at.X);
        Assert.Equal(150 + 0.35 * Size, at.Y, 1e-9);   // centred on the arrow's line: 0.35 of the ascent
        Assert.Equal(TextAlignment.Left, alignment);
    }

    [Fact]
    public void ALabelLeft_PutsTheTailOnThePoint_AndEndsTheLabelTheLabelSepBeforeIt()
    {
        var ctx = Render(QuiverKeyLabelPosition.Left);

        var (from, to) = Shaft(ctx);
        var (at, alignment) = Label(ctx);
        Assert.Equal(new Point(200, 150), from);   // 'tail': the point is the tail of the arrow
        Assert.Equal(new Point(280, 150), to);
        Assert.Equal(200 - LabelSep, at.X);
        Assert.Equal(150 + 0.35 * Size, at.Y, 1e-9);
        Assert.Equal(TextAlignment.Right, alignment);
    }

    [Theory]
    [InlineData(QuiverKeyLabelPosition.Above, 240)]
    [InlineData(QuiverKeyLabelPosition.Below, 240)]
    [InlineData(QuiverKeyLabelPosition.Right, 200)]
    [InlineData(QuiverKeyLabelPosition.Left, 280)]
    public void TheArrowHead_StandsOnTheHead_WhateverThePosition(QuiverKeyLabelPosition position, double headX)
    {
        var lines = Render(position).Of<DrawLineCall>();

        Assert.Equal(3, lines.Count);
        Assert.Equal(headX, lines[0].P2.X);
        Assert.Equal(headX, lines[1].P1.X);   // the two barbs leave the head
        Assert.Equal(headX, lines[2].P1.X);
    }

    [Theory]
    [InlineData(QuiverKeyLabelPosition.Above)]
    [InlineData(QuiverKeyLabelPosition.Below)]
    [InlineData(QuiverKeyLabelPosition.Right)]
    [InlineData(QuiverKeyLabelPosition.Left)]
    public void AKeyThatCannotBePlaced_DrawsNeitherArrowNorLabel_WhateverThePosition(QuiverKeyLabelPosition position)
    {
        var ctx = new RecordingRenderContext();
        var series = new QuiverKeySeries(0.5, 0.5, double.NaN, "key") { LabelPosition = position };

        new QuiverKeySeriesRenderer(new SeriesRenderContext(new DataTransform(0, 10, 0, 10, Bounds), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)))
            .Render(series);

        Assert.Empty(ctx.Calls);
    }

    // ── the position travels in the JSON ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void TheDefaultPosition_IsLeftOutOfTheJson()
    {
        var dto = new QuiverKeySeries(0.5, 0.5, 2.0, "key").ToSeriesDto();

        Assert.Null(dto.QuiverKeyLabelPosition);
    }

    [Fact]
    public void ADocumentWrittenBeforeThePositionExisted_ReadsAsAbove()
    {
        const string json = """{"width":800,"height":600,"subPlots":[{"series":[{"type":"quiverkey","quiverKeyX":0.5,"quiverKeyY":0.9,"quiverKeyU":1,"quiverKeyLabel":"key"}]}]}""";

        var read = (QuiverKeySeries)new ChartSerializer().FromJson(json).SubPlots[0].Series[0];

        Assert.Equal(QuiverKeyLabelPosition.Above, read.LabelPosition);
    }
}
