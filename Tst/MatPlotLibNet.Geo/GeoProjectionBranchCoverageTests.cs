// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Branch-coverage edge cases for projection classes that have low branch coverage:
/// PlateCarree (Inverse out-of-bounds path), Mercator (out-of-bounds inverse path),
/// Sinusoidal (Inverse cosLat==0 path), Stereographic (antipode k&lt;0 path),
/// TransverseMercator (|b|≥1 NaN path), Robinson (Inverse always-null path).</summary>
public class GeoProjectionBranchCoverageTests
{
    // ─── PlateCarree ───────────────────────────────────────────────────────────
    public class PlateCarree
    {
        private static readonly IGeoProjection P = GeoProjection.PlateCarree;

        [Fact]
        public void Inverse_OutOfBoundsLat_ReturnsNull()
            => Assert.Null(P.Inverse(0, 91));

        [Fact]
        public void Inverse_OutOfBoundsNegLat_ReturnsNull()
            => Assert.Null(P.Inverse(0, -91));

        [Fact]
        public void Inverse_OutOfBoundsLon_ReturnsNull()
            => Assert.Null(P.Inverse(181, 0));

        [Fact]
        public void Inverse_OutOfBoundsNegLon_ReturnsNull()
            => Assert.Null(P.Inverse(-181, 0));

        [Fact]
        public void Inverse_AtBounds_ReturnsValid()
        {
            var inv = P.Inverse(180, 90);
            Assert.NotNull(inv);
            Assert.Equal(90, inv.Value.Lat);
            Assert.Equal(180, inv.Value.Lon);
        }
    }

    // ─── Mercator ─────────────────────────────────────────────────────────────
    public class Mercator
    {
        private static readonly IGeoProjection P = GeoProjection.Mercator;

        [Fact]
        public void Inverse_AtEquatorPrimeMeridian_RoundTrips()
        {
            var inv = P.Inverse(0, 0);
            Assert.NotNull(inv);
            Assert.Equal(0, inv.Value.Lat, 1e-9);
            Assert.Equal(0, inv.Value.Lon, 1e-9);
        }

        [Fact]
        public void Inverse_AtMidLatitude_RoundTrips()
        {
            var (x, y) = P.Forward(45, 30);
            var inv = P.Inverse(x, y);
            Assert.NotNull(inv);
            Assert.Equal(45, inv.Value.Lat, 1e-6);
            Assert.Equal(30, inv.Value.Lon, 1e-6);
        }

        [Fact]
        public void Inverse_OutsideMaxLat_ReturnsNull()
        {
            // A Y far above the top bound corresponds to lat > 85.05 → returns null
            var inv = P.Inverse(0, 1000);
            Assert.Null(inv);
        }

        [Fact]
        public void Inverse_OutsideMinLat_ReturnsNull()
        {
            var inv = P.Inverse(0, -1000);
            Assert.Null(inv);
        }
    }

    // ─── Sinusoidal ───────────────────────────────────────────────────────────
    public class Sinusoidal
    {
        private static readonly IGeoProjection P = GeoProjection.Sinusoidal;

        [Fact]
        public void Inverse_AtEquator_RoundTrips()
        {
            var inv = P.Inverse(30, 0);
            Assert.NotNull(inv);
            Assert.Equal(0, inv.Value.Lat, 1e-9);
            Assert.Equal(30, inv.Value.Lon, 1e-9);
        }

        [Fact]
        public void Inverse_OutOfBoundsY_ReturnsNull()
        {
            Assert.Null(P.Inverse(0, 91));
            Assert.Null(P.Inverse(0, -91));
        }

        [Fact]
        public void Inverse_AtMidLatitude_RoundTrips()
        {
            var (x, y) = P.Forward(45, 30);
            var inv = P.Inverse(x, y);
            Assert.NotNull(inv);
            Assert.Equal(45, inv.Value.Lat, 1e-6);
            Assert.Equal(30, inv.Value.Lon, 1e-6);
        }

        /// <summary>y = ±90 — passes the |y|&gt;90 guard (90 is NOT &gt; 90)
        /// but cos(90°)=0 → cosLat==0 TRUE arm → returns null.</summary>
        [Fact]
        public void Inverse_AtExactPole_ReturnsNull()
        {
            Assert.Null(P.Inverse(0, 90));
            Assert.Null(P.Inverse(0, -90));
        }
    }

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

        [Fact]
        public void Forward_NotInvertible_ReturnsNull()
        {
            var p = new Projections.Stereographic();
            Assert.Null(p.Inverse(0, 0));
            Assert.Null(p.Inverse(100, 100));
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

        [Fact]
        public void Forward_AtCentre_ReturnsZero()
        {
            var p = new Projections.Stereographic(centerLat: 45, centerLon: 30);
            var (x, y) = p.Forward(45, 30);
            Assert.Equal(0, x, 1e-6);
            Assert.Equal(0, y, 1e-6);
        }
    }

    // ─── TransverseMercator ────────────────────────────────────────────────────
    public class TransverseMercator
    {
        [Fact]
        public void Forward_AtSingularity_ReturnsNaN()
        {
            // |b| = |cos(phi) sin(lam-lam0)| = 1 at lat=0, lon=±90 from central meridian
            var p = new Projections.TransverseMercator(centerLon: 0);
            var (x, y) = p.Forward(0, 90);
            Assert.True(double.IsNaN(x) || double.IsNaN(y),
                $"Singularity at (0, 90) should give NaN; got ({x}, {y})");
        }

        [Fact]
        public void Forward_AwayFromSingularity_IsFinite()
        {
            var p = new Projections.TransverseMercator();
            var (x, y) = p.Forward(45, 10);
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsNaN(y));
        }

        [Fact]
        public void Inverse_AlwaysReturnsNull()
        {
            // Source comment: not analytically invertible.
            var p = new Projections.TransverseMercator();
            Assert.Null(p.Inverse(0, 0));
            Assert.Null(p.Inverse(45, 30));
        }
    }

    // ─── Robinson ──────────────────────────────────────────────────────────────
    public class Robinson
    {
        private static readonly IGeoProjection P = GeoProjection.Robinson;

        [Fact]
        public void Inverse_AlwaysReturnsNull()
        {
            // Robinson is tabulated → no analytic inverse.
            Assert.Null(P.Inverse(0, 0));
            Assert.Null(P.Inverse(100, 50));
        }

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
        public void Forward_AtPole_HitsLastTableEntry()
        {
            // |lat| = 90 → index >= _plen.Length-1 → falls into the index clamp branch
            var (x, y) = P.Forward(90, 180);
            Assert.False(double.IsNaN(x));
            Assert.False(double.IsNaN(y));
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
}
