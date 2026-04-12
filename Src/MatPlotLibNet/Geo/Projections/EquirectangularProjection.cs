// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Maps longitude/latitude linearly to screen coordinates (equirectangular / plate carrée projection).</summary>
public sealed class EquirectangularProjection : IMapProjection
{
    private readonly double _centerLon;
    private readonly double _lonExtent;
    private readonly double _latExtent;

    /// <summary>Initializes a new equirectangular projection.</summary>
    /// <param name="centerLon">Center meridian in degrees (default 0 = prime meridian).</param>
    /// <param name="lonExtent">Total longitude span in degrees (default 360 = whole world).</param>
    /// <param name="latExtent">Total latitude span in degrees (default 180 = whole world).</param>
    public EquirectangularProjection(double centerLon = 0, double lonExtent = 360, double latExtent = 180)
    {
        _centerLon = centerLon;
        _lonExtent = lonExtent;
        _latExtent = latExtent;
    }

    /// <inheritdoc />
    public NormalizedPoint Project(double lon, double lat)
    {
        double nx = (lon - _centerLon + _lonExtent / 2.0) / _lonExtent;
        double ny = 1.0 - (lat + _latExtent / 2.0) / _latExtent;
        return new(nx, ny);
    }

    /// <inheritdoc />
    public GeoBounds Bounds =>
        new(_centerLon - _lonExtent / 2.0, _centerLon + _lonExtent / 2.0,
            -_latExtent / 2.0, _latExtent / 2.0);
}
