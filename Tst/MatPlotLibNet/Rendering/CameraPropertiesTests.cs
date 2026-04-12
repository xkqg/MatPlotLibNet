// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies camera properties on Axes and perspective on Projection3D.</summary>
public class CameraPropertiesTests
{
    // --- Axes camera properties ---

    [Fact]
    public void Axes_Elevation_DefaultIs30()
    {
        var axes = new Axes();
        Assert.Equal(30, axes.Elevation);
    }

    [Fact]
    public void Axes_Azimuth_DefaultIsMinus60()
    {
        var axes = new Axes();
        Assert.Equal(-60, axes.Azimuth);
    }

    [Fact]
    public void Axes_CameraDistance_DefaultIsNull()
    {
        var axes = new Axes();
        Assert.Null(axes.CameraDistance);
    }

    [Fact]
    public void Axes_CameraDistance_CanBeSet()
    {
        var axes = new Axes { CameraDistance = 5.0 };
        Assert.Equal(5.0, axes.CameraDistance);
    }

    // --- Projection3D perspective ---

    [Fact]
    public void Projection3D_Distance_IsStored()
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: 5.0);
        Assert.Equal(5.0, proj.Distance);
    }

    [Fact]
    public void Projection3D_NullDistance_IsOrthographic()
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        Assert.Null(proj.Distance);
    }

    [Fact]
    public void Projection3D_DistanceClamped_MinimumTwo()
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: 0.5);
        Assert.Equal(2.0, proj.Distance);
    }

    [Fact]
    public void Projection3D_Perspective_CenterPointStillInBounds()
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: 5.0);
        var pt = proj.Project(5, 5, 5);
        Assert.InRange(pt.X, -200, 600); // perspective can project slightly outside but not wildly
        Assert.InRange(pt.Y, -200, 600);
    }

    [Fact]
    public void Projection3D_Perspective_ParallaxEffect()
    {
        // Two points at different depths should have different 2D separation than orthographic
        var ortho = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        var persp = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: 3.0);

        var p1Ortho = ortho.Project(0, 0, 0);
        var p2Ortho = ortho.Project(10, 10, 10);
        var p1Persp = persp.Project(0, 0, 0);
        var p2Persp = persp.Project(10, 10, 10);

        double distOrtho = Math.Sqrt(Math.Pow(p2Ortho.X - p1Ortho.X, 2) + Math.Pow(p2Ortho.Y - p1Ortho.Y, 2));
        double distPersp = Math.Sqrt(Math.Pow(p2Persp.X - p1Persp.X, 2) + Math.Pow(p2Persp.Y - p1Persp.Y, 2));

        // Perspective projection should produce different spread
        Assert.NotEqual(distOrtho, distPersp, 1.0);
    }

    [Fact]
    public void Projection3D_Orthographic_BackwardCompatible()
    {
        // Original constructor (no distance) must still produce same results as before
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        var pt = proj.Project(5, 5, 5);
        Assert.InRange(pt.X, 0, 400);
        Assert.InRange(pt.Y, 0, 400);
    }

    [Fact]
    public void Projection3D_Normalize_ReturnsNormalizedCoords()
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        var (nx, ny, nz) = proj.Normalize(5, 5, 5);
        Assert.Equal(0.0, nx, 6);
        Assert.Equal(0.0, ny, 6);
        Assert.Equal(0.0, nz, 6);
    }
}
