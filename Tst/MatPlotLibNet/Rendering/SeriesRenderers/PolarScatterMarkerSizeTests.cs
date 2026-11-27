// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A polar scatter's <c>MarkerSize</c> is in points, as its documentation says and as matplotlib states a marker's size,
/// and a marker is drawn that wide in pixels at 100 / 72 to the point. matplotlib 3.11, <c>Axes.scatter</c>: "s: The marker size
/// in points**2 ... Default is <c>rcParams['lines.markersize'] ** 2</c>" (36 for the 6 pt default), and
/// <c>MarkerStyle._set_circle</c> scales the unit circle by 0.5, so a circle marker is sqrt(s) = 6 points across: 8.33 px, a
/// radius of 4.17 px. Ours drew the 6 as a diameter in pixels, a radius of 3.</summary>
public class PolarScatterMarkerSizeTests
{
    private static List<double> MarkerRadii(double markerSize)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.PolarScatter([0.5, 1.0], [0.0, 1.0]).MarkerSize = markerSize;
        var ctx = new RecordingRenderContext();
        new PolarAxesRenderer(axes, new Rect(0, 0, 400, 400), ctx, Theme.MatplotlibV2).Render();
        return [.. ctx.Of<DrawCircleCall>()
            .Where(a => a.Fill == Theme.MatplotlibV2.CycleColors[0]).Select(a => a.Radius)];
    }

    [Theory]
    [InlineData(6.0)]
    [InlineData(10.0)]
    public void AMarker_IsItsSizeInPointsAcross(double size)
    {
        var radii = MarkerRadii(size);

        Assert.Equal(2, radii.Count);
        Assert.All(radii, r => Assert.Equal(size.PointsInPixels / 2, r, 9));
    }
}
