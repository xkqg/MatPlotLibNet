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

    // ── v1.7.0 additions (8 new projections → 13 total) ──

    /// <summary>Mollweide — equal-area pseudo-cylindrical for global thematic maps.</summary>
    public static IGeoProjection Mollweide => new Mollweide();

    /// <summary>Sinusoidal — simplest equal-area projection.</summary>
    public static IGeoProjection Sinusoidal => new Sinusoidal();

    /// <summary>Albers Equal Area Conic — standard for US maps (29.5°/45.5° parallels).</summary>
    public static IGeoProjection AlbersEqualArea => new AlbersEqualArea();

    /// <summary>Albers Equal Area with custom standard parallels.</summary>
    public static IGeoProjection AlbersEqualAreaWith(double sp1, double sp2, double centerLon = -96, double centerLat = 37.5) =>
        new AlbersEqualArea(sp1, sp2, centerLon, centerLat);

    /// <summary>Azimuthal Equidistant — preserves distances from center. Default: North Pole.</summary>
    public static IGeoProjection AzimuthalEquidistant => new AzimuthalEquidistant();

    /// <summary>Azimuthal Equidistant centered on a specific point.</summary>
    public static IGeoProjection AzimuthalEquidistantAt(double centerLat, double centerLon) =>
        new AzimuthalEquidistant(centerLat, centerLon);

    /// <summary>Stereographic — conformal azimuthal. Default: North Pole.</summary>
    public static IGeoProjection Stereographic => new Stereographic();

    /// <summary>Stereographic centered on a specific point.</summary>
    public static IGeoProjection StereographicAt(double centerLat, double centerLon) =>
        new Stereographic(centerLat, centerLon);

    /// <summary>Transverse Mercator — conformal cylindrical rotated 90°. Foundation of UTM.</summary>
    public static IGeoProjection TransverseMercator => new TransverseMercator();

    /// <summary>Transverse Mercator with custom central meridian.</summary>
    public static IGeoProjection TransverseMercatorAt(double centerLon) =>
        new TransverseMercator(centerLon);

    /// <summary>Natural Earth — compromise pseudo-cylindrical (Tom Patterson, 2012).</summary>
    public static IGeoProjection NaturalEarth => new NaturalEarthProjection();

    /// <summary>Equal Earth — modern equal-area (Šavrič, Patterson, Jenny, 2018).</summary>
    public static IGeoProjection EqualEarth => new EqualEarth();
}
