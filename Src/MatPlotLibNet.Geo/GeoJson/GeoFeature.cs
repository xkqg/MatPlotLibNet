// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>A single geographic feature parsed from GeoJSON.</summary>
/// <param name="Geometry">The feature's geometry (polygon, line, etc.).</param>
/// <param name="Properties">Key-value properties from the GeoJSON feature.</param>
public sealed record GeoFeature(GeoGeometry Geometry, Dictionary<string, string> Properties);

/// <summary>Represents the geometry of a GeoJSON feature.</summary>
public sealed record GeoGeometry
{
    /// <summary>Type of geometry: "Polygon", "MultiPolygon", "LineString", "MultiLineString".</summary>
    public required string Type { get; init; }

    /// <summary>For Polygon/MultiPolygon: list of coordinate rings. Each ring is a list of (lon, lat) pairs.
    /// For LineString: a single ring. For MultiLineString: multiple rings.</summary>
    public required List<List<(double Lon, double Lat)>> Rings { get; init; }
}
