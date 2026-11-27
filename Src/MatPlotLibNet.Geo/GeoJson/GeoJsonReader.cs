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
        var type = geom.GetProperty("type").GetString() ?? UnknownGeometryKind.TypeName;
        var rings = new List<IReadOnlyList<GeoPosition>>();
        var polygons = new List<GeoPolygon>();

        // The kind the type names reads its own coordinates; a type the library does not read leaves both lists empty.
        GeoGeometryKind.Of(type).Read(geom, rings, polygons);

        return new GeoGeometry { Type = type, PositionRings = rings, Polygons = polygons };
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
