// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using MatPlotLibNet.Geo.Data;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Series;

/// <summary>What a map writes of itself into the figure's package data (<c>SeriesDto.PackageData</c>), and reads back: the
/// projection (its name and the numbers it was made with, by their PROJ names), the features (the id of a built-in set, or the
/// features themselves as a GeoJSON FeatureCollection), and whether the features are already projected. The one writer and the one
/// reader of that shape.</summary>
internal static class GeoPackageData
{
    private const string ProjectionKey = "projection", NameKey = "name", ParametersKey = "parameters";
    private const string SourceKey = "source", FeaturesKey = "features", RawProjectedKey = "rawProjected";

    /// <summary>The package data of <paramref name="series"/>.</summary>
    internal static JsonElement Write(GeoPolygonSeries series)
    {
        var parameters = new JsonObject();
        foreach (var (key, value) in series.Projection.ParametersOf()) parameters[key] = value;

        var data = new JsonObject
        {
            [ProjectionKey] = new JsonObject { [NameKey] = series.Projection.Name, [ParametersKey] = parameters },
        };

        if (NaturalEarth110m.SourceOf(series.Features) is { } source)
            data[SourceKey] = source;
        else
            data[FeaturesKey] = JsonNode.Parse(GeoJsonWriter.Write(series.Features));

        if (series.IsRawProjected) data[RawProjectedKey] = true;

        using var document = JsonDocument.Parse(data.ToJsonString());
        return document.RootElement.Clone();
    }

    /// <summary>The map the package data describes: its projection, its features and whether they are raw-projected.</summary>
    /// <returns>The parts; <see langword="null"/> when the data does not describe a map this package can make (no data, a
    /// projection or a source it does not know, features it cannot read).</returns>
    internal static GeoMapData? Read(JsonElement? data)
    {
        if (data is not { ValueKind: JsonValueKind.Object } root) return null;
        if (!root.TryGetProperty(ProjectionKey, out var projectionElement) || projectionElement.ValueKind != JsonValueKind.Object) return null;
        if (!projectionElement.TryGetProperty(NameKey, out var name) || name.GetString() is not { } projectionName) return null;

        var parameters = new Dictionary<string, double>();
        if (projectionElement.TryGetProperty(ParametersKey, out var written) && written.ValueKind == JsonValueKind.Object)
        {
            foreach (var parameter in written.EnumerateObject())
            {
                if (parameter.Value.ValueKind != JsonValueKind.Number) return null;
                parameters[parameter.Name] = parameter.Value.GetDouble();
            }
        }

        if (GeoProjectionExtensions.Make(projectionName, parameters) is not { } projection) return null;

        List<GeoFeature>? features = null;
        if (root.TryGetProperty(SourceKey, out var source))
            features = source.GetString() is { } id ? NaturalEarth110m.OfSource(id) : null;
        else if (root.TryGetProperty(FeaturesKey, out var embedded) && embedded.ValueKind == JsonValueKind.Object)
            features = GeoJsonReader.Parse(embedded.GetRawText());
        if (features is null) return null;

        bool raw = root.TryGetProperty(RawProjectedKey, out var rawElement) && rawElement.ValueKind == JsonValueKind.True;
        return new GeoMapData(projection, features, raw);
    }
}

/// <summary>What the package data of a map describes.</summary>
/// <param name="Projection">The projection the map is drawn in.</param>
/// <param name="Features">The features of the map.</param>
/// <param name="RawProjected">Whether the features are already in the plane of the projection.</param>
internal readonly record struct GeoMapData(IGeoProjection Projection, List<GeoFeature> Features, bool RawProjected);
