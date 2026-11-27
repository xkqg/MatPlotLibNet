// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The view of a 3-D axes: the projection sets the angles it is given (the camera is <c>CameraBuilderTests</c>').</summary>
public class AxesBuilderThreeDVerbsTests
{
    [Fact]
    public void WithProjection_SetsTheCameraAngles()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Scatter3D([1.0, 2], [3.0, 4], [5.0, 6]).WithProjection(elevation: 45, azimuth: -90))
            .Build().SubPlots[0];

        Assert.Equal(45.0, axes.Elevation);
        Assert.Equal(-90.0, axes.Azimuth);
    }
}
