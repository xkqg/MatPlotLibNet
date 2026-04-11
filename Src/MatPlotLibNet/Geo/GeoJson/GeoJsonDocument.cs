// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>GeoJSON geometry type discriminator.</summary>
public enum GeoJsonGeometryType
{
    Point,
    MultiPoint,
    LineString,
    MultiLineString,
    Polygon,
    MultiPolygon,
    GeometryCollection
}

/// <summary>A GeoJSON geometry object containing typed coordinate arrays.</summary>
/// <param name="Type">The geometry type.</param>
/// <param name="Coordinates">Flat coordinate array for Point (one [lon,lat] pair) or LineString (N [lon,lat] pairs).</param>
/// <param name="CoordinateRings">Nested rings for Polygon: outer ring + optional holes.</param>
/// <param name="MultiPolygonRings">Array of polygons, each containing rings, for MultiPolygon.</param>
/// <param name="Geometries">Child geometries for GeometryCollection.</param>
public sealed record GeoJsonGeometry(
    GeoJsonGeometryType Type,
    double[][]? Coordinates = null,
    double[][][]? CoordinateRings = null,
    double[][][][]? MultiPolygonRings = null,
    GeoJsonGeometry[]? Geometries = null);

/// <summary>A GeoJSON Feature containing a geometry and optional property bag.</summary>
/// <param name="Geometry">The geometry for this feature.</param>
/// <param name="Properties">The property bag (arbitrary JSON values keyed by string).</param>
public sealed record GeoJsonFeature(
    GeoJsonGeometry? Geometry,
    IReadOnlyDictionary<string, JsonElement>? Properties = null);

/// <summary>A GeoJSON FeatureCollection.</summary>
/// <param name="Features">The array of features.</param>
public sealed record GeoJsonFeatureCollection(GeoJsonFeature[] Features);

/// <summary>Top-level GeoJSON document. May wrap a FeatureCollection, a single Feature, or a bare geometry.</summary>
/// <param name="Type">The GeoJSON type string (e.g., "FeatureCollection", "Feature", "Polygon").</param>
/// <param name="FeatureCollection">Set when Type is "FeatureCollection".</param>
/// <param name="Feature">Set when Type is "Feature".</param>
/// <param name="Geometry">Set when Type is a geometry type.</param>
public sealed record GeoJsonDocument(
    string Type,
    GeoJsonFeatureCollection? FeatureCollection = null,
    GeoJsonFeature? Feature = null,
    GeoJsonGeometry? Geometry = null);
