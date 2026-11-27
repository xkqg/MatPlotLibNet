// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using MatPlotLibNet.Geo.GeoJson;

namespace MatPlotLibNet.Geo.Data;

/// <summary>Loads Natural Earth 110m resolution geographic data from embedded GeoJSON resources.
/// Data includes coastlines, the countries and the boundaries between them on land at the coarsest (global overview) resolution.</summary>
public static class NaturalEarth110m
{
    /// <summary>The id a map's JSON gives the country set: the Natural Earth file it is read from.</summary>
    internal const string CountriesSource = "ne_110m_admin_0_countries";

    /// <summary>The id a map's JSON gives the coastline set.</summary>
    internal const string CoastlinesSource = "ne_110m_coastline";

    /// <summary>The id a map's JSON gives the set of boundary lines between countries on land.</summary>
    internal const string BoundaryLinesSource = "ne_110m_admin_0_boundary_lines_land";

    private static Lazy<List<GeoFeature>> _coastlines = Loaded("coastlines.geojson");
    private static Lazy<List<GeoFeature>> _countries = Loaded("countries.geojson");
    private static Lazy<List<GeoFeature>> _boundaryLines = Loaded("boundary_lines_land.geojson");

    /// <summary>A set read from its embedded resource the first time it is asked for, by one caller however many ask at once: the
    /// callers that asked together get the one list, which a map names its built-in set by.</summary>
    private static Lazy<List<GeoFeature>> Loaded(string resource) =>
        new(() => LoadResource(resource), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>The id of the built-in set <paramref name="features"/> is, when it is that very list; otherwise
    /// <see langword="null"/> (a filtered or a made set is not a built-in one). A set that was never loaded is no list a map holds,
    /// so none is loaded to ask.</summary>
    /// <param name="features">A feature list a map holds.</param>
    /// <returns>The id of the built-in set, or <see langword="null"/> when the list is not one.</returns>
    internal static string? SourceOf(List<GeoFeature> features) =>
        IsTheListOf(_countries, features) ? CountriesSource
        : IsTheListOf(_coastlines, features) ? CoastlinesSource
        : IsTheListOf(_boundaryLines, features) ? BoundaryLinesSource
        : null;

    private static bool IsTheListOf(Lazy<List<GeoFeature>> set, List<GeoFeature> features) =>
        set.IsValueCreated && ReferenceEquals(features, set.Value);

    /// <summary>The built-in set of the given id; <see langword="null"/> for an id this package does not know.</summary>
    /// <param name="source">The id of a built-in set.</param>
    /// <returns>The set, or <see langword="null"/> for an id this package does not know.</returns>
    internal static List<GeoFeature>? OfSource(string source) => source switch
    {
        CountriesSource => Countries(),
        CoastlinesSource => Coastlines(),
        BoundaryLinesSource => BoundaryLines(),
        _ => null,
    };

    /// <summary>Loads coastline features (line geometries).</summary>
    /// <returns>The coastlines, read once and shared.</returns>
    public static List<GeoFeature> Coastlines() => _coastlines.Value;

    /// <summary>Loads the boundaries between countries on land (line geometries): Natural Earth's <c>admin_0_boundary_lines_land</c>,
    /// what cartopy draws as its <c>BORDERS</c> feature. A coast is no boundary; the coastlines draw it.</summary>
    /// <returns>The boundary lines, read once and shared.</returns>
    public static List<GeoFeature> BoundaryLines() => _boundaryLines.Value;

    /// <summary>Loads country boundary features (polygon geometries).</summary>
    /// <returns>The countries, read once and shared.</returns>
    public static List<GeoFeature> Countries() => _countries.Value;

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
