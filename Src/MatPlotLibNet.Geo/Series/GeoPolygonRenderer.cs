// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Geo.Series;

/// <summary>Draws a map (<see cref="GeoPolygonSeries"/>) through the library's series pass, as every series of the library is drawn by
/// its renderer: the features are projected and placed by the transform of the axes (<see cref="GeoPixelMapping"/>), so the projection's
/// bounds fill the plot, which the map asks for as its limits, until a zoom or a pan moves them, and then the marks of every other series
/// and the map move together. An area is one path for each polygon, a line one polyline for each ring. A map fills its plot, so it adds
/// no mark a legend keeps clear of.</summary>
internal sealed class GeoPolygonRenderer(SeriesRenderContext context) : SeriesRenderer<GeoPolygonSeries>(context)
{
    /// <inheritdoc />
    public override void Render(GeoPolygonSeries series)
    {
        var theme = Context.Theme;
        var outline = series.OutlineUnder(theme);
        var fillOfAll = series.FillOfAll(SeriesColor);
        var mapping = GeoPixelMapping.For(series.Projection, series.IsRawProjected, Transform);

        // The colour of each region: the colour map's colour for its value, over the range of the values that can be drawn.
        var values = series.Values;
        // The map named, else the theme's (ColorMapLook.Of).
        var regions = series.ColorScaleUnder(theme);

        for (int index = 0; index < series.Features.Count; index++)
        {
            var geometry = series.Features[index].Geometry;
            if (geometry.Kind.IsArea)
            {
                var fill = values is null ? fillOfAll : regions.ColorOf(values[index]);
                DrawArea(geometry, mapping, ShapeStyle.Edged(fill, outline, series.StrokeWidth));
            }
            else
            {
                DrawLines(geometry, mapping, StrokeStyle.Solid(series.LineColorOver(outline), series.StrokeWidth));
            }
        }
    }

    /// <summary>One path for each polygon, its exterior and its holes together under the non-zero fill rule, so what a hole
    /// encloses is not filled. A part of a ring that keeps fewer than three pixels is left out, and a polygon none of whose exterior
    /// remains. On a globe a polygon across the horizon is the parts of it that are shown, closed along the edge of the globe
    /// (<see cref="GeoPixelMapping.Pieces"/>), each part of the exterior a subpath of the same path.</summary>
    private void DrawArea(GeoGeometry geometry, GeoPixelMapping mapping, ShapeStyle style)
    {
        foreach (var polygon in geometry.Polygons)
        {
            var segments = AddRings(null, mapping.Pieces(polygon.Exterior, closed: true));
            if (segments is null) continue;

            foreach (var hole in polygon.Holes) segments = AddRings(segments, mapping.Pieces(hole, closed: true));
            Ctx.DrawPath(segments, style);
        }
    }

    /// <summary>One polyline for each part of each ring of a line that the map shows; a part that is no line (one pixel) is left
    /// out.</summary>
    private void DrawLines(GeoGeometry geometry, GeoPixelMapping mapping, StrokeStyle style)
    {
        foreach (var ring in geometry.PositionRings)
        {
            var pieces = mapping.Pieces(ring, closed: false);
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].FormsALine) Ctx.DrawLines(pieces[i], style);
            }
        }
    }

    /// <summary>A closed subpath through each of <paramref name="pieces"/> that keeps three pixels or more, added to the path.</summary>
    /// <param name="segments">The path so far; <see langword="null"/> for none yet.</param>
    /// <param name="pieces">The parts of a ring.</param>
    /// <returns>The path; <see langword="null"/> while there is no subpath in it.</returns>
    [return: NotNullIfNotNull(nameof(segments))]
    private static List<PathSegment>? AddRings(List<PathSegment>? segments, RingPieces pieces)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            var ring = pieces[i];
            if (ring.Count < 3) continue;

            segments ??= [];
            segments.Add(new MoveToSegment(ring[0]));
            for (int k = 1; k < ring.Count; k++) segments.Add(new LineToSegment(ring[k]));
            segments.Add(new CloseSegment());
        }

        return segments;
    }
}
