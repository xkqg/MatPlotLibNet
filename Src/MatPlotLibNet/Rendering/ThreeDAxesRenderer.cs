// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using System.Text;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the 3D (X, Y, Z) coordinate system with projection.</summary>
public sealed partial class ThreeDAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new 3D axes renderer.</summary>
    public ThreeDAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <summary>Where the 3-D scene stands: the projection every series is drawn through (with the camera it looks from) and the
    /// range of each axis of the cube. The square the cube fills is the plot area: the layout gives a 3-D axes the centred
    /// square of its cell (<see cref="PlotAspectExtensions"/>), as matplotlib's <c>Axes3D.apply_aspect</c> does.</summary>
    private readonly record struct Scene(Projection3D Projection, Box3D Cube);

    /// <summary>Works the scene out from the axes without drawing or measuring anything: the one place the projection is
    /// built, which <see cref="Render"/> draws through and <see cref="LegendObstacles"/> hands to a legend.</summary>
    private Scene ComputeScene()
    {
        var box = Axes.ComputeCubeRange(Theme);
        return new Scene(Axes.Camera.DrawnView.ProjectionOver(PlotArea, box), box);
    }

    /// <inheritdoc />
    /// <remarks>Every 3-D series, hidden ones too, through the projection <see cref="Render"/> draws them through; nothing is
    /// drawn.</remarks>
    internal override MarkObstacles LegendObstacles() =>
        CollectMarks(Axes.Series, SeriesPass.UnitSquare(PlotArea), new(), ComputeScene().Projection).Build();

    /// <inheritdoc />
    /// <remarks>A 3-D axes stands each patch on its plane (<see cref="Patch.Z"/> across <see cref="Patch.ZDir"/>), as matplotlib's
    /// <c>pathpatch_2d_to_3d</c> does.</remarks>
    internal override bool DrawsPatches => true;

    /// <inheritdoc />
    /// <remarks>The outline's points in data (<see cref="PathSegmentExtensions"/>' <c>Flattened</c>: a curve cut into short straight
    /// pieces) are placed on the patch's plane by matplotlib's <c>juggle_axes</c> (across X: (z, x, y); across Y: (x, z, y); across Z:
    /// (x, y, z)) and projected one by one; the patch joins the one depth queue the series draw into, at its nearest point
    /// (matplotlib's <c>PathPatch3D.do_3d_projection</c> returns <c>min(vzs)</c>). A closed outline is a polygon, an open one (an
    /// arc) a line, a filled open one a polygon under its line; in a scene the browser turns each carries its points
    /// (<c>data-v3d</c>), so it turns with the scene. A patch with a point that cannot be projected is not drawn.</remarks>
    private protected override int DrawLayerAt(in SeriesPass pass, ReadOnlySpan<LayerStep> steps, int at)
    {
        DrawPatch(pass, steps[at].Index);
        return at;
    }

    /// <summary>Draws patch <paramref name="index"/> of <see cref="Axes.Patches"/> on its plane into the pass' depth queue (the one step
    /// of a 3-D pass that is not a series: a 3-D pass draws no grid among its series).</summary>
    private void DrawPatch(in SeriesPass pass, int index)
    {
        var patch = Axes.Patches[index];
        if (!patch.Visible) return;
        var proj = pass.Projection!;
        var queue = pass.DepthQueue!;
        var flat = patch.Outline().Flattened();
        if (!flat.Points.FormsALine) return;

        var points = new Vec3[flat.Points.Length];
        var pixels = new Point[flat.Points.Length];
        double nearest = double.NegativeInfinity;
        for (int i = 0; i < points.Length; i++)
        {
            var p = flat.Points[i];
            points[i] = patch.ZDir switch
            {
                ZDirection.X => new Vec3(patch.Z, p.X, p.Y),
                ZDirection.Y => new Vec3(p.X, patch.Z, p.Y),
                _ => new Vec3(p.X, p.Y, patch.Z),
            };
            pixels[i] = proj.Project(points[i].X, points[i].Y, points[i].Z);
            if (!pixels[i].IsDrawable()) return;
            nearest = Math.Max(nearest, proj.Depth(points[i].X, points[i].Y, points[i].Z));
        }

        var drawing = new PatchDrawing(Axes, Ctx, Theme);
        var style = patch.DrawnStyleUnder(Theme);
        var vertices = new SceneVertexWriter(Ctx, proj, pass.Emit3D);
        bool closed = flat.Closed;
        queue.Add(nearest, () =>
        {
            bool grouped = drawing.OpenGroup(index);
            if (closed || style.Fill is not null)
            {
                vertices.Vertices(PinnedAxes.None, points);
                Ctx.DrawPolygon(pixels, closed ? style : style with { Stroke = null });
            }

            if (!closed && style.Stroke is not null)
            {
                vertices.Vertices(PinnedAxes.None, points);
                Ctx.DrawLines(pixels, style.OutlineStroke());
            }

            if (grouped) Ctx.EndGroup();
        });
    }

    /// <inheritdoc />
    public override void Render()
    {
        // matplotlib draws a 3-D axes' background patch at its active position, the square the layout gives it.
        FillAxesBackground();

        var (proj, box) = ComputeScene();

        // Open the scene BEFORE the axis frame (panes, edges, grid, labels, ticks), so all of it carries vertex data and turns
        // with the scene in the browser. The scene is fitted to the plot area (the square of the subplot's cell), the
        // rectangle the projection was built with. A surface without markup opens no scene and writes no vertex data.
        // The browser turns the scene from the camera the projection draws from: its distance is the one it took (at least 2).
        bool sceneGroup = Emit.VertexData3D
            && Ctx.Begin3DSceneGroup(proj.Elevation, proj.Azimuth, proj.Distance, PlotArea, proj.Faces);
        var vertices = new SceneVertexWriter(Ctx, proj, sceneGroup);

        // Three tiers in matplotlib's draw order (axes3d.py): the axis frame at the back, the series in the middle (the only
        // tier the browser's depth sort touches), the ticks in front, so a back pane never paints over a back-corner face.
        if (sceneGroup) Ctx.Begin3DSubgroup("mpl-3d-back");

        // Which three faces the camera puts at the back: the panes, the nine edges, the wall grids, the tick rows and the axis
        // titles all read it, so the frame follows the camera out of the default quadrant.
        var frame = new CubeFrame(proj, box, Ctx, vertices);
        frame.DrawPanes(Axes.Pane3D, Theme.Pane3DColor);
        if (Theme.Looks.Cube.AxisLineWidth is null) frame.DrawEdges(Colors.EdgeGray);

        // The row of each axis, worked out once for this draw: its tick values, its edge on the screen, the way out of the
        // cube and the one rule for where a mark, a label or a grid line of it stands. The grid, the axis titles and the tick
        // marks and labels all read these.
        var centroid = proj.Project((box.X.Lo + box.X.Hi) / 2, (box.Y.Lo + box.Y.Hi) / 2, (box.Z.Lo + box.Z.Hi) / 2);
        var xRow = CubeAxisRow.Of(CubeAxis.X, Axes.XAxis, box, proj, centroid, Theme, vertices, PlotArea.Width);
        var yRow = CubeAxisRow.Of(CubeAxis.Y, Axes.YAxis, box, proj, centroid, Theme, vertices, PlotArea.Width);
        var zRow = CubeAxisRow.Of(CubeAxis.Z, Axes.ZAxis, box, proj, centroid, Theme, vertices, PlotArea.Width);

        // The grid on the back faces, before the series so they paint over it; then, under a theme that bounds the box as
        // matplotlib does, the axis line of each row over it (axis3d.py draws pane, grid, then the axis and its line).
        frame.DrawGrid(Axes.WallGridUnder(Theme), xRow, yRow, zRow);
        if (Theme.Looks.Cube.AxisLineWidth is { } axisLine)
            frame.DrawAxisLines(xRow, yRow, zRow, StrokeStyle.Solid(Theme.ForegroundText, axisLine));

        // The axis titles stand on the edges their ticks stand on, pushed out far enough to clear the labels the row draws
        // (CubeAxisRow.TitlePad). The Y title sits at 0.35 along its edge, clear of the corner where the X and Y rows meet,
        // as matplotlib puts it; X and Z sit at their middles.
        var labelFont = LabelFont();
        string? xTitle = Axes.TitleOf(Axes.XAxis, Theme), yTitle = Axes.TitleOf(Axes.YAxis, Theme), zTitle = Axes.TitleOf(Axes.ZAxis, Theme);
        RenderAxisTitle(xRow, xTitle, 0.5, xRow.TitlePad(Ctx, Axes.XAxis, xTitle, labelFont), labelFont, vertices);
        RenderAxisTitle(yRow, yTitle, 0.35, yRow.TitlePad(Ctx, Axes.YAxis, yTitle, labelFont), labelFont, vertices);
        RenderAxisTitle(zRow, zTitle, 0.5, zRow.TitlePad(Ctx, Axes.ZAxis, zTitle, labelFont), labelFont, vertices);

        if (sceneGroup) { Ctx.End3DSubgroup(); Ctx.Begin3DSubgroup("mpl-3d-data"); }

        // Every 3-D series pushes what it draws into one depth queue, flushed back to front in one sort, so the faces of
        // different series interleave (a front bar row paints over a back one whatever order they were added in).
        var depthQueue = new DepthQueue3D();
        RenderSeries(proj, Axes.LightSource, depthQueue);
        depthQueue.Flush(Ctx);

        if (sceneGroup) { Ctx.End3DSubgroup(); Ctx.Begin3DSubgroup("mpl-3d-front"); }

        // The tick marks and labels last, on top of the series.
        RenderAxisEdgeTicks(Axes.XAxis, xRow, proj, vertices);
        RenderAxisEdgeTicks(Axes.YAxis, yRow, proj, vertices);
        RenderAxisEdgeTicks(Axes.ZAxis, zRow, proj, vertices);

        if (sceneGroup) { Ctx.End3DSubgroup(); Ctx.EndGroup(); }

        RenderLegend();
        RenderColorBar();
        RenderTitle();
    }

    /// <summary>Draws one axis title along the edge of its row, at <paramref name="fraction"/> of the edge, pushed out of the
    /// cube by <paramref name="padPx"/> the way the row's labels leave it. In the browser the title moves with the faces its
    /// row is pinned to, so it does not stay on the side the server picked while the ticks below it jump to the other.</summary>
    private void RenderAxisTitle(in CubeAxisRow row, string? label, double fraction, double padPx, Font font,
        SceneVertexWriter vertices)
    {
        if (label is null) return;

        var a = row.From;
        var b = row.To;
        var at = new Point(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
        var edge = row.Line;
        var anchor = new Vec3(
            edge.From.X + (edge.To.X - edge.From.X) * fraction,
            edge.From.Y + (edge.To.Y - edge.From.Y) * fraction,
            edge.From.Z + (edge.To.Z - edge.From.Z) * fraction);
        if (vertices.PadWire(padPx) is { } pad)
            vertices.TextAnchor(anchor, row.EdgeWire!, pad, OtherAxes(row.Axis));
        Ctx.Draw(TextLabel.Of(label), font, new TextPlacement(new Point(at.X + row.Outward.X * padPx, at.Y + row.Outward.Y * padPx), TextAlignment.Center));
    }

    /// <summary>
    /// Draws the major marks with their labels, and the minor marks when they are visible, along the edge of one row: each
    /// where a tick of the row stands (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.Holds"/>), out of the cube the way the row leaves it, by the
    /// axis' <see cref="TickConfig"/> (length, width, colour, label size and colour, pad) and its formatter. The minor values
    /// are those of the walk the 2-D axes take (the axis' own minor spacing, the five-way split otherwise, none on a major).
    /// A mark's two ends share the tick's point, so the browser moves it with the tick.
    /// </summary>
    private void RenderAxisEdgeTicks(Axis axis, in CubeAxisRow row, Projection3D proj, SceneVertexWriter vertices)
    {
        var major = axis.MajorTicks;
        if (!major.Visible) return;

        // Every coordinate of the row except the one it runs along sits on a selected face: those the browser flips.
        var pinned = OtherAxes(row.Axis);
        var perp = row.Outward;
        var tickColor = major.Color ?? Theme.ForegroundText;
        var labelFont = row.Labels.LabelFont(major);
        double labelOffset = CubeAxisRow.LabelOffset(major);
        string? pad = vertices.PadWire(labelOffset);

        foreach (double t in row.Ticks)
        {
            if (!row.Holds(t)) continue;

            var point = row.TickPoint(t);
            var p = proj.Project(point.X, point.Y, point.Z);
            vertices.Vertices(pinned, point, point);
            Ctx.DrawLine(p, new Point(p.X + perp.X * major.Length, p.Y + perp.Y * major.Length),
                StrokeStyle.Solid(tickColor, major.Width));

            if (pad is not null) vertices.TextAnchor(point, row.EdgeWire!, pad, pinned);
            Ctx.Draw(TextLabel.Of(row.LabelText(t)), labelFont, new TextPlacement(new Point(p.X + perp.X * labelOffset, p.Y + perp.Y * labelOffset), TextAlignment.Center));
        }

        var minor = axis.MinorTicks;
        if (!minor.Visible) return;

        var minorColor = minor.Color ?? Theme.ForegroundText;
        foreach (double t in Theme.Looks.Ticks.MinorValuesBetween(minor, row.Ticks))
        {
            if (!row.Holds(t)) continue;
            var point = row.TickPoint(t);
            var p = proj.Project(point.X, point.Y, point.Z);
            vertices.Vertices(pinned, point, point);
            Ctx.DrawLine(p, new Point(p.X + perp.X * minor.Length, p.Y + perp.Y * minor.Length),
                StrokeStyle.Solid(minorColor, minor.Width));
        }
    }

    /// <summary>The compact <c>data-v3d-pinned</c> attribute value: the axis letters, in x-y-z order.</summary>
    /// <param name="axes">The <see cref="Rendering.ThreeDAxesRenderer.PinnedAxes"/> it works on.</param>
    internal static string ToWire(PinnedAxes axes)
    {
        Span<char> buffer = stackalloc char[3];
        int length = 0;
        if (axes.HasFlag(PinnedAxes.X)) buffer[length++] = 'x';
        if (axes.HasFlag(PinnedAxes.Y)) buffer[length++] = 'y';
        if (axes.HasFlag(PinnedAxes.Z)) buffer[length++] = 'z';
        return new string(buffer[..length]);
    }

    /// <summary>The two axes other than this one — the components a tick row along it is pinned to.</summary>
    /// <param name="axis">The <see cref="CubeAxis"/> it works on.</param>
    private static PinnedAxes OtherAxes(CubeAxis axis) => axis switch
    {
        CubeAxis.X => PinnedAxes.Y | PinnedAxes.Z,
        CubeAxis.Y => PinnedAxes.X | PinnedAxes.Z,
        _ => PinnedAxes.X | PinnedAxes.Y,
    };

    /// <summary>
    /// Which coordinate components of an emitted 3-D vertex sit ON a camera-selected cube face, and
    /// therefore have to be mirrored when the browser re-runs the face selection mid-drag. In the
    /// centred normalized space the SVG carries, the opposite plane of an axis is the exact negation of
    /// that component, so "mirror" is a sign flip and needs no extra geometry on the wire.
    /// </summary>
    [Flags]
    internal enum PinnedAxes
    {
        /// <summary>Nothing on this element follows the face selection.</summary>
        None = 0,

        /// <summary>The X component sits on the selected X plane.</summary>
        X = 1,

        /// <summary>The Y component sits on the selected Y plane.</summary>
        Y = 2,

        /// <summary>The Z component sits on the selected Z plane.</summary>
        Z = 4,
    }

    /// <summary>Writes the vertex data a browser turns a 3-D scene with, onto the next element the scene draws: its points
    /// normalised by the projection (<c>data-v3d</c>), which of their components sit on a face the camera selects
    /// (<c>data-v3d-pinned</c>), and for a text the edge it stands beside and its pad (<c>data-v3d-edge</c>, <c>data-pad</c>).
    /// A scene that writes no vertex data (no browser asked for it, or a surface without markup) sets nothing.</summary>
    /// <remarks>Every point is written in the digits of the frame (<see cref="Normalized3DPoint.FrameDigits"/>) into one builder the
    /// scene keeps for its whole draw, so a point costs no string of its own.</remarks>
    internal readonly struct SceneVertexWriter
    {
        private readonly IRenderContext _ctx;
        private readonly Projection3D _proj;

        /// <summary>The builder the wire is written into; <see langword="null"/> when the scene writes no vertex data.</summary>
        private readonly StringBuilder? _wire;

        /// <summary>A writer for one draw of a scene.</summary>
        /// <param name="ctx">The context the scene draws into.</param>
        /// <param name="proj">The projection the scene is drawn through.</param>
        /// <param name="writes">Whether the scene writes vertex data.</param>
        internal SceneVertexWriter(IRenderContext ctx, Projection3D proj, bool writes)
        {
            _ctx = ctx;
            _proj = proj;
            _wire = writes ? new StringBuilder(96) : null;
        }

        /// <summary>Whether the scene writes vertex data.</summary>
        internal bool Writes => _wire is not null;

        /// <summary>The points normalised by the projection, each as <c>nx,ny,nz</c> in the digits of the frame, separated by a
        /// space. Only for a scene that writes vertex data.</summary>
        /// <param name="points">The points, in data units.</param>
        /// <returns>The wire.</returns>
        internal string Wire(params ReadOnlySpan<Vec3> points)
        {
            var sb = _wire!.Clear();
            Span<char> point = stackalloc char[Normalized3DPoint.PointLength];
            for (int i = 0; i < points.Length; i++)
            {
                var n = _proj.Normalize(points[i].X, points[i].Y, points[i].Z);
                if (i > 0) sb.Append(' ');
                sb.Append(point[..n.FormatWire(point, Normalized3DPoint.FrameDigits)]);
            }
            return sb.ToString();
        }

        /// <summary>Gives the next drawn element its points, and the components of them that sit on a selected cube face: the
        /// browser re-runs the face selection every frame and negates exactly those components, so panes, edges, grid lines and
        /// tick rows jump to the other side mid-drag instead of staying behind.</summary>
        /// <param name="pinned">The components that sit on a selected face; <see cref="Rendering.ThreeDAxesRenderer.PinnedAxes.None"/> writes none.</param>
        /// <param name="points">The element's points, in data units.</param>
        internal void Vertices(PinnedAxes pinned, params ReadOnlySpan<Vec3> points)
        {
            if (_wire is null) return;
            _ctx.SetNextElementData("v3d", Wire(points));
            if (pinned != PinnedAxes.None)
                _ctx.SetNextElementData("v3d-pinned", ToWire(pinned));
        }

        /// <summary>The pad of a text as the browser reads it: the pixel distance in the digits of the frame; <see langword="null"/>
        /// for a scene that writes no vertex data.</summary>
        /// <param name="pad">The pixel distance from the edge.</param>
        /// <returns>The text of the pad.</returns>
        internal string? PadWire(double pad) => _wire is null ? null : pad.InvariantText(Normalized3DPoint.FrameDigits);

        /// <summary>Gives the next drawn text its anchor, the edge it stands beside and its pad, so the browser keeps it that far
        /// outside the cube on the perpendicular of the edge as the scene turns, rather than on the edge itself.</summary>
        /// <param name="anchor">The point the text names, in data units.</param>
        /// <param name="edgeWire">The edge's two ends as a wire (<see cref="Rendering.ThreeDAxesRenderer.SceneVertexWriter.Wire"/>), written once for its row.</param>
        /// <param name="pad">The pixel distance from the edge, as <see cref="Rendering.ThreeDAxesRenderer.SceneVertexWriter.PadWire"/> writes it.</param>
        /// <param name="pinned">The components of the anchor that sit on a selected face.</param>
        internal void TextAnchor(Vec3 anchor, string edgeWire, string pad, PinnedAxes pinned)
        {
            if (_wire is null) return;
            Vertices(pinned, anchor);
            _ctx.SetNextElementData("v3d-edge", edgeWire);
            _ctx.SetNextElementData("pad", pad);
        }
    }

    /// <summary>The frame of a 3-D cube in one draw: which side of each axis the camera puts at the back, worked out once, and
    /// what stands on those back faces, the shaded panes, the nine edges that bound them or the three axis lines matplotlib draws
    /// instead, and the grid lines on them. Every line
    /// and pane carries its vertex data (<see cref="Rendering.ThreeDAxesRenderer.SceneVertexWriter"/>), so a browser turns it with the scene.</summary>
    internal readonly struct CubeFrame
    {
        private readonly Projection3D _proj;
        private readonly IRenderContext _ctx;
        private readonly SceneVertexWriter _vertices;

        /// <summary>The frame of <paramref name="box"/> as <paramref name="proj"/> sees it.</summary>
        /// <param name="proj">The projection the scene is drawn through; its face selection decides the back faces.</param>
        /// <param name="box">The cube, in data units.</param>
        /// <param name="ctx">The context the frame is drawn into.</param>
        /// <param name="vertices">The scene's vertex data writer.</param>
        internal CubeFrame(Projection3D proj, Box3D box, IRenderContext ctx, SceneVertexWriter vertices)
        {
            _proj = proj;
            _ctx = ctx;
            _vertices = vertices;
            Box = box;
            var faces = proj.Faces;
            XBack = faces.X.Coordinate(box, faces.X.Back);
            XFront = faces.X.Coordinate(box, faces.X.Front);
            YBack = faces.Y.Coordinate(box, faces.Y.Back);
            YFront = faces.Y.Coordinate(box, faces.Y.Front);
            ZBack = faces.Z.Coordinate(box, faces.Z.Back);
            ZFront = faces.Z.Coordinate(box, faces.Z.Front);
        }

        /// <summary>The cube.</summary>
        internal Box3D Box { get; }

        /// <summary>The X of the wall the camera puts at the back.</summary>
        internal double XBack { get; }

        /// <summary>The X of the open side facing the camera.</summary>
        internal double XFront { get; }

        /// <summary>The Y of the wall the camera puts at the back.</summary>
        internal double YBack { get; }

        /// <summary>The Y of the open side facing the camera.</summary>
        internal double YFront { get; }

        /// <summary>The Z of the floor the camera puts at the back.</summary>
        internal double ZBack { get; }

        /// <summary>The Z of the open top.</summary>
        internal double ZFront { get; }

        /// <summary>Shades the three back faces, the floor first, then the X wall, then the Y wall, the paint order the browser's
        /// depth sort relies on (it skips the <c>mpl-pane</c> class). The colour is bound to the axis, never to a side, so a wall
        /// keeps its colour when it swaps sides. A pane given no colour, under a theme that names none, wears matplotlib's shade of
        /// its axis (<see cref="Rendering.ThreeDAxesRenderer.CubeFrame.DefaultPaneColor"/>).</summary>
        /// <param name="pane">The axes' pane settings.</param>
        /// <param name="themeColor">The theme's pane colour, when it sets one.</param>
        internal void DrawPanes(Pane3DConfig pane, Color? themeColor)
        {
            if (!pane.Visible) return;

            foreach (var axis in (ReadOnlySpan<CubeAxis>)[CubeAxis.Z, CubeAxis.X, CubeAxis.Y])
            {
                var quad = PaneQuad(axis);
                _vertices.Vertices(PinnedAxes.X | PinnedAxes.Y | PinnedAxes.Z, quad);
                _ctx.SetNextElementClass("mpl-pane");
                _ctx.DrawPolygon(
                    [_proj.Project(quad[0].X, quad[0].Y, quad[0].Z), _proj.Project(quad[1].X, quad[1].Y, quad[1].Z),
                     _proj.Project(quad[2].X, quad[2].Y, quad[2].Z), _proj.Project(quad[3].X, quad[3].Y, quad[3].Z)],
                    ShapeStyle.Filled((pane.ColorFor(axis) ?? themeColor ?? DefaultPaneColor(axis)).ScaleAlpha(pane.Alpha)));
            }
        }

        /// <summary>matplotlib's shade of the pane of <paramref name="axis"/> laid over a white axes, as its Agg backend lays it in
        /// eight bits: <c>axes3d.xaxis.panecolor</c> (0.95, 0.95, 0.95, 0.5) gives #F8F8F8, <c>axes3d.yaxis.panecolor</c> (0.9,
        /// 0.9, 0.9, 0.5) #F2F2F2 and <c>axes3d.zaxis.panecolor</c> (0.925, 0.925, 0.925, 0.5) #F5F5F5, the same in matplotlib's
        /// default and classic styles.</summary>
        /// <param name="axis">The axis the pane is the back wall of (<see cref="CubeAxis.Z"/>: the floor).</param>
        /// <returns>The opaque grey of that pane.</returns>
        internal static Color DefaultPaneColor(CubeAxis axis) => axis switch
        {
            CubeAxis.X => XPane,
            CubeAxis.Y => YPane,
            _ => ZPane,
        };

        private static readonly Color XPane = Color.FromHex("#F8F8F8");
        private static readonly Color YPane = Color.FromHex("#F2F2F2");
        private static readonly Color ZPane = Color.FromHex("#F5F5F5");

        /// <summary>The four corners of one back pane, wound so the polygon is convex on screen.</summary>
        /// <param name="axis">The axis the pane is the back wall of (<see cref="CubeAxis.Z"/>: the floor).</param>
        /// <returns>The corners.</returns>
        internal Vec3[] PaneQuad(CubeAxis axis) => axis switch
        {
            CubeAxis.Z => [new(XBack, YFront, ZBack), new(XFront, YFront, ZBack), new(XFront, YBack, ZBack), new(XBack, YBack, ZBack)],
            CubeAxis.X => [new(XBack, YFront, ZBack), new(XBack, YBack, ZBack), new(XBack, YBack, ZFront), new(XBack, YFront, ZFront)],
            _ => [new(XBack, YBack, ZBack), new(XFront, YBack, ZBack), new(XFront, YBack, ZFront), new(XBack, YBack, ZFront)],
        };

        /// <summary>Draws the nine edges that bound the back panes, half a pixel wide: the four of the floor, the top edge of each
        /// wall, and the three verticals that touch a wall. The other three edges form the cube's front silhouette and would cut
        /// across the data, so matplotlib leaves them out too.</summary>
        /// <param name="color">The edge colour.</param>
        internal void DrawEdges(Color color)
        {
            double x0 = Box.X.Lo, x1 = Box.X.Hi;
            DrawEdge(new(x0, YFront, ZBack), new(x1, YFront, ZBack), PinnedAxes.Y | PinnedAxes.Z, color);
            DrawEdge(new(x1, YFront, ZBack), new(x1, YBack, ZBack), PinnedAxes.Z, color);
            DrawEdge(new(x1, YBack, ZBack), new(x0, YBack, ZBack), PinnedAxes.Y | PinnedAxes.Z, color);
            DrawEdge(new(x0, YBack, ZBack), new(x0, YFront, ZBack), PinnedAxes.Z, color);

            DrawEdge(new(x1, YBack, ZFront), new(x0, YBack, ZFront), PinnedAxes.Y | PinnedAxes.Z, color);
            DrawEdge(new(XBack, YBack, ZFront), new(XBack, YFront, ZFront), PinnedAxes.X | PinnedAxes.Z, color);

            DrawEdge(new(XBack, YFront, ZBack), new(XBack, YFront, ZFront), PinnedAxes.X | PinnedAxes.Y, color);
            DrawEdge(new(XFront, YBack, ZBack), new(XFront, YBack, ZFront), PinnedAxes.X | PinnedAxes.Y, color);
            DrawEdge(new(XBack, YBack, ZBack), new(XBack, YBack, ZFront), PinnedAxes.X | PinnedAxes.Y, color);
        }

        /// <summary>Draws one axis line per axis along the edge its row's ticks stand on, as matplotlib's 3-D axis draws its
        /// <c>line</c>, pinned as the row's ticks are, so a browser turns it with them.</summary>
        /// <param name="x">The X row.</param>
        /// <param name="y">The Y row.</param>
        /// <param name="z">The Z row.</param>
        /// <param name="stroke">The axis lines' colour, width and line style.</param>
        internal void DrawAxisLines(in CubeAxisRow x, in CubeAxisRow y, in CubeAxisRow z, StrokeStyle stroke)
        {
            DrawAxisLine(x, stroke);
            DrawAxisLine(y, stroke);
            DrawAxisLine(z, stroke);
        }

        private void DrawAxisLine(in CubeAxisRow row, StrokeStyle stroke) =>
            DrawLine(row.Line.From, row.Line.To, OtherAxes(row.Axis), stroke);

        private void DrawEdge(Vec3 from, Vec3 to, PinnedAxes pinned, Color color) =>
            DrawLine(from, to, pinned, StrokeStyle.Solid(color, LineWidthRule.OwnHairline.Resting));

        /// <summary>Draws the grid lines on the three back faces, at the grid ticks of each row where a grid line stands
        /// (<see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.GridTicks"/>, <see cref="Rendering.ThreeDAxesRenderer.CubeAxisRow.HoldsGridLine"/>), so a line never stands where its axis shows
        /// no mark: the major ticks, and under the matplotlib themes the minor ones of an axis whose minor ticks are on, in the grid's own
        /// colour, width and line style: matplotlib's <c>Axis.draw_grid</c> hands its lines those and nothing else. Two families per axis: X on the floor and the Y wall, Y on
        /// the floor and the X wall, Z on the X wall and the Y wall.</summary>
        /// <param name="grid">The grid the walls wear (<see cref="AxesGridExtensions.WallGridUnder"/>); a hidden one draws nothing.</param>
        /// <param name="x">The X row.</param>
        /// <param name="y">The Y row.</param>
        /// <param name="z">The Z row.</param>
        internal void DrawGrid(GridStyle grid, in CubeAxisRow x, in CubeAxisRow y, in CubeAxisRow z)
        {
            if (!grid.Visible) return;

            var stroke = grid.Stroke;
            double x0 = Box.X.Lo, x1 = Box.X.Hi;
            double y0 = Box.Y.Lo, y1 = Box.Y.Hi;
            double z0 = Box.Z.Lo, z1 = Box.Z.Hi;

            foreach (var t in x.GridTicks)
                if (x.HoldsGridLine(t)) DrawLine(new(t, y0, ZBack), new(t, y1, ZBack), PinnedAxes.Z, stroke);
            foreach (var t in y.GridTicks)
                if (y.HoldsGridLine(t)) DrawLine(new(x0, t, ZBack), new(x1, t, ZBack), PinnedAxes.Z, stroke);

            foreach (var t in y.GridTicks)
                if (y.HoldsGridLine(t)) DrawLine(new(XBack, t, z0), new(XBack, t, z1), PinnedAxes.X, stroke);
            foreach (var t in z.GridTicks)
                if (z.HoldsGridLine(t)) DrawLine(new(XBack, y0, t), new(XBack, y1, t), PinnedAxes.X, stroke);

            foreach (var t in x.GridTicks)
                if (x.HoldsGridLine(t)) DrawLine(new(t, YBack, z0), new(t, YBack, z1), PinnedAxes.Y, stroke);
            foreach (var t in z.GridTicks)
                if (z.HoldsGridLine(t)) DrawLine(new(x0, YBack, t), new(x1, YBack, t), PinnedAxes.Y, stroke);
        }

        /// <summary>A line between two points of the scene, with its vertex data.</summary>
        /// <param name="from">One end, in data units.</param>
        /// <param name="to">The other end.</param>
        /// <param name="pinned">The components that sit on a selected face.</param>
        /// <param name="stroke">The colour, width and dash pattern of the line.</param>
        internal void DrawLine(Vec3 from, Vec3 to, PinnedAxes pinned, StrokeStyle stroke)
        {
            _vertices.Vertices(pinned, from, to);
            _ctx.DrawLine(_proj.Project(from.X, from.Y, from.Z), _proj.Project(to.X, to.Y, to.Z), stroke);
        }
    }
}
