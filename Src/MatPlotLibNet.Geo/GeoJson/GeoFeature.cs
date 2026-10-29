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
        && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double number)
            ? number
            : double.NaN;
}

/// <summary>Represents the geometry of a GeoJSON feature.</summary>
public sealed record GeoGeometry
{
    private IReadOnlyList<GeoPolygon>? _polygons;

    /// <summary>Type of geometry: "Polygon", "MultiPolygon", "LineString", "MultiLineString".</summary>
    public required string Type { get; init; }

    /// <summary>The flattened view: every ring of the geometry, each a list of (lon, lat) pairs. For a polygon or a multi-polygon
    /// that is every exterior ring and every hole one after the other, the structure lost (see <see cref="Polygons"/>); for a
    /// LineString a single ring, for a MultiLineString one ring for each line.</summary>
    public required List<List<(double Lon, double Lat)>> Rings { get; init; }

    /// <summary>The polygons of a Polygon or a MultiPolygon, each with the holes cut out of it; none for a line. A geometry read
    /// from GeoJSON keeps its polygons as the file has them. One built from <see cref="Rings"/> alone has the ones the rings
    /// say: a Polygon reads its first ring as the exterior and the rest as holes (RFC 7946), a MultiPolygon, whose rings carry no
    /// grouping, reads every ring as an exterior.</summary>
    public IReadOnlyList<GeoPolygon> Polygons
    {
        get => _polygons ??= PolygonsOfTheRings();
        init => _polygons = value;
    }

    private IReadOnlyList<GeoPolygon> PolygonsOfTheRings()
    {
        static IReadOnlyList<GeoPosition> Positions(List<(double Lon, double Lat)> ring) => [.. ring.Select(p => new GeoPosition(p.Lon, p.Lat))];

        return Type switch
        {
            "Polygon" when Rings.Count > 0 => [GeoPolygon.FromRings(Positions(Rings[0]), Rings.Skip(1).Select(Positions))],
            "MultiPolygon" => [.. Rings.Select(ring => GeoPolygon.FromRings(Positions(ring), []))],
            _ => [],
        };
    }
}
