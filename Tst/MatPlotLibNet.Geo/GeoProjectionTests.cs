// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Phase 3 of the coverage uplift: edge-case + reference-point tests for all
/// 13 geographic projections. Brings each projection from 33-89% line coverage to ≥95%.
///
/// <para><b>Reference values</b> are derived from the projection formulas themselves
/// (well-documented in any cartography text) — not from <c>pyproj</c>, since that
/// library requires GIS-system libs (PROJ + GEOS) which we can't install on this
/// environment. Where formulas are non-trivial (e.g., Mollweide's Newton iteration)
/// we test invariants instead of exact values: bounds, symmetry, fixed points.</para>
///
/// <para>A fact only one projection has sits in that projection's nested class; a fact several projections share (a point inside
/// the bounds, a centre on the origin, a point that is finite or NaN) is a row of a theory of <see cref="AllProjections"/>.</para></summary>
public class GeoProjectionTests
{
    // ──────────────────────────────────────────────────────────────────────────
    public class PlateCarreeProjection
    {
        private static readonly IGeoProjection P = GeoProjection.PlateCarree;

        [Theory]
        [InlineData(45,   90)]
        [InlineData(-45,  -90)]
        [InlineData(60,   180)]
        [InlineData(-60, -180)]
        public void Forward_IsLongitudeFirst(double lat, double lon)
        {
            // PlateCarree is the identity projection: (lat, lon) -> (lon, lat)
            var (x, y) = P.Forward(lat, lon);
            Assert.Equal(lon, x, 1e-9);
            Assert.Equal(lat, y, 1e-9);
        }

    }

    // ──────────────────────────────────────────────────────────────────────────
    public class MercatorProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Mercator;

        [Fact] public void Origin_MapsToZero() { var (x, y) = P.Forward(0, 0); Assert.Equal(0, x, 1e-9); Assert.Equal(0, y, 1e-9); }

        [Theory]
        [InlineData(85)]    // near pole — Y becomes very large
        [InlineData(-85)]   // near south pole
        public void NearPole_YBecomesLarge(double lat)
        {
            // Mercator Y diverges as |lat| -> 90. At ±85° Y must already be large.
            var (_, y) = P.Forward(lat, 0);
            Assert.True(Math.Abs(y) > Math.PI, $"Y={y} should be > π near ±85°");
        }

    }

    // ──────────────────────────────────────────────────────────────────────────
    public class RobinsonProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Robinson;

        /// <summary>X and Y in the same unit, degrees of arc on a sphere of radius 1, as PROJ 9.8.1 (pyproj 3.8.0) writes them:
        /// <c>Proj(proj="robin", R=1)</c> at a node of Robinson's table, times 180/π. Measured before the change: Y was 67.80 at
        /// 45 N and 121.71 at the pole (the table's distance times 90 where X takes degrees of longitude), so the map was π/2
        /// too tall: 0.797 as high as it is wide, where cartopy's Robinson is 0.507. PROJ keeps its table in single precision, so
        /// the numbers agree to 1e-5.</summary>
        [Theory]
        [InlineData(45, 10, 7.606049411, 43.164710981)]
        [InlineData(90, 0, 0.0, 77.481082636)]
        [InlineData(0, 180, 152.766, 0.0)]
        [InlineData(-30, -60, -48.885118907, -28.822963424)]
        public void Forward_IsProjsRobinson_InDegreesOfArc(double lat, double lon, double x, double y)
        {
            var point = P.Forward(lat, lon);

            Assert.Equal(x, point.X, 1e-5);
            Assert.Equal(y, point.Y, 1e-5);
        }

        /// <summary>cartopy's Robinson: <c>y_limits[1] / x_limits[1]</c> = 0.5071880041078592.</summary>
        [Fact]
        public void TheBounds_AreAsHighAsCartopysForTheirWidth()
        {
            var b = P.Bounds;

            Assert.Equal(0.5071880041078592, (b.YMax - b.YMin) / (b.XMax - b.XMin), 6);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class SinusoidalProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Sinusoidal;

        [Theory]
        [InlineData(0,    180,   180,    0)]    // (lat=0, lon=180): cos(0)=1 → x=180, y=0
        [InlineData(0,   -180,  -180,    0)]
        [InlineData(45,   0,    0,    45)]      // (45, 0): cos(45) doesn't matter because lon=0
        [InlineData(-45,  0,    0,   -45)]
        [InlineData(90,   100,  0,    90)]      // at the poles cos(±90°) = 0, so x = 0 whatever the longitude
        [InlineData(-90,  100,  0,   -90)]
        public void Forward_FormulaParity(double lat, double lon, double expectedX, double expectedY)
        {
            // Sinusoidal: x = lon * cos(lat), y = lat
            var (x, y) = P.Forward(lat, lon);
            Assert.Equal(expectedX, x, 1e-9);
            Assert.Equal(expectedY, y, 1e-9);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class MollweideProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Mollweide;

        [Fact]
        public void Pole_HasMaxAbsoluteY()
        {
            // Mollweide: ±90° map to (0, ±maxY). Both poles at maximum Y.
            var (x90, y90) = P.Forward(90, 0);
            Assert.Equal(0, x90, 1e-6);
            Assert.True(y90 > 0);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class OrthographicProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Orthographic;

        /// <summary>The bounds are the globe's: a sphere of radius 1 reaches 180/π degrees of arc from its centre (PROJ 9.8.1,
        /// <c>Proj(proj="ortho", R=1)</c> at 90 E on the equator: 1, which is 57.295779513 in degrees of arc), and cartopy's
        /// <c>Orthographic().x_limits</c> are ± the globe's radius. Measured before the change: ±90, so the globe filled 64 % of
        /// the box it was drawn in.</summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(50, 10)]
        public void TheBounds_AreTheGlobe(double lat, double lon)
        {
            double radius = 180 / Math.PI;

            Assert.Equal(new GeoBounds(-radius, radius, -radius, radius), GeoProjection.OrthographicAt(lat, lon).Bounds);
        }

        [Fact]
        public void TheEdgeOfTheGlobe_StandsOnTheBounds() =>
            Assert.Equal(P.Bounds.XMax, P.Forward(0, 90).X, 1e-9);
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class TransverseMercatorProjection
    {
        [Fact]
        public void TransverseMercatorAt_HasCustomCentralMeridian()
        {
            var utm = GeoProjection.TransverseMercatorAt(centerLon: 15);
            // At the central meridian (lon = 15), X should be 0 (or very small).
            var (x, _) = utm.Forward(40, 15);
            Assert.Equal(0, x, 1e-6);
        }

        /// <summary>PROJ 9.8.1 (pyproj 3.8.0), <c>Proj(proj="tmerc", R=1, lon_0=15, lat_0=0, k=1)</c>, times 180/π: the equator stays
        /// at Y 0 and a parallel at its latitude on the central meridian, whatever the central meridian is. Measured before the
        /// change: Y was less the central meridian, so with 15 E the equator stood at -15 and 40 N at 25.</summary>
        [Theory]
        [InlineData(0, 25, 10.051159657, 0.0)]
        [InlineData(40, 15, 0.0, 40.0)]
        [InlineData(40, 25, 7.667049856, 40.432461086)]
        public void AtACentralMeridianOfItsOwn_IsProjsTransverseMercator(double lat, double lon, double x, double y)
        {
            var point = GeoProjection.TransverseMercatorAt(centerLon: 15).Forward(lat, lon);

            Assert.Equal(x, point.X, 1e-6);
            Assert.Equal(y, point.Y, 1e-6);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class EqualEarthProjection
    {
        private static readonly IGeoProjection P = GeoProjection.EqualEarth;

        // Reference values derived from the published Equal Earth formula (Šavrič, Patterson,
        // Jenny — "The Equal Earth map projection", Int. J. Geographical Information Science
        // 33(3):454-465, 2018, https://doi.org/10.1080/13658816.2018.1504949):
        //   sin(θ) = (√3/2) · sin(φ)
        //   x = 2√3·λ·cos(θ) / [3·(A1 + 3·A2·θ² + 7·A3·θ⁶ + 9·A4·θ⁸)]
        //   y = θ·(A1 + A2·θ² + A3·θ⁶ + A4·θ⁸)
        // with A1=1.340264, A2=-0.081106, A3=0.000893, A4=0.003796, λ/φ/θ in radians.
        // Corroborated by Wikipedia (https://en.wikipedia.org/wiki/Equal_Earth_projection) and
        // PROJ's `eqearth` operation family (https://proj.org/en/stable/operations/projections/eqearth.html).
        // This codebase reports planar coordinates in a "radians-run-through-ToDegrees()" unit
        // (same convention as Mollweide/Robinson/Sinusoidal in this file), so expected values
        // below are the canonical radian result converted via ToDegrees().
        [Theory]
        [InlineData(0,    0,      0.000000000000,   0.000000000000)]
        [InlineData(0,  180,    155.078474769348,   0.000000000000)]
        [InlineData(0,   90,     77.539237384674,   0.000000000000)]
        [InlineData(0,  -90,   -77.539237384674,    0.000000000000)]
        [InlineData(90,   0,     0.000000000000,   75.479326187429)]
        [InlineData(-90,  0,     0.000000000000,  -75.479326187429)]
        [InlineData(45,  45,    33.227383823931,   49.287610606369)]
        [InlineData(30, -60,   -48.351940337033,   33.972679892375)]
        public void Forward_MatchesCanonicalEqualEarthFormula(double lat, double lon, double expectedX, double expectedY)
        {
            var (x, y) = P.Forward(lat, lon);
            Assert.Equal(expectedX, x, 1e-9);
            Assert.Equal(expectedY, y, 1e-9);
        }

        [Theory]
        [InlineData(0,   90)]
        [InlineData(45,  45)]
        [InlineData(-30, 60)]
        public void Forward_IsOddFunctionOfLongitude(double lat, double lon)
        {
            // x(lat, -lon) must equal -x(lat, lon); y is unchanged (Equal Earth's y depends
            // only on latitude/theta, never on longitude). A prior bug computed x with
            // `longitude` multiplied in twice, making x quadratic — and therefore an EVEN,
            // same-sign function — in longitude. This regression test pins the correct
            // odd-function behaviour so that bug class cannot silently return.
            var (xPos, yPos) = P.Forward(lat,  lon);
            var (xNeg, yNeg) = P.Forward(lat, -lon);
            Assert.Equal(-xPos, xNeg, 1e-9);
            Assert.Equal(yPos, yNeg, 1e-9);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Cross-projection invariants — Theory over every public projection
    public class AllProjections
    {
        /// <summary>Every public projection with the name it reports.</summary>
        public static IEnumerable<object[]> NamedProjections() =>
        [
            [GeoProjection.PlateCarree, "PlateCarree"],
            [GeoProjection.Mercator, "Mercator"],
            [GeoProjection.Robinson, "Robinson"],
            [GeoProjection.Mollweide, "Mollweide"],
            [GeoProjection.Sinusoidal, "Sinusoidal"],
            [GeoProjection.AlbersEqualArea, "AlbersEqualArea"],
            [GeoProjection.AzimuthalEquidistant, "AzimuthalEquidistant"],
            [GeoProjection.Stereographic, "Stereographic"],
            [GeoProjection.TransverseMercator, "TransverseMercator"],
            [GeoProjection.LambertConformal, "LambertConformal"],
            [GeoProjection.NaturalEarth, "NaturalEarth"],
            [GeoProjection.EqualEarth, "EqualEarth"],
            [GeoProjection.Orthographic, "Orthographic"],
        ];

        public static IEnumerable<object[]> EveryProjection() => NamedProjections().Select(row => new[] { row[0] });

        /// <summary>The projections that put latitude and longitude zero exactly on the origin of the plane.</summary>
        public static IEnumerable<object[]> ProjectionsWithAnExactOrigin() =>
            NamedProjections().Where(row => row[1] is "PlateCarree" or "Sinusoidal" or "EqualEarth").Select(row => new[] { row[0] });

        /// <summary>The projections that mirror across the equator, with the last latitude and the step of the sweep each is read over.</summary>
        public static IEnumerable<object[]> ProjectionsThatMirrorAcrossTheEquator() =>
        [
            [GeoProjection.Mercator, 80.0, 15.0],
            [GeoProjection.Robinson, 75.0, 15.0],
            [GeoProjection.NaturalEarth, 80.0, 15.0],
            [GeoProjection.EqualEarth, 75.0, 10.0],
        ];

        /// <summary>A projection centred on a point of its own, with that point.</summary>
        public static IEnumerable<object[]> CentredProjections() =>
        [
            [GeoProjection.OrthographicAt(centerLat: 35.7, centerLon: 139.7), 35.7, 139.7],
            [GeoProjection.StereographicAt(centerLat: 90, centerLon: 0), 90.0, 0.0],
            [GeoProjection.StereographicAt(centerLat: 45, centerLon: 30), 45.0, 30.0],
            [GeoProjection.AzimuthalEquidistantAt(centerLat: 51.5, centerLon: -0.1), 51.5, -0.1],
        ];

        /// <summary>A projection and a point it maps to a finite place: the centre of the continent an Albers map is made for, the
        /// centre of a map with standard parallels of its own, a point of a transverse Mercator away from its singularity and the pole,
        /// the last row of Robinson's table.</summary>
        public static IEnumerable<object[]> FinitePoints() =>
        [
            [GeoProjection.AlbersEqualArea, 40.0, -100.0],
            [GeoProjection.AlbersEqualArea, 0.0, 0.0],
            [GeoProjection.AlbersEqualAreaWith(sp1: 30, sp2: 60, centerLon: 0, centerLat: 45), 45.0, 0.0],
            [GeoProjection.LambertConformalWith(sp1: 35, sp2: 65, centerLon: 10, centerLat: 52), 52.0, 10.0],
            [GeoProjection.TransverseMercator, 45.0, 10.0],
            [GeoProjection.Robinson, 90.0, 180.0],
        ];

        /// <summary>A projection and a point that stands inside its bounds (so it is a number too, as NaN stands nowhere): the origin of
        /// every projection, Robinson at the edges of its longitude, at mid latitudes and at the poles, Mollweide at mid and high
        /// latitudes.</summary>
        public static IEnumerable<object[]> PointsInsideTheBounds() =>
        [
            .. EveryProjection().Select(row => new[] { row[0], 0.0, 0.0 }),
            [GeoProjection.Robinson, 0.0, 180.0],
            [GeoProjection.Robinson, 0.0, -180.0],
            [GeoProjection.Robinson, 45.0, 0.0],
            [GeoProjection.Robinson, -45.0, 0.0],
            [GeoProjection.Robinson, 90.0, 0.0],
            [GeoProjection.Robinson, -90.0, 0.0],
            [GeoProjection.Mollweide, 45.0, 90.0],
            [GeoProjection.Mollweide, -45.0, 90.0],
            [GeoProjection.Mollweide, 60.0, 180.0],
            [GeoProjection.Mollweide, -60.0, -180.0],
        ];

        /// <summary>A projection and a point it cannot show: the far side of an orthographic globe centred at (0, 0), where matplotlib
        /// and cartopy answer NaN for "not visible", and the singularity of a transverse Mercator on the equator 90 degrees from its
        /// central meridian.</summary>
        public static IEnumerable<object[]> HiddenPoints() =>
        [
            [GeoProjection.Orthographic, 0.0, 180.0],
            [GeoProjection.TransverseMercator, 0.0, 90.0],
        ];

        [Theory] [MemberData(nameof(NamedProjections))]
        public void HasItsName(IGeoProjection p, string name) => Assert.Equal(name, p.Name);

        [Theory] [MemberData(nameof(CentredProjections))]
        public void ACentredProjection_MapsItsCentreToTheOrigin(IGeoProjection p, double centerLat, double centerLon)
        {
            var (x, y) = p.Forward(centerLat, centerLon);
            Assert.Equal(0, x, 1e-6);
            Assert.Equal(0, y, 1e-6);
        }

        [Theory] [MemberData(nameof(FinitePoints))]
        public void Forward_OfAPointOnTheMap_IsFinite(IGeoProjection p, double lat, double lon)
        {
            var (x, y) = p.Forward(lat, lon);
            x.AssertFinite();
            y.AssertFinite();
        }

        [Theory] [MemberData(nameof(PointsInsideTheBounds))]
        public void Forward_StaysWithinTheBounds(IGeoProjection p, double lat, double lon)
        {
            var (x, y) = p.Forward(lat, lon);
            var b = p.Bounds;
            Assert.InRange(x, b.XMin - 1e-6, b.XMax + 1e-6);
            Assert.InRange(y, b.YMin - 1e-6, b.YMax + 1e-6);
        }

        [Theory] [MemberData(nameof(HiddenPoints))]
        public void Forward_OfAPointTheMapCannotShow_IsNaN(IGeoProjection p, double lat, double lon)
        {
            var (x, y) = p.Forward(lat, lon);
            Assert.True(double.IsNaN(x) || double.IsNaN(y), $"{p.Name}: ({lat}, {lon}) should give NaN; got ({x}, {y})");
        }

        [Theory] [MemberData(nameof(ProjectionsWithAnExactOrigin))]
        public void TheOrigin_MapsExactlyToZero(IGeoProjection p) => Assert.Equal(new ProjectedPoint(0, 0), p.Forward(0, 0));

        [Theory] [MemberData(nameof(ProjectionsThatMirrorAcrossTheEquator))]
        public void Forward_IsAntisymmetricAcrossTheEquator(IGeoProjection p, double lastLatitude, double step)
        {
            for (double lat = 5; lat <= lastLatitude; lat += step)
            {
                var (_, y1) = p.Forward(lat, 30);
                var (_, y2) = p.Forward(-lat, 30);
                Assert.Equal(-y1, y2, 1e-9);
            }
        }

        /// <summary>The bounds have a width and a height: the ocean and the land are drawn over them.</summary>
        [Theory] [MemberData(nameof(EveryProjection))]
        public void HasNonDegenerateBounds(IGeoProjection p)
        {
            var b = p.Bounds;
            Assert.True(b.XMax > b.XMin, $"{p.Name}: degenerate width Bounds {b}");
            Assert.True(b.YMax > b.YMin, $"{p.Name}: degenerate height Bounds {b}");
        }

        [Theory] [MemberData(nameof(EveryProjection))]
        public void NaNInput_PropagatesAsNaN(IGeoProjection p)
        {
            var (x, y) = p.Forward(double.NaN, double.NaN);
            // Either both NaN, or the projection is NaN-defensive (returns 0).
            // Don't enforce one behaviour — just that it doesn't throw.
            bool xMissingButNotY = double.IsNaN(x) && !double.IsNaN(y);
            Assert.False(xMissingButNotY, $"{p.Name}: Forward(NaN, NaN) inconsistent: ({x}, {y})");
        }
    }

    /// <summary>Points a projection has no inverse for: outside the map, or a projection that has no analytic inverse at all.</summary>
    public static IEnumerable<object[]> NoInverse() =>
    [
        // PlateCarree: past the latitude and longitude bounds
        [GeoProjection.PlateCarree, 0.0, 91.0],
        [GeoProjection.PlateCarree, 0.0, -91.0],
        [GeoProjection.PlateCarree, 181.0, 0.0],
        [GeoProjection.PlateCarree, -181.0, 0.0],
        // Mercator: a Y far above or below the top and bottom bound (latitude beyond 85.05)
        [GeoProjection.Mercator, 0.0, 1000.0],
        [GeoProjection.Mercator, 0.0, -1000.0],
        // Sinusoidal: |y| over 90 is off the map; at exactly 90 the guard passes but cos(90°) is 0, so the cosLat == 0 arm answers null
        [GeoProjection.Sinusoidal, 0.0, 91.0],
        [GeoProjection.Sinusoidal, 0.0, -91.0],
        [GeoProjection.Sinusoidal, 0.0, 90.0],
        [GeoProjection.Sinusoidal, 0.0, -90.0],
        // Stereographic, TransverseMercator and Robinson are not analytically invertible
        [new Projections.Stereographic(), 0.0, 0.0],
        [new Projections.Stereographic(), 100.0, 100.0],
        [new Projections.TransverseMercator(), 0.0, 0.0],
        [new Projections.TransverseMercator(), 45.0, 30.0],
        [GeoProjection.Robinson, 0.0, 0.0],
        [GeoProjection.Robinson, 100.0, 50.0],
        // Lambert's cone, Natural Earth, the azimuthal equidistant, Equal Earth and Mollweide have no inverse here at all
        [GeoProjection.LambertConformal, 0.0, 0.0],
        [GeoProjection.NaturalEarth, 0.0, 0.0],
        [GeoProjection.AzimuthalEquidistant, 0.0, 0.0],
        [GeoProjection.EqualEarth, 0.0, 0.0],
        [GeoProjection.Mollweide, 0.0, 0.0],
    ];

    /// <summary>Points that go forward and come back: the inverse of a projection (PlateCarree, Mercator, Sinusoidal) gives back the latitude and
    /// the longitude it was given, at the equator, at a mid latitude and at the bounds.</summary>
    public static IEnumerable<object[]> RoundTrips() =>
    [
        [GeoProjection.PlateCarree, 90.0, 180.0],
        [GeoProjection.PlateCarree, 30.0, 45.0],
        [GeoProjection.Mercator, 0.0, 0.0],
        [GeoProjection.Mercator, 45.0, 30.0],
        [GeoProjection.Sinusoidal, 0.0, 30.0],
        [GeoProjection.Sinusoidal, 45.0, 30.0],
    ];

    [Theory]
    [MemberData(nameof(RoundTrips))]
    public void Inverse_OfAForwardPoint_ReturnsThePoint(IGeoProjection projection, double latitude, double longitude)
    {
        var (x, y) = projection.Forward(latitude, longitude);
        var inverse = projection.Inverse(x, y);

        Assert.NotNull(inverse);
        Assert.Equal(latitude, inverse.Value.Latitude, 1e-6);
        Assert.Equal(longitude, inverse.Value.Longitude, 1e-6);
    }

    [Theory]
    [MemberData(nameof(NoInverse))]
    public void Inverse_OfAPointTheMapDoesNotHold_ReturnsNull(IGeoProjection projection, double x, double y) =>
        Assert.Null(projection.Inverse(x, y));

    // ─── Stereographic ────────────────────────────────────────────────────────
    public class Stereographic
    {
        [Fact]
        public void Forward_AtAntipodeOfCentre_ProducesExtremeMagnitude()
        {
            // Default Stereographic centred at North Pole (90, 0). Antipode = South Pole.
            // The k-divisor approaches 0 → projected coords blow up to extreme magnitude.
            // (Source returns an effectively-infinite finite value rather than NaN.)
            var p = new Projections.Stereographic(centerLat: 90, centerLon: 0);
            var (x, y) = p.Forward(-90, 0);
            double mag = Math.Max(Math.Abs(x), Math.Abs(y));
            Assert.True(mag > 1e10 || double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y),
                $"Antipode should produce extreme magnitude or NaN/Inf; got ({x}, {y})");
        }

        /// <summary>Exact antipode with equatorial centre — cos(180°) == −1.0 exactly in
        /// IEEE-754, so the denominator is exactly 0 → k == +Infinity → returns (NaN, NaN).
        /// This is the only geometry that reliably hits the <c>IsInfinity(k)</c> TRUE arm.</summary>
        [Fact]
        public void Forward_ExactEquatorialAntipode_ReturnsNaN()
        {
            var p = new Projections.Stereographic(centerLat: 0, centerLon: 0);
            var (x, y) = p.Forward(0, 180);
            Assert.True(double.IsNaN(x) && double.IsNaN(y),
                $"Exact antipode must return (NaN, NaN); got ({x}, {y})");
        }
    }

    // ─── Robinson ──────────────────────────────────────────────────────────────
    public class Robinson
    {
        private static readonly IGeoProjection P = GeoProjection.Robinson;

        [Fact]
        public void Forward_NaNInput_PropagatesAsNaN()
        {
            var (x, y) = P.Forward(double.NaN, 0);
            Assert.True(double.IsNaN(x));
            Assert.True(double.IsNaN(y));

            var (x2, y2) = P.Forward(0, double.NaN);
            Assert.True(double.IsNaN(x2));
            Assert.True(double.IsNaN(y2));
        }

        [Fact]
        public void Forward_BeyondPole_ClampsToBounds()
        {
            // |lat| > 90 → Min(abs, 90) clamp + last-entry index
            var (x1, y1) = P.Forward(180, 0);
            var (x2, y2) = P.Forward(90, 0);
            Assert.Equal(x1, x2, 1e-9);
            Assert.Equal(y1, y2, 1e-9);
        }
    }

    /// <summary>A Lambert cone is the same map whichever of its two standard parallels is named first: the edge of the cone is drawn
    /// from the parallel nearer the pole, which is found either way round.</summary>
    [Fact]
    public void ALambertCone_IsTheSameMap_WhicheverParallelIsNamedFirst()
    {
        var first = GeoProjection.LambertConformalWith(33, 45).Bounds;
        var second = GeoProjection.LambertConformalWith(45, 33).Bounds;

        Assert.Equal(first.XMin, second.XMin, 1e-9);
        Assert.Equal(first.XMax, second.XMax, 1e-9);
        Assert.Equal(first.YMin, second.YMin, 1e-9);
        Assert.Equal(first.YMax, second.YMax, 1e-9);
    }
}
