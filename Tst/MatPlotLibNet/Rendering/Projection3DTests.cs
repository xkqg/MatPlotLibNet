// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies <see cref="Projection3D"/> coordinate projection and depth ordering.</summary>
public class Projection3DTests
{
    private static Projection3D CreateProjection(double elevation = 30, double azimuth = -60) =>
        new(elevation, azimuth, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);

    /// <summary>Verifies that Project returns a point within the plot bounds.</summary>
    [Fact]
    public void Project_ReturnsPointWithinBounds()
    {
        var proj = CreateProjection();
        var pt = proj.Project(5, 5, 5);
        Assert.InRange(pt.X, 0, 400);
        Assert.InRange(pt.Y, 0, 400);
    }

    /// <summary>Verifies that different 3D points map to different 2D points.</summary>
    [Fact]
    public void Project_DifferentInputs_ProduceDifferentOutputs()
    {
        var proj = CreateProjection();
        var p1 = proj.Project(0, 0, 0);
        var p2 = proj.Project(10, 10, 10);
        Assert.NotEqual(p1, p2);
    }

    /// <summary>Verifies that Depth returns different values for points at different positions.</summary>
    [Fact]
    public void Depth_DifferentPositions_ReturnsDifferentValues()
    {
        var proj = CreateProjection();
        double d1 = proj.Depth(0, 0, 0);
        double d2 = proj.Depth(10, 10, 10);
        Assert.NotEqual(d1, d2);
    }

    /// <summary>Verifies that a point closer to the viewer has a different depth than one further away.</summary>
    [Fact]
    public void Depth_OrderingIsConsistent()
    {
        var proj = CreateProjection();
        // Two points at same X/Z but different Y should have different depths
        double d1 = proj.Depth(5, 0, 5);
        double d2 = proj.Depth(5, 10, 5);
        Assert.NotEqual(d1, d2);
    }

    /// <summary>Verifies that Elevation property is stored correctly.</summary>
    [Fact]
    public void Elevation_IsStored()
    {
        var proj = CreateProjection(elevation: 45);
        Assert.Equal(45, proj.Elevation);
    }

    /// <summary>Verifies that Azimuth property is stored correctly.</summary>
    [Fact]
    public void Azimuth_IsStored()
    {
        var proj = CreateProjection(azimuth: -30);
        Assert.Equal(-30, proj.Azimuth);
    }

    /// <summary>Verifies that a center point projects near the center of the plot bounds.</summary>
    [Fact]
    public void Project_CenterPoint_NearCenter()
    {
        var proj = new Projection3D(0, 0, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);
        var pt = proj.Project(5, 5, 5);
        // With 0 elevation and 0 azimuth, center point should be near center
        Assert.InRange(pt.X, 150, 250);
        Assert.InRange(pt.Y, 150, 250);
    }
}
