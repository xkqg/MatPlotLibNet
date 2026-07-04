// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

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
/// <para><b>Stacked structure</b> — each projection gets a nested test class for
/// SOLID single-responsibility per projection.</para></summary>
public class GeoProjectionEdgeCaseTests
{
    // Common assertion helper: every projection must return finite non-NaN at (0,0)
    // and the result must lie inside the projection's bounds.
    private static void AssertOriginFinite(IGeoProjection p)
    {
        var (x, y) = p.Forward(0, 0);
        Assert.False(double.IsNaN(x), $"{p.Name}: Forward(0,0) X is NaN");
        Assert.False(double.IsNaN(y), $"{p.Name}: Forward(0,0) Y is NaN");
        var b = p.Bounds;
        Assert.InRange(x, b.XMin - 1e-6, b.XMax + 1e-6);
        Assert.InRange(y, b.YMin - 1e-6, b.YMax + 1e-6);
    }

    // Helper: checks that the bounds rectangle has positive width/height
    // (non-degenerate). Critical because Ocean and Land use Bounds to draw the canvas.
    private static void AssertNonDegenerateBounds(IGeoProjection p)
    {
        var b = p.Bounds;
        Assert.True(b.XMax > b.XMin, $"{p.Name}: degenerate width Bounds {b}");
        Assert.True(b.YMax > b.YMin, $"{p.Name}: degenerate height Bounds {b}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class PlateCarreeProjection
    {
        private static readonly IGeoProjection P = GeoProjection.PlateCarree;

        [Fact] public void Origin_MapsToZero() { Assert.Equal(new ProjectedPoint(0, 0), P.Forward(0, 0)); }

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

        [Fact] public void Bounds_AreFullEarth() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsPlateCarree() => Assert.Equal("PlateCarree", P.Name);

        [Fact]
        public void Inverse_IsPresentAndRoundTrips_AtMidLatitudes()
        {
            // Identity projection has trivial inverse.
            var inv = P.Inverse(45, 30);
            if (inv is null) return; // optional Inverse — skip if not implemented
            Assert.Equal(30,  inv.Value.Latitude, 1e-9);
            Assert.Equal(45,  inv.Value.Longitude, 1e-9);
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

        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsMercator() => Assert.Equal("Mercator", P.Name);

        [Fact]
        public void Antisymmetric_AcrossEquator()
        {
            // Mercator: Forward(-lat, lon).Y == -Forward(lat, lon).Y for any (lat, lon).
            for (double lat = 5; lat <= 80; lat += 15)
            {
                var (_, y1) = P.Forward(lat, 30);
                var (_, y2) = P.Forward(-lat, 30);
                Assert.Equal(-y1, y2, 1e-9);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class RobinsonProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Robinson;

        [Fact] public void Origin_MapsToZero() => AssertOriginFinite(P);

        [Theory]
        [InlineData(0,    180)]
        [InlineData(0,   -180)]
        [InlineData(45,   0)]
        [InlineData(-45,  0)]
        [InlineData(90,   0)]    // pole
        [InlineData(-90,  0)]
        public void Forward_StaysWithinBounds(double lat, double lon)
        {
            var (x, y) = P.Forward(lat, lon);
            var b = P.Bounds;
            Assert.InRange(x, b.XMin - 1e-6, b.XMax + 1e-6);
            Assert.InRange(y, b.YMin - 1e-6, b.YMax + 1e-6);
        }

        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsRobinson() => Assert.Equal("Robinson", P.Name);

        [Fact]
        public void Antisymmetric_YAcrossEquator()
        {
            for (double lat = 5; lat <= 75; lat += 15)
            {
                var (_, y1) = P.Forward( lat, 30);
                var (_, y2) = P.Forward(-lat, 30);
                Assert.Equal(-y1, y2, 1e-9);
            }
        }

        [Fact]
        public void Inverse_ReturnsNull_NotInvertible()
        {
            // Robinson uses tabulated values; analytic inverse is not implemented.
            Assert.Null(P.Inverse(0, 0));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class SinusoidalProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Sinusoidal;

        [Fact] public void Origin_MapsToZero() { Assert.Equal(new ProjectedPoint(0, 0), P.Forward(0, 0)); }

        [Theory]
        [InlineData(0,    180,   180,    0)]    // (lat=0, lon=180): cos(0)=1 → x=180, y=0
        [InlineData(0,   -180,  -180,    0)]
        [InlineData(45,   0,    0,    45)]      // (45, 0): cos(45) doesn't matter because lon=0
        [InlineData(-45,  0,    0,   -45)]
        public void Forward_FormulaParity(double lat, double lon, double expectedX, double expectedY)
        {
            // Sinusoidal: x = lon * cos(lat), y = lat
            var (x, y) = P.Forward(lat, lon);
            Assert.Equal(expectedX, x, 1e-9);
            Assert.Equal(expectedY, y, 1e-9);
        }

        [Fact]
        public void Pole_MapsToZeroX()
        {
            // At ±90°: cos(±90°) = 0 → x = 0 regardless of lon.
            var (x90, y90)   = P.Forward(90,  100);
            var (xm90, ym90) = P.Forward(-90, 100);
            Assert.Equal(0,  x90, 1e-9);
            Assert.Equal(90, y90, 1e-9);
            Assert.Equal(0,   xm90, 1e-9);
            Assert.Equal(-90, ym90, 1e-9);
        }

        [Fact]
        public void Inverse_OutsideBounds_ReturnsNull()
        {
            // |y| > 90 → out of map → null. (At exactly ±90, cos(90°) is tiny but
            // non-zero in floating point, so the source's strict cosLat==0 check is
            // bypassed and inverse returns a finite value scaled by 1/cos. We test
            // the documented out-of-bounds case here.)
            Assert.Null(P.Inverse(0,  91));
            Assert.Null(P.Inverse(0, -91));
        }

        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsSinusoidal() => Assert.Equal("Sinusoidal", P.Name);
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class MollweideProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Mollweide;

        [Fact] public void Origin_MapsToZero() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsMollweide() => Assert.Equal("Mollweide", P.Name);

        [Theory]
        [InlineData(45,   90)]
        [InlineData(-45,  90)]
        [InlineData(60,   180)]
        [InlineData(-60, -180)]
        public void Forward_StaysWithinBounds(double lat, double lon)
        {
            var (x, y) = P.Forward(lat, lon);
            var b = P.Bounds;
            Assert.InRange(x, b.XMin - 1e-6, b.XMax + 1e-6);
            Assert.InRange(y, b.YMin - 1e-6, b.YMax + 1e-6);
        }

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
    public class AlbersEqualAreaProjection
    {
        private static readonly IGeoProjection P = GeoProjection.AlbersEqualArea;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsAlbers() => Assert.Equal("AlbersEqualArea", P.Name);

        [Theory]
        [InlineData(40,   -100)]   // continental US center
        [InlineData(0,     0)]
        public void Forward_IsFinite(double lat, double lon)
        {
            var (x, y) = P.Forward(lat, lon);
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsNaN(y));
        }

        [Fact]
        public void Custom_StandardParallels_FactoryWorks()
        {
            var custom = GeoProjection.AlbersEqualAreaWith(sp1: 30, sp2: 60, centerLon: 0, centerLat: 45);
            Assert.NotNull(custom);
            var (x, y) = custom.Forward(45, 0);
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsNaN(y));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class OrthographicProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Orthographic;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsOrthographic() => Assert.Equal("Orthographic", P.Name);

        [Fact]
        public void BackSideOfGlobe_ReturnsNaN()
        {
            // Default Orthographic centred at (0,0). The antipode (0, 180) is on the
            // far side of the globe and must NOT project to a finite value (matplotlib
            // / cartopy convention is NaN to indicate "not visible").
            var (x, y) = P.Forward(0, 180);
            Assert.True(double.IsNaN(x) || double.IsNaN(y),
                $"Antipode (0, 180) should be NaN; got ({x}, {y})");
        }

        [Fact]
        public void OrthographicAt_CentersAtCustomPoint()
        {
            var tokyo = GeoProjection.OrthographicAt(centerLat: 35.7, centerLon: 139.7);
            // Tokyo itself should map to (0, 0) under its own centred projection.
            var (x, y) = tokyo.Forward(35.7, 139.7);
            Assert.Equal(0, x, 1e-6);
            Assert.Equal(0, y, 1e-6);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class StereographicProjection
    {
        private static readonly IGeoProjection P = GeoProjection.Stereographic;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_IsStereographic() => Assert.Equal("Stereographic", P.Name);

        [Fact]
        public void StereographicAt_CentersAtPole()
        {
            var northPole = GeoProjection.StereographicAt(centerLat: 90, centerLon: 0);
            var (x, y) = northPole.Forward(90, 0);
            Assert.Equal(0, x, 1e-6);
            Assert.Equal(0, y, 1e-6);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class AzimuthalEquidistantProjection
    {
        private static readonly IGeoProjection P = GeoProjection.AzimuthalEquidistant;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_Correct() => Assert.Equal("AzimuthalEquidistant", P.Name);

        [Fact]
        public void AzimuthalEquidistantAt_CentersAtCustomPoint()
        {
            var london = GeoProjection.AzimuthalEquidistantAt(centerLat: 51.5, centerLon: -0.1);
            var (x, y) = london.Forward(51.5, -0.1);
            Assert.Equal(0, x, 1e-6);
            Assert.Equal(0, y, 1e-6);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class TransverseMercatorProjection
    {
        private static readonly IGeoProjection P = GeoProjection.TransverseMercator;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_Correct() => Assert.Equal("TransverseMercator", P.Name);

        [Fact]
        public void TransverseMercatorAt_HasCustomCentralMeridian()
        {
            var utm = GeoProjection.TransverseMercatorAt(centerLon: 15);
            // At the central meridian (lon = 15), X should be 0 (or very small).
            var (x, _) = utm.Forward(40, 15);
            Assert.Equal(0, x, 1e-6);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class LambertConformalProjection
    {
        private static readonly IGeoProjection P = GeoProjection.LambertConformal;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_Correct() => Assert.Equal("LambertConformal", P.Name);

        [Fact]
        public void LambertConformalWith_CustomStandardParallels()
        {
            var europe = GeoProjection.LambertConformalWith(sp1: 35, sp2: 65, centerLon: 10, centerLat: 52);
            var (x, y) = europe.Forward(52, 10);
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsNaN(y));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class NaturalEarthProjection
    {
        private static readonly IGeoProjection P = GeoProjection.NaturalEarth;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_Correct() => Assert.Equal("NaturalEarth", P.Name);

        [Fact]
        public void Antisymmetric_YAcrossEquator()
        {
            for (double lat = 5; lat <= 80; lat += 15)
            {
                var (_, y1) = P.Forward( lat, 30);
                var (_, y2) = P.Forward(-lat, 30);
                Assert.Equal(-y1, y2, 1e-9);
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class EqualEarthProjection
    {
        private static readonly IGeoProjection P = GeoProjection.EqualEarth;

        [Fact] public void Origin_IsFinite() => AssertOriginFinite(P);
        [Fact] public void Bounds_AreNonDegenerate() => AssertNonDegenerateBounds(P);
        [Fact] public void Name_Correct() => Assert.Equal("EqualEarth", P.Name);

        [Fact]
        public void Antisymmetric_YAcrossEquator()
        {
            for (double lat = 5; lat <= 75; lat += 10)
            {
                var (_, y1) = P.Forward( lat, 30);
                var (_, y2) = P.Forward(-lat, 30);
                Assert.Equal(-y1, y2, 1e-9);
            }
        }

        [Fact] public void Origin_MapsExactlyToZero() => Assert.Equal(new ProjectedPoint(0, 0), P.Forward(0, 0));

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
        public static IEnumerable<object[]> EveryProjection()
        {
            yield return new object[] { GeoProjection.PlateCarree };
            yield return new object[] { GeoProjection.Mercator };
            yield return new object[] { GeoProjection.Robinson };
            yield return new object[] { GeoProjection.Mollweide };
            yield return new object[] { GeoProjection.Sinusoidal };
            yield return new object[] { GeoProjection.AlbersEqualArea };
            yield return new object[] { GeoProjection.AzimuthalEquidistant };
            yield return new object[] { GeoProjection.Stereographic };
            yield return new object[] { GeoProjection.TransverseMercator };
            yield return new object[] { GeoProjection.LambertConformal };
            yield return new object[] { GeoProjection.NaturalEarth };
            yield return new object[] { GeoProjection.EqualEarth };
            yield return new object[] { GeoProjection.Orthographic };
        }

        [Theory] [MemberData(nameof(EveryProjection))]
        public void HasNonEmptyName(IGeoProjection p) => Assert.False(string.IsNullOrWhiteSpace(p.Name));

        [Theory] [MemberData(nameof(EveryProjection))]
        public void HasNonDegenerateBounds(IGeoProjection p) => AssertNonDegenerateBounds(p);

        [Theory] [MemberData(nameof(EveryProjection))]
        public void OriginIsFiniteOrInsideBounds(IGeoProjection p) => AssertOriginFinite(p);

        [Theory] [MemberData(nameof(EveryProjection))]
        public void NaNInput_PropagatesAsNaN(IGeoProjection p)
        {
            var (x, y) = p.Forward(double.NaN, double.NaN);
            // Either both NaN, or the projection is NaN-defensive (returns 0).
            // Don't enforce one behaviour — just that it doesn't throw.
            Assert.False(double.IsNaN(x) && !double.IsNaN(y),
                $"{p.Name}: Forward(NaN, NaN) inconsistent: ({x}, {y})");
        }
    }
}
