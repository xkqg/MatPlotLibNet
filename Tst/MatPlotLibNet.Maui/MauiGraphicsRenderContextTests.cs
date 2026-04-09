// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.Maui.Graphics;
using NSubstitute;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MplColor = MatPlotLibNet.Styling.Color;
using MplFont = MatPlotLibNet.Styling.Font;
using MplPoint = MatPlotLibNet.Rendering.Point;
using MplRect = MatPlotLibNet.Rendering.Rect;

namespace MatPlotLibNet.Maui.Tests;

public class MauiGraphicsRenderContextTests
{
    [Fact]
    public void DrawLine_CallsCanvasDrawLine()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(10, 20), new MplPoint(30, 40),
            MplColor.Red, 2.0, Styling.LineStyle.Solid);

        canvas.Received(1).DrawLine(10f, 20f, 30f, 40f);
    }

    [Fact]
    public void DrawLine_SetsStrokeColor()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            MplColor.Blue, 1.5, Styling.LineStyle.Solid);

        canvas.Received().StrokeColor = Arg.Is<Microsoft.Maui.Graphics.Color>(c =>
            Math.Abs(c.Red - 0f) < 0.01 && Math.Abs(c.Blue - 1f) < 0.01);
    }

    [Fact]
    public void DrawLine_SetsStrokeSize()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            MplColor.Red, 3.0, Styling.LineStyle.Solid);

        canvas.Received().StrokeSize = 3.0f;
    }

    [Fact]
    public void DrawLine_DashedStyle_SetsDashPattern()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawLine(new MplPoint(0, 0), new MplPoint(10, 10),
            MplColor.Black, 1, Styling.LineStyle.Dashed);

        canvas.Received().StrokeDashPattern = Arg.Is<float[]>(p => p.Length > 0);
    }

    [Fact]
    public void DrawRectangle_WithFill_CallsFillRectangle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            fill: MplColor.Blue, stroke: null, strokeThickness: 0);

        canvas.Received(1).FillRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void DrawRectangle_WithStroke_CallsDrawRectangle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            fill: null, stroke: MplColor.Red, strokeThickness: 2);

        canvas.Received(1).DrawRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void DrawRectangle_WithBoth_CallsFillAndDraw()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            fill: MplColor.Blue, stroke: MplColor.Red, strokeThickness: 2);

        canvas.Received(1).FillRectangle(10f, 20f, 100f, 50f);
        canvas.Received(1).DrawRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void DrawCircle_WithFill_CallsFillCircle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawCircle(new MplPoint(50, 50), 10, MplColor.Green, null, 0);

        canvas.Received(1).FillCircle(50f, 50f, 10f);
    }

    [Fact]
    public void DrawText_CallsDrawString()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Hello", new MplPoint(100, 200), new MplFont(), MatPlotLibNet.Rendering.TextAlignment.Left);

        canvas.Received(1).DrawString(
            "Hello",
            Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<HorizontalAlignment>());
    }

    [Fact]
    public void DrawText_SetsFontSize()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Test", new MplPoint(0, 0), new MplFont { Size = 18 }, MatPlotLibNet.Rendering.TextAlignment.Center);

        canvas.Received().FontSize = 18f;
    }

    [Fact]
    public void MeasureText_ReturnsNonZeroSize()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var size = ctx.MeasureText("Hello World", new MplFont { Size = 12 });

        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void DrawLines_CallsDrawPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var points = new List<MplPoint> { new(0, 0), new(10, 10), new(20, 0) };
        ctx.DrawLines(points, MplColor.Red, 1.0, Styling.LineStyle.Solid);

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    [Fact]
    public void DrawPolygon_WithFill_CallsFillPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var points = new List<MplPoint> { new(0, 0), new(50, 0), new(50, 50), new(0, 50) };
        ctx.DrawPolygon(points, fill: MplColor.Blue, stroke: null, strokeThickness: 0);

        canvas.Received(1).FillPath(Arg.Any<PathF>());
    }

    [Fact]
    public void DrawPolygon_WithStroke_CallsDrawPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var points = new List<MplPoint> { new(0, 0), new(50, 0), new(50, 50), new(0, 50) };
        ctx.DrawPolygon(points, fill: null, stroke: MplColor.Red, strokeThickness: 2);

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    [Fact]
    public void DrawEllipse_WithFill_CallsFillEllipse()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawEllipse(new MplRect(10, 20, 60, 40),
            fill: MplColor.Green, stroke: null, strokeThickness: 0);

        canvas.Received(1).FillEllipse(10f, 20f, 60f, 40f);
    }

    [Fact]
    public void DrawEllipse_WithStroke_CallsDrawEllipse()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawEllipse(new MplRect(10, 20, 60, 40),
            fill: null, stroke: MplColor.Red, strokeThickness: 2);

        canvas.Received(1).DrawEllipse(10f, 20f, 60f, 40f);
    }

    [Fact]
    public void DrawPath_MoveToLineTo_CallsDrawPath()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        var segments = new List<MatPlotLibNet.Rendering.PathSegment>
        {
            new MatPlotLibNet.Rendering.MoveToSegment(new MplPoint(0, 0)),
            new MatPlotLibNet.Rendering.LineToSegment(new MplPoint(50, 50)),
            new MatPlotLibNet.Rendering.LineToSegment(new MplPoint(100, 0))
        };
        ctx.DrawPath(segments, fill: null, stroke: MplColor.Black, strokeThickness: 1);

        canvas.Received(1).DrawPath(Arg.Any<PathF>());
    }

    [Fact]
    public void PushClip_CallsSaveStateAndClipRectangle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.PushClip(new MplRect(10, 20, 100, 50));

        canvas.Received(1).SaveState();
        canvas.Received(1).ClipRectangle(10f, 20f, 100f, 50f);
    }

    [Fact]
    public void PopClip_CallsRestoreState()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.PopClip();

        canvas.Received(1).RestoreState();
    }

    [Fact]
    public void SetOpacity_SetsCanvasAlpha()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.SetOpacity(0.5);

        canvas.Received().Alpha = 0.5f;
    }

    [Fact]
    public void DrawCircle_WithStroke_CallsDrawCircle()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawCircle(new MplPoint(50, 50), 10, fill: null, stroke: MplColor.Red, strokeThickness: 2);

        canvas.Received(1).DrawCircle(50f, 50f, 10f);
    }

    [Fact]
    public void DrawText_CenterAlignment_CallsDrawString()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawText("Center", new MplPoint(50, 50), new MplFont { Size = 14 },
            MatPlotLibNet.Rendering.TextAlignment.Center);

        canvas.Received(1).DrawString(
            "Center",
            Arg.Any<float>(), Arg.Any<float>(),
            HorizontalAlignment.Center);
    }

    [Fact]
    public void DrawRectangle_NullFill_NullStroke_NoCanvasCalls()
    {
        var canvas = Substitute.For<ICanvas>();
        var ctx = new MauiGraphicsRenderContext(canvas);

        ctx.DrawRectangle(new MplRect(10, 20, 100, 50),
            fill: null, stroke: null, strokeThickness: 0);

        canvas.DidNotReceive().FillRectangle(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>());
        canvas.DidNotReceive().DrawRectangle(Arg.Any<float>(), Arg.Any<float>(),
            Arg.Any<float>(), Arg.Any<float>());
    }
}
