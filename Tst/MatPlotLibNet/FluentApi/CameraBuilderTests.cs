// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies WithCamera builder methods on AxesBuilder and FigureBuilder.</summary>
public class CameraBuilderTests
{
    [Fact]
    public void AxesBuilder_WithCamera_SetsAllThreeProperties()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Surface(x, y, z)
                .WithCamera(elevation: 45, azimuth: -30, distance: 8.0))
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal(45, axes.Elevation);
        Assert.Equal(-30, axes.Azimuth);
        Assert.Equal(8.0, axes.CameraDistance);
    }

    [Fact]
    public void FigureBuilder_WithCamera_SetsOnDefaultAxes()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var figure = Plt.Create()
            .Surface(x, y, z)
            .WithCamera(elevation: 60, azimuth: 45)
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal(60, axes.Elevation);
        Assert.Equal(45, axes.Azimuth);
        Assert.Null(axes.CameraDistance);
    }
}
