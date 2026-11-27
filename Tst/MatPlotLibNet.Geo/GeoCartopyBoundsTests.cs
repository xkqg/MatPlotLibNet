// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>The bounds of the projections whose box cartopy works out otherwise than from the world edge, read from cartopy 0.26's
/// <c>crs.py</c> and worked out in this library's plane (degrees of arc on a sphere whose radius is the semimajor axis). Each was a
/// guess before (the stereographic +-360, the two cones +-200).</summary>
public class GeoCartopyBoundsTests
{
    private static void AssertBox(GeoBounds bounds, double xMin, double xMax, double yMin, double yMax)
    {
        Assert.Equal(xMin, bounds.XMin, 1e-9);
        Assert.Equal(xMax, bounds.XMax, 1e-9);
        Assert.Equal(yMin, bounds.YMin, 1e-9);
        Assert.Equal(yMax, bounds.YMax, 1e-9);
    }

    /// <summary>cartopy's <c>Stereographic</c>: <c>x_axis_offset = 5e7 / WGS84_SEMIMAJOR_AXIS</c>, <c>y_axis_offset = 5e7 /
    /// WGS84_SEMIMINOR_AXIS</c>, <c>_x_limits = (-a * x_axis_offset, a * x_axis_offset)</c> and the same for Y with <c>b</c>: on the
    /// default globe a square of 5e7 m to each side, 5e7 / 6378137 radians of the semimajor axis, 449.157642... degrees of arc; the
    /// outline is the circle of that square (<c>_ellipse_boundary(..., 91)</c>), wherever the map is centred.</summary>
    [Theory]
    [InlineData(90, 0)]
    [InlineData(-60, 20)]
    public void TheStereographicBounds_AreCartopysSquareOf5e7Metres(double centerLat, double centerLon)
    {
        const double half = 449.15764205976075;

        var bounds = GeoProjection.StereographicAt(centerLat, centerLon).Bounds;

        AssertBox(bounds, -half, half, -half, half);
    }

    /// <summary>cartopy's <c>AlbersEqualArea</c>: the boundary is 103 longitudes from the central meridian less 180 to it plus 180
    /// (<c>_determine_longitude_bounds</c>, 1e-10 in from the side the meridian is off zero) along the north pole and back along the
    /// south pole, and the limits are the least and the greatest of its projected points (<c>mins = np.min(points, axis=0)</c>).
    /// The numbers are that boundary through this library's own spherical Albers (latitude clamped to 89.99), worked out in python
    /// (<c>math</c>).</summary>
    [Theory]
    [InlineData(29.5, 45.5, -96, 37.5, -151.9680371824074, 151.96803718240915, -77.3060307198982, 122.92141053548619)]
    [InlineData(30, 60, 0, 45, -140.33151754645374, 140.33151754645374, -83.01400558207207, 133.65041346000572)]
    [InlineData(40, 50, 10, 45, -138.53788055157307, 138.5378805515716, -81.24691050707625, 140.276884187514)]
    public void TheAlbersBounds_AreTheBoxRoundCartopysBoundary(double sp1, double sp2, double lon0, double lat0,
        double xMin, double xMax, double yMin, double yMax)
    {
        var bounds = GeoProjection.AlbersEqualAreaWith(sp1, sp2, lon0, lat0).Bounds;

        AssertBox(bounds, xMin, xMax, yMin, yMax);
    }

    /// <summary>cartopy's <c>LambertConformal</c> (its default <c>cutoff=-30</c>): the boundary is the pole the cone converges to
    /// (north when the standard parallel further from the equator is north of it) at longitude 0, 91 longitudes from the central
    /// meridian plus 180 less 0.001 to it less 180 plus 0.001 along the cutoff latitude, and the pole again; the limits are the least and
    /// the greatest of its projected points. The numbers are that boundary through this library's own spherical Lambert (latitude
    /// clamped to 89.99), worked out in python (<c>math</c>).</summary>
    [Theory]
    [InlineData(33, 45, 0, 39, -158.3563523780794, 158.3563523780794, -88.13394503529021, 133.35069585027372)]
    [InlineData(20, 60, 10, 45, -148.5857429848136, 148.5857429848136, -90.53421392468029, 128.34966197589668)]
    [InlineData(-20, -60, -70, -45, -72.20810202087694, 72.20810202087694, -92.21949125609578, 14.151341666745347)]
    public void TheLambertBounds_AreTheBoxRoundCartopysBoundary(double sp1, double sp2, double lon0, double lat0,
        double xMin, double xMax, double yMin, double yMax)
    {
        var bounds = GeoProjection.LambertConformalWith(sp1, sp2, lon0, lat0).Bounds;

        AssertBox(bounds, xMin, xMax, yMin, yMax);
    }
}
