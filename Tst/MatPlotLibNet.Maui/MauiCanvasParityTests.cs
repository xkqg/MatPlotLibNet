// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using Microsoft.Maui.Graphics;
using NSubstitute;
using MplColor = MatPlotLibNet.Styling.Color;
using MplFont = MatPlotLibNet.Styling.Font;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplRect = MatPlotLibNet.Rendering.Rect;

namespace MatPlotLibNet.Maui.Tests;

/// <summary>The MAUI canvas drew a chart that is not the one the SVG and the PNG draw: a hatched bar was flat (with a note on
/// the diagnostics channel), a gradient was flat, an arc of a pie slice or a donut ring was dropped from the path without a
/// word, and the width of a label was guessed at 0.6 of the font size, which is not the width the layout had assumed. Each of
/// them has a primitive in <see cref="ICanvas"/> to be drawn with: a pattern paint, a gradient paint, curves, and the
/// library's own text metrics.</summary>
public class MauiCanvasParityTests
{
    private static MplColor Blue => MplColor.FromHex("#4C72B0");

    private static ShapeStyle Hatched(HatchPattern pattern) =>
        new(Blue, null, 0) { Hatch = pattern, HatchColor = MplColor.FromHex("#000000") };

    /// <summary>Records the fill paints a canvas was asked to use.</summary>
    private static (ICanvas Canvas, List<(Paint Paint, RectF Bounds)> Fills) RecordingCanvas()
    {
        var canvas = Substitute.For<ICanvas>();
        var fills = new List<(Paint, RectF)>();
        canvas.When(c => c.SetFillPaint(Arg.Any<Paint>(), Arg.Any<RectF>()))
            .Do(call => fills.Add((call.Arg<Paint>(), call.Arg<RectF>())));
        return (canvas, fills);
    }

    // ── hatch ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(HatchPattern.ForwardDiagonal)]
    [InlineData(HatchPattern.BackDiagonal)]
    [InlineData(HatchPattern.Horizontal)]
    [InlineData(HatchPattern.Vertical)]
    [InlineData(HatchPattern.Cross)]
    [InlineData(HatchPattern.DiagonalCross)]
    [InlineData(HatchPattern.Dots)]
    [InlineData(HatchPattern.Stars)]
    public void AHatchedRectangle_IsFilledWithAPatternPaintOfThatHatch(HatchPattern pattern)
    {
        var (canvas, fills) = RecordingCanvas();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50), Hatched(pattern));

        var pattern_ = Assert.IsType<PatternPaint>(Assert.Single(fills).Paint).Pattern!;
        var tile = Substitute.For<ICanvas>();
        pattern_.Draw(tile);
        int lines = HatchTile.MarksOf(pattern).Count(m => m.Kind == HatchMarkKind.Line);
        int dots = HatchTile.MarksOf(pattern).Count(m => m.Kind == HatchMarkKind.Dot);
        tile.Received(lines).DrawLine(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
        // The canvas has no circle of its own: FillCircle is an ellipse of twice the radius about the centre.
        tile.Received(dots).FillEllipse(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>());
        Assert.Equal((float)HatchTile.Size, pattern_.Width);
        Assert.Equal((float)HatchTile.Size, pattern_.Height);
    }

    [Fact]
    public void TheHatchLines_AreDrawnWhereTheTileSaysTheyAre()
    {
        var (canvas, fills) = RecordingCanvas();
        new MauiGraphicsRenderContext(canvas).DrawRectangle(new MplRect(0, 0, 60, 60), Hatched(HatchPattern.Horizontal));
        var tile = Substitute.For<ICanvas>();

        Assert.IsType<PatternPaint>(fills.Single().Paint).Pattern!.Draw(tile);

        tile.Received(1).DrawLine(0f, 3f, 6f, 3f);
    }

    [Fact]
    public void AHatch_IsBothPaintedOverTheFlatFill_AndNoLongerAnnouncedAsLost()
    {
        var (canvas, fills) = RecordingCanvas();
        var announced = new List<string>();
        void Collect(ChartDiagnostic d) => announced.Add(d.Message);
        ChartDiagnostics.Emitted += Collect;
        try
        {
            new MauiGraphicsRenderContext(canvas).DrawRectangle(new MplRect(0, 0, 60, 60), Hatched(HatchPattern.Cross));
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        canvas.Received(2).FillRectangle(0f, 0f, 60f, 60f);   // the flat fill, then the hatch over it
        Assert.DoesNotContain(announced, m => m.Contains("Hatch pattern", StringComparison.Ordinal));
    }

    [Fact]
    public void TheHatchColour_IsTheOneAsked_ElseOneThatShowsOnTheFill()
    {
        var (canvas, fills) = RecordingCanvas();
        var ctx = new MauiGraphicsRenderContext(canvas);
        ctx.DrawRectangle(new MplRect(0, 0, 10, 10), new ShapeStyle(MplColor.FromHex("#FFFFFF"), null, 0) { Hatch = HatchPattern.Vertical });
        var tile = Substitute.For<ICanvas>();

        Assert.IsType<PatternPaint>(fills.Single().Paint).Pattern!.Draw(tile);

        tile.Received().StrokeColor = Arg.Is<Microsoft.Maui.Graphics.Color>(c => c.Red < 0.2f && c.Green < 0.2f && c.Blue < 0.2f);   // dark on white
    }

    [Fact]
    public void EveryShapeThatCanBeFilled_CarriesTheHatch()
    {
        var (canvas, fills) = RecordingCanvas();
        var ctx = new MauiGraphicsRenderContext(canvas);
        var style = Hatched(HatchPattern.Dots);

        ctx.DrawCircle(new MplPoint(20, 20), 10, style);
        ctx.DrawEllipse(new MplRect(0, 0, 30, 20), style);
        ctx.DrawPolygon([new MplPoint(0, 0), new MplPoint(10, 0), new MplPoint(5, 8)], style);
        ctx.DrawPath([new MoveToSegment(new MplPoint(0, 0)), new LineToSegment(new MplPoint(10, 0)), new LineToSegment(new MplPoint(5, 8)), new CloseSegment()], style);

        Assert.Equal(4, fills.Count(f => f.Paint is PatternPaint));
    }

    // ── gradient ────────────────────────────────────────────────────────────────

    [Fact]
    public void AGradientFill_IsALinearGradientPaint_WithItsEndsRelativeToTheShape()
    {
        var (canvas, fills) = RecordingCanvas();
        var gradient = new LinearGradient(new MplPoint(10, 20), new MplPoint(110, 20), MplColor.FromHex("#FF0000"), MplColor.FromHex("#0000FF"));
        var style = new ShapeStyle(MplColor.FromHex("#FF0000"), null, 0) { Gradient = gradient };

        new MauiGraphicsRenderContext(canvas).DrawRectangle(new MplRect(10, 20, 100, 50), style);

        var (paint, bounds) = Assert.Single(fills);
        var linear = Assert.IsType<LinearGradientPaint>(paint);
        Assert.Equal(new RectF(10, 20, 100, 50), bounds);
        Assert.Equal(0.0, linear.StartPoint.X, 6);
        Assert.Equal(0.0, linear.StartPoint.Y, 6);
        Assert.Equal(1.0, linear.EndPoint.X, 6);
        Assert.Equal(0.0, linear.EndPoint.Y, 6);
        Assert.True(linear.StartColor!.Red > 0.9f && linear.StartColor.Blue < 0.1f);
        Assert.True(linear.EndColor!.Blue > 0.9f && linear.EndColor.Red < 0.1f);
    }

    [Fact]
    public void AGradientWithNoSizeToLieAcross_FallsBackToTheFlatFill()
    {
        var (canvas, fills) = RecordingCanvas();
        var gradient = new LinearGradient(new MplPoint(0, 0), new MplPoint(10, 0), MplColor.FromHex("#FF0000"), MplColor.FromHex("#0000FF"));

        new MauiGraphicsRenderContext(canvas).DrawRectangle(new MplRect(5, 5, 0, 20), new ShapeStyle(MplColor.FromHex("#FF0000"), null, 0) { Gradient = gradient });

        Assert.Empty(fills);
    }

    [Fact]
    public void AHatchOverAGradient_IsPaintedAfterIt()
    {
        var (canvas, fills) = RecordingCanvas();
        var gradient = new LinearGradient(new MplPoint(0, 0), new MplPoint(60, 0), MplColor.FromHex("#FF0000"), MplColor.FromHex("#0000FF"));
        var style = new ShapeStyle(MplColor.FromHex("#FF0000"), null, 0) { Gradient = gradient, Hatch = HatchPattern.Cross };

        new MauiGraphicsRenderContext(canvas).DrawRectangle(new MplRect(0, 0, 60, 30), style);

        Assert.IsType<LinearGradientPaint>(fills[0].Paint);
        Assert.IsType<PatternPaint>(fills[1].Paint);
    }

    // ── arcs ────────────────────────────────────────────────────────────────────

    [Fact]
    public void AnArcInAPath_IsFollowedWithCurves_NotDropped()
    {
        var canvas = Substitute.For<ICanvas>();
        PathF? drawn = null;
        canvas.When(c => c.FillPath(Arg.Any<PathF>(), Arg.Any<WindingMode>())).Do(call => drawn = call.Arg<PathF>());
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawPath(
        [
            new MoveToSegment(new MplPoint(10, 0)),
            new ArcSegment(new MplPoint(0, 0), 10, 10, 0, 90),
            new LineToSegment(new MplPoint(0, 0)),
            new CloseSegment(),
        ], new ShapeStyle(Blue, null, 0));

        Assert.NotNull(drawn);
        var operations = Enumerable.Range(0, drawn!.OperationCount).Select(i => drawn.GetSegmentType(i)).ToArray();
        Assert.Contains(PathOperation.Cubic, operations);
        var arcEnd = drawn.GetPointsForSegment(Array.IndexOf(operations, PathOperation.Cubic))[^1];
        Assert.Equal(0f, arcEnd.X, 3);
        Assert.Equal(10f, arcEnd.Y, 3);
    }

    // ── text ────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALabelIsMeasured_ByTheMetricsTheLayoutUses()
    {
        var ctx = new MauiGraphicsRenderContext(Substitute.For<ICanvas>());
        var font = new MplFont { Size = 14 };

        var size = ctx.MeasureText("Temperature (°C)", font);

        Assert.Equal(FontMetricsDefault.Current.Measure("Temperature (°C)", font), size);
    }
}
