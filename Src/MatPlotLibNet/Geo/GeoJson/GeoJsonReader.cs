// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>Parses GeoJSON text into <see cref="GeoJsonDocument"/> instances.</summary>
public static class GeoJsonReader
{
    /// <summary>Parses a GeoJSON string.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the JSON cannot be parsed as valid GeoJSON.</exception>
    public static GeoJsonDocument FromJson(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid GeoJSON: failed to parse JSON.", ex);
        }

        return ParseDocument(doc.RootElement);
    }

    /// <summary>Reads a GeoJSON file from disk and parses it.</summary>
    public static GeoJsonDocument FromFile(string path) => FromJson(File.ReadAllText(path));

    // ── Internal parse helpers ────────────────────────────────────────

    private static GeoJsonDocument ParseDocument(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeProp))
            throw new InvalidOperationException("Invalid GeoJSON: missing 'type' property.");

        string type = typeProp.GetString() ?? throw new InvalidOperationException("GeoJSON 'type' is null.");

        return type switch
        {
            "FeatureCollection" => new GeoJsonDocument(type, FeatureCollection: ParseFeatureCollection(root)),
            "Feature" => new GeoJsonDocument(type, Feature: ParseFeature(root)),
            _ => new GeoJsonDocument(type, Geometry: ParseGeometry(root))
        };
    }

    private static GeoJsonFeatureCollection ParseFeatureCollection(JsonElement el)
    {
        if (!el.TryGetProperty("features", out var featuresEl))
            return new GeoJsonFeatureCollection([]);

        var features = featuresEl.EnumerateArray()
            .Select(ParseFeature)
            .ToArray();

        return new GeoJsonFeatureCollection(features);
    }

    private static GeoJsonFeature ParseFeature(JsonElement el)
    {
        GeoJsonGeometry? geometry = null;
        if (el.TryGetProperty("geometry", out var geomEl) && geomEl.ValueKind != JsonValueKind.Null)
            geometry = ParseGeometry(geomEl);

        IReadOnlyDictionary<string, JsonElement>? properties = null;
        if (el.TryGetProperty("properties", out var propsEl) && propsEl.ValueKind == JsonValueKind.Object)
            properties = propsEl.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

        return new GeoJsonFeature(geometry, properties);
    }

    private static GeoJsonGeometry ParseGeometry(JsonElement el)
    {
        if (!el.TryGetProperty("type", out var typeProp))
            throw new InvalidOperationException("GeoJSON geometry missing 'type'.");

        string type = typeProp.GetString() ?? throw new InvalidOperationException("GeoJSON geometry 'type' is null.");

        var geoType = type switch
        {
            "Point" => GeoJsonGeometryType.Point,
            "MultiPoint" => GeoJsonGeometryType.MultiPoint,
            "LineString" => GeoJsonGeometryType.LineString,
            "MultiLineString" => GeoJsonGeometryType.MultiLineString,
            "Polygon" => GeoJsonGeometryType.Polygon,
            "MultiPolygon" => GeoJsonGeometryType.MultiPolygon,
            "GeometryCollection" => GeoJsonGeometryType.GeometryCollection,
            _ => throw new InvalidOperationException($"Unknown GeoJSON geometry type: {type}")
        };

        return geoType switch
        {
            GeoJsonGeometryType.Point => new GeoJsonGeometry(geoType,
                Coordinates: [ParsePosition(el.GetProperty("coordinates"))]),

            GeoJsonGeometryType.MultiPoint or GeoJsonGeometryType.LineString => new GeoJsonGeometry(geoType,
                Coordinates: ParsePositionArray(el.GetProperty("coordinates"))),

            GeoJsonGeometryType.MultiLineString => new GeoJsonGeometry(geoType,
                CoordinateRings: ParseRings(el.GetProperty("coordinates"))),

            GeoJsonGeometryType.Polygon => new GeoJsonGeometry(geoType,
                CoordinateRings: ParseRings(el.GetProperty("coordinates"))),

            GeoJsonGeometryType.MultiPolygon => new GeoJsonGeometry(geoType,
                MultiPolygonRings: ParseMultiPolygon(el.GetProperty("coordinates"))),

            GeoJsonGeometryType.GeometryCollection => new GeoJsonGeometry(geoType,
                Geometries: el.GetProperty("geometries").EnumerateArray()
                    .Select(ParseGeometry).ToArray()),

            _ => throw new InvalidOperationException($"Unhandled geometry type: {geoType}")
        };
    }

    private static double[] ParsePosition(JsonElement el)
    {
        var arr = el.EnumerateArray().ToArray();
        return arr.Select(v => v.GetDouble()).ToArray();
    }

    private static double[][] ParsePositionArray(JsonElement el) =>
        el.EnumerateArray().Select(ParsePosition).ToArray();

    private static double[][][] ParseRings(JsonElement el) =>
        el.EnumerateArray().Select(ParsePositionArray).ToArray();

    private static double[][][][] ParseMultiPolygon(JsonElement el) =>
        el.EnumerateArray().Select(ParseRings).ToArray();
}
