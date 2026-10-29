// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Data;

/// <summary>Loads Natural Earth 110m resolution geographic data from embedded GeoJSON resources.
/// Data includes coastlines and country borders at the coarsest (global overview) resolution.</summary>
public static class NaturalEarth110m
{
    /// <summary>The id a map's JSON gives the country set: the Natural Earth file it is read from.</summary>
    internal const string CountriesSource = "ne_110m_admin_0_countries";

    /// <summary>The id a map's JSON gives the coastline set.</summary>
    internal const string CoastlinesSource = "ne_110m_coastline";

    private static List<GeoFeature>? _coastlines;
    private static List<GeoFeature>? _countries;

    /// <summary>The id of the built-in set <paramref name="features"/> is, when it is that very list; otherwise
    /// <see langword="null"/> (a filtered or a made set is not a built-in one).</summary>
    internal static string? SourceOf(List<GeoFeature> features) =>
        ReferenceEquals(features, Countries()) ? CountriesSource
        : ReferenceEquals(features, Coastlines()) ? CoastlinesSource
        : null;

    /// <summary>The built-in set of the given id; <see langword="null"/> for an id this package does not know.</summary>
    internal static List<GeoFeature>? OfSource(string source) => source switch
    {
        CountriesSource => Countries(),
        CoastlinesSource => Coastlines(),
        _ => null,
    };

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
            .FirstOrDefault(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase))!;
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return GeoJsonReader.Parse(reader.ReadToEnd());
    }
}
