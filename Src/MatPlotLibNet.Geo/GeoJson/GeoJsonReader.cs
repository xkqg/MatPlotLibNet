// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>Parses GeoJSON FeatureCollection strings into <see cref="GeoFeature"/> objects.</summary>
public static class GeoJsonReader
{
    /// <summary>Parses a GeoJSON FeatureCollection string.</summary>
    /// <param name="json">GeoJSON string.</param>
    /// <returns>List of parsed features.</returns>
    public static List<GeoFeature> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var features = new List<GeoFeature>();

        if (root.TryGetProperty("features", out var featuresArray))
        {
            foreach (var f in featuresArray.EnumerateArray())
            {
                var geometry = ParseGeometry(f.GetProperty("geometry"));
                var properties = ParseProperties(f.GetProperty("properties"));
                features.Add(new GeoFeature(geometry, properties));
            }
        }

        return features;
    }

    private static GeoGeometry ParseGeometry(JsonElement geom)
    {
        var type = geom.GetProperty("type").GetString() ?? "Unknown";
        var rings = new List<List<(double Lon, double Lat)>>();

        if (type == "Polygon")
        {
            foreach (var ring in geom.GetProperty("coordinates").EnumerateArray())
                rings.Add(ParseRing(ring));
        }
        else if (type == "MultiPolygon")
        {
            foreach (var polygon in geom.GetProperty("coordinates").EnumerateArray())
                foreach (var ring in polygon.EnumerateArray())
                    rings.Add(ParseRing(ring));
        }
        else if (type == "LineString")
        {
            rings.Add(ParseRing(geom.GetProperty("coordinates")));
        }
        else if (type == "MultiLineString")
        {
            foreach (var line in geom.GetProperty("coordinates").EnumerateArray())
                rings.Add(ParseRing(line));
        }

        return new GeoGeometry { Type = type, Rings = rings };
    }

    private static List<(double Lon, double Lat)> ParseRing(JsonElement coords)
    {
        var ring = new List<(double, double)>();
        foreach (var point in coords.EnumerateArray())
        {
            var lon = point[0].GetDouble();
            var lat = point[1].GetDouble();
            ring.Add((lon, lat));
        }
        return ring;
    }

    private static Dictionary<string, string> ParseProperties(JsonElement props)
    {
        var dict = new Dictionary<string, string>();
        foreach (var prop in props.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                ? ""
                : prop.Value.ToString();
        }
        return dict;
    }
}
