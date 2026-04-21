// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Phase L.2 (v1.7.2, 2026-04-21) — direct unit tests for the three
/// internal helpers extracted from <see cref="CartesianAxesRenderer"/>:
/// <see cref="CartesianAxesRenderer.RenderGridLines"/>,
/// <see cref="CartesianAxesRenderer.RenderAxisTicks"/>, and
/// <see cref="CartesianAxesRenderer.DrawBreakSegments"/>.
/// Uses <see cref="RecordingRenderContext"/> to assert on DrawLine / DrawText counts.</summary>
public class CartesianAxesRendererUnitTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly Theme DefaultTheme = Theme.Default;

    private static CartesianAxesRenderer Make(RecordingRenderContext ctx) =>
        new(new Axes(), Plot, ctx, DefaultTheme);

    private static DataTransform Tf(double xMin = 0, double xMax = 2, double yMin = 0, double yMax = 2) =>
        new(xMin, xMax, yMin, yMax, Plot);

    // ── RenderGridLines ──────────────────────────────────────────────────────

    [Fact]
    public void RenderGridLines_Horizontal_Major_CountEqualsTickCount()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);
        double[] ticks = [0.0, 1.0, 2.0];

        renderer.RenderGridLines(
            Orientation.Horizontal, ticks, fixedCoord: 0.0,
            Tf(), Colors.Gray, 1.0, LineStyle.Solid, minor: false);

        Assert.Equal(3, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void RenderGridLines_Vertical_Major_CountEqualsTickCount()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);
        double[] ticks = [0.0, 1.0, 2.0];

        renderer.RenderGridLines(
            Orientation.Vertical, ticks, fixedCoord: 0.0,
            Tf(), Colors.Gray, 1.0, LineStyle.Solid, minor: false);

        Assert.Equal(3, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void RenderGridLines_Minor_CountIsSubdivisionsMinusMajorOverlapAndBounds()
    {
        // ticks = [0,1,2], majorStep=1, minorStep=0.2
        // Minor positions in (0,2) exclusive: 0.2,0.4,0.6,0.8, 1.2,1.4,1.6,1.8 → 8 lines
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);
        double[] ticks = [0.0, 1.0, 2.0];

        renderer.RenderGridLines(
            Orientation.Horizontal, ticks, fixedCoord: 0.0,
            Tf(), Colors.LightGray, 0.5, LineStyle.Solid, minor: true);

        Assert.Equal(8, ctx.CountOf("DrawLine"));
    }

    // ── DrawBreakSegments ────────────────────────────────────────────────────

    [Fact]
    public void DrawBreakSegments_Horizontal_Zigzag_DrawsFourLines()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);

        renderer.DrawBreakSegments(
            Orientation.Horizontal,
            crossStart: 0, crossEnd: 300,
            perpPos: 200, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: BreakStyle.Zigzag);

        Assert.Equal(4, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void DrawBreakSegments_Vertical_Zigzag_DrawsTwoLines()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);

        renderer.DrawBreakSegments(
            Orientation.Vertical,
            crossStart: 0, crossEnd: 400,
            perpPos: 150, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: BreakStyle.Zigzag);

        Assert.Equal(2, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void DrawBreakSegments_Horizontal_Straight_DrawsTwoLines()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);

        renderer.DrawBreakSegments(
            Orientation.Horizontal,
            crossStart: 0, crossEnd: 300,
            perpPos: 200, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: BreakStyle.Straight);

        Assert.Equal(2, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void DrawBreakSegments_Vertical_Straight_DrawsTwoLines()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);

        renderer.DrawBreakSegments(
            Orientation.Vertical,
            crossStart: 0, crossEnd: 400,
            perpPos: 150, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: BreakStyle.Straight);

        Assert.Equal(2, ctx.CountOf("DrawLine"));
    }

    [Fact]
    public void DrawBreakSegments_None_DrawsNothing()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);

        renderer.DrawBreakSegments(
            Orientation.Horizontal,
            crossStart: 0, crossEnd: 300,
            perpPos: 200, halfSize: 6.0,
            thickness: 1.5, lineColor: Colors.Black,
            style: BreakStyle.None);

        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    // ── RenderAxisTicks ──────────────────────────────────────────────────────

    [Fact]
    public void RenderAxisTicks_Horizontal_DrawsMarkAndLabelPerTick()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);
        double[] ticks = [0.0, 1.0, 2.0];
        double axisY = Plot.Y + Plot.Height;
        var font = new Font { Size = 10 };
        string Fmt(double v) => v.ToString("G");

        var tickCtx = new TickDrawContext
        {
            LabelFont     = font,
            Formatter     = null,
            UniformFormat = Fmt,
            AxisEdge      = axisY,
            SpineHalf     = 0.5,
            LabelRotation = 0,
            Alignment     = TextAlignment.Center,
            Pad           = 2.0,
        };

        renderer.RenderAxisTicks(
            ticks, Orientation.Horizontal, fixedCoord: 0.0,
            tickLength: 5.0, tickColor: Colors.Black, tickWidth: 1.0,
            direction: TickDirection.Out, Tf(), tickCtx);

        Assert.Equal(3, ctx.CountOf("DrawLine"));
        Assert.Equal(3, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void RenderAxisTicks_Vertical_DrawsMarkAndLabelPerTick()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);
        double[] ticks = [0.0, 1.0, 2.0];
        var font = new Font { Size = 10 };
        string Fmt(double v) => v.ToString("G");

        var tickCtx = new TickDrawContext
        {
            LabelFont     = font,
            Formatter     = null,
            UniformFormat = Fmt,
            AxisEdge      = Plot.X,
            SpineHalf     = 0.5,
            LabelRotation = 0,
            Alignment     = TextAlignment.Right,
            Pad           = 2.0,
        };

        renderer.RenderAxisTicks(
            ticks, Orientation.Vertical, fixedCoord: 0.0,
            tickLength: 5.0, tickColor: Colors.Black, tickWidth: 1.0,
            direction: TickDirection.Out, Tf(), tickCtx);

        Assert.Equal(3, ctx.CountOf("DrawLine"));
        Assert.Equal(3, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void RenderAxisTicks_Horizontal_ReturnsMeasuredMaxHeight()
    {
        var ctx = new RecordingRenderContext();
        var renderer = Make(ctx);
        double[] ticks = [0.0, 1.0, 2.0];
        // RecordingRenderContext.MeasureText returns (text.Length * font.Size * 0.6, font.Size)
        // All three labels "0", "1", "2" have length 1 → height = font.Size = 12
        var font = new Font { Size = 12 };
        string Fmt(double v) => v.ToString("G");

        var tickCtx = new TickDrawContext
        {
            LabelFont     = font,
            Formatter     = null,
            UniformFormat = Fmt,
            AxisEdge      = Plot.Y + Plot.Height,
            SpineHalf     = 0.5,
            LabelRotation = 0,
            Alignment     = TextAlignment.Center,
            Pad           = 2.0,
        };

        double maxDim = renderer.RenderAxisTicks(
            ticks, Orientation.Horizontal, fixedCoord: 0.0,
            tickLength: 5.0, tickColor: Colors.Black, tickWidth: 1.0,
            direction: TickDirection.Out, Tf(), tickCtx);

        Assert.Equal(12.0, maxDim, precision: 6);
    }
}
