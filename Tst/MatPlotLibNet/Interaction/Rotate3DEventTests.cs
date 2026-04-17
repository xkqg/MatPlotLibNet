// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Interaction;

public sealed class Rotate3DEventTests
{
    private static Figure CreateFigureWith3DAxes()
    {
        var figure = new Figure { ChartId = "test" };
        var axes = figure.AddSubPlot();
        axes.CoordinateSystem = CoordinateSystem.ThreeD;
        axes.Elevation = 30;
        axes.Azimuth = -60;
        return figure;
    }

    [Fact]
    public void ApplyTo_MutatesAzimuth()
    {
        var figure = CreateFigureWith3DAxes();
        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 15.0, DeltaElevation: 0);
        evt.ApplyTo(figure);
        Assert.Equal(-45.0, figure.SubPlots[0].Azimuth);
    }

    [Fact]
    public void ApplyTo_MutatesElevation()
    {
        var figure = CreateFigureWith3DAxes();
        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 0, DeltaElevation: 10.0);
        evt.ApplyTo(figure);
        Assert.Equal(40.0, figure.SubPlots[0].Elevation);
    }

    [Fact]
    public void ApplyTo_ClampsElevationToPositive90()
    {
        var figure = CreateFigureWith3DAxes();
        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 0, DeltaElevation: 100.0);
        evt.ApplyTo(figure);
        Assert.Equal(90.0, figure.SubPlots[0].Elevation);
    }

    [Fact]
    public void ApplyTo_ClampsElevationToNegative90()
    {
        var figure = CreateFigureWith3DAxes();
        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 0, DeltaElevation: -200.0);
        evt.ApplyTo(figure);
        Assert.Equal(-90.0, figure.SubPlots[0].Elevation);
    }

    [Fact]
    public void ApplyTo_NullsProjectionToForceRebuild()
    {
        var figure = CreateFigureWith3DAxes();
        figure.SubPlots[0].Projection = new MatPlotLibNet.Rendering.Projection3D(
            30, -60, new MatPlotLibNet.Rendering.Rect(0, 0, 400, 400),
            -1, 1, -1, 1, -1, 1);
        Assert.NotNull(figure.SubPlots[0].Projection);

        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 5, DeltaElevation: 5);
        evt.ApplyTo(figure);
        Assert.Null(figure.SubPlots[0].Projection);
    }

    [Fact]
    public void ApplyTo_AzimuthWrapsAround()
    {
        var figure = CreateFigureWith3DAxes();
        figure.SubPlots[0].Azimuth = 350;
        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 20, DeltaElevation: 0);
        evt.ApplyTo(figure);
        Assert.Equal(370.0, figure.SubPlots[0].Azimuth); // no clamping on azimuth
    }

    [Fact]
    public void ApplyTo_BothDeltasApplied()
    {
        var figure = CreateFigureWith3DAxes();
        var evt = new Rotate3DEvent("test", 0, DeltaAzimuth: 10, DeltaElevation: -5);
        evt.ApplyTo(figure);
        Assert.Equal(-50.0, figure.SubPlots[0].Azimuth);
        Assert.Equal(25.0, figure.SubPlots[0].Elevation);
    }
}
