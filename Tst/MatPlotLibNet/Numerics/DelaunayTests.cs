// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Delaunay"/> triangulation correctness.</summary>
public class DelaunayTests
{
    [Fact]
    public void ThreePoints_ProducesOneTriangle()
    {
        double[] x = [0.0, 1.0, 0.5];
        double[] y = [0.0, 0.0, 1.0];
        var mesh = Delaunay.Triangulate(x, y);
        Assert.Equal(3, mesh.Triangles.Length); // 1 triangle × 3 indices
    }

    [Fact]
    public void FourPoints_Square_ProducesTwoTriangles()
    {
        double[] x = [0.0, 1.0, 1.0, 0.0];
        double[] y = [0.0, 0.0, 1.0, 1.0];
        var mesh = Delaunay.Triangulate(x, y);
        // A square produces exactly 2 triangles
        Assert.Equal(6, mesh.Triangles.Length);
    }

    [Fact]
    public void LessThanThreePoints_ReturnsEmptyTriangles()
    {
        double[] x = [0.0, 1.0];
        double[] y = [0.0, 0.0];
        var mesh = Delaunay.Triangulate(x, y);
        Assert.Empty(mesh.Triangles);
    }

    [Fact]
    public void AllIndicesInBounds()
    {
        double[] x = [0.0, 1.0, 2.0, 0.5, 1.5, 1.0];
        double[] y = [0.0, 0.0, 0.0, 1.0, 1.0, 2.0];
        var mesh = Delaunay.Triangulate(x, y);
        foreach (int idx in mesh.Triangles)
            Assert.InRange(idx, 0, x.Length - 1);
    }

    [Fact]
    public void CollinearPoints_ReturnsResult()
    {
        // All points on a line — jitter should prevent degeneracy
        double[] x = [0.0, 1.0, 2.0, 3.0];
        double[] y = [0.0, 0.0, 0.0, 0.0];
        var mesh = Delaunay.Triangulate(x, y);
        // Should not throw; triangle count ≥ 0
        Assert.NotNull(mesh);
        Assert.Equal(x.Length, mesh.X.Length);
    }

    [Fact]
    public void OriginalCoordinatesPreserved()
    {
        double[] x = [0.0, 3.0, 1.5];
        double[] y = [0.0, 0.0, 2.0];
        var mesh = Delaunay.Triangulate(x, y);
        // Y coordinates are never jittered
        Assert.Equal(y, mesh.Y);
    }

    /// <summary>Covers the <c>scale == 0</c> fallback in <c>JitterCollinear</c> — when every
    /// input point is at the same coordinate, <c>(xMax - xMin) + (yMax - yMin) == 0</c> and the
    /// jitter scale falls back to <c>Epsilon</c>. Without this fallback the deterministic jitter
    /// would be a no-op and the Bowyer-Watson insertion would see coincident points.</summary>
    [Fact]
    public void IdenticalPoints_TriggersZeroScaleFallback()
    {
        // All 3 points at the origin — degenerate input that exercises the scale==0 arm.
        double[] x = [0.0, 0.0, 0.0];
        double[] y = [0.0, 0.0, 0.0];
        var mesh = Delaunay.Triangulate(x, y);
        Assert.NotNull(mesh);
        Assert.Equal(x.Length, mesh.X.Length);
    }
}
