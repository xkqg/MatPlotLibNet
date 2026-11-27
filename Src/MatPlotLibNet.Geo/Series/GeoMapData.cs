// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Series;

/// <summary>What the package data of a map describes, and the one writer and the one reader of its shape in the figure's package
/// data (<c>SeriesDto.PackageData</c>): the projection (its name and the numbers it was made with, by their PROJ names), the
/// features (the id of a built-in set, or the features themselves as a GeoJSON FeatureCollection), and whether the features are
/// already projected.</summary>
/// <param name="Projection">The projection the map is drawn in.</param>
/// <param name="Features">The features of the map.</param>
/// <param name="RawProjected">Whether the features are already in the plane of the projection.</param>
internal readonly record struct GeoMapData(IGeoProjection Projection, List<GeoFeature> Features, bool RawProjected)
{
    private const string ProjectionKey = "projection", NameKey = "name", ParametersKey = "parameters";
    private const string SourceKey = "source", FeaturesKey = "features", RawProjectedKey = "rawProjected";

    /// <summary>What the package data of a map holds, in words, for a reader that tells an author what to write: the projection by
    /// its name and its numbers, the features by a built-in set or as GeoJSON, and whether they are already projected. Made from
    /// the same keys and tables <see cref="Read"/> reads by.</summary>
    internal static string Described { get; } =
        $"{{\"{ProjectionKey}\": {{\"{NameKey}\": one of {GeoProjectionExtensions.KindsDescribed}; \"{ParametersKey}\": the numbers in brackets "
        + $"by name, in degrees, each one left out taking the projection's default}}, then either \"{SourceKey}\": a built-in set, "
        + $"{NaturalEarth110m.CountriesSource} (the countries), {NaturalEarth110m.CoastlinesSource} (the coastlines) or "
        + $"{NaturalEarth110m.BoundaryLinesSource} (the borders between countries on land), or \"{FeaturesKey}\": "
        + $"a GeoJSON FeatureCollection in longitude and latitude; \"{RawProjectedKey}\": true when the features are already in the plane "
        + "of the projection}";

    /// <summary>The package data of this map.</summary>
    internal JsonElement Write()
    {
        var parameters = new JsonObject();
        foreach (var (key, value) in Projection.ParametersOf()) parameters[key] = value;

        var data = new JsonObject
        {
            [ProjectionKey] = new JsonObject { [NameKey] = Projection.Name, [ParametersKey] = parameters },
        };

        if (NaturalEarth110m.SourceOf(Features) is { } source)
            data[SourceKey] = source;
        else
            data[FeaturesKey] = JsonNode.Parse(GeoJsonWriter.Write(Features));

        if (RawProjected) data[RawProjectedKey] = true;

        using var document = JsonDocument.Parse(data.ToJsonString());
        return document.RootElement.Clone();
    }

    /// <summary>The map the package data describes: its projection, its features and whether they are raw-projected, or why the
    /// data describes none this package can make.</summary>
    /// <param name="data">The package data of a series, or <see langword="null"/> for a series that has none.</param>
    /// <returns>The map, or no map and the reason (no data, a projection or a source it does not know, features it cannot read), in
    /// words the chart diagnostics and the MCP server's refusal print.</returns>
    internal static GeoMapRead Read(JsonElement? data)
    {
        if (data is not { } given) return GeoMapRead.No("it carries no package data");
        if (given.ValueKind != JsonValueKind.Object) return GeoMapRead.No("its package data is no JSON object");
        if (!given.TryGetProperty(ProjectionKey, out var projectionElement) || projectionElement.ValueKind != JsonValueKind.Object
            || !projectionElement.TryGetProperty(NameKey, out var name) || name.ValueKind != JsonValueKind.String)
            return GeoMapRead.No("its package data names no projection");
        string projectionName = name.GetString()!;

        var parameters = new Dictionary<string, double>();
        if (projectionElement.TryGetProperty(ParametersKey, out var written) && written.ValueKind == JsonValueKind.Object)
        {
            foreach (var parameter in written.EnumerateObject())
            {
                if (parameter.Value.ValueKind != JsonValueKind.Number)
                    return GeoMapRead.No($"the number {parameter.Name} of its projection is no number");
                parameters[parameter.Name] = parameter.Value.GetDouble();
            }
        }

        if (GeoProjectionExtensions.Make(projectionName, parameters) is not { } projection)
        {
            string numbers = parameters.Count == 0 ? "no numbers" : "the numbers " + string.Join(", ", parameters.Keys);
            return GeoMapRead.No($"its projection '{projectionName}' with {numbers} is not one this package makes");
        }

        List<GeoFeature>? features;
        if (given.TryGetProperty(SourceKey, out var source))
        {
            if (source.ValueKind != JsonValueKind.String) return GeoMapRead.No("its source is no name of a built-in set");
            string id = source.GetString()!;
            features = NaturalEarth110m.OfSource(id);
            if (features is null) return GeoMapRead.No($"its source '{id}' is not a built-in set of this package");
        }
        else if (given.TryGetProperty(FeaturesKey, out var embedded) && embedded.ValueKind == JsonValueKind.Object)
            features = GeoJsonReader.Parse(embedded.GetRawText());
        else
            return GeoMapRead.No("it names no features: neither a built-in source nor a GeoJSON FeatureCollection");

        bool raw = given.TryGetProperty(RawProjectedKey, out var rawElement) && rawElement.ValueKind == JsonValueKind.True;
        return new GeoMapRead(new GeoMapData(projection, features, raw), null);
    }
}

/// <summary>What <see cref="GeoMapData.Read"/> made of a map's package data: the map, or why there is none.</summary>
/// <param name="Map">The map; <see langword="null"/> when the data describes none this package can make.</param>
/// <param name="Why">Why there is no map, in plain words; <see langword="null"/> when there is one.</param>
internal readonly record struct GeoMapRead(GeoMapData? Map, string? Why)
{
    /// <summary>No map, for <paramref name="why"/>.</summary>
    /// <param name="why">What is wrong with the map.</param>
    /// <returns>A result that holds no map.</returns>
    internal static GeoMapRead No(string why) => new(null, why);
}
