// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>Utility methods for handling geographic edge cases: dateline wraparound,
/// NaN filtering, and projection boundary clipping.</summary>
public static class GeoClipping
{
    /// <summary>Cuts a ring that crosses the antimeridian (±180° longitude) where it crosses, as RFC 7946 asks of a geometry that
    /// crosses it. An edge whose two ends lie more than 180° of longitude apart crosses; it is cut at the latitude where it reaches
    /// the meridian, and each piece of the ring between two such edges is closed along the meridian, so every position is kept and
    /// every piece reaches ±180. A ring that goes round a pole crosses once: its one piece runs from one side of the meridian to the
    /// other and is closed along that pole. A ring that does not cross is handed back as it is; a closed ring (its last position
    /// its first) gives closed pieces.</summary>
    /// <param name="ring">The ring, its longitudes from -180 to 180.</param>
    /// <returns>The pieces, those west of the meridian of Greenwich first.</returns>
    public static IReadOnlyList<IReadOnlyList<GeoPosition>> SplitAtDateline(IReadOnlyList<GeoPosition> ring)
    {
        bool closed = ring.Count > 1 && ring[0] == ring[^1];
        int count = closed ? ring.Count - 1 : ring.Count;

        int firstCrossing = -1;
        for (int i = 0; i < count && firstCrossing < 0; i++)
        {
            if (Crosses(ring[i], ring[(i + 1) % count])) firstCrossing = i;
        }

        if (firstCrossing < 0) return [ring];

        // Walk once round the ring from the position after the first crossing: a crossing ends a piece and begins the next.
        var pieces = new List<List<GeoPosition>>();
        int start = (firstCrossing + 1) % count;
        var piece = new List<GeoPosition> { Entering(ring[firstCrossing], ring[start]) };
        for (int step = 0; step < count; step++)
        {
            var from = ring[(start + step) % count];
            var to = ring[(start + step + 1) % count];
            piece.Add(from);
            if (!Crosses(from, to)) continue;
            piece.Add(Leaving(from, to));
            pieces.Add(piece);
            piece = [Entering(from, to)];
        }

        double pole = ring.Take(count).Sum(p => p.Lat) >= 0 ? 90 : -90;
        foreach (var cut in pieces)
        {
            // A piece that leaves on the other side of the meridian goes round the pole: it is closed along it.
            if (cut[0].Lon != cut[^1].Lon)
            {
                cut.Add(new GeoPosition(cut[^1].Lon, pole));
                cut.Add(new GeoPosition(cut[0].Lon, pole));
            }

            if (closed) cut.Add(cut[0]);
        }

        return [.. pieces.OrderBy(cut => cut[0].Lon < 0 ? 0 : 1)];
    }

    /// <summary>Whether the edge from <paramref name="from"/> to <paramref name="to"/> crosses the antimeridian: the shorter way
    /// round between its ends passes through ±180.</summary>
    private static bool Crosses(GeoPosition from, GeoPosition to) => Math.Abs(to.Lon - from.Lon) > 180;

    /// <summary>Where a crossing edge reaches the meridian, on the side of <paramref name="from"/>.</summary>
    private static GeoPosition Leaving(GeoPosition from, GeoPosition to)
    {
        double meridian = from.Lon > 0 ? 180 : -180;
        return new GeoPosition(meridian, LatitudeAtMeridian(from, to, meridian));
    }

    /// <summary>Where a crossing edge reaches the meridian, on the side of <paramref name="to"/>.</summary>
    private static GeoPosition Entering(GeoPosition from, GeoPosition to)
    {
        double meridian = from.Lon > 0 ? 180 : -180;
        return new GeoPosition(-meridian, LatitudeAtMeridian(from, to, meridian));
    }

    /// <summary>The latitude at which the edge reaches <paramref name="meridian"/>, along the edge in longitude and latitude with
    /// <paramref name="to"/> taken past the meridian (360° added on its far side).</summary>
    private static double LatitudeAtMeridian(GeoPosition from, GeoPosition to, double meridian)
    {
        double toLon = to.Lon + 2 * meridian;
        double t = (meridian - from.Lon) / (toLon - from.Lon);
        return from.Lat + t * (to.Lat - from.Lat);
    }

    /// <summary>Removes points with NaN coordinates from a list of projected points.</summary>
    public static List<ProjectedPoint> FilterNaN(List<ProjectedPoint> points)
    {
        var result = new List<ProjectedPoint>(points.Count);
        foreach (var point in points)
        {
            if (!point.IsOffDomain) result.Add(point);
        }
        return result;
    }

    /// <summary>Clips projected points to the projection's bounding box.</summary>
    public static List<ProjectedPoint> ClipToBounds(
        List<ProjectedPoint> points,
        double xMin, double xMax, double yMin, double yMax)
    {
        var result = new List<ProjectedPoint>(points.Count);
        foreach (var (x, y) in points)
        {
            result.Add(new ProjectedPoint(Math.Clamp(x, xMin, xMax), Math.Clamp(y, yMin, yMax)));
        }
        return result;
    }
}
