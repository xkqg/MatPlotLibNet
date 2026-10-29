// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The shortest path between two places on the globe.</summary>
internal static class GreatCircle
{
    /// <summary>The places along the great circle from <paramref name="from"/> to <paramref name="to"/>, evenly spread in angle.</summary>
    /// <param name="from">Where the path starts.</param>
    /// <param name="to">Where it ends.</param>
    /// <param name="segments">How many pieces the path is cut into; it has one place more than that.</param>
    /// <returns>The places, the first and the last exactly the ends given. Two places that coincide, or that lie opposite each other
    /// (every great circle between them is as short as another), are the two ends alone.</returns>
    internal static IReadOnlyList<GeoCoordinate> Between(GeoCoordinate from, GeoCoordinate to, int segments)
    {
        var a = DirectionOf(from);
        var b = DirectionOf(to);
        double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        double crossLength = Math.Sqrt(Math.Pow(a.Y * b.Z - a.Z * b.Y, 2) + Math.Pow(a.Z * b.X - a.X * b.Z, 2) + Math.Pow(a.X * b.Y - a.Y * b.X, 2));
        double angle = Math.Atan2(crossLength, dot);
        if (crossLength < 1e-12) return [from, to];

        var places = new List<GeoCoordinate>(segments + 1) { from };
        for (int i = 1; i < segments; i++)
        {
            double t = (double)i / segments;
            double wa = Math.Sin((1 - t) * angle) / Math.Sin(angle), wb = Math.Sin(t * angle) / Math.Sin(angle);
            double x = wa * a.X + wb * b.X, y = wa * a.Y + wb * b.Y, z = wa * a.Z + wb * b.Z;
            places.Add(new GeoCoordinate(Math.Atan2(z, Math.Sqrt(x * x + y * y)).ToDegrees(), Math.Atan2(y, x).ToDegrees()));
        }

        places.Add(to);
        return places;
    }

    /// <summary>A place on the globe as a unit vector from its centre.</summary>
    private readonly record struct Direction(double X, double Y, double Z);

    private static Direction DirectionOf(GeoCoordinate place)
    {
        double lat = place.Latitude.ToRadians(), lon = place.Longitude.ToRadians();
        return new Direction(Math.Cos(lat) * Math.Cos(lon), Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat));
    }
}
