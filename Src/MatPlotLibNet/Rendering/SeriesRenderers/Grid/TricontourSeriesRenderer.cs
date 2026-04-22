// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TricontourSeries"/> as contour polylines on a Delaunay-triangulated mesh.</summary>
internal sealed class TricontourSeriesRenderer : SeriesRenderer<TricontourSeries>
{
    /// <inheritdoc />
    public TricontourSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(TricontourSeries series)
    {
        if (series.X.Length < 3) return;

        var mesh = Delaunay.Triangulate(series.X, series.Y);
        if (mesh.Triangles.Length == 0) return;

        double zMin = series.Z.Min(), zMax = series.Z.Max();
        if (zMin == zMax) return;

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        int levels = Math.Max(2, series.Levels);
        for (int l = 0; l < levels; l++)
        {
            double levelValue = zMin + (zMax - zMin) * l / (levels - 1);
            var color = cmap.GetColor(norm.Normalize(levelValue, zMin, zMax));

            // March through triangles finding crossing segments at this level
            int nTriangles = mesh.Triangles.Length / 3;
            for (int t = 0; t < nTriangles; t++)
            {
                int ia = mesh.Triangles[t * 3], ib = mesh.Triangles[t * 3 + 1], ic = mesh.Triangles[t * 3 + 2];
                var segments = ContourSegments(
                    mesh.X[ia], mesh.Y[ia], series.Z[ia],
                    mesh.X[ib], mesh.Y[ib], series.Z[ib],
                    mesh.X[ic], mesh.Y[ic], series.Z[ic],
                    levelValue);
                foreach (var seg in segments)
                {
                    var px1 = Transform.DataToPixel(seg.From.X, seg.From.Y);
                    var px2 = Transform.DataToPixel(seg.To.X, seg.To.Y);
                    Ctx.DrawLine(px1, px2, color, 1.0, Styling.LineStyle.Solid);
                }
            }
        }
    }

    private static IEnumerable<LineSegment> ContourSegments(
        double ax, double ay, double az,
        double bx, double by, double bz,
        double cx, double cy, double cz,
        double level)
    {
        bool aAbove = az >= level, bAbove = bz >= level, cAbove = cz >= level;
        int count = (aAbove ? 1 : 0) + (bAbove ? 1 : 0) + (cAbove ? 1 : 0);
        if (count == 0 || count == 3) yield break;

        // Find the two edges where the level crosses
        var crossings = new List<Point>(2);
        AddCrossing(crossings, ax, ay, az, bx, by, bz, level);
        AddCrossing(crossings, bx, by, bz, cx, cy, cz, level);
        AddCrossing(crossings, cx, cy, cz, ax, ay, az, level);

        if (crossings.Count >= 2)
            yield return new(crossings[0], crossings[1]);
    }

    private static void AddCrossing(List<Point> list,
        double ax, double ay, double az, double bx, double by, double bz, double level)
    {
        if ((az < level) == (bz < level)) return;
        double t = (level - az) / (bz - az);
        list.Add(new Point(ax + t * (bx - ax), ay + t * (by - ay)));
    }
}
