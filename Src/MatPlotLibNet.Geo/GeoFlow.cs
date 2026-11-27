// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Geo;

/// <summary>The line of a flow in the plane of a map: its great circle, each piece projected, with a pair of NaN where the line
/// has a gap.</summary>
/// <param name="Value">How much of it moves; the line is as wide as this, in proportion to the other flows.</param>
/// <param name="X">The projected X of each point, with a NaN where the line has a gap.</param>
/// <param name="Y">The projected Y of each point, with a NaN where the line has a gap.</param>
internal readonly record struct GeoFlowPath(double Value, double[] X, double[] Y);

/// <summary>A quantity that moves from one place to another: a trade route, a migration, a flight. A map draws it as a line along
/// the great circle between the two places, as wide as the quantity.</summary>
/// <param name="From">Where it starts.</param>
/// <param name="To">Where it ends.</param>
/// <param name="Value">How much of it moves; a flow of nothing, or of no number, is not drawn.</param>
public readonly record struct GeoFlow(GeoCoordinate From, GeoCoordinate To, double Value)
{
    /// <summary>How many pieces the great circle is cut into: a flow is drawn through one more place than that.</summary>
    private const int GreatCircleSegments = 64;

    /// <summary>The line this flow draws on a map in <paramref name="projection"/>: its great circle cut into pieces, each piece
    /// projected; where the circle crosses the antimeridian, or a position cannot be placed, the line has a gap (a pair of NaN),
    /// so the pieces are not joined across the map. Each end is projected once, and the line starts and ends on those points.</summary>
    /// <param name="projection">The projection of the map.</param>
    /// <returns>The line; <see langword="null"/> when the flow has no quantity (zero, below zero, no number) or an end the
    /// projection cannot place, since such a flow is not drawn.</returns>
    internal GeoFlowPath? PathOn(IGeoProjection projection)
    {
        if (!Value.IsPositiveFinite()) return null;
        if (projection.Place(From) is not { } start || projection.Place(To) is not { } end) return null;

        var places = Between(From, To, GreatCircleSegments);
        var line = new GappedLine(places.Count);
        double? previousLon = null;
        for (int i = 0; i < places.Count; i++)
        {
            var place = places[i];
            // The first place and the last are the ends given, which are already placed.
            ProjectedPoint? at = i == 0 ? start : i == places.Count - 1 ? end : projection.Place(place);
            bool crosses = previousLon is { } before && Math.Abs(place.Longitude - before) > 180;
            if (crosses || at is null)
            {
                line.Gap();
            }

            if (at is { } placed)
            {
                line.Add(placed.X, placed.Y);
            }

            previousLon = place.Longitude;
        }

        var data = line.ToData();
        return new GeoFlowPath(Value, data.X, data.Y);
    }

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
        double crossLength = new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X).Length;
        double angle = Math.Atan2(crossLength, dot);
        if (crossLength < 1e-12) return [from, to];

        var places = new List<GeoCoordinate>(segments + 1) { from };
        for (int i = 1; i < segments; i++)
        {
            double t = (double)i / segments;
            double wa = Math.Sin((1 - t) * angle) / Math.Sin(angle), wb = Math.Sin(t * angle) / Math.Sin(angle);
            double x = wa * a.X + wb * b.X, y = wa * a.Y + wb * b.Y, z = wa * a.Z + wb * b.Z;
            places.Add(new GeoCoordinate(Math.Atan2(z, new Point(x, y).Length).ToDegrees(), Math.Atan2(y, x).ToDegrees()));
        }

        places.Add(to);
        return places;
    }

    /// <summary>A place on the globe as a unit vector from its centre.</summary>
    private static Vec3 DirectionOf(GeoCoordinate place)
    {
        double lat = place.Latitude.ToRadians(), lon = place.Longitude.ToRadians();
        return new Vec3(Math.Cos(lat) * Math.Cos(lon), Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat));
    }
}
