// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

public sealed class ThreeDColorBarTests
{
    [Fact]
    public void SurfaceWithColorMap_AndColorBar_ProducesGradientInSvg()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } },
                    s => s.ColorMap = ColorMaps.Viridis)
                .WithColorBar())
            .Build()
            .ToSvg();

        // Should contain colorbar gradient elements
        Assert.Contains("colorbar", svg.ToLowerInvariant());
    }

    [Fact]
    public void SurfaceWithoutColorBar_NoGradient()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } }))
            .Build()
            .ToSvg();

        Assert.DoesNotContain("colorbar", svg.ToLowerInvariant());
    }

    [Fact]
    public void SurfaceWithColorBar_LabelRendered()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } },
                    s => s.ColorMap = ColorMaps.Plasma)
                .WithColorBar(cb => cb with { Label = "Intensity" }))
            .Build()
            .ToSvg();

        Assert.Contains("Intensity", svg);
    }
}
