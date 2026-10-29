// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>Writes <see cref="GeoFeature"/> objects as a GeoJSON FeatureCollection (RFC 7946): the counterpart of
/// <see cref="GeoJsonReader"/>, which reads exactly what this writes. A polygon is written with its holes, a MultiPolygon with its
/// polygons, a line as it is.</summary>
public static class GeoJsonWriter
{
    /// <summary>Writes a GeoJSON FeatureCollection string.</summary>
    /// <param name="features">The features to write.</param>
    /// <returns>The FeatureCollection, compact. Coordinates are written to the precision a <see cref="double"/> round-trips.</returns>
    public static string Write(IEnumerable<GeoFeature> features)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            foreach (var feature in features) WriteFeature(writer, feature);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteFeature(Utf8JsonWriter writer, GeoFeature feature)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "Feature");
        writer.WriteStartObject("properties");
        foreach (var (key, value) in feature.Properties) writer.WriteString(key, value);
        writer.WriteEndObject();
        writer.WriteStartObject("geometry");
        WriteGeometry(writer, feature.Geometry);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteGeometry(Utf8JsonWriter writer, GeoGeometry geometry)
    {
        writer.WriteString("type", geometry.Type);
        writer.WriteStartArray("coordinates");
        switch (geometry.Type)
        {
            case "Polygon":
                foreach (var polygon in geometry.Polygons) WritePolygonRings(writer, polygon);
                break;
            case "MultiPolygon":
                foreach (var polygon in geometry.Polygons)
                {
                    writer.WriteStartArray();
                    WritePolygonRings(writer, polygon);
                    writer.WriteEndArray();
                }
                break;
            case "LineString":   // the coordinates of a line are its positions, not a ring of them
                if (geometry.Rings.Count > 0)
                    foreach (var (lon, lat) in geometry.Rings[0]) WritePosition(writer, new GeoPosition(lon, lat));
                break;
            default:   // MultiLineString
                foreach (var ring in geometry.Rings) WriteRing(writer, ring.Select(p => new GeoPosition(p.Lon, p.Lat)));
                break;
        }

        writer.WriteEndArray();
    }

    private static void WritePolygonRings(Utf8JsonWriter writer, GeoPolygon polygon)
    {
        WriteRing(writer, polygon.Exterior);
        foreach (var hole in polygon.Holes) WriteRing(writer, hole);
    }

    private static void WriteRing(Utf8JsonWriter writer, IEnumerable<GeoPosition> ring)
    {
        writer.WriteStartArray();
        foreach (var position in ring) WritePosition(writer, position);
        writer.WriteEndArray();
    }

    private static void WritePosition(Utf8JsonWriter writer, GeoPosition position)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(position.Lon);
        writer.WriteNumberValue(position.Lat);
        writer.WriteEndArray();
    }
}
