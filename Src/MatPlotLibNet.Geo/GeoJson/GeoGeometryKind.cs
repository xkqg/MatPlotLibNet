// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>The kind of a GeoJSON geometry: one sealed leaf for each <c>type</c> the library reads, chosen from the type's name in
/// one place (<see cref="Of"/>). A kind reads its coordinates, writes them, builds the polygons its rings stand for, and says
/// whether it encloses an area; the reader, the writer, the geometry and the map series ask it instead of comparing the name.</summary>
internal abstract class GeoGeometryKind
{
    /// <summary>A polygon: its first ring the exterior, the rest its holes.</summary>
    internal static readonly PolygonKind Polygon = new();

    /// <summary>Several polygons.</summary>
    internal static readonly MultiPolygonKind MultiPolygon = new();

    /// <summary>One line.</summary>
    internal static readonly LineStringKind LineString = new();

    /// <summary>Several lines.</summary>
    internal static readonly MultiLineStringKind MultiLineString = new();

    /// <summary>A type the library does not read.</summary>
    internal static readonly UnknownGeometryKind Unknown = new();

    /// <summary>The kind <paramref name="type"/> names: the one place a GeoJSON type name is mapped to its kind. A name the
    /// library does not read (a point, a misspelling; the names are case-sensitive, as RFC 7946 has them) is
    /// <see cref="Unknown"/>.</summary>
    /// <param name="type">The geometry's <c>type</c>.</param>
    internal static GeoGeometryKind Of(string type) => type switch
    {
        PolygonKind.TypeName => Polygon,
        MultiPolygonKind.TypeName => MultiPolygon,
        LineStringKind.TypeName => LineString,
        MultiLineStringKind.TypeName => MultiLineString,
        _ => Unknown,
    };

    /// <summary>Whether the geometry encloses an area (a map fills it) rather than being lines (a map strokes them).</summary>
    internal virtual bool IsArea => false;

    /// <summary>Reads the coordinates of <paramref name="geometry"/>: every ring into <paramref name="rings"/>, in the order the
    /// file has them, and every polygon into <paramref name="polygons"/>.</summary>
    /// <param name="geometry">The geometry object of the file.</param>
    /// <param name="rings">The flattened rings the geometry is read into.</param>
    /// <param name="polygons">The polygons the geometry is read into.</param>
    internal abstract void Read(JsonElement geometry, List<IReadOnlyList<GeoPosition>> rings, List<GeoPolygon> polygons);

    /// <summary>The polygons a geometry built from its rings alone stands for; none for a kind that is not an area.</summary>
    /// <param name="rings">The geometry's rings.</param>
    internal virtual IReadOnlyList<GeoPolygon> PolygonsOf(IReadOnlyList<IReadOnlyList<GeoPosition>> rings) => [];

    /// <summary>Writes the coordinates of <paramref name="geometry"/> inside the <c>coordinates</c> array.</summary>
    /// <param name="writer">The writer, inside the array.</param>
    /// <param name="geometry">The geometry to write.</param>
    internal abstract void Write(Utf8JsonWriter writer, GeoGeometry geometry);
}

/// <summary>A polygon: its first ring the exterior and the rest its holes (RFC 7946).</summary>
internal sealed class PolygonKind : GeoGeometryKind
{
    /// <summary>The GeoJSON <c>type</c> of the kind.</summary>
    internal const string TypeName = "Polygon";

    /// <inheritdoc />
    internal override bool IsArea => true;

    /// <inheritdoc />
    internal override void Read(JsonElement geometry, List<IReadOnlyList<GeoPosition>> rings, List<GeoPolygon> polygons) =>
        polygons.Add(geometry.GetProperty("coordinates").ReadPolygon(rings));

    /// <inheritdoc />
    internal override IReadOnlyList<GeoPolygon> PolygonsOf(IReadOnlyList<IReadOnlyList<GeoPosition>> rings) =>
        rings.Count > 0 ? [GeoPolygon.FromRings(rings[0], rings.Skip(1))] : [];

    /// <inheritdoc />
    internal override void Write(Utf8JsonWriter writer, GeoGeometry geometry)
    {
        foreach (var polygon in geometry.Polygons) writer.WritePolygonRings(polygon);
    }
}

/// <summary>Several polygons, each with its holes. Built from rings alone, which carry no grouping, every ring is an exterior.</summary>
internal sealed class MultiPolygonKind : GeoGeometryKind
{
    /// <summary>The GeoJSON <c>type</c> of the kind.</summary>
    internal const string TypeName = "MultiPolygon";

    /// <inheritdoc />
    internal override bool IsArea => true;

    /// <inheritdoc />
    internal override void Read(JsonElement geometry, List<IReadOnlyList<GeoPosition>> rings, List<GeoPolygon> polygons)
    {
        foreach (var polygon in geometry.GetProperty("coordinates").EnumerateArray())
            polygons.Add(polygon.ReadPolygon(rings));
    }

    /// <inheritdoc />
    internal override IReadOnlyList<GeoPolygon> PolygonsOf(IReadOnlyList<IReadOnlyList<GeoPosition>> rings) =>
        [.. rings.Select(ring => GeoPolygon.FromRings(ring, []))];

    /// <inheritdoc />
    internal override void Write(Utf8JsonWriter writer, GeoGeometry geometry)
    {
        foreach (var polygon in geometry.Polygons)
        {
            writer.WriteStartArray();
            writer.WritePolygonRings(polygon);
            writer.WriteEndArray();
        }
    }
}

/// <summary>One line: its coordinates are its positions, not a ring of them.</summary>
internal sealed class LineStringKind : GeoGeometryKind
{
    /// <summary>The GeoJSON <c>type</c> of the kind.</summary>
    internal const string TypeName = "LineString";

    /// <inheritdoc />
    internal override void Read(JsonElement geometry, List<IReadOnlyList<GeoPosition>> rings, List<GeoPolygon> polygons) =>
        rings.Add(geometry.GetProperty("coordinates").ReadRing());

    /// <inheritdoc />
    internal override void Write(Utf8JsonWriter writer, GeoGeometry geometry)
    {
        if (geometry.PositionRings.Count > 0)
            foreach (var position in geometry.PositionRings[0]) writer.WritePosition(position);
    }
}

/// <summary>Several lines, one ring for each.</summary>
internal sealed class MultiLineStringKind : GeoGeometryKind
{
    /// <summary>The GeoJSON <c>type</c> of the kind.</summary>
    internal const string TypeName = "MultiLineString";

    /// <inheritdoc />
    internal override void Read(JsonElement geometry, List<IReadOnlyList<GeoPosition>> rings, List<GeoPolygon> polygons)
    {
        foreach (var line in geometry.GetProperty("coordinates").EnumerateArray())
            rings.Add(line.ReadRing());
    }

    /// <inheritdoc />
    internal override void Write(Utf8JsonWriter writer, GeoGeometry geometry) => WriteLines(writer, geometry);

    /// <summary>Writes every ring of <paramref name="geometry"/> as a line.</summary>
    internal static void WriteLines(Utf8JsonWriter writer, GeoGeometry geometry)
    {
        foreach (var ring in geometry.PositionRings) writer.WriteRing(ring);
    }
}

/// <summary>A type the library does not read (a point, say). Read from a file it has no rings; built by a caller it keeps the
/// rings it was given, and they are written as lines, the shape that holds any list of rings, so nothing put in is lost on the way
/// out.</summary>
internal sealed class UnknownGeometryKind : GeoGeometryKind
{
    /// <summary>The <c>type</c> a geometry that names none is given.</summary>
    internal const string TypeName = "Unknown";

    /// <inheritdoc />
    internal override void Read(JsonElement geometry, List<IReadOnlyList<GeoPosition>> rings, List<GeoPolygon> polygons) { }

    /// <inheritdoc />
    internal override void Write(Utf8JsonWriter writer, GeoGeometry geometry) => MultiLineStringKind.WriteLines(writer, geometry);
}

/// <summary>The coordinates of GeoJSON as members of the values that carry them: the JSON array a ring or a polygon is read
/// from, and the writer they are written to.</summary>
internal static class GeoJsonCoordinateExtensions
{
    extension(JsonElement coordinates)
    {
        /// <summary>The ring this array of positions is.</summary>
        internal IReadOnlyList<GeoPosition> ReadRing()
        {
            var ring = new List<GeoPosition>();
            foreach (var point in coordinates.EnumerateArray())
                ring.Add(new GeoPosition(point[0].GetDouble(), point[1].GetDouble()));
            return ring;
        }

        /// <summary>The polygon this array of rings is: its first ring the exterior and the rest its holes, wound by their signed
        /// area. Every ring read is also added to <paramref name="flattened"/>, in the order the file has them.</summary>
        /// <param name="flattened">The flattened rings of the geometry.</param>
        internal GeoPolygon ReadPolygon(List<IReadOnlyList<GeoPosition>> flattened)
        {
            var parsed = coordinates.EnumerateArray().Select(ring => ring.ReadRing()).ToList();
            flattened.AddRange(parsed);
            return GeoPolygon.FromRings(parsed.Count > 0 ? parsed[0] : [], parsed.Skip(1));
        }
    }

    extension(Utf8JsonWriter writer)
    {
        /// <summary>Writes a polygon's rings: the exterior, then every hole.</summary>
        internal void WritePolygonRings(GeoPolygon polygon)
        {
            writer.WriteRing(polygon.Exterior);
            foreach (var hole in polygon.Holes) writer.WriteRing(hole);
        }

        /// <summary>Writes one ring as an array of positions.</summary>
        internal void WriteRing(IEnumerable<GeoPosition> ring)
        {
            writer.WriteStartArray();
            foreach (var position in ring) writer.WritePosition(position);
            writer.WriteEndArray();
        }

        /// <summary>Writes one position as <c>[lon, lat]</c>.</summary>
        internal void WritePosition(GeoPosition position)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(position.Lon);
            writer.WriteNumberValue(position.Lat);
            writer.WriteEndArray();
        }
    }
}
