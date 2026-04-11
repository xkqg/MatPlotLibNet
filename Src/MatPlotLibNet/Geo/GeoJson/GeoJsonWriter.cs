// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>Serializes <see cref="GeoJsonDocument"/> instances back to compact GeoJSON text.</summary>
public static class GeoJsonWriter
{
    /// <summary>Serializes a <see cref="GeoJsonDocument"/> to a compact GeoJSON string.</summary>
    public static string ToJson(GeoJsonDocument document)
    {
        var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false });
        WriteDocument(writer, document);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteDocument(Utf8JsonWriter w, GeoJsonDocument doc)
    {
        w.WriteStartObject();
        w.WriteString("type", doc.Type);

        if (doc.FeatureCollection is not null)
        {
            w.WriteStartArray("features");
            foreach (var f in doc.FeatureCollection.Features)
                WriteFeature(w, f);
            w.WriteEndArray();
        }
        else if (doc.Feature is not null)
        {
            WriteFeatureBody(w, doc.Feature);
        }
        else if (doc.Geometry is not null)
        {
            WriteGeometryBody(w, doc.Geometry);
        }

        w.WriteEndObject();
    }

    private static void WriteFeature(Utf8JsonWriter w, GeoJsonFeature feature)
    {
        w.WriteStartObject();
        w.WriteString("type", "Feature");
        WriteFeatureBody(w, feature);
        w.WriteEndObject();
    }

    private static void WriteFeatureBody(Utf8JsonWriter w, GeoJsonFeature feature)
    {
        w.WritePropertyName("geometry");
        if (feature.Geometry is null)
            w.WriteNullValue();
        else
        {
            w.WriteStartObject();
            WriteGeometryBody(w, feature.Geometry);
            w.WriteEndObject();
        }

        w.WritePropertyName("properties");
        if (feature.Properties is null || feature.Properties.Count == 0)
        {
            w.WriteStartObject();
            w.WriteEndObject();
        }
        else
        {
            w.WriteStartObject();
            foreach (var kvp in feature.Properties)
            {
                w.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(w);
            }
            w.WriteEndObject();
        }
    }

    private static void WriteGeometryBody(Utf8JsonWriter w, GeoJsonGeometry geom)
    {
        w.WriteString("type", GeometryTypeName(geom.Type));
        w.WritePropertyName("coordinates");

        switch (geom.Type)
        {
            case GeoJsonGeometryType.Point:
                WritePosition(w, geom.Coordinates![0]);
                break;

            case GeoJsonGeometryType.MultiPoint:
            case GeoJsonGeometryType.LineString:
                WritePositionArray(w, geom.Coordinates!);
                break;

            case GeoJsonGeometryType.MultiLineString:
            case GeoJsonGeometryType.Polygon:
                WriteRings(w, geom.CoordinateRings!);
                break;

            case GeoJsonGeometryType.MultiPolygon:
                w.WriteStartArray();
                foreach (var poly in geom.MultiPolygonRings!)
                    WriteRings(w, poly);
                w.WriteEndArray();
                break;

            case GeoJsonGeometryType.GeometryCollection:
                // GeometryCollection uses "geometries", not "coordinates"
                // Remove the "coordinates" key we just started and write "geometries" instead
                // (We can't un-write, so handle separately)
                break;
        }

        if (geom.Type == GeoJsonGeometryType.GeometryCollection)
        {
            // Undo the orphaned "coordinates" property name by writing null,
            // then write "geometries" array.
            w.WriteNullValue();
            w.WriteStartArray("geometries");
            foreach (var child in geom.Geometries ?? [])
            {
                w.WriteStartObject();
                WriteGeometryBody(w, child);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }
    }

    private static void WritePosition(Utf8JsonWriter w, double[] pos)
    {
        w.WriteStartArray();
        foreach (var v in pos) w.WriteNumberValue(v);
        w.WriteEndArray();
    }

    private static void WritePositionArray(Utf8JsonWriter w, double[][] positions)
    {
        w.WriteStartArray();
        foreach (var pos in positions) WritePosition(w, pos);
        w.WriteEndArray();
    }

    private static void WriteRings(Utf8JsonWriter w, double[][][] rings)
    {
        w.WriteStartArray();
        foreach (var ring in rings) WritePositionArray(w, ring);
        w.WriteEndArray();
    }

    private static string GeometryTypeName(GeoJsonGeometryType type) => type switch
    {
        GeoJsonGeometryType.Point => "Point",
        GeoJsonGeometryType.MultiPoint => "MultiPoint",
        GeoJsonGeometryType.LineString => "LineString",
        GeoJsonGeometryType.MultiLineString => "MultiLineString",
        GeoJsonGeometryType.Polygon => "Polygon",
        GeoJsonGeometryType.MultiPolygon => "MultiPolygon",
        GeoJsonGeometryType.GeometryCollection => "GeometryCollection",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
