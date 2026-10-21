// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Direct unit tests for the internal members of <see cref="CartesianAxesRenderer"/>:
/// <see cref="CartesianAxesRenderer.RenderGridLines"/> and <see cref="CartesianAxesRenderer.DrawBreakSegments"/>.
/// The tick drawing moved to <see cref="AxisTickExtensions"/>; its tests live in <c>AxisTickExtensionsTests</c>.
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
}
