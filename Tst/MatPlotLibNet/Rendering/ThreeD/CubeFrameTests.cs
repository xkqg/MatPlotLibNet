// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>The frame of a 3-D cube (<see cref="Rendering.ThreeDAxesRenderer.CubeFrame"/>): which side of each axis the camera puts at the back, worked out
/// once, the three back panes, the nine edges that bound them and the grid lines on them.</summary>
public sealed class CubeFrameTests
{
    private static readonly Box3D Box = new(-1, 3, 0, 2, -5, 5);

    private static (ThreeDAxesRenderer.CubeFrame Frame, RecordingRenderContext Ctx, Projection3D Proj) Frame(double azimuth = -60)
    {
        var ctx = new RecordingRenderContext();
        var proj = new Projection3D(30, azimuth, new Rect(0, 0, 400, 400), Box.X.Lo, Box.X.Hi, Box.Y.Lo, Box.Y.Hi, Box.Z.Lo,
            Box.Z.Hi);
        return (new ThreeDAxesRenderer.CubeFrame(proj, Box, ctx, new ThreeDAxesRenderer.SceneVertexWriter(ctx, proj, writes: false)), ctx, proj);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(45)]
    [InlineData(135)]
    [InlineData(-145)]
    public void TheBackAndFrontOfEachAxis_AreTheFacesTheCameraSelects(double azimuth)
    {
        var (frame, _, proj) = Frame(azimuth);
        var faces = proj.Faces;

        Assert.Equal(faces.X.Coordinate(Box, faces.X.Back), frame.XBack);
        Assert.Equal(faces.X.Coordinate(Box, faces.X.Front), frame.XFront);
        Assert.Equal(faces.Y.Coordinate(Box, faces.Y.Back), frame.YBack);
        Assert.Equal(faces.Y.Coordinate(Box, faces.Y.Front), frame.YFront);
        Assert.Equal(faces.Z.Coordinate(Box, faces.Z.Back), frame.ZBack);
        Assert.Equal(faces.Z.Coordinate(Box, faces.Z.Front), frame.ZFront);
    }

    [Fact]
    public void EachPane_IsWoundAroundItsOwnBackFace()
    {
        var (frame, _, _) = Frame();
        double xb = frame.XBack, xf = frame.XFront, yb = frame.YBack, yf = frame.YFront, zb = frame.ZBack, zf = frame.ZFront;

        Assert.Equal([new Vec3(xb, yf, zb), new Vec3(xf, yf, zb), new Vec3(xf, yb, zb), new Vec3(xb, yb, zb)],
            frame.PaneQuad(CubeAxis.Z));
        Assert.Equal([new Vec3(xb, yf, zb), new Vec3(xb, yb, zb), new Vec3(xb, yb, zf), new Vec3(xb, yf, zf)],
            frame.PaneQuad(CubeAxis.X));
        Assert.Equal([new Vec3(xb, yb, zb), new Vec3(xf, yb, zb), new Vec3(xf, yb, zf), new Vec3(xb, yb, zf)],
            frame.PaneQuad(CubeAxis.Y));
    }

    [Fact]
    public void ThePanes_AreTheFloorThenTheXWallThenTheYWall_InTheirOwnColours()
    {
        var (frame, ctx, _) = Frame();
        var pane = new Pane3DConfig { FloorColor = Colors.Red, LeftWallColor = Colors.Green };

        frame.DrawPanes(pane, Colors.Blue);

        Color[] fills = [.. ctx.Of<DrawPolygonCall>().Select(c => c.Fill!.Value)];
        Assert.Equal([Colors.Red, Colors.Green, Colors.Blue], fills);
    }

    /// <summary>With no colour set, each pane wears matplotlib's own shade of its axis over a white axes (matplotlib 3.11,
    /// rcParams in both styles: <c>axes3d.xaxis.panecolor (0.95, 0.95, 0.95, 0.5)</c>, <c>axes3d.yaxis.panecolor (0.9, 0.9,
    /// 0.9, 0.5)</c>, <c>axes3d.zaxis.panecolor (0.925, 0.925, 0.925, 0.5)</c>, laid over white in 8 bits as Agg lays them:
    /// #F8F8F8, #F2F2F2, #F5F5F5, the three greys the references show). The floor first, then the X wall, then the Y wall.
    /// All three were #F5F5F5, the floor's.</summary>
    [Fact]
    public void ThePanes_FallBackOnMatplotlibsShadeOfTheirAxis_AndAreNotDrawnWhenHidden()
    {
        var (frame, ctx, _) = Frame();

        frame.DrawPanes(new Pane3DConfig(), null);
        Color[] fills = [.. ctx.Of<DrawPolygonCall>().Select(c => c.Fill!.Value)];
        Assert.Equal([Color.FromHex("#F5F5F5"), Color.FromHex("#F8F8F8"), Color.FromHex("#F2F2F2")], fills);

        var (hiddenFrame, hiddenCtx, _) = Frame();
        hiddenFrame.DrawPanes(new Pane3DConfig { Visible = false }, null);
        Assert.Empty(hiddenCtx.Of<DrawPolygonCall>());
    }

    [Fact]
    public void TheEdges_AreTheNineThatBoundTheBackPanes_HalfAPixelWide()
    {
        var (frame, ctx, _) = Frame();

        frame.DrawEdges(Colors.Gray);

        var lines = ctx.Of<DrawLineCall>().ToList();
        Assert.Equal(9, lines.Count);
        Assert.All(lines, l =>
        {
            Assert.Equal(Colors.Gray, l.Color);
            Assert.Equal(0.5, l.Thickness);
            Assert.Equal(LineStyle.Solid, l.Style);
        });
    }

    /// <summary>An axis line runs the whole edge its row's ticks stand on, one per axis, in the stroke it is handed, as
    /// matplotlib's 3-D axis draws its <c>line</c> from one edge point to the other.</summary>
    [Fact]
    public void TheAxisLines_RunTheEdgeOfEachRow_InTheStrokeTheyAreHanded()
    {
        var (frame, ctx, proj) = Frame();
        var x = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.X, Line = proj.Faces.AxisEdge(CubeAxis.X, Box) };
        var y = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.Y, Line = proj.Faces.AxisEdge(CubeAxis.Y, Box) };
        var z = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.Z, Line = proj.Faces.AxisEdge(CubeAxis.Z, Box) };
        var stroke = new StrokeStyle(Colors.Black, 1.25, LineStyle.Solid);

        frame.DrawAxisLines(x, y, z, stroke);

        var lines = ctx.Of<DrawLineCall>().ToList();
        Assert.Equal(3, lines.Count);
        var rows = new[] { x, y, z };
        for (int i = 0; i < 3; i++)
        {
            var edge = rows[i].Line;
            Assert.Equal(proj.Project(edge.From.X, edge.From.Y, edge.From.Z), lines[i].P1);
            Assert.Equal(proj.Project(edge.To.X, edge.To.Y, edge.To.Z), lines[i].P2);
            Assert.Equal(stroke, new StrokeStyle(lines[i].Color, lines[i].Thickness, lines[i].Style));
        }
    }

    [Fact]
    public void TheGrid_DrawsTwoFamiliesPerRow_AtTheTicksInsideTheEdge_AndNothingWhenHidden()
    {
        var (frame, ctx, _) = Frame();
        var x = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.X, Ticks = [-1, 0, 1, 2, 3], Edge = Box.X };
        var y = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.Y, Ticks = [0, 1, 2], Edge = Box.Y };
        var z = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.Z, Ticks = [-5, 0, 5], Edge = Box.Z };
        var grid = new GridStyle { Visible = true, Color = Colors.Green, LineWidth = 0.4 };

        frame.DrawGrid(grid, x, y, z);

        var lines = ctx.Of<DrawLineCall>().ToList();
        // X: 0, 1, 2 on the floor and the Y wall; Y: 1 on the floor and the X wall; Z: 0 on both walls.
        Assert.Equal(3 * 2 + 1 * 2 + 1 * 2, lines.Count);
        // The grid's own width, as matplotlib's Axis.draw_grid hands gridinfo['linewidth'] to its lines; no floor of a pixel.
        Assert.All(lines, l => Assert.Equal(0.4, l.Thickness));

        var (hiddenFrame, hiddenCtx, _) = Frame();
        hiddenFrame.DrawGrid(grid with { Visible = false }, x, y, z);
        Assert.Empty(hiddenCtx.Of<DrawLineCall>());
    }

    /// <summary>The wall grid is drawn in the grid's own colour, width and line style, as matplotlib's <c>Axis.draw_grid</c>
    /// sets them on its lines (<c>set_color</c>, <c>set_linewidth</c>, <c>set_linestyle</c> from the grid's settings): a dashed
    /// grid dashes the walls as it dashes the rings of a polar chart.</summary>
    [Fact]
    public void TheGrid_IsDrawnInTheGridsOwnLineStyle()
    {
        var (frame, ctx, _) = Frame();
        var row = new ThreeDAxesRenderer.CubeAxisRow { Axis = CubeAxis.X, Ticks = [0, 1], Edge = Box.X };
        var grid = new GridStyle { Visible = true, Color = Colors.Red, LineWidth = 2, LineStyle = LineStyle.Dashed };

        frame.DrawGrid(grid, row, row with { Axis = CubeAxis.Y, Edge = Box.Y }, row with { Axis = CubeAxis.Z, Edge = Box.Z });

        var lines = ctx.Of<DrawLineCall>().ToList();
        Assert.NotEmpty(lines);
        Assert.All(lines, l =>
        {
            Assert.Equal(Colors.Red, l.Color);
            Assert.Equal(2.0, l.Thickness);
            Assert.Equal(LineStyle.Dashed, l.Style);
        });
    }
}
