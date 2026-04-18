// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

public sealed class DepthResortTests
{
    [Fact]
    public void RotationScript_ContainsResortDepth()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Bar3D([0.0, 1.0], [0.0, 1.0], [3.0, 5.0],
                    s => s.Color = Colors.Tomato))
            .Build()
            .ToSvg();

        Assert.Contains("resortDepth", svg);
    }

    [Fact]
    public void RotationScript_ContainsAvgViewZ()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } }))
            .Build()
            .ToSvg();

        Assert.Contains("avgViewZ", svg);
    }

    [Fact]
    public void RotationScript_CallsResortAfterReproject()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .With3DRotation()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Scatter3D([0.0], [0.0], [0.0]))
            .Build()
            .ToSvg();

        // resortDepth(b) is called inside reprojectAll() body — the basis arg `b` was
        // added in Phase B.4 of v1.7.2 follow-on (full matplotlib projection port; viewZ
        // now needs the camera basis to compute depth).
        Assert.Contains("resortDepth(b);", svg); // the call inside reprojectAll
        Assert.Contains("function resortDepth(b)", svg); // the function definition
    }
}
