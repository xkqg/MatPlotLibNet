// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Contour3DSeries"/> as projected contour lines at discrete Z levels: one mark of the scene at its nearest
/// point (<see cref="WholeDrawing"/>), its lines drawn level by level.</summary>
/// <remarks>Each level is traced by the one marching squares of a 2-D contour (<see cref="MarchingSquares"/>, contourpy's decisions:
/// a corner equal to the level is below it, the saddles paired one way), and each line is projected into 3D at Z = level.</remarks>
internal sealed class Contour3DSeriesRenderer : SceneSeriesRenderer<Contour3DSeries>
{
    /// <inheritdoc />
    public Contour3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Contour3DSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        // Compute Z range for contour levels and color mapping, over the values that are there: a missing value (NaN) and
        // an infinity are left out, and a cell with such a corner is masked below.
        if (series.Z.DrawableRange() is not { } z) return;
        double zMin = z.Lo, zMax = z.Hi;
        double zRange = zMax - zMin;
        if (zRange == 0) zRange = 1;

        var proj = ProjectionFor(series.X.DrawableRange(), series.Y.DrawableRange(), z);

        // The levels are coloured linearly over the heights, by the series' map, else the theme's.
        var scale = series.LinearMappingUnder(Context.Theme).Over(new(zMin, zMax));

        // Generate evenly spaced contour levels; the segments are kept in the order traced, with the depth of the nearest crossing,
        // each level's ending where the next one's begin, drawn in that level's stroke.
        var segments = new List<PixelSegment>();
        double nearest = double.NegativeInfinity;
        int levels = Math.Max(2, series.Levels);
        var strokes = new StrokeStyle[levels];
        var levelEnds = new int[levels];
        for (int lev = 0; lev < levels; lev++)
        {
            double t = (lev + 0.5) / levels;
            double zLevel = zMin + t * zRange;

            strokes[lev] = StrokeStyle.Solid(series.Color ?? scale.ColorOf(zLevel), series.LineWidth);

            // The lines of the level, as the 2-D contour traces them, each projected at the level's height.
            foreach (var line in MarchingSquares.Extract(series.X, series.Y, series.Z, [zLevel]))
            {
                var points = line.Points;
                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    nearest = Math.Max(nearest, proj.Depth(point.X, point.Y, zLevel));
                    if (i > 0) segments.Add(new(proj.Project(points[i - 1].X, points[i - 1].Y, zLevel), proj.Project(point.X, point.Y, zLevel)));
                }
            }

            levelEnds[lev] = segments.Count;
        }

        if (segments.Count == 0) return;
        DrawAsOneMark(nearest, () =>
        {
            for (int lev = 0, at = 0; lev < levels; lev++)
            {
                for (; at < levelEnds[lev]; at++) Ctx.DrawLine(segments[at].From, segments[at].To, strokes[lev]);
            }
        });
    }
}
