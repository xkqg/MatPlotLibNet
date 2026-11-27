// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The orthographic projection of a place is the same number to the last bit however the arithmetic is arranged: the
/// formula of the spherical orthographic projection, spelled out in full here, is what <see cref="Orthographic.Forward"/> answers, for
/// every place of a grid and every centre, the far side included. A map projects ten thousand places a frame, so the arithmetic the
/// projection shares between them (the centre's sine and cosine, the sine and cosine of a place's own latitude and of its distance
/// in longitude) is worked out once, not once for each use; the number that comes out must not move by a bit.</summary>
public class OrthographicForwardTests
{
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    /// <summary>The formula as written: every sine and cosine at the place it is used.</summary>
    private static ProjectedPoint Reference(double centerLat, double centerLon, double latitude, double longitude)
    {
        double lat = latitude * DegToRad;
        double lon = longitude * DegToRad;
        double lat0 = centerLat * DegToRad;
        double lon0 = centerLon * DegToRad;

        double cosC = Math.Sin(lat0) * Math.Sin(lat) + Math.Cos(lat0) * Math.Cos(lat) * Math.Cos(lon - lon0);
        if (cosC < 0) return new(double.NaN, double.NaN);

        double x = (Math.Cos(lat) * Math.Sin(lon - lon0)) * RadToDeg;
        double y = (Math.Cos(lat0) * Math.Sin(lat) - Math.Sin(lat0) * Math.Cos(lat) * Math.Cos(lon - lon0)) * RadToDeg;
        return new(x, y);
    }

    public static TheoryData<double, double> Centres => new()
    {
        { 0, 0 }, { 45, -30 }, { 20, 10 }, { -30, 120 }, { 89.9, 5 }, { -90, 0 }, { 35.7, 139.7 },
    };

    [Theory]
    [MemberData(nameof(Centres))]
    public void EveryPlaceOfAGrid_IsTheNumberTheFormulaGivesToTheLastBit(double centerLat, double centerLon)
    {
        var projection = new Orthographic(centerLat, centerLon);
        int compared = 0;
        for (double lat = -90; lat <= 90; lat += 7.5)
        {
            for (double lon = -180; lon <= 180; lon += 7.5)
            {
                var expected = Reference(centerLat, centerLon, lat, lon);
                var actual = projection.Forward(lat, lon);

                Assert.Equal(BitConverter.DoubleToInt64Bits(expected.X), BitConverter.DoubleToInt64Bits(actual.X));
                Assert.Equal(BitConverter.DoubleToInt64Bits(expected.Y), BitConverter.DoubleToInt64Bits(actual.Y));
                compared++;
            }
        }

        Assert.Equal(25 * 49, compared);
    }
}
