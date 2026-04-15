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
        // Two projections sharing the same camera angles and data bounds but different
        // camera distances must produce visibly different screen-space separations for
        // a diagonal 3-D line. Distance 10 (default) places the camera far enough that
        // near/far points project at nearly the same scale; distance 3 brings the camera
        // close enough that the far point foreshortens noticeably, widening the gap.
        var farCam  = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        var nearCam = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: 3.0);

        var p1Far  = farCam.Project(0, 0, 0);
        var p2Far  = farCam.Project(10, 10, 10);
        var p1Near = nearCam.Project(0, 0, 0);
        var p2Near = nearCam.Project(10, 10, 10);

        double distFar  = Math.Sqrt(Math.Pow(p2Far.X - p1Far.X, 2) + Math.Pow(p2Far.Y - p1Far.Y, 2));
        double distNear = Math.Sqrt(Math.Pow(p2Near.X - p1Near.X, 2) + Math.Pow(p2Near.Y - p1Near.Y, 2));

        // Bringing the camera closer should visibly change the projected diagonal length.
        Assert.NotEqual(distFar, distNear, 1.0);
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
