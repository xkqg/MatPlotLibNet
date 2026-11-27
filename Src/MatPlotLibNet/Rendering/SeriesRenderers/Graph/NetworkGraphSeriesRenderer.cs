// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="NetworkGraphSeries"/> as nodes (circles) connected by
/// edges (line segments), with optional arrowheads on directed edges and per-node
/// labels. The nodes are placed by the series (<see cref="NetworkGraphSeries.Placement"/>); this renderer
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

        // 1. The placed nodes (X/Y in data space): the placement the series' data range was worked out from, so the axes are
        // scaled to the very nodes drawn here.
        var (_, layout, positioned) = series.Placement;

        // 2. Build ID → index → pixel-position map for fast edge lookup.
        var idToIndex = new Dictionary<string, int>(positioned.Length);
        for (int i = 0; i < positioned.Length; i++) idToIndex[positioned[i].Id] = i;

        var pixelPositions = new Point[positioned.Length];
        for (int i = 0; i < positioned.Length; i++)
            pixelPositions[i] = Transform.DataToPixel(positioned[i].X, positioned[i].Y);

        // 3. Resolve colour map (Viridis default); a node's ColorScalar is its position on the map, 0 to 1 (GraphNode.ColorScalar).
        var cmap = series.ColorMapUnder(Context.Theme);

        // 4. Draw edges first (so nodes paint over them).
        foreach (var edge in series.Edges)
        {
            if (!idToIndex.TryGetValue(edge.From, out int u)) continue;
            if (!idToIndex.TryGetValue(edge.To,   out int v)) continue;

            var p1 = pixelPositions[u];
            var p2 = pixelPositions[v];
            double thickness = edge.Weight * series.EdgeThicknessScale;
            if (layout.DrawsEdgesAsArcs)
            {
                // An arc diagram draws every edge as a semicircle over the row of nodes; a self-loop has none to draw.
                if (u != v) DrawArc(series, edge, positioned[u].X, positioned[v].X, thickness);
                continue;
            }

            Ctx.DrawLine(p1, p2, StrokeStyle.Solid(SeriesColor, thickness));

            if (edge.IsDirected)
                DrawArrowhead(p1, p2, SeriesColor);

            if (series.ShowEdgeWeights)
            {
                var mid = new Point((p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0);
                Ctx.Draw(TextLabel.Of(edge.Weight.InvariantText("G3")), Context.Theme.DataTextFont(series.TextSizeUnder(Context.Theme)), new TextPlacement(mid, TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
            }
        }

        // 5. Draw nodes.
        for (int i = 0; i < positioned.Length; i++)
        {
            var n = positioned[i];
            double radius = n.SizeScalar * series.NodeRadiusScale;
            var fill = cmap.ColorOf(n.ColorScalar, NoNormNormalizer.Instance, 0, 1);
            Ctx.DrawCircle(pixelPositions[i], radius, ShapeStyle.Edged(fill, Colors.Black, Slot.LineWidthOf(LineWidthRule.OwnHairline)));

            if (series.ShowNodeLabels)
            {
                string label = n.Label ?? n.Id;
                // The name stands slightly to the right of the node, centred on it.
                var labelPos = new Point(pixelPositions[i].X + radius + 2, pixelPositions[i].Y);
                Ctx.Draw(TextLabel.Of(label), Context.Theme.DataTextFont(), new TextPlacement(labelPos, TextAlignment.Left) { Vertical = TextVerticalAlignment.Center });
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
        double radius = ArcGraphLayout.Radius(xFrom, xTo);
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
        Ctx.DrawPath(arc, ShapeStyle.Outlined(SeriesColor, thickness));

        if (edge.IsDirected)
            DrawArrowhead(lastControl, end, SeriesColor);

        if (series.ShowEdgeWeights)
        {
            Ctx.Draw(TextLabel.Of(edge.Weight.InvariantText("G3")), Context.Theme.DataTextFont(series.TextSizeUnder(Context.Theme)), new TextPlacement(At(centre, radius), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
        }
    }

    /// <summary>Paints a small filled triangle at the target end of a directed edge: the head of
    /// <see cref="ArrowStyle.FancyArrow"/>, so the geometry matches the annotation arrows used elsewhere in the library.</summary>
    private void DrawArrowhead(Point from, Point to, Color color) =>
        ArrowHead.Of(ArrowStyle.FancyArrow).Draw(Ctx, from, to, ArrowSizePx, color);
}
