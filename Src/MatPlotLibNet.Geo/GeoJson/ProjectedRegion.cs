// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.GeoJson;

/// <summary>A polygon of a region as the map draws it: its rings in the plane of the projection.</summary>
/// <param name="Exterior">The ring that bounds the polygon.</param>
/// <param name="Holes">The rings cut out of it.</param>
internal readonly record struct ProjectedPolygon(IReadOnlyList<ProjectedPoint> Exterior, IReadOnlyList<IReadOnlyList<ProjectedPoint>> Holes);

/// <summary>What a region measures in the plane of its projection.</summary>
/// <param name="Area">The area of its polygons, their holes taken out.</param>
/// <param name="Centroid">The centre of that area.</param>
internal readonly record struct RegionMeasure(double Area, ProjectedPoint Centroid);

/// <summary>A region (a feature) in the plane of a projection: where its polygons stand, how large they are and where their centre
/// is, and the same region resized about its centre. The ground of the maps that draw a region by a value.</summary>
internal static class ProjectedRegionExtensions
{
    extension(GeoFeature feature)
    {
        /// <summary>The polygons of the feature on <paramref name="projection"/>. A position the projection cannot place is
        /// left out of its ring; a polygon whose exterior is left with fewer than three positions, and a hole with fewer than
        /// three, are left out.</summary>
        internal IReadOnlyList<ProjectedPolygon> ProjectedBy(IGeoProjection projection)
        {
            IReadOnlyList<ProjectedPoint> Ring(IReadOnlyList<GeoPosition> ring) =>
                [.. ring.Select(p => projection.Forward(p.Lat, p.Lon)).Where(p => !double.IsNaN(p.X) && !double.IsNaN(p.Y))];

            var polygons = new List<ProjectedPolygon>();
            foreach (var polygon in feature.Geometry.Polygons)
            {
                var exterior = Ring(polygon.Exterior);
                if (exterior.Count < 3) continue;
                polygons.Add(new ProjectedPolygon(exterior, [.. polygon.Holes.Select(Ring).Where(hole => hole.Count >= 3)]));
            }

            return polygons;
        }
    }

    extension(IReadOnlyList<ProjectedPolygon> region)
    {
        /// <summary>The area and the centre of the region.</summary>
        /// <returns>The measure; <see langword="null"/> for a region with no area (no polygon, or a degenerate one).</returns>
        internal RegionMeasure? Measure()
        {
            double area = 0, cx = 0, cy = 0;
            foreach (var polygon in region)
            {
                Add(polygon.Exterior, 1);
                foreach (var hole in polygon.Holes) Add(hole, -1);
            }

            return area > 0 ? new RegionMeasure(area, new ProjectedPoint(cx / area, cy / area)) : null;

            void Add(IReadOnlyList<ProjectedPoint> ring, double sign)
            {
                double twiceArea = 0, rx = 0, ry = 0;
                for (int i = 0; i < ring.Count; i++)
                {
                    var p = ring[i];
                    var q = ring[(i + 1) % ring.Count];
                    double cross = p.X * q.Y - q.X * p.Y;
                    twiceArea += cross;
                    rx += (p.X + q.X) * cross;
                    ry += (p.Y + q.Y) * cross;
                }

                if (twiceArea == 0) return;
                double ringArea = Math.Abs(twiceArea) / 2;
                area += sign * ringArea;
                cx += sign * ringArea * rx / (3 * twiceArea);
                cy += sign * ringArea * ry / (3 * twiceArea);
            }
        }

        /// <summary>The region resized about <paramref name="centre"/>: every position moved to <paramref name="factor"/> times
        /// its distance from the centre, so the centre stays where it is and the area becomes <c>factor²</c> times as large.</summary>
        /// <returns>The geometry, its coordinates already projected (a series that draws it is raw-projected).</returns>
        internal GeoGeometry ScaledAbout(ProjectedPoint centre, double factor)
        {
            GeoPosition Scale(ProjectedPoint p) => new(centre.X + factor * (p.X - centre.X), centre.Y + factor * (p.Y - centre.Y));

            var polygons = region.Select(polygon => GeoPolygon.FromRings(
                [.. polygon.Exterior.Select(Scale)], polygon.Holes.Select(hole => (IReadOnlyList<GeoPosition>)[.. hole.Select(Scale)]))).ToList();
            return new GeoGeometry
            {
                Type = polygons.Count == 1 ? "Polygon" : "MultiPolygon",
                Rings = [.. polygons.SelectMany(p => p.Holes.Prepend(p.Exterior)).Select(ring => ring.Select(p => (p.Lon, p.Lat)).ToList())],
                Polygons = polygons,
            };
        }
    }
}
