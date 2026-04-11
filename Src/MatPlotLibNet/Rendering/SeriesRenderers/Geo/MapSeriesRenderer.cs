// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="MapSeries"/> instances (GeoJSON geometry) onto an <see cref="IRenderContext"/>.</summary>
internal class MapSeriesRenderer : SeriesRenderer<MapSeries>
{
    /// <inheritdoc />
    public MapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(MapSeries series)
    {
        if (series.GeoData is null) return;
        foreach (var feature in ExtractFeatures(series.GeoData))
            RenderFeature(feature, series.Projection, series.FaceColor, series.EdgeColor, series.LineWidth);
    }

    /// <summary>Renders a single GeoJSON feature using the supplied projection and style.</summary>
    internal void RenderFeature(GeoJsonFeature feature, IMapProjection proj,
        Color? face, Color? edge, double lineWidth)
    {
        if (feature.Geometry is null) return;
        RenderGeometry(feature.Geometry, proj, face, edge, lineWidth);
    }

    private void RenderGeometry(GeoJsonGeometry geom, IMapProjection proj,
        Color? face, Color? edge, double lineWidth)
    {
        switch (geom.Type)
        {
            case GeoJsonGeometryType.Polygon:
                if (geom.CoordinateRings is { Length: > 0 })
                    RenderRing(geom.CoordinateRings[0], proj, face, edge, lineWidth);
                break;

            case GeoJsonGeometryType.MultiPolygon:
                if (geom.MultiPolygonRings is not null)
                    foreach (var poly in geom.MultiPolygonRings)
                        if (poly.Length > 0)
                            RenderRing(poly[0], proj, face, edge, lineWidth);
                break;

            case GeoJsonGeometryType.LineString:
                if (geom.Coordinates is not null)
                    RenderPolyline(geom.Coordinates, proj, edge, lineWidth);
                break;

            case GeoJsonGeometryType.MultiLineString:
                if (geom.CoordinateRings is not null)
                    foreach (var line in geom.CoordinateRings)
                        RenderPolyline(line, proj, edge, lineWidth);
                break;

            case GeoJsonGeometryType.GeometryCollection:
                if (geom.Geometries is not null)
                    foreach (var child in geom.Geometries)
                        RenderGeometry(child, proj, face, edge, lineWidth);
                break;

            // Point / MultiPoint — skip (no meaningful single-pixel render without marker size context)
            default:
                break;
        }
    }

    private void RenderRing(double[][] ring, IMapProjection proj,
        Color? face, Color? edge, double lineWidth)
    {
        if (ring.Length < 3) return;
        var pts = ProjectRing(ring, proj);
        var strokeColor = edge ?? SeriesColor;
        Ctx.DrawPolygon(pts, face, strokeColor, lineWidth);
    }

    private void RenderPolyline(double[][] coords, IMapProjection proj,
        Color? edge, double lineWidth)
    {
        if (coords.Length < 2) return;
        var pts = ProjectRing(coords, proj);
        // Draw as open polygon (no fill, stroke only) — DrawPolygon with null fill
        Ctx.DrawPolygon(pts, null, edge ?? SeriesColor, lineWidth);
    }

    private Point[] ProjectRing(double[][] ring, IMapProjection proj)
    {
        var pts = new Point[ring.Length];
        for (int i = 0; i < ring.Length; i++)
        {
            double lon = ring[i].Length > 0 ? ring[i][0] : 0;
            double lat = ring[i].Length > 1 ? ring[i][1] : 0;
            var (nx, ny) = proj.Project(lon, lat);
            // Map normalized [0,1] to pixel coords within plot area
            pts[i] = new Point(
                Area.PlotBounds.X + nx * Area.PlotBounds.Width,
                Area.PlotBounds.Y + ny * Area.PlotBounds.Height);
        }
        return pts;
    }

    internal static IEnumerable<GeoJsonFeature> ExtractFeatures(GeoJsonDocument doc)
    {
        if (doc.FeatureCollection is not null)
            return doc.FeatureCollection.Features;
        if (doc.Feature is not null)
            return [doc.Feature];
        if (doc.Geometry is not null)
            return [new GeoJsonFeature(doc.Geometry)];
        return [];
    }
}
