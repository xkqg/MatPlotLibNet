// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>A position on the globe, in degrees.</summary>
/// <param name="Lon">Longitude, east positive.</param>
/// <param name="Lat">Latitude, north positive.</param>
public readonly record struct GeoPosition(double Lon, double Lat);

/// <summary>A polygon on the globe: one exterior ring and the holes cut out of it (Lesotho is a hole in South Africa). A
/// polygon made by <see cref="FromRings"/> is wound the way a fill needs it: the exterior counter-clockwise and each hole
/// clockwise in the plane of longitude and latitude (the winding RFC 7946 asks for), so a path with the exterior and the holes
/// together leaves a hole unfilled under the non-zero fill rule whatever the winding of the source was.</summary>
/// <param name="Exterior">The ring that bounds the polygon.</param>
/// <param name="Holes">The rings cut out of it; none for a polygon without holes.</param>
public readonly record struct GeoPolygon(IReadOnlyList<GeoPosition> Exterior, IReadOnlyList<IReadOnlyList<GeoPosition>> Holes)
{
    /// <summary>The polygon of an exterior ring and its holes, each wound by its signed area: a ring that runs the wrong way
    /// round is reversed, one that runs the right way is kept as it is.</summary>
    /// <param name="exterior">The ring that bounds the polygon.</param>
    /// <param name="holes">The rings cut out of it.</param>
    public static GeoPolygon FromRings(IReadOnlyList<GeoPosition> exterior, IEnumerable<IReadOnlyList<GeoPosition>> holes) =>
        new(Wound(exterior, counterClockwise: true), [.. holes.Select(hole => Wound(hole, counterClockwise: false))]);

    private static IReadOnlyList<GeoPosition> Wound(IReadOnlyList<GeoPosition> ring, bool counterClockwise)
    {
        double area = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            area += a.Lon * b.Lat - b.Lon * a.Lat;
        }

        return (area > 0) == counterClockwise || area == 0 ? ring : [.. ring.Reverse()];
    }
}
