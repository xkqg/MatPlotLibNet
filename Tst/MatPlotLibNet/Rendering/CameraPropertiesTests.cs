// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies camera properties on Axes and perspective on Projection3D.</summary>
public class CameraPropertiesTests
{
    // --- Projection3D perspective ---

    /// <summary>A camera distance is kept from 2 up, a nearer one is held at 2, and none at all is the orthographic view.</summary>
    [Theory]
    [InlineData(5.0, 5.0)]
    [InlineData(0.5, 2.0)]
    [InlineData(null, null)]
    public void Projection3D_Distance_IsHeldAtTwoOrMore_AndNoneIsOrthographic(double? given, double? kept)
    {
        var proj = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: given);
        Assert.Equal(kept, proj.Distance);
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
        // camera distances must draw an edge near the camera at visibly different lengths.
        // The box keeps its size (matplotlib's view limits scale with the distance), so the
        // diagonal through its centre barely moves; the front X edge, nearer the camera than
        // the centre, grows as the camera comes closer (matplotlib: 273 px at focal length 1,
        // 305 px at focal length 0.2 for an 800 x 600 figure).
        var farCam  = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        var nearCam = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10, distance: 3.0);

        var p1Far  = farCam.Project(0, 0, 0);
        var p2Far  = farCam.Project(10, 0, 0);
        var p1Near = nearCam.Project(0, 0, 0);
        var p2Near = nearCam.Project(10, 0, 0);

        double distFar  = Math.Sqrt(Math.Pow(p2Far.X - p1Far.X, 2) + Math.Pow(p2Far.Y - p1Far.Y, 2));
        double distNear = Math.Sqrt(Math.Pow(p2Near.X - p1Near.X, 2) + Math.Pow(p2Near.Y - p1Near.Y, 2));

        // Bringing the camera closer draws the near edge visibly longer.
        Assert.True(distNear > distFar + 5, $"near {distNear:F1} px against far {distFar:F1} px");
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
