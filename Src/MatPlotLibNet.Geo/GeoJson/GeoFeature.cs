// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>A single geographic feature parsed from GeoJSON.</summary>
/// <param name="Geometry">The feature's geometry (polygon, line, etc.).</param>
/// <param name="Properties">Key-value properties from the GeoJSON feature.</param>
public sealed record GeoFeature(GeoGeometry Geometry, Dictionary<string, string> Properties)
{
    /// <summary>The number a property holds. Properties are text; this reads one as a number, invariantly (a point is the decimal
    /// separator whatever the culture of the process), as GeoJSON writes numbers.</summary>
    /// <param name="property">The name of the property.</param>
    /// <returns>The number; <see cref="double.NaN"/> when the feature has no such property or it does not hold a number - a
    /// missing value, which a map colours in its colour map's bad colour.</returns>
    public double NumberOf(string property) =>
        Properties.TryGetValue(property, out var text)
        && text.TryReadInvariant(System.Globalization.NumberStyles.Float, out double number)
            ? number
            : double.NaN;
}

/// <summary>Represents the geometry of a GeoJSON feature.</summary>
public sealed record GeoGeometry
{
    private IReadOnlyList<GeoPolygon>? _polygons;
    private IReadOnlyList<IReadOnlyList<GeoPosition>>? _positionRings;

    /// <summary>Type of geometry: "Polygon", "MultiPolygon", "LineString", "MultiLineString".</summary>
    public required string Type { get; init; }

    /// <summary>The flattened view: every ring of the geometry, each a list of positions. For a polygon or a multi-polygon that is
    /// every exterior ring and every hole one after the other, the structure lost (see <see cref="Polygons"/>); for a LineString a
    /// single ring, for a MultiLineString one ring for each line. A geometry built with no rings has none.</summary>
    public IReadOnlyList<IReadOnlyList<GeoPosition>> PositionRings
    {
        get => _positionRings ?? [];
        init => _positionRings = value;
    }

    /// <summary>The polygons of a Polygon or a MultiPolygon, each with the holes cut out of it; none for a line. A geometry read
    /// from GeoJSON keeps its polygons as the file has them. One built from <see cref="PositionRings"/> alone has the ones the
    /// rings say: a Polygon reads its first ring as the exterior and the rest as holes (RFC 7946), a MultiPolygon, whose rings
    /// carry no grouping, reads every ring as an exterior.</summary>
    public IReadOnlyList<GeoPolygon> Polygons
    {
        get => _polygons ??= Kind.PolygonsOf(PositionRings);
        init => _polygons = value;
    }

    /// <summary>The kind <see cref="Type"/> names, which reads, writes and draws the geometry.</summary>
    internal GeoGeometryKind Kind => GeoGeometryKind.Of(Type);

    /// <summary>The geometry of <paramref name="polygons"/>: a Polygon for one, a MultiPolygon for any other number. The one place the
    /// maps that make a region from polygons (the cells of H3, a region a cartogram resized or flowed, the ocean) take its type, its
    /// flattened rings (each polygon's exterior, then its holes) and the polygons themselves.</summary>
    /// <param name="polygons">The polygons, already wound (<see cref="GeoPolygon.FromRings"/>).</param>
    /// <returns>The geometry of the polygons.</returns>
    internal static GeoGeometry OfPolygons(IReadOnlyList<GeoPolygon> polygons) => new()
    {
        Type = polygons.Count == 1 ? PolygonKind.TypeName : MultiPolygonKind.TypeName,
        PositionRings = [.. polygons.SelectMany(polygon => polygon.Holes.Prepend(polygon.Exterior))],
        Polygons = polygons,
    };
}
