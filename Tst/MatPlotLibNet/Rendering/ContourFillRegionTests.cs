// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A filled contour paints, for each level from the second up, the whole region where the grid is above that
/// level in the colour of the band that starts there, over the lower bands: a band's region reaches to the edge of the grid
/// and into its corners, and a region with a hole (a valley inside it) leaves the hole to the bands below. The regions are
/// read off the shapes the renderer draws, in a colour of their own: on grids whose iso-lines are straight, the area of the
/// region of a level is known in closed form. Before, a band was the two iso-lines of its levels joined end to end, which
/// cut a region off along the straight line between the ends of an iso-line that met the edge, and filled a valley's ring
/// instead of the ground around it (matplotlib's <c>contourf</c> of the peaks surface: the far field that runs to the edges
/// stood in the lowest band's colour).</summary>
public class ContourFillRegionTests
{
    private static readonly Rect Bounds = new(0, 0, 600, 600);

    // One data unit is 100 pixels both ways.
    private const double PixelsPerUnitArea = 100.0 * 100.0;

    private static readonly double[] Axis = [0, 1, 2, 3, 4, 5, 6];

    private static double[,] Grid(Func<double, double, double> f)
    {
        var z = new double[Axis.Length, Axis.Length];
        for (int r = 0; r < Axis.Length; r++)
            for (int c = 0; c < Axis.Length; c++)
                z[r, c] = f(Axis[c], Axis[r]);
        return z;
    }

    private static RecordingRenderContext Render(double[,] z, double[] levels)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(Bounds.Linear(xMax: 6, yMax: 6), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx));
        new ContourfSeriesRenderer(context).Render(
            new ContourfSeries(Axis, Axis, z) { LevelValues = levels, ShowLines = false, ColorMap = ColorMaps.Viridis });
        return ctx;
    }

    private static Color BandColour(double[] levels, int band)
    {
        double mid = levels[band] + (levels[band + 1] - levels[band]) * 0.5;
        return ColorMaps.Viridis.GetColor((mid - levels[0]) / (levels[^1] - levels[0]));
    }

    /// <summary>The area, in data units, of every shape drawn in <paramref name="colour"/>: each ring's shoelace area, the rings
    /// of one path being the pieces of one region that do not overlap.</summary>
    private static double PaintedArea(RecordingRenderContext ctx, Color colour)
    {
        double area = 0;
        foreach (var call in ctx.Calls.OfType<ShapeCall>().Where(c => c.Fill == colour))
        {
            area += call switch
            {
                DrawPolygonCall polygon => Ring(polygon.Points),
                DrawPathCall path => Rings(path.Segments),
                DrawRectangleCall { Rect: var rect } => rect.Width * rect.Height,
                _ => 0,
            };
        }

        return area / PixelsPerUnitArea;
    }

    private static double Rings(PathSegment[] segments)
    {
        double area = 0;
        var ring = new List<Point>();
        foreach (var segment in segments)
        {
            switch (segment)
            {
                case MoveToSegment move:
                    area += Ring([.. ring]);
                    ring.Clear();
                    ring.Add(move.Point);
                    break;
                case LineToSegment line:
                    ring.Add(line.Point);
                    break;
            }
        }

        return area + Ring([.. ring]);
    }

    private static double Ring(Point[] points)
    {
        double twice = 0;
        for (int i = 0; i < points.Length; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Length];
            twice += a.X * b.Y - b.X * a.Y;
        }

        return Math.Abs(twice) / 2;
    }

    /// <summary>A ramp z = x + y: the region above 8 is the corner triangle x + y >= 8 of the 6 x 6 grid, area 8; its
    /// iso-line runs from the top edge to the right edge, and the region holds the corner (6, 6) between them.</summary>
    [Fact]
    public void ARegionThatReachesACorner_IsPaintedIntoTheCorner()
    {
        double[] levels = [0, 4, 8, 12];
        var ctx = Render(Grid((x, y) => x + y), levels);

        Assert.Equal(8, PaintedArea(ctx, BandColour(levels, 2)), 6);
    }

    /// <summary>A valley z = |x - 3| + |y - 3|: the region above 2 is the grid less the diamond of radius 2 around the
    /// middle, area 36 - 8 = 28; the diamond is a hole the lower band shows through.</summary>
    [Fact]
    public void ARegionAroundAValley_LeavesTheValleyOut()
    {
        double[] levels = [0, 2, 6];
        var ctx = Render(Grid((x, y) => Math.Abs(x - 3) + Math.Abs(y - 3)), levels);

        Assert.Equal(28, PaintedArea(ctx, BandColour(levels, 1)), 6);
    }

    /// <summary>A ramp in x alone, z = x: the region above 2 is the strip x >= 2, area 4 x 6 = 24, bounded by the iso-line
    /// x = 2 and three edges of the grid.</summary>
    [Fact]
    public void ARegionBoundedByEdges_IsTheWholeStrip()
    {
        double[] levels = [0, 2, 6];
        var ctx = Render(Grid((x, _) => x), levels);

        Assert.Equal(24, PaintedArea(ctx, BandColour(levels, 1)), 6);
    }
}
