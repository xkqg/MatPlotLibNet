// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The outline of a map: the edge of the area a projection draws the globe in, as cartopy's <c>projection.boundary</c>
/// is. The ocean fills it, and under the matplotlib themes it is the map's background and spine.</summary>
internal static class ProjectionOutlineExtensions
{
    /// <summary>The points along each outer meridian of a pseudo-cylindrical outline, pole to pole (cartopy's
    /// <c>_WarpedRectangularProjection</c>: <c>n = 91</c>).</summary>
    private const int MeridianPoints = 91;

    /// <summary>The outline of each projection, traced once (<see cref="IProjectionOutline.TraceOutline"/>) and kept for as long as the
    /// projection lives: a map reads it at every draw, through its axes' background and spine, its ocean and its horizon.</summary>
    private static readonly ConditionalWeakTable<IGeoProjection, IReadOnlyList<GeoPosition>> Traced = new();

    extension(IGeoProjection projection)
    {
        /// <summary>The edge of the map in the projected plane, closed: the projection's own (<see cref="IProjectionOutline"/>, traced
        /// once for the projection), else the rectangle of its bounds.</summary>
        internal IReadOnlyList<GeoPosition> Outline() =>
            projection is IProjectionOutline
                ? Traced.GetValue(projection, static outlined => ((IProjectionOutline)outlined).TraceOutline())
                : projection.Bounds.Rectangle();

        /// <summary>The outline of a pseudo-cylindrical projection: the meridian at 180° W from the south pole to the north, the
        /// meridian at 180° E back, and the first point again, as cartopy's <c>_WarpedRectangularProjection</c> builds it (91 latitudes
        /// from -90 to 90 on each).</summary>
        internal IReadOnlyList<GeoPosition> MeridianOutline()
        {
            var outline = new GeoPosition[2 * MeridianPoints + 1];
            for (int i = 0; i < MeridianPoints; i++)
            {
                double latitude = -90 + 180.0 * i / (MeridianPoints - 1);
                var west = projection.Forward(latitude, -180);
                var east = projection.Forward(-latitude, 180);
                outline[i] = new GeoPosition(west.X, west.Y);
                outline[MeridianPoints + i] = new GeoPosition(east.X, east.Y);
            }

            outline[^1] = outline[0];
            return outline;
        }
    }

    extension(GeoBounds bounds)
    {
        /// <summary>The rectangle of the bounds, closed, from its lower left corner anticlockwise.</summary>
        internal IReadOnlyList<GeoPosition> Rectangle() =>
            [new(bounds.XMin, bounds.YMin), new(bounds.XMax, bounds.YMin), new(bounds.XMax, bounds.YMax), new(bounds.XMin, bounds.YMax),
             new(bounds.XMin, bounds.YMin)];

        /// <summary>The circle the bounds are the square of, through <paramref name="points"/> points of which the last is the first,
        /// clockwise from the east, as cartopy's <c>_ellipse_boundary</c> lays out its points (<c>np.linspace(0, -2 * pi, n)</c>).</summary>
        /// <param name="points">How many points, the first again at the end among them.</param>
        internal IReadOnlyList<GeoPosition> InscribedCircle(int points)
        {
            double cx = (bounds.XMin + bounds.XMax) / 2, cy = (bounds.YMin + bounds.YMax) / 2;
            double rx = (bounds.XMax - bounds.XMin) / 2, ry = (bounds.YMax - bounds.YMin) / 2;
            var circle = new GeoPosition[points];
            for (int i = 0; i < points; i++)
            {
                double t = -2 * Math.PI * i / (points - 1);
                var rim = new Point(cx, cy).OnRim(rx, ry, t);
                circle[i] = new GeoPosition(rim.X, rim.Y);
            }

            circle[^1] = circle[0];
            return circle;
        }
    }
}
