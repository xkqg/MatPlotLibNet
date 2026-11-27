// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Geo.GeoJson;
using MatPlotLibNet.Geo.Projections;

namespace MatPlotLibNet.Geo.Tests;

/// <summary>Coverage for <see cref="GeoClipping"/>: every branch of <c>FilterNaN</c> and <c>ClipToBounds</c>. The cut of a
/// ring at the antimeridian is held by <see cref="GeoClippingPositionTests"/>.</summary>
public class GeoClippingTests
{
    // ─── FilterNaN ─────────────────────────────────────────────────────────────
    public class FilterNaN
    {
        [Fact]
        public void NoNaN_ReturnsAllPoints()
        {
            var pts = new List<ProjectedPoint> { new(1, 2), new(3, 4), new(5, 6) };
            var result = GeoClipping.FilterNaN(pts);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void AllNaN_ReturnsEmpty()
        {
            var pts = new List<ProjectedPoint>
            {
                new(double.NaN, double.NaN),
                new(1, double.NaN),
                new(double.NaN, 2),
            };
            var result = GeoClipping.FilterNaN(pts);
            Assert.Empty(result);
        }

        [Fact]
        public void MixedNaN_OnlyKeepsFinite()
        {
            var pts = new List<ProjectedPoint>
            {
                new(1, 2),
                new(double.NaN, 3),
                new(4, 5),
                new(6, double.NaN),
                new(7, 8),
            };
            var result = GeoClipping.FilterNaN(pts);
            Assert.Equal(3, result.Count);
            Assert.Contains(new ProjectedPoint(1.0, 2.0), result);
            Assert.Contains(new ProjectedPoint(4.0, 5.0), result);
            Assert.Contains(new ProjectedPoint(7.0, 8.0), result);
        }

        [Fact]
        public void EmptyList_ReturnsEmpty()
        {
            var result = GeoClipping.FilterNaN(new List<ProjectedPoint>());
            Assert.Empty(result);
        }
    }

    // ─── ClipToBounds ─────────────────────────────────────────────────────────
    public class ClipToBounds
    {
        [Fact]
        public void AllInside_PointsUnchanged()
        {
            var pts = new List<ProjectedPoint> { new(0, 0), new(5, 5), new(-5, -5) };
            var result = GeoClipping.ClipToBounds(pts, -10, 10, -10, 10);
            Assert.Equal(pts.Count, result.Count);
            for (int i = 0; i < pts.Count; i++)
            {
                Assert.Equal(pts[i].X, result[i].X);
                Assert.Equal(pts[i].Y, result[i].Y);
            }
        }

        [Fact]
        public void OutOfBounds_GetsClampedToBoundary()
        {
            var pts = new List<ProjectedPoint> { new(-100, -100), new(100, 100), new(50, -50) };
            var result = GeoClipping.ClipToBounds(pts, -10, 10, -20, 20);
            Assert.Equal(new ProjectedPoint(-10.0, -20.0), result[0]);
            Assert.Equal(new ProjectedPoint(10.0, 20.0),   result[1]);
            Assert.Equal(new ProjectedPoint(10.0, -20.0),  result[2]);
        }

        [Fact]
        public void EmptyList_ReturnsEmpty()
            => Assert.Empty(GeoClipping.ClipToBounds(new(), -1, 1, -1, 1));
    }
}
