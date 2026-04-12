// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Verifies interactive 3D rotation: flag, SVG output with script and data attributes.</summary>
public class InteractiveRotationTests
{
    [Fact]
    public void Figure_Enable3DRotation_DefaultIsFalse()
    {
        var figure = new Figure();
        Assert.False(figure.Enable3DRotation);
    }

    [Fact]
    public void SvgOutput_With3DRotation_ContainsScript()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var figure = Plt.Create()
            .Surface(x, y, z)
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("mpl-3d-scene", svg);
    }

    [Fact]
    public void SvgOutput_With3DRotation_PolygonsHaveDataV3d()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var figure = Plt.Create()
            .Surface(x, y, z)
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("data-v3d=", svg);
    }

    [Fact]
    public void FigureBuilder_With3DRotation_SetsFlag()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        Assert.True(figure.Enable3DRotation);
    }
}
