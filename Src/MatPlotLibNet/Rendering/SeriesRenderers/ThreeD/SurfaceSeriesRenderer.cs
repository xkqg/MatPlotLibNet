// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SurfaceSeries"/> as colored quadrilaterals projected from 3D to 2D.</summary>
internal sealed class SurfaceSeriesRenderer : SceneSeriesRenderer<SurfaceSeries>
{
    /// <inheritdoc />
    public SurfaceSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SurfaceSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        // The extent spans the values that are there: a missing value (NaN) and an infinity are left out.
        var proj = ProjectionFor(series.X.DrawableRange(), series.Y.DrawableRange(), series.Z.DrawableRange());

        // What the camera does not move — each face's colour after the light and its vertex data — is kept on the series
        // from the draw before and made again only when the data or what colours it changed. Turning the surface then
        // only projects and sorts. The vertex data is asked for only by a draw that writes it.
        var faces = SurfaceFaces.For(series.DrawnFaces, series, proj,
            new(SurfaceFaces.Strides.Of(series, Context.Theme.Looks.Cube.SurfaceStride), series.ColorMappingUnder(Context.Theme), Context.LightSource));
        series.DrawnFaces = faces;
        var vertexData = Context.Emit3DData ? faces.VertexData(series, proj) : null;

        // Every patch projected along its perimeter, with its mean depth for the painter's sort.
        var cells = faces.Faces;
        var quads = new List<ProjectedQuad>(cells.Length);
        int longest = SurfaceFaces.LongestPerimeter(cells);
        Span<SurfaceFaces.GridPoint> walk = longest <= SurfaceFaces.MostOnTheStack
            ? stackalloc SurfaceFaces.GridPoint[SurfaceFaces.MostOnTheStack] : new SurfaceFaces.GridPoint[longest];
        double[] xs = series.X, ys = series.Y;
        double[,] zs = series.Z;
        for (int i = 0; i < cells.Length; i++)
        {
            if (!cells[i].Finite) continue;
            int n = cells[i].Perimeter(walk);
            var vertices = new Point[n];
            double depth = 0;
            bool drawable = true;
            for (int k = 0; k < n; k++)
            {
                var p = walk[k];
                double px = xs[p.Col], py = ys[p.Row], pz = zs[p.Row, p.Col];
                var at = proj.Project(px, py, pz);
                drawable &= at.IsDrawable();
                vertices[k] = at;
                depth += proj.Depth(px, py, pz);
            }

            // A patch with a vertex that cannot be placed is skipped.
            if (!drawable) continue;

            quads.Add(new(depth / n, vertices, i));
        }

        var edge = Context.Theme.Looks.Patch.SurfaceEdgeOver(series.GivenWireframe, series.EdgeColor);
        Color? stroke = edge?.Color;
        double strokeWidth = edge?.Thickness ?? 0;
        var theme = Context.Theme;
        var join = theme.ShapeJoinOf(series.Artist);
        DrawBackToFront(quads, q =>
        {
            if (vertexData?[q.Face] is { } v3d)
                Ctx.SetNextElementData("v3d", v3d);
            Ctx.DrawPolygon(q.Vertices, ShapeStyle.Edged(cells[q.Face].Fill, stroke, strokeWidth, join));
        }, series.OpacityUnder(theme));
    }

    /// <summary>A drawable face where this camera puts it: its depth, its corners on the canvas and its index among the
    /// kept faces.</summary>
    private readonly record struct ProjectedQuad(double Depth, Point[] Vertices, int Face) : IDepthMark;
}
