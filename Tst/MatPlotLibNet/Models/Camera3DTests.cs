// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The camera a 3-D axes sees its scene from (<see cref="Camera3D"/>): where it stands at rest, how a turn, a restore and a
/// projection that was handed in move it, and the projection it builds for a plot.</summary>
public sealed class Camera3DTests
{
    /// <summary>matplotlib 3.11.2, <c>Axes3D.__init__(elev=30, azim=-60, ...)</c> and <c>self._dist = 10</c>
    /// (read with python: <c>ax.elev, ax.azim, ax._dist</c> gives <c>30 -60 10</c>).</summary>
    [Fact]
    public void TheRestingCamera_IsMatplotlibsDefaultView()
    {
        Assert.Equal(30, Camera3D.RestingElevation);
        Assert.Equal(-60, Camera3D.RestingAzimuth);
        Assert.Equal(10, Camera3D.RestingDistance);
        Assert.Equal(new CameraView(30, -60, null), CameraView.Resting);
    }

    [Fact]
    public void ANewAxes_LooksFromTheRestingCamera()
    {
        var axes = new Axes();

        Assert.Equal(CameraView.Resting, axes.Camera.View);
        Assert.Null(axes.Camera.Projection);
        Assert.Null(axes.Camera.Light);
    }

    /// <summary>Most axes are flat: reading the camera members of one, or its change mark, makes no camera (a new axes allocates
    /// nothing for a feature it does not use); the first setting does.</summary>
    [Fact]
    public void ReadingTheCameraOfAnAxes_MakesNoCamera_UntilOneIsSet()
    {
        var axes = new Axes();
        var field = typeof(Axes).GetField("_camera", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        _ = (axes.Elevation, axes.Azimuth, axes.CameraDistance, axes.Projection, axes.LightSource, axes.CameraView, axes.LatestChange);
        Assert.Null(field.GetValue(axes));

        axes.Elevation = 12;
        Assert.NotNull(field.GetValue(axes));
    }

    [Fact]
    public void TheAxesProperties_AreTheCamerasOwn()
    {
        var light = new DirectionalLight(1, 0, 1);
        var projection = new Projection3D(10, 20, new Rect(0, 0, 100, 100), 0, 1, 0, 1, 0, 1);
        var axes = new Axes { Elevation = 12, Azimuth = 34, CameraDistance = 5, LightSource = light, Projection = projection };

        Assert.Equal(new CameraView(12, 34, 5), axes.Camera.View);
        Assert.Same(light, axes.Camera.Light);
        Assert.Same(projection, axes.Camera.Projection);
    }

    [Theory]
    [InlineData(30, -60, 15, 20, 45, -40)]
    [InlineData(80, 0, 30, 25, 90, 25)]
    [InlineData(-80, 0, -30, -5, -90, -5)]
    public void ATurn_MovesTheAzimuth_AndHoldsTheElevationBetweenItsPoles(double elevation, double azimuth, double turnElevation,
        double turnAzimuth, double expectedElevation, double expectedAzimuth)
    {
        var axes = new Axes { Elevation = elevation, Azimuth = azimuth };

        axes.Camera.Turn(turnAzimuth, turnElevation);

        Assert.Equal(expectedElevation, axes.Elevation);
        Assert.Equal(expectedAzimuth, axes.Azimuth);
    }

    [Fact]
    public void ATurn_DropsTheProjectionThatWasHandedIn()
    {
        var axes = new Axes { Projection = new Projection3D(10, 20, new Rect(0, 0, 100, 100), 0, 1, 0, 1, 0, 1) };

        axes.Camera.Turn(5, 0);

        Assert.Null(axes.Projection);
    }

    [Fact]
    public void LookingFromAView_PutsTheCameraThere_AndDropsTheProjection()
    {
        var axes = new Axes { Projection = new Projection3D(10, 20, new Rect(0, 0, 100, 100), 0, 1, 0, 1, 0, 1) };

        axes.Camera.LookFrom(new CameraView(5, 6, 7));

        Assert.Equal(new CameraView(5, 6, 7), axes.Camera.View);
        Assert.Null(axes.Projection);
    }

    [Fact]
    public void TheDrawnView_TakesTheAnglesOfAProjectionHandedIn_AndTheDistanceOfTheCamera()
    {
        var axes = new Axes { Elevation = 1, Azimuth = 2, CameraDistance = 9 };
        Assert.Equal(new CameraView(1, 2, 9), axes.Camera.DrawnView);

        axes.Projection = new Projection3D(10, 20, new Rect(0, 0, 100, 100), 0, 1, 0, 1, 0, 1, distance: 4);

        Assert.Equal(new CameraView(10, 20, 9), axes.Camera.DrawnView);
    }

    [Fact]
    public void AView_BuildsTheProjectionOfItsAngles_AndItsDistance_OverAPlotAndABox()
    {
        var plot = new Rect(10, 20, 300, 200);
        var box = new Box3D(-1, 2, 0, 5, 3, 4);

        var projection = new CameraView(25, -70, 6).ProjectionOver(plot, box);
        var expected = new Projection3D(25, -70, plot, -1, 2, 0, 5, 3, 4, 6);

        Assert.Equal(25, projection.Elevation);
        Assert.Equal(-70, projection.Azimuth);
        Assert.Equal(6, projection.Distance);
        Assert.Equal(expected.Project(1, 2, 3.5), projection.Project(1, 2, 3.5));
    }

    [Fact]
    public void AChangeOfTheCamera_MarksItsAxesChanged()
    {
        var axes = new Axes();
        long before = axes.LatestChange;

        axes.Camera.Turn(1, 1);

        Assert.True(axes.LatestChange > before);
    }
}
