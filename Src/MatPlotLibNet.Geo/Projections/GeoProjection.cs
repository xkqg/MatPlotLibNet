// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>Static factory for common map projections.</summary>
public static class GeoProjection
{
    /// <summary>Equirectangular (Plate Carrée) — simplest projection, identity mapping.</summary>
    public static IGeoProjection PlateCarree => new PlateCarree();

    /// <summary>Mercator — conformal cylindrical, used by web maps.</summary>
    public static IGeoProjection Mercator => new Mercator();

    /// <summary>Robinson — compromise projection designed for world maps.</summary>
    public static IGeoProjection Robinson => new Robinson();

    /// <summary>Orthographic — globe view centered on (0, 0).</summary>
    public static IGeoProjection Orthographic => new Orthographic();

    /// <summary>Orthographic centered on a specific point.</summary>
    public static IGeoProjection OrthographicAt(double centerLat, double centerLon) =>
        new Orthographic(centerLat, centerLon);

    /// <summary>Lambert Conformal Conic — good for mid-latitude regions (US, Europe).</summary>
    public static IGeoProjection LambertConformal => new LambertConformal();

    /// <summary>Lambert Conformal Conic with custom standard parallels.</summary>
    public static IGeoProjection LambertConformalWith(double sp1, double sp2, double centerLon = 0, double centerLat = 39) =>
        new LambertConformal(sp1, sp2, centerLon, centerLat);
}
