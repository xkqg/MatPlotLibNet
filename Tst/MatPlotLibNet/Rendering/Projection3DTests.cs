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

    /// <summary>An axis whose range has no width (one point, a constant column) places its values in the middle of the
    /// box, the rule every 2-D axis follows (a flat range is widened by half a unit each side). Before, the box was fitted
    /// to its eight corners, which a one-point range collapsed onto one point: the fit divided by zero and a one-point
    /// 3-D scatter on 2-D axes was written as <c>cx="NaN" cy="NaN"</c>.</summary>
    [Fact]
    public void AnAxisWithNoWidth_PlacesItsValuesInTheMiddleOfTheBox()
    {
        var flatX = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 2, 2, 0, 10, 0, 10);
        var wideX = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 0, 10, 0, 10, 0, 10);

        Assert.Equal(0, flatX.Normalize(2, 5, 5).Nx);
        Assert.Equal(wideX.Project(5, 5, 5).X, flatX.Project(2, 5, 5).X, 9);
        Assert.Equal(wideX.Project(5, 5, 5).Y, flatX.Project(2, 5, 5).Y, 9);
        Assert.Equal(new Box3D(1.5, 2.5, 0, 10, 0, 10), flatX.DataBox);
    }

    /// <summary>A reversed range (a 3-D axis limit set high to low reaches the projection as it is) is not flat: it is
    /// not widened, and every point still projects to a finite pixel.</summary>
    [Fact]
    public void AReversedRange_IsNotWidened_AndProjectsFinitePoints()
    {
        var reversed = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 10, 0, 10, 0, 10, 0);

        var pt = reversed.Project(5, 5, 5);
        var n = reversed.Normalize(5, 5, 5);

        Assert.Equal(new Box3D(10, 0, 10, 0, 10, 0), reversed.DataBox);
        pt.AssertFinite();
        new[] { n.Nx, n.Ny, n.Nz }.AssertAllFinite();
    }

    /// <summary>A range set high to low draws its axis the other way round, as matplotlib's world transformation does with a
    /// negative scale: a value lands where the mirrored value of the forward range lands, and normalises to the mirrored
    /// place, so the browser turns it where it is drawn.</summary>
    [Theory]
    [InlineData(-60)]
    [InlineData(45)]
    [InlineData(150)]
    public void AReversedRange_DrawsTheAxisTheOtherWayRound(double azimuth)
    {
        var bounds = new Rect(0, 0, 400, 400);
        var forward = new Projection3D(30, azimuth, bounds, -1, 4, 0, 2, 0, 3);
        var reversed = new Projection3D(30, azimuth, bounds, 4, -1, 0, 2, 0, 3);

        foreach (double x in (double[])[-1, 0, 1.5, 4])
        {
            var drawn = reversed.Project(x, 1, 2);
            var mirrored = forward.Project(3 - x, 1, 2);
            Assert.Equal(mirrored.X, drawn.X, 9);
            Assert.Equal(mirrored.Y, drawn.Y, 9);
            Assert.Equal(forward.Normalize(3 - x, 1, 2).Nx, reversed.Normalize(x, 1, 2).Nx, 12);
        }
    }

    [Fact]
    public void AOnePointBox_ProjectsItsPointInsideThePlot()
    {
        var onePoint = new Projection3D(30, -60, new Rect(0, 0, 400, 400), 1, 1, 2, 2, 3, 3);

        var pt = onePoint.Project(1, 2, 3);

        Assert.InRange(pt.X, 0, 400);
        Assert.InRange(pt.Y, 0, 400);
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
