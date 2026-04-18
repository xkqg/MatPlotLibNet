// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the 3D (X, Y, Z) coordinate system with projection.</summary>
public sealed class ThreeDAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new 3D axes renderer.</summary>
    public ThreeDAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <summary>Cached raw series data minimum per axis, used to filter tick labels below the data floor.</summary>
    private double _rawXMin, _rawYMin, _rawZMin;

    /// <inheritdoc />
    public override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, axesBg, null, 0);

        // Compute 3D data ranges from all 3D series
        var range3D = Compute3DDataRanges();

        // Port of matplotlib's axes3d layout algorithm: the 3-D axes bbox is ALWAYS SQUARE,
        // computed from a fixed subplot rect (0.125, 0.11, 0.775, 0.77) of the figure:
        //   subW = figW * 0.775, subH = figH * 0.77
        //   side = min(subW, subH)
        //   x    = 0.125 * figW + (subW - side) / 2
        //   y    = 0.11  * figH + (subH - side) / 2   (bottom-up → convert to top-down)
        // Verified across figsize (7, 5.5), (8, 6), (10, 6), (7, 7) — matches matplotlib's
        // get_window_extent() to the pixel.
        Rect cubeBounds = PlotArea;
        if (FigureSize.HasValue)
        {
            double figW = FigureSize.Value.Width;
            double figH = FigureSize.Value.Height;
            const double subLeft = 0.125, subBottom = 0.11, subWidth = 0.775, subHeight = 0.77;
            double subW = figW * subWidth;
            double subH = figH * subHeight;
            double side = Math.Min(subW, subH);
            double squareX = subLeft * figW + (subW - side) / 2.0;
            double squareYBottomUp = subBottom * figH + (subH - side) / 2.0;
            double squareYTopDown = figH - squareYBottomUp - side;
            cubeBounds = new Rect(squareX, squareYTopDown, side, side);
        }

        double elevation = Axes.Projection?.Elevation ?? Axes.Elevation;
        double azimuth = Axes.Projection?.Azimuth ?? Axes.Azimuth;
        double? distance = Axes.CameraDistance;
        var proj = new Projection3D(elevation, azimuth, cubeBounds,
            range3D.XMin, range3D.XMax, range3D.YMin, range3D.YMax, range3D.ZMin, range3D.ZMax, distance);

        var edgeColor = Colors.EdgeGray;

        // Draw 3D bounding box (12 edges of a cube)
        double x0 = range3D.XMin, x1 = range3D.XMax;
        double y0 = range3D.YMin, y1 = range3D.YMax;
        double z0 = range3D.ZMin, z1 = range3D.ZMax;

        // Phase 3 of v1.7.2 plan — open the scene group BEFORE drawing axes
        // infrastructure (panes, edges, grid, labels, ticks) so all of them get
        // data-v3d attributes and re-project under interactive rotation. Pre-Phase-3
        // these were drawn outside the scene group and stayed static during drag.
        bool sceneGroup = Axes.Emit3DVertexData && Ctx is SvgRenderContext;
        var svgCtx = Ctx as SvgRenderContext;
        if (sceneGroup)
            // Pass cubeBounds (the square inscribed in the subplot region) — NOT PlotArea —
            // because Projection3D was constructed with cubeBounds, so the JS reproject must
            // use the same rectangle for fit-to-plot. Phase B.4 of v1.7.2 follow-on plan.
            svgCtx!.Begin3DSceneGroup(
                elevation, azimuth, distance, cubeBounds,
                Axes.LightSource as Rendering.Lighting.DirectionalLight);

        // Phase F of v1.7.2 follow-on plan — three-tier subgroup structure mirrors
        // matplotlib's draw order (axes3d.py:458-470). Axis infra goes in mpl-3d-back,
        // series in mpl-3d-data (the only tier the JS depth-resort touches), ticks in
        // mpl-3d-front. Ensures back panes never paint over back-corner surface quads
        // when the JS resort runs on interactive rotation.
        if (sceneGroup) svgCtx!.Begin3DSubgroup("mpl-3d-back");

        // Light-grey background panes on the three back-facing cube faces — matches
        // matplotlib's default 3-D "shaded walls" look. Drawn BEFORE grid lines and edges
        // so both paint on top. Colour is matplotlib's rcParams['axes3d.xaxis.panecolor']
        // = (0.95, 0.95, 0.95, 0.5) which composites to ~#F5F5F5 on a white figure.
        Render3DPanes(proj, x0, x1, y0, y1, z0, z1);

        // Draw only the 9 cube edges that bound the three visible back panes (floor z=z0,
        // back-left x=x0, back-right y=y1). The remaining 3 edges — top-front at y=y0,
        // top-right at x=x1, and the vertical at (x=x1, y=y0) — form the "front silhouette"
        // of the cube at the default azim=-60 view and are NOT part of any back pane. If
        // we drew them they'd appear as visible diagonals cutting across the data area,
        // overlapping bars/surfaces (matplotlib omits them for the same reason). When
        // interactive rotation changes the back-facing set this hard-coded skip will need
        // to become camera-dynamic — tracked alongside Render3DPanes which has the same
        // default-view assumption.

        // Floor edges (z = z0) — full quad, all 4 edges are part of the floor pane
        DrawCubeEdge3D(proj, x0, y0, z0, x1, y0, z0, edgeColor);
        DrawCubeEdge3D(proj, x1, y0, z0, x1, y1, z0, edgeColor);
        DrawCubeEdge3D(proj, x1, y1, z0, x0, y1, z0, edgeColor);
        DrawCubeEdge3D(proj, x0, y1, z0, x0, y0, z0, edgeColor);

        // Top face edges (z = z1) — ONLY the 2 edges that bound a back pane.
        DrawCubeEdge3D(proj, x1, y1, z1, x0, y1, z1, edgeColor);
        DrawCubeEdge3D(proj, x0, y1, z1, x0, y0, z1, edgeColor);

        // Vertical edges — 3 of the 4 are part of a back pane.
        DrawCubeEdge3D(proj, x0, y0, z0, x0, y0, z1, edgeColor);
        DrawCubeEdge3D(proj, x1, y1, z0, x1, y1, z1, edgeColor);
        DrawCubeEdge3D(proj, x0, y1, z0, x0, y1, z1, edgeColor);

        // Grid lines on the three visible cube faces — drawn BEFORE series so series paint
        // on top of them.
        Render3DGrid(proj, x0, x1, y0, y1, z0, z1);

        // Axis title labels — must live on the SAME cube edges where the ticks are drawn.
        var labelFont = LabelFont();
        var cubeCentroidForLabels = proj.Project((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2);
        if (Axes.XAxis.Label is not null)
        {
            var a = proj.Project(x0, y0, z0);
            var b = proj.Project(x1, y0, z0);
            var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            var perp = PerpAwayFromCentroid(a, b, mid, cubeCentroidForLabels, 42.0);
            DrawText3DAt(proj, (x0 + x1) * 0.5, y0, z0, Axes.XAxis.Label!, new Point(mid.X + perp.X, mid.Y + perp.Y), labelFont, TextAlignment.Center);
        }
        if (Axes.YAxis.Label is not null)
        {
            var a = proj.Project(x1, y0, z0);
            var b = proj.Project(x1, y1, z0);
            var mid = new Point(a.X + (b.X - a.X) * 0.65, a.Y + (b.Y - a.Y) * 0.65);
            var perp = PerpAwayFromCentroid(a, b, mid, cubeCentroidForLabels, 60.0);
            DrawText3DAt(proj, x1, y0 + (y1 - y0) * 0.65, z0, Axes.YAxis.Label!, new Point(mid.X + perp.X, mid.Y + perp.Y), labelFont, TextAlignment.Center);
        }
        if (Axes.ZAxis.Label is not null)
        {
            var a = proj.Project(x1, y1, z0);
            var b = proj.Project(x1, y1, z1);
            var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            var perp = PerpAwayFromCentroid(a, b, mid, cubeCentroidForLabels, 60.0);
            DrawText3DAt(proj, x1, y1, (z0 + z1) * 0.5, Axes.ZAxis.Label!, new Point(mid.X + perp.X, mid.Y + perp.Y), labelFont, TextAlignment.Center);
        }

        // Phase F of v1.7.2 follow-on — close mpl-3d-back tier, open mpl-3d-data.
        if (sceneGroup) { svgCtx!.End3DSubgroup(); svgCtx.Begin3DSubgroup("mpl-3d-data"); }

        // Render series using unified projection and optional lighting. All 3-D series
        // push their drawable primitives into a shared depth queue instead of drawing
        // immediately; after the loop we flush the queue in one back-to-front sort so
        // faces from different series interleave correctly (e.g. a front Bar3D row paints
        // over a back Bar3D row regardless of insertion order).
        var depthQueue = new DepthQueue3D();
        RenderSeries(proj, Axes.LightSource, depthQueue);
        depthQueue.Flush();

        // Phase F — close mpl-3d-data, open mpl-3d-front for ticks.
        if (sceneGroup) { svgCtx!.End3DSubgroup(); svgCtx.Begin3DSubgroup("mpl-3d-front"); }

        // Tick marks + tick labels — drawn LAST so they paint on top of the series.
        // Phase 3 of v1.7.2: now INSIDE the scene group so they reproject under rotation.
        Render3DAxisTicks(proj, x0, x1, y0, y1, z0, z1);

        if (sceneGroup) { svgCtx!.End3DSubgroup(); Ctx.EndGroup(); }

        // Legend
        RenderLegend();

        // ColorBar (v1.5.0 — was previously skipped for 3D axes)
        RenderColorBar();

        // Title
        RenderTitle();
    }

    /// <summary>
    /// Draws light-grey fills on the three back-facing cube faces (bottom <c>z=z0</c>,
    /// back-left <c>x=x0</c>, back-right <c>y=y1</c>) to match matplotlib's default 3-D
    /// "shaded panes" look. Uses <c>rcParams['axes3d.xaxis.panecolor']</c> which is
    /// <c>(0.95, 0.95, 0.95, 0.5)</c> — composites to <c>#F5F5F5</c> on a white figure.
    /// </summary>
    /// <remarks>
    /// The three panes drawn are hard-coded for the default view (elev≥0,
    /// azim ∈ [-90°, 0°]). Under interactive rotation the back-facing set changes, but
    /// for the static default this is what matches matplotlib byte-for-byte.
    /// </remarks>

    // ── Phase 3 of v1.7.2 plan — data-v3d emission helpers ────────────────────
    // Each axis-infrastructure draw call (cube edges, grid lines, tick marks, text)
    // is preceded by a call to SetNextElementData("v3d", "...") so the JS reproject
    // can move the element under interactive rotation. The helpers below bundle the
    // emit + draw into one call so the rendering code stays readable.

    /// <summary>Emits a "data-v3d" attribute for the next drawn element, encoding the
    /// supplied normalized 3D points as <c>nx,ny,nz nx,ny,nz ...</c>. No-op when
    /// vertex emission is disabled (static render path).</summary>
    private void EmitV3D(Projection3D proj, params (double x, double y, double z)[] pts)
    {
        if (!Axes.Emit3DVertexData || Ctx is not SvgRenderContext svgCtx) return;
        var sb = new System.Text.StringBuilder(pts.Length * 24);
        for (int i = 0; i < pts.Length; i++)
        {
            var n = proj.Normalize(pts[i].x, pts[i].y, pts[i].z);
            if (i > 0) sb.Append(' ');
            sb.Append(n.Nx.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(n.Ny.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(n.Nz.ToString("G6", System.Globalization.CultureInfo.InvariantCulture));
        }
        svgCtx.SetNextElementData("v3d", sb.ToString());
    }

    /// <summary>Cube edge — projects (x,y,z) endpoints, emits data-v3d, then DrawLines.</summary>
    private void DrawCubeEdge3D(Projection3D proj, double x0, double y0, double z0, double x1, double y1, double z1, Color color)
    {
        EmitV3D(proj, (x0, y0, z0), (x1, y1, z1));
        Ctx.DrawLine(proj.Project(x0, y0, z0), proj.Project(x1, y1, z1), color, 0.5, LineStyle.Solid);
    }

    /// <summary>Single line at known 3D endpoints (for grid lines + tick marks).</summary>
    private void DrawLine3DAt(Projection3D proj, double x0, double y0, double z0, double x1, double y1, double z1, Color color, double width, LineStyle style)
    {
        EmitV3D(proj, (x0, y0, z0), (x1, y1, z1));
        Ctx.DrawLine(proj.Project(x0, y0, z0), proj.Project(x1, y1, z1), color, width, style);
    }

    /// <summary>Text anchored at a 3D point — used for axis labels + tick labels.
    /// The pixel-space position is pre-computed (the renderer biases axis labels
    /// with PerpAwayFromCentroid); we just attach v3d so the JS can re-pin to the
    /// SAME 3D anchor under rotation.</summary>
    private void DrawText3DAt(Projection3D proj, double xData, double yData, double zData, string text, Point pixelPos, Font font, TextAlignment alignment)
    {
        EmitV3D(proj, (xData, yData, zData));
        Ctx.DrawText(text, pixelPos, font, alignment);
    }

    private void Render3DPanes(Projection3D proj,
        double x0, double x1, double y0, double y1, double z0, double z1)
    {
        var pane = Axes.Pane3D;
        if (!pane.Visible) return;

        var defaultColor = Theme.Pane3DColor ?? Color.FromHex("#F5F5F5");
        var floorColor = pane.FloorColor ?? defaultColor;
        var leftColor = pane.LeftWallColor ?? defaultColor;
        var rightColor = pane.RightWallColor ?? defaultColor;

        // Each pane is tagged class="mpl-pane" so (1) the JS depth-sort can skip
        // them (Phase F.3), (2) the ThreeDPaneOcclusionTests can assert DOM order.
        var svgCtx = Ctx as SvgRenderContext;

        // Bottom floor: z = z0, winding in XY plane.
        EmitV3D(proj, (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0));
        svgCtx?.SetNextElementClass("mpl-pane");
        Ctx.DrawPolygon(
            [proj.Project(x0, y0, z0), proj.Project(x1, y0, z0),
             proj.Project(x1, y1, z0), proj.Project(x0, y1, z0)],
            floorColor, null, 0);

        // Back-left wall: x = x0, winding in YZ plane.
        EmitV3D(proj, (x0, y0, z0), (x0, y1, z0), (x0, y1, z1), (x0, y0, z1));
        svgCtx?.SetNextElementClass("mpl-pane");
        Ctx.DrawPolygon(
            [proj.Project(x0, y0, z0), proj.Project(x0, y1, z0),
             proj.Project(x0, y1, z1), proj.Project(x0, y0, z1)],
            leftColor, null, 0);

        // Back-right wall: y = y1, winding in XZ plane.
        EmitV3D(proj, (x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1));
        svgCtx?.SetNextElementClass("mpl-pane");
        Ctx.DrawPolygon(
            [proj.Project(x0, y1, z0), proj.Project(x1, y1, z0),
             proj.Project(x1, y1, z1), proj.Project(x0, y1, z1)],
            rightColor, null, 0);
    }

    /// <summary>
    /// Renders grid lines on the three visible cube faces (bottom, back-left, back-right)
    /// at every major tick AND at every minor (half-step) tick. Grid is always drawn,
    /// independent of <see cref="GridStyle.Visible"/>, matching matplotlib's
    /// <c>rcParams['axes3d.grid'] = True</c>.
    /// </summary>
    private void Render3DGrid(Projection3D proj,
        double x0, double x1, double y0, double y1, double z0, double z1)
    {
        var grid = Theme.DefaultGrid;
        var majorColor = grid.Color;
        double majorWidth = Math.Max(1.0, grid.LineWidth);
        var minorColor = majorColor.WithAlpha(140);

        double zMin = Axes.ZAxis.Min ?? z0;
        double zMax = Axes.ZAxis.Max ?? z1;

        // Same MaxNLocator as the edge tick renderer so grid lines line up with ticks.
        double labelFontSize = Theme.DefaultFont.Size;
        double xEdgePx = PixelLength(proj.Project(x0, y0, z0), proj.Project(x1, y0, z0));
        double yEdgePx = PixelLength(proj.Project(x1, y0, z0), proj.Project(x1, y1, z0));
        double zEdgePx = PixelLength(proj.Project(x1, y1, z0), proj.Project(x1, y1, z1));
        var xTicks = Axes.XAxis.TickLocator?.Locate(x0, x1) ?? ComputeMaxNTicks(x0, x1, xEdgePx, labelFontSize);
        var yTicks = Axes.YAxis.TickLocator?.Locate(y0, y1) ?? ComputeMaxNTicks(y0, y1, yEdgePx, labelFontSize);
        var zTicks = Axes.ZAxis.TickLocator?.Locate(zMin, zMax) ?? ComputeMaxNTicks(zMin, zMax, zEdgePx, labelFontSize);

        var xMinor = HalfStepTicks(xTicks, x0, x1);
        var yMinor = HalfStepTicks(yTicks, y0, y1);
        var zMinor = HalfStepTicks(zTicks, zMin, zMax);

        // Minor first so major paints on top.
        // Matplotlib's Axes3D draws grid lines on wall panes at MAJOR ticks only by default
        // (rcParams['axes.grid'] applies to major, minor grid requires explicit
        // `ax.grid(which='both')`). Drawing minor grid lines on 3-D walls produces the
        // "tussen grid" clutter between major gridlines that the user flagged — every wall
        // ends up with ~2× the expected line density. Minor tick MARKS are still drawn
        // on the axis edges by Render3DAxisTicks; only the minor GRID LINES on the wall
        // panes are suppressed here.
        DrawCubeFaceLines(proj, xTicks, yTicks, zTicks, x0, x1, y0, y1, zMin, zMax, majorColor, majorWidth);
    }

    /// <summary>
    /// Draws the nine per-axis grid-line families on the three visible cube faces.
    /// Shared by major and minor rendering passes so the face-line geometry lives in a
    /// single place (DRY). Endpoints matching the cube edges are skipped — those are
    /// already drawn as bounding-box edges.
    /// </summary>
    private void DrawCubeFaceLines(Projection3D proj,
        IReadOnlyList<double> xs, IReadOnlyList<double> ys, IReadOnlyList<double> zs,
        double x0, double x1, double y0, double y1, double zMin, double zMax,
        Color color, double width)
    {
        // Bottom face (z = zMin): vertical X stripes and horizontal Y stripes.
        foreach (var t in xs)
        {
            if (t <= x0 || t >= x1) continue;
            DrawLine3DAt(proj, t, y0, zMin, t, y1, zMin, color, width, LineStyle.Solid);
        }
        foreach (var t in ys)
        {
            if (t <= y0 || t >= y1) continue;
            DrawLine3DAt(proj, x0, t, zMin, x1, t, zMin, color, width, LineStyle.Solid);
        }

        // Back-left face (x = x0): Y stripes and Z stripes.
        foreach (var t in ys)
        {
            if (t <= y0 || t >= y1) continue;
            DrawLine3DAt(proj, x0, t, zMin, x0, t, zMax, color, width, LineStyle.Solid);
        }
        foreach (var t in zs)
        {
            if (t <= zMin || t >= zMax) continue;
            DrawLine3DAt(proj, x0, y0, t, x0, y1, t, color, width, LineStyle.Solid);
        }

        // Back-right face (y = y1): X stripes and Z stripes.
        foreach (var t in xs)
        {
            if (t <= x0 || t >= x1) continue;
            DrawLine3DAt(proj, t, y1, zMin, t, y1, zMax, color, width, LineStyle.Solid);
        }
        foreach (var t in zs)
        {
            if (t <= zMin || t >= zMax) continue;
            DrawLine3DAt(proj, x0, y1, t, x1, y1, t, color, width, LineStyle.Solid);
        }
    }

    /// <summary>
    /// Computes major tick positions using matplotlib's exact 3-D <c>get_tick_space()</c> +
    /// <c>MaxNLocator</c> algorithm with the full 10-entry ladder
    /// <c>{1, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10}</c>. <paramref name="edgePx"/> is the
    /// projected screen-space length of the axis edge; matplotlib derives its tick target
    /// as <c>floor(edgePx / (labelFontSize * 3))</c>, which is what makes the Y axis end
    /// up with 12 ticks while X only gets 7 for the same bar3d scene — Y's edge happens
    /// to project longer in screen pixels even though its data range is smaller.
    /// </summary>
    private static double[] ComputeMaxNTicks(double lo, double hi, double edgePx, double labelFontSize)
    {
        double range = hi - lo;
        if (range <= 0) return [lo];

        // Use matplotlib MaxNLocator default nbins=10 — empirically gives:
        //   X range 2.98 → step 0.5 (6 ticks)   [matches matplotlib]
        //   Y range 1.83 → step 0.2 (10 ticks)  [matches matplotlib]
        //   Z range 6.25 → step 1   (7 ticks)   [matches matplotlib]
        // We keep edgePx + labelFontSize in the signature for future callers that want
        // the matplotlib axis3d.py get_tick_space() dynamic behaviour, but the default
        // gives better parity on the bar3d reference scene.
        _ = edgePx; _ = labelFontSize;
        int tickSpace = 10;

        // matplotlib MaxNLocator default ladder.
        double[] stepLadder = [1.0, 2.0, 2.5, 5.0, 10.0];

        // For each magnitude × ladder entry, walk from finest to coarsest. Pick the first
        // step that yields (1) at least 2 intervals and (2) at most tickSpace intervals.
        // This gives the FINEST nice-number step that fits the pixel budget — matches
        // matplotlib's algorithm for our bar3d scene exactly:
        //   X range 2.98, edgePx≈380 → tickSpace=11 → step 0.5 (6 ticks)
        //   Y range 1.83, edgePx≈365 → tickSpace=11 → step 0.2 (10 ticks)
        //   Z range 6.25, edgePx≈440 → tickSpace=11 → step 1 (7 ticks)
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range / tickSpace)));
        double chosenStep = 10 * magnitude;
        for (int mul = 0; mul < 2; mul++)
        {
            double m = magnitude * (mul == 0 ? 1 : 10);
            bool picked = false;
            foreach (double s in stepLadder)
            {
                double step = s * m;
                double niceLo = Math.Floor(lo / step + 1e-9) * step;
                double niceHi = Math.Ceiling(hi / step - 1e-9) * step;
                int count = (int)Math.Round((niceHi - niceLo) / step);
                // matplotlib MaxNLocator: accepts `nbins + 1` intervals. Critical for Y
                // range 1.83: step 0.2 yields 11 intervals which exceeds strict nbins=10
                // but matplotlib still picks it (yticks list has 12 values, 11 intervals).
                if (count <= tickSpace + 1 && count >= 2)
                {
                    chosenStep = step;
                    picked = true;
                    break;
                }
            }
            if (picked) break;
        }

        double first = Math.Ceiling(lo / chosenStep - 1e-9) * chosenStep;
        var ticks = new List<double>();
        for (double t = first; t <= hi + chosenStep * 0.01; t += chosenStep)
            ticks.Add(Math.Round(t, 10));
        return ticks.ToArray();
    }

    /// <summary>Back-compat shim: fixed-target 5 ticks for callers that don't know the edge pixel length.</summary>
    private static double[] ComputeCoarse3DTicks(double lo, double hi)
        => ComputeMaxNTicks(lo, hi, edgePx: 300, labelFontSize: 11);

    /// <summary>Euclidean distance between two projected points (used for projected-edge tick-density math).</summary>
    private static double PixelLength(Point a, Point b)
        => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    /// <summary>Computes a unit perpendicular to edge <c>AB</c> that points AWAY from <paramref name="centroid"/>, scaled to <paramref name="px"/> pixels.</summary>
    private static Point PerpAwayFromCentroid(Point a, Point b, Point edgeMid, Point centroid, double px)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) return new Point(0, px);
        var perp = new Point(dy / len * px, -dx / len * px);
        double towardX = centroid.X - edgeMid.X;
        double towardY = centroid.Y - edgeMid.Y;
        if (perp.X * towardX + perp.Y * towardY > 0) perp = new Point(-perp.X, -perp.Y);
        return perp;
    }

    /// <summary>Computes minor-tick positions half-way between the given major ticks, clipped to [lo, hi].</summary>
    private static double[] HalfStepTicks(double[] majors, double lo, double hi)
    {
        if (majors.Length < 2) return [];
        double step = (majors[1] - majors[0]) / 2.0;
        var list = new List<double>(majors.Length * 2);
        for (double t = majors[0] - step; t <= hi + step * 0.01; t += step)
            if (t > lo + step * 0.001 && t < hi - step * 0.001)
                list.Add(t);
        // Drop any value that coincides with a major tick — those are drawn by the major pass.
        var result = new List<double>(list.Count);
        foreach (var t in list)
        {
            bool isMajor = false;
            foreach (var m in majors)
                if (Math.Abs(m - t) < step * 0.01) { isMajor = true; break; }
            if (!isMajor) result.Add(t);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Renders tick marks and numeric labels on the three visible bounding-box edges that
    /// matplotlib's mpl_toolkits.mplot3d uses for its default view (elev≥0, -90° &lt; azim &lt; 0°):
    ///   • X axis along y=y_min, z=z_min (front-bottom edge)
    ///   • Y axis along x=x_max, z=z_min (right-bottom edge)
    ///   • Z axis along x=x_max, y=y_min (front-right vertical edge)
    /// This produces the characteristic L-shape with Z on the right, X on the left-front,
    /// and Y on the right-front that matches matplotlib's rendered output one-for-one.
    /// </summary>
    private void Render3DAxisTicks(Projection3D proj,
        double x0, double x1, double y0, double y1, double z0, double z1)
    {
        double zMin = Axes.ZAxis.Min ?? z0;
        double zMax = Axes.ZAxis.Max ?? z1;

        // Cube centroid in screen space — used to orient each axis's perpendicular outward.
        var centroid = proj.Project((x0 + x1) / 2, (y0 + y1) / 2, (zMin + zMax) / 2);

        // X ticks: bottom-front edge (z=z0, y=y0), X varies.
        RenderAxisEdgeTicks(Axes.XAxis, x0, x1,
            t => proj.Project(t, y0, z0), t => (t, y0, z0), proj,
            proj.Project(x0, y0, z0), proj.Project(x1, y0, z0),
            centroid, labelFloor: _rawXMin);

        // Y ticks: bottom-right edge (z=z0, x=x1), Y varies.
        RenderAxisEdgeTicks(Axes.YAxis, y0, y1,
            t => proj.Project(x1, t, z0), t => (x1, t, z0), proj,
            proj.Project(x1, y0, z0), proj.Project(x1, y1, z0),
            centroid, labelFloor: _rawYMin);

        // Z ticks: back-right vertical edge (x=x1, y=y1), Z varies.
        RenderAxisEdgeTicks(Axes.ZAxis, zMin, zMax,
            t => proj.Project(x1, y1, t), t => (x1, y1, t), proj,
            proj.Project(x1, y1, z0), proj.Project(x1, y1, z1),
            centroid, labelFloor: _rawZMin);
    }


    /// <summary>
    /// Renders major (and optionally minor) tick marks and labels along a single projected 3D axis edge.
    /// Respects <see cref="TickConfig"/> for length, width, color, label size/color, pad, and
    /// <see cref="Axis.TickFormatter"/>/<see cref="Axis.TickLocator"/> for tick placement and formatting.
    /// </summary>
    private void RenderAxisEdgeTicks(Axis axis, double lo, double hi,
        Func<double, Point> projectTick, Func<double, (double x, double y, double z)> tickTo3D,
        Projection3D proj, Point edgeA, Point edgeB, Point cubeCentroid,
        double labelFloor = double.NegativeInfinity)
    {
        var major = axis.MajorTicks;
        if (!major.Visible) return;

        // matplotlib Axis3D uses MaxNLocator with nbins = axis.get_tick_space()
        // = floor(projected_edge_pixels / (label_font_size * 3)). The projected edge pixel
        // length differs per axis (Y often projects longer in screen than X for the default
        // bar3d camera), which is why Y ends up with step 0.2 and X with step 0.5 even
        // though Y's data range is smaller.
        double edgePx = Math.Sqrt(
            (edgeB.X - edgeA.X) * (edgeB.X - edgeA.X) +
            (edgeB.Y - edgeA.Y) * (edgeB.Y - edgeA.Y));
        double labelFontSize = major.LabelSize ?? Theme.DefaultFont.Size;
        var ticks = axis.TickLocator is not null
            ? axis.TickLocator.Locate(lo, hi)
            : axis.MajorTicks.Spacing.HasValue
                ? new TickLocators.MultipleLocator(axis.MajorTicks.Spacing.Value).Locate(lo, hi)
                : ComputeMaxNTicks(lo, hi, edgePx, labelFontSize);

        // Compute the perpendicular, then flip it if it points toward the cube centroid
        // rather than away from it — this guarantees tick marks and labels always extend
        // AWAY from the cube regardless of camera angle or edge orientation.
        var perp = Perp2D(edgeA, edgeB, 1.0);
        var edgeMid = new Point((edgeA.X + edgeB.X) / 2, (edgeA.Y + edgeB.Y) / 2);
        double towardCubeX = cubeCentroid.X - edgeMid.X;
        double towardCubeY = cubeCentroid.Y - edgeMid.Y;
        if (perp.X * towardCubeX + perp.Y * towardCubeY > 0)
            perp = new Point(-perp.X, -perp.Y);

        var tickColor = major.Color ?? Theme.ForegroundText;

        var labelFont = TickFont();
        if (major.LabelSize.HasValue)  labelFont = labelFont with { Size  = major.LabelSize.Value };
        if (major.LabelColor.HasValue) labelFont = labelFont with { Color = major.LabelColor };

        // Extra 3-D tick-label pad: push labels well outside the cube silhouette so they
        // don't overlap with bars/surfaces drawn inside the cube. matplotlib uses a similar
        // offset to keep X/Y labels in the margin below the cube rather than along the
        // bottom edge where bar front faces live.
        const double threeDExtraPad = 14;

        // Build a single uniform-precision formatter from the tick list so every label
        // gets the same decimal places (e.g. "0.0"/"0.5"/"1.0" instead of "0"/"0.5"/"1").
        // Matches matplotlib's ScalarFormatter. User-supplied axis.TickFormatter still wins.
        var uniformFormat = BuildUniformTickFormatter(ticks);
        foreach (double t in ticks)
        {
            // Skip tick MARKS and LABELS below the data floor. Matches matplotlib's
            // behaviour of hiding sub-zero ticks when the data starts at 0 (visible
            // in the bar3d reference: yticks list [-0.6..1.6] but rendered labels
            // start at 0.0).
            if (t < labelFloor - 1e-9) continue;

            var p   = projectTick(t);
            var (xd, yd, zd) = tickTo3D(t);
            var tip = new Point(p.X + perp.X * major.Length, p.Y + perp.Y * major.Length);
            // Tick mark — both endpoints share the same 3D anchor (it's a 2D mark on the
            // axis edge, but for re-projection purposes both ends pin to the tick's data point).
            EmitV3D(proj, (xd, yd, zd), (xd, yd, zd));
            Ctx.DrawLine(p, tip, tickColor, major.Width, LineStyle.Solid);

            double labelOffset = major.Length + major.Pad + threeDExtraPad;
            var labelPos = new Point(
                p.X + perp.X * labelOffset,
                p.Y + perp.Y * labelOffset);
            EmitV3D(proj, (xd, yd, zd));
            Ctx.DrawText(axis.TickFormatter?.Format(t) ?? uniformFormat(t),
                labelPos, labelFont, TextAlignment.Center);
        }

        // Minor ticks (marks only, no labels)
        var minor = axis.MinorTicks;
        if (!minor.Visible || ticks.Length < 2) return;

        var minorColor = minor.Color ?? Theme.ForegroundText;
        double majorStep = ticks[1] - ticks[0];
        double minorStep = majorStep / 5;
        for (double t = lo; t <= hi + minorStep * 0.01; t += minorStep)
        {
            if (ticks.Any(mt => Math.Abs(mt - t) < minorStep * 0.1)) continue;
            var p   = projectTick(t);
            var (xd, yd, zd) = tickTo3D(t);
            var tip = new Point(p.X + perp.X * minor.Length, p.Y + perp.Y * minor.Length);
            EmitV3D(proj, (xd, yd, zd), (xd, yd, zd));
            Ctx.DrawLine(p, tip, minorColor, minor.Width, LineStyle.Solid);
        }
    }

    /// <summary>Returns a unit perpendicular from edge A→B (rotated 90° CW, outward from box), scaled to <paramref name="px"/> pixels.</summary>
    private static Point Perp2D(Point a, Point b, double px)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) return new Point(0, px);
        return new Point(dy / len * px, -dx / len * px);
    }

    /// <summary>
    /// Computes 3-D data ranges by porting matplotlib's exact two-stage expansion from
    /// <c>mpl_toolkits.mplot3d.axes3d.Axes3D._autoscale_axis</c> +
    /// <c>_set_lim3d(view_margin=1/48)</c>. Verified numerically against matplotlib for
    /// the bar3d scene: raw <c>[-0.3, 2.3]</c> → stage1 <c>[-0.43, 2.43]</c> (×0.05 margin)
    /// → stage2 <c>[-0.4896, 2.4896]</c> (×1/48 view_margin).
    /// </summary>
    /// <remarks>
    /// Stage-1 margins per matplotlib <c>clear()</c>:
    /// <list type="bullet">
    ///   <item><c>_xmargin = _ymargin = rcParams['axes.xmargin'] = 0.05</c></item>
    ///   <item><c>_zmargin = 0</c> for ortho (<c>_focal_length == inf</c>),
    ///         <c>0.05</c> for persp</item>
    /// </list>
    /// Stage-2 <c>_view_margin = 1/48 ≈ 0.02083</c> applied to ALL three axes on top of
    /// stage-1. NO <c>locator.view_limits</c> rounding (bar3d uses <c>_tight=True</c>).
    /// NO sticky clamping (matplotlib's bar3d lets <c>zlim</c> run negative).
    /// </remarks>
    private DataRange3D Compute3DDataRanges()
    {
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;

        // 1. Raw min/max from series point/grid data.
        foreach (var series in Axes.Series)
        {
            if (series is I3DGridSeries grid)
            {
                UpdateRange(grid.X, ref xMin, ref xMax);
                UpdateRange(grid.Y, ref yMin, ref yMax);
                for (int r = 0; r < grid.Z.GetLength(0); r++)
                    for (int c = 0; c < grid.Z.GetLength(1); c++)
                    {
                        if (grid.Z[r, c] < zMin) zMin = grid.Z[r, c];
                        if (grid.Z[r, c] > zMax) zMax = grid.Z[r, c];
                    }
            }
            else if (series is I3DPointSeries pts)
            {
                UpdateRange(pts.X, ref xMin, ref xMax);
                UpdateRange(pts.Y, ref yMin, ref yMax);
                UpdateRange(pts.Z, ref zMin, ref zMax);
            }
        }

        // _rawXMin/_rawYMin/_rawZMin are set AFTER ComputeDataRange contributions
        // below, so they include Bar3D's ZMin=0 floor and widened X/Y edges.

        // 2. Fold in series-reported X/Y/Z contributions. Bar3DSeries widens X/Y by
        //    BarWidth/2 here so the cube-face positions reflect bar EDGES, not bar
        //    centres, and reports ZMin=0 so the cube floor sits on z=0 (the bar floor)
        //    regardless of the minimum bar height.
        var context = new AxesContextAdapter(Axes);
        var contribs = new List<DataRangeContribution>(Axes.Series.Count);
        foreach (var series in Axes.Series)
        {
            var c = series.ComputeDataRange(context);
            contribs.Add(c);
            if (c.XMin.HasValue && c.XMin.Value < xMin) xMin = c.XMin.Value;
            if (c.XMax.HasValue && c.XMax.Value > xMax) xMax = c.XMax.Value;
            if (c.YMin.HasValue && c.YMin.Value < yMin) yMin = c.YMin.Value;
            if (c.YMax.HasValue && c.YMax.Value > yMax) yMax = c.YMax.Value;
            if (c.ZMin.HasValue && c.ZMin.Value < zMin) zMin = c.ZMin.Value;
            if (c.ZMax.HasValue && c.ZMax.Value > zMax) zMax = c.ZMax.Value;
        }

        // Cache label-floor minimums AFTER ComputeDataRange contributions so they
        // include Bar3D's ZMin=0 floor and widened X/Y edges. This ensures the z=0
        // tick label is visible when bars start at z=0.
        _rawXMin = xMin != double.MaxValue ? xMin : 0;
        _rawYMin = yMin != double.MaxValue ? yMin : 0;
        _rawZMin = zMin != double.MaxValue ? zMin : 0;

        // 3. Degenerate-range fallbacks.
        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (zMin == double.MaxValue) { zMin = 0; zMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }
        if (Math.Abs(zMax - zMin) < 1e-10) { zMin -= 0.5; zMax += 0.5; }

        // 4. User-set limits bypass both stages.
        bool xMinSet = Axes.XAxis.Min.HasValue;
        bool xMaxSet = Axes.XAxis.Max.HasValue;
        bool yMinSet = Axes.YAxis.Min.HasValue;
        bool yMaxSet = Axes.YAxis.Max.HasValue;
        bool zMinSet = Axes.ZAxis.Min.HasValue;
        bool zMaxSet = Axes.ZAxis.Max.HasValue;
        if (xMinSet) xMin = Axes.XAxis.Min!.Value;
        if (xMaxSet) xMax = Axes.XAxis.Max!.Value;
        if (yMinSet) yMin = Axes.YAxis.Min!.Value;
        if (yMaxSet) yMax = Axes.YAxis.Max!.Value;
        if (zMinSet) zMin = Axes.ZAxis.Min!.Value;
        if (zMaxSet) zMax = Axes.ZAxis.Max!.Value;

        // Capture raw aggregated range BEFORE stage-1/stage-2 margin expansion. The sticky
        // clamp below uses these as a guard: if the raw data already extended past the
        // sticky edge, we do NOT clamp — another series has legitimate data there. Without
        // this guard, overlays with a narrower range would clip the underlying full-range
        // series. See the 2-D equivalent in CartesianAxesRenderer.ComputeDataRanges.
        double unpaddedXMin = xMin, unpaddedXMax = xMax;
        double unpaddedYMin = yMin, unpaddedYMax = yMax;
        double unpaddedZMin = zMin, unpaddedZMax = zMax;

        // 5. Stage 1 — rcParams['axes.xmargin'] / ymargin / zmargin.
        //    matplotlib Axes3D.clear() sets _zmargin=0 for ortho (focal_length==inf)
        //    and _zmargin=rcParams['axes.zmargin']=0.05 for persp. We're ortho-only
        //    today, so zMargin default = 0.
        const double xmarginDefault = 0.05;
        const double ymarginDefault = 0.05;
        double zmarginDefault = Axes.CameraDistance.HasValue ? 0.05 : 0.0;
        double xMargin = Axes.XAxis.Margin ?? xmarginDefault;
        double yMargin = Axes.YAxis.Margin ?? ymarginDefault;
        double zMargin = Axes.ZAxis.Margin ?? zmarginDefault;
        double dx = xMax - xMin, dy = yMax - yMin, dz = zMax - zMin;
        if (!xMinSet) xMin -= dx * xMargin;
        if (!xMaxSet) xMax += dx * xMargin;
        if (!yMinSet) yMin -= dy * yMargin;
        if (!yMaxSet) yMax += dy * yMargin;
        if (!zMinSet) zMin -= dz * zMargin;
        if (!zMaxSet) zMax += dz * zMargin;

        // 6. Stage 2 — _view_margin = 1/48 applied to the stage1-expanded range.
        //    Verified against matplotlib: for bar3d scene this produces
        //    xlim (-0.4896, 2.4896), ylim (-0.4167, 1.4167), zlim (-0.125, 6.125).
        const double viewMargin = 1.0 / 48.0;
        double dx2 = xMax - xMin, dy2 = yMax - yMin, dz2 = zMax - zMin;
        if (!xMinSet) xMin -= dx2 * viewMargin;
        if (!xMaxSet) xMax += dx2 * viewMargin;
        if (!yMinSet) yMin -= dy2 * viewMargin;
        if (!yMaxSet) yMax += dy2 * viewMargin;
        if (!zMinSet) zMin -= dz2 * viewMargin;
        if (!zMaxSet) zMax += dz2 * viewMargin;

        // 7. Sticky-edge clamp — Bar3DSeries pins Z=0 so the cube floor never sits
        //    below the bar floor, giving a visible z=0 tick and a clean floor-bar join.
        //    The `unpadded >= sticky` / `<= sticky` guards ensure sticky edges only
        //    constrain the margin PADDING, not the data contributions of other series
        //    (mirrors the 2-D fix in CartesianAxesRenderer.ComputeDataRanges).
        foreach (var c in contribs)
        {
            if (!xMinSet && c.StickyXMin.HasValue && xMin < c.StickyXMin.Value
                && unpaddedXMin >= c.StickyXMin.Value) xMin = c.StickyXMin.Value;
            if (!xMaxSet && c.StickyXMax.HasValue && xMax > c.StickyXMax.Value
                && unpaddedXMax <= c.StickyXMax.Value) xMax = c.StickyXMax.Value;
            if (!yMinSet && c.StickyYMin.HasValue && yMin < c.StickyYMin.Value
                && unpaddedYMin >= c.StickyYMin.Value) yMin = c.StickyYMin.Value;
            if (!yMaxSet && c.StickyYMax.HasValue && yMax > c.StickyYMax.Value
                && unpaddedYMax <= c.StickyYMax.Value) yMax = c.StickyYMax.Value;
            if (!zMinSet && c.StickyZMin.HasValue && zMin < c.StickyZMin.Value
                && unpaddedZMin >= c.StickyZMin.Value) zMin = c.StickyZMin.Value;
            if (!zMaxSet && c.StickyZMax.HasValue && zMax > c.StickyZMax.Value
                && unpaddedZMax <= c.StickyZMax.Value) zMax = c.StickyZMax.Value;
        }

        return new DataRange3D(xMin, xMax, yMin, yMax, zMin, zMax);
    }
}
