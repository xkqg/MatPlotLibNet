// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>How a projection's bounds are worked out, the one place each way is written: a box centred on the origin of the plane
/// (<see cref="Symmetric"/>), and the box round the edge of the world as the projection draws it (<see cref="WorldEdgeBounds"/>): the
/// antimeridian on the equator and the pole on the central meridian, cartopy's limits of a cylindrical and a pseudo-cylindrical map.
/// A projection whose limits cartopy states otherwise says so with the numbers cartopy uses.</summary>
internal static class GeoBoundsExtensions
{
    extension(GeoBounds)
    {
        /// <summary>The box centred on the origin of the plane, <paramref name="halfWidth"/> to each side and
        /// <paramref name="halfHeight"/> up and down.</summary>
        /// <param name="halfWidth">How far the box reaches along X from the origin.</param>
        /// <param name="halfHeight">How far the box reaches along Y from the origin.</param>
        /// <returns>The box.</returns>
        internal static GeoBounds Symmetric(double halfWidth, double halfHeight) => new(-halfWidth, halfWidth, -halfHeight, halfHeight);
    }

    extension(IGeoProjection projection)
    {
        /// <summary>The box round the edge of the world on this projection: as far along X as the antimeridian stands on the equator,
        /// as far along Y as the pole stands on the central meridian, each to both sides.</summary>
        /// <returns>The bounds.</returns>
        internal GeoBounds WorldEdgeBounds() => GeoBounds.Symmetric(projection.Forward(0, 180).X, projection.Forward(90, 0).Y);

        /// <summary>The box round where the projection places <paramref name="boundary"/>: the least and the greatest X and Y of its
        /// points, as cartopy takes the limits of a projection whose boundary it projects (<c>mins = np.min(points, axis=0)</c>); a
        /// place off the domain is left out.</summary>
        /// <param name="boundary">The places along the edge of what the projection draws.</param>
        /// <returns>The bounds.</returns>
        internal GeoBounds BoxRound(IEnumerable<GeoCoordinate> boundary)
        {
            double xMin = double.PositiveInfinity, xMax = double.NegativeInfinity, yMin = double.PositiveInfinity, yMax = double.NegativeInfinity;
            foreach (var place in boundary)
            {
                var point = projection.Forward(place.Latitude, place.Longitude);
                if (point.IsOffDomain) continue;
                xMin = Math.Min(xMin, point.X);
                xMax = Math.Max(xMax, point.X);
                yMin = Math.Min(yMin, point.Y);
                yMax = Math.Max(yMax, point.Y);
            }

            return new(xMin, xMax, yMin, yMax);
        }
    }
}
