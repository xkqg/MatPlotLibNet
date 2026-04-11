// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Factory for creating built-in <see cref="IMapProjection"/> instances.</summary>
public static class MapProjections
{
    /// <summary>Creates an equirectangular (plate carrée) projection.</summary>
    /// <param name="centerLon">Center meridian in degrees (default 0).</param>
    /// <param name="lonExtent">Total longitude span in degrees (default 360).</param>
    /// <param name="latExtent">Total latitude span in degrees (default 180).</param>
    public static IMapProjection Equirectangular(double centerLon = 0, double lonExtent = 360, double latExtent = 180)
        => new EquirectangularProjection(centerLon, lonExtent, latExtent);

    /// <summary>Creates a Web Mercator projection.</summary>
    /// <param name="centerLon">Center meridian in degrees (default 0).</param>
    /// <param name="lonExtent">Total longitude span in degrees (default 360).</param>
    public static IMapProjection Mercator(double centerLon = 0, double lonExtent = 360)
        => new MercatorProjection(centerLon, lonExtent);
}
