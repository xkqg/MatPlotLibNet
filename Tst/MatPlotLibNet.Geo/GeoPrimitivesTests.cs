// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Pin tests for the F5 projection primitives — <see cref="ProjectedPoint"/>,
/// <see cref="GeoCoordinate"/>, and <see cref="GeoBounds"/>. Locks equality, deconstruction,
/// and the off-domain NaN sentinel so the record-struct migration keeps the tuple ergonomics
/// (value equality + positional deconstruction) that call sites relied on.</summary>
public class GeoPrimitivesTests
{
    // ─── ProjectedPoint (Forward, projected-plane space) ────────────────────────
    public class ProjectedPointTests
    {
        [Fact]
        public void ValueEquality_SameComponents_AreEqual()
        {
            Assert.Equal(new ProjectedPoint(1.5, -2.5), new ProjectedPoint(1.5, -2.5));
            Assert.True(new ProjectedPoint(1.5, -2.5) == new ProjectedPoint(1.5, -2.5));
        }

        [Fact]
        public void ValueEquality_DifferentComponents_AreNotEqual()
        {
            Assert.NotEqual(new ProjectedPoint(1.5, -2.5), new ProjectedPoint(1.5, 2.5));
            Assert.True(new ProjectedPoint(1.0, 2.0) != new ProjectedPoint(2.0, 1.0));
        }

        [Fact]
        public void Deconstructs_Positionally_IntoXThenY()
        {
            var (x, y) = new ProjectedPoint(3.0, 4.0);
            Assert.Equal(3.0, x);
            Assert.Equal(4.0, y);
        }

        [Fact]
        public void OffDomainSentinel_IsNaNOnBothComponents()
        {
            // Forward signals off-globe / out-of-domain points as (NaN, NaN); consumers
            // test double.IsNaN on the components (never == against a sentinel value).
            var offGlobe = new ProjectedPoint(double.NaN, double.NaN);
            Assert.True(double.IsNaN(offGlobe.X));
            Assert.True(double.IsNaN(offGlobe.Y));
        }
    }

    // ─── GeoCoordinate (Inverse, geographic space) ──────────────────────────────
    public class GeoCoordinateTests
    {
        [Fact]
        public void ValueEquality_SameComponents_AreEqual()
        {
            Assert.Equal(new GeoCoordinate(52.0, 4.0), new GeoCoordinate(52.0, 4.0));
            Assert.True(new GeoCoordinate(52.0, 4.0) == new GeoCoordinate(52.0, 4.0));
        }

        [Fact]
        public void Deconstructs_Positionally_IntoLatitudeThenLongitude()
        {
            var (lat, lon) = new GeoCoordinate(51.5, -0.1);
            Assert.Equal(51.5, lat);
            Assert.Equal(-0.1, lon);
        }

        [Fact]
        public void Nullable_DefaultsToNull_ForOutOfDomainInverse()
        {
            GeoCoordinate? missing = null;
            Assert.Null(missing);
            GeoCoordinate? present = new GeoCoordinate(1.0, 2.0);
            Assert.NotNull(present);
            Assert.Equal(1.0, present.Value.Latitude);
            Assert.Equal(2.0, present.Value.Longitude);
        }
    }

    // ─── GeoBounds (projection bounding box) ────────────────────────────────────
    public class GeoBoundsTests
    {
        [Fact]
        public void ValueEquality_SameComponents_AreEqual()
        {
            Assert.Equal(new GeoBounds(-180, 180, -90, 90), new GeoBounds(-180, 180, -90, 90));
        }

        [Fact]
        public void Deconstructs_Positionally_IntoXMinXMaxYMinYMax()
        {
            var (xMin, xMax, yMin, yMax) = new GeoBounds(-180, 180, -90, 90);
            Assert.Equal(-180, xMin);
            Assert.Equal(180, xMax);
            Assert.Equal(-90, yMin);
            Assert.Equal(90, yMax);
        }

        [Fact]
        public void Members_ExposeNamedEdges()
        {
            var b = new GeoBounds(-200, 200, -100, 100);
            Assert.Equal(-200, b.XMin);
            Assert.Equal(200, b.XMax);
            Assert.Equal(-100, b.YMin);
            Assert.Equal(100, b.YMax);
        }
    }
}
