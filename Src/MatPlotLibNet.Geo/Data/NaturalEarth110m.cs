// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Data;

/// <summary>Loads Natural Earth 110m resolution geographic data from embedded GeoJSON resources.
/// Data includes coastlines, country borders, and lakes at the coarsest (global overview) resolution.</summary>
public static class NaturalEarth110m
{
    private static List<GeoFeature>? _coastlines;
    private static List<GeoFeature>? _countries;

    /// <summary>Loads coastline features (line geometries).</summary>
    public static List<GeoFeature> Coastlines() =>
        _coastlines ??= LoadResource("coastlines.geojson");

    /// <summary>Loads country boundary features (polygon geometries).</summary>
    public static List<GeoFeature> Countries() =>
        _countries ??= LoadResource("countries.geojson");

    private static List<GeoFeature> LoadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return []; // no embedded data yet — placeholder for real Natural Earth data

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return GeoJsonReader.Parse(reader.ReadToEnd());
    }
}
