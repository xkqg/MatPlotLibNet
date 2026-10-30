// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="NetworkGraphSeries"/> as nodes (circles) connected by
/// edges (line segments), with optional arrowheads on directed edges and per-node
/// labels. Layout is delegated to <see cref="NetworkGraphLayouts"/>; this renderer
/// owns coordinate-mapping (data→pixel) and per-element drawing only.</summary>
internal sealed class NetworkGraphSeriesRenderer : SeriesRenderer<NetworkGraphSeries>
{
    /// <summary>Pixel size of the arrowhead drawn at the tip of directed edges.</summary>
    private const double ArrowSizePx = 8.0;

    /// <inheritdoc />
    public NetworkGraphSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(NetworkGraphSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(NetworkGraphSeries series)
    {
        if (series.Nodes.Count == 0) return;

        // 1. Apply layout to get positioned nodes (X/Y in data space).
        // The seed, the iteration count and the convergence threshold come from the series, exactly as they do
        // in NetworkGraphSeries.ComputeDataRange. Leave them out here and the axes are scaled to the
        // caller's layout while the nodes are drawn from the default one, which puts nodes outside the plot.
        var positioned = NetworkGraphLayouts.Apply(series.Layout, series.Nodes, series.Edges,
            series.LayoutSeed, series.LayoutIterations, series.ConvergenceThreshold);

        // 2. Build ID → index → pixel-position map for fast edge lookup.
        var idToIndex = new Dictionary<string, int>(positioned.Length);
        for (int i = 0; i < positioned.Length; i++) idToIndex[positioned[i].Id] = i;

        var pixelPositions = new Point[positioned.Length];
        for (int i = 0; i < positioned.Length; i++)
            pixelPositions[i] = Transform.DataToPixel(positioned[i].X, positioned[i].Y);

        // 3. Resolve colour map (Viridis default; per-node ColorScalar already in [0,1] for now).
        var cmap = series.ColorMap ?? ColorMaps.Viridis;

        // 4. Draw edges first (so nodes paint over them).
        foreach (var edge in series.Edges)
        {
            if (!idToIndex.TryGetValue(edge.From, out int u)) continue;
            if (!idToIndex.TryGetValue(edge.To,   out int v)) continue;

            var p1 = pixelPositions[u];
            var p2 = pixelPositions[v];
            double thickness = edge.Weight * series.EdgeThicknessScale;
            if (series.Layout == GraphLayout.Arc)
            {
                // An arc diagram draws every edge as a semicircle over the row of nodes; a self-loop has none to draw.
                if (u != v) DrawArc(series, edge, positioned[u].X, positioned[v].X, thickness);
                continue;
            }

            Ctx.DrawLine(p1, p2, new StrokeStyle(SeriesColor, thickness, LineStyle.Solid));

            if (edge.IsDirected)
                DrawArrowhead(p1, p2, SeriesColor);

            if (series.ShowEdgeWeights)
            {
                var mid = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
                Ctx.DrawText(edge.Weight.ToString("G3", System.Globalization.CultureInfo.InvariantCulture),
                    mid, new Font { Family = "sans-serif", Size = 9, Color = Colors.Black },
                    TextAlignment.Center);
            }
        }

        // 5. Draw nodes.
        for (int i = 0; i < positioned.Length; i++)
        {
            var n = positioned[i];
            double radius = n.SizeScalar * series.NodeRadiusScale;
            var fill = cmap.ColorOf(n.ColorScalar, NoNormNormalizer.Instance, 0, 1);
            Ctx.DrawCircle(pixelPositions[i], radius, new ShapeStyle(fill, Colors.Black, 0.5));

            if (series.ShowNodeLabels)
            {
                string label = n.Label ?? n.Id;
                // Place label slightly to the right of the node.
                var labelPos = new Point(pixelPositions[i].X + radius + 2, pixelPositions[i].Y);
                Ctx.DrawText(label, labelPos, ThemedFontProvider.DataTextFont(Context.Theme), TextAlignment.Left);
            }
        }
    }

    /// <summary>The control points of the quarter circle of a semicircle, as a share of its radius: the cubic Bézier curve that
    /// follows a quarter circle closest (4/3 of the tangent of an eighth of a turn), a constant so that no trigonometry is
    /// needed.</summary>
    private const double QuarterCircle = 0.5522847498307936;

    /// <summary>Strokes one edge of an arc diagram: a semicircle over the row, from the node the edge leaves to the node it
    /// reaches, as two cubic Bézier curves worked out in data units (so they are a half circle when the plot has an aspect of
    /// one) and mapped to the page. A directed edge ends in an arrowhead whose direction is the end of the arc: from the
    /// last control point to the node.</summary>
    private void DrawArc(NetworkGraphSeries series, GraphEdge edge, double xFrom, double xTo, double thickness)
    {
        double radius = NetworkGraphLayouts.ArcRadius(xFrom, xTo);
        double centre = (xFrom + xTo) / 2, side = xTo > xFrom ? 1 : -1, bend = QuarterCircle * radius;
        Point At(double x, double y) => Transform.DataToPixel(x, y);

        var end = At(xTo, 0);
        var lastControl = At(xTo, bend);
        PathSegment[] arc =
        [
            new MoveToSegment(At(xFrom, 0)),
            new BezierSegment(At(xFrom, bend), At(centre - side * bend, radius), At(centre, radius)),
            new BezierSegment(At(centre + side * bend, radius), lastControl, end),
        ];
        Ctx.DrawPath(arc, new ShapeStyle(null, SeriesColor, thickness));

        if (edge.IsDirected)
            DrawArrowhead(lastControl, end, SeriesColor);

        if (series.ShowEdgeWeights)
        {
            Ctx.DrawText(edge.Weight.ToString("G3", System.Globalization.CultureInfo.InvariantCulture),
                At(centre, radius), new Font { Family = "sans-serif", Size = 9, Color = Colors.Black },
                TextAlignment.Center);
        }
    }

    /// <summary>Paints a small filled triangle at the target end of a directed edge,
    /// reusing <see cref="ArrowHeadBuilder"/> so the geometry matches annotation-style
    /// arrows used elsewhere in the library.</summary>
    private void DrawArrowhead(Point from, Point to, Color color)
    {
        var verts = ArrowHeadBuilder.BuildPolygon(from, to, ArrowStyle.FancyArrow, ArrowSizePx);
        if (verts.Count > 0) Ctx.DrawPolygon(verts, new ShapeStyle(color, null, 0.0));
    }
}
