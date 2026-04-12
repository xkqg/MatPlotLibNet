// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Geographic bounding box (longitude/latitude in degrees) returned by
/// <see cref="IMapProjection.Bounds"/>.</summary>
public readonly record struct GeoBounds(double LonMin, double LonMax, double LatMin, double LatMax)
{
    /// <summary>Center longitude of the bounding box.</summary>
    public double LonCenter => (LonMin + LonMax) / 2;

    /// <summary>Center latitude of the bounding box.</summary>
    public double LatCenter => (LatMin + LatMax) / 2;
}
