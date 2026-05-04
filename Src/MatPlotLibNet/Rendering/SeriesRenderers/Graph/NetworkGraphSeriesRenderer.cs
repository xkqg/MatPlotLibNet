// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
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
    public override void Render(NetworkGraphSeries series)
    {
        if (series.Nodes.Count == 0) return;

        // 1. Apply layout to get positioned nodes (X/Y in data space).
        var positioned = NetworkGraphLayouts.Apply(series.Layout, series.Nodes, series.Edges);

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
            Ctx.DrawLine(p1, p2, SeriesColor, thickness, LineStyle.Solid);

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
            var fill = cmap.GetColor(Math.Clamp(n.ColorScalar, 0.0, 1.0));
            Ctx.DrawCircle(pixelPositions[i], radius, fill, Colors.Black, strokeThickness: 0.5);

            if (series.ShowNodeLabels)
            {
                string label = n.Label ?? n.Id;
                // Place label slightly to the right of the node.
                var labelPos = new Point(pixelPositions[i].X + radius + 2, pixelPositions[i].Y);
                Ctx.DrawText(label, labelPos, new Font { Family = "sans-serif", Size = 10, Color = Colors.Black },
                    TextAlignment.Left);
            }
        }
    }

    /// <summary>Paints a small filled triangle at the target end of a directed edge,
    /// reusing <see cref="ArrowHeadBuilder"/> so the geometry matches annotation-style
    /// arrows used elsewhere in the library.</summary>
    private void DrawArrowhead(Point from, Point to, Color color)
    {
        double dx = to.X - from.X, dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return;
        double ux = dx / len, uy = dy / len;
        var verts = ArrowHeadBuilder.BuildPolygon(to, ux, uy, ArrowStyle.FancyArrow, ArrowSizePx);
        if (verts.Count > 0) Ctx.DrawPolygon(verts, color, null, 0.0);
    }
}
