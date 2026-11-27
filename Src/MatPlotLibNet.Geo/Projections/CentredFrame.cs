// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Geo.Projections;

/// <summary>The centre of an azimuthal projection (an orthographic, an azimuthal equidistant, a stereographic map), worked out once:
/// the sine and the cosine of its latitude and its longitude in radians, from which every place is seen
/// (<see cref="Of(double, double)"/>). The one place an azimuthal projection's centre is turned into numbers.</summary>
/// <param name="SinLat0">The sine of the centre's latitude.</param>
/// <param name="CosLat0">The cosine of the centre's latitude.</param>
/// <param name="Lon0">The centre's longitude, in radians.</param>
internal readonly record struct CentredFrame(double SinLat0, double CosLat0, double Lon0)
{
    /// <summary>The frame of a projection centred on <paramref name="latitude"/>, <paramref name="longitude"/>.</summary>
    /// <param name="latitude">The latitude of the centre, in degrees.</param>
    /// <param name="longitude">The longitude of the centre, in degrees.</param>
    internal static CentredFrame At(double latitude, double longitude)
    {
        double lat0 = latitude.ToRadians();
        return new(Math.Sin(lat0), Math.Cos(lat0), longitude.ToRadians());
    }

    /// <summary>A place as the centre sees it.</summary>
    /// <param name="latitude">The latitude of the place, in degrees.</param>
    /// <param name="longitude">The longitude of the place, in degrees.</param>
    internal CentredPlace Of(double latitude, double longitude)
    {
        double lat = latitude.ToRadians(), turn = longitude.ToRadians() - Lon0;
        return new(this, Math.Sin(lat), Math.Cos(lat), Math.Sin(turn), Math.Cos(turn));
    }
}
