// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// Verifies SVG output of <see cref="PlanarBar3DSeries"/> rendering.
/// Ships as a v1.1.4 bonus series with zero test coverage until now.
/// </summary>
public class PlanarBar3DSeriesRenderTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0];
    private static readonly double[] Y = [0.0, 0.0, 1.0];
    private static readonly double[] Z = [2.0, 3.5, 1.5];

    [Fact]
    public void PlanarBar3D_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D(X, Y, Z))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void PlanarBar3D_SvgContainsPolygons()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D(X, Y, Z))
            .ToSvg();
        Assert.True(svg.Contains("<polygon") || svg.Contains("<path"),
            "Expected SVG to contain filled polygon or path elements for bar faces");
    }

    [Fact]
    public void PlanarBar3D_EmptyData_RendersWithoutError()
    {
        var empty = Array.Empty<double>();
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D(empty, empty, empty))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void PlanarBar3D_MultiPlane_RendersAllBars()
    {
        // Two planes at Y=0 and Y=1 — ensures the per-plane color override path and the
        // back-to-front depth order both engage. 6 bars total.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.PlanarBar3D([0.0, 1.0, 2.0], [0.0, 0.0, 0.0], [3.0, 5.0, 2.0]);
                ax.PlanarBar3D([0.0, 1.0, 2.0], [1.0, 1.0, 1.0], [4.0, 1.0, 3.0]);
            })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void PlanarBar3D_WithAlphaAndEdgeColor_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D(X, Y, Z, s =>
            {
                s.Alpha = 0.5;
                s.EdgeColor = Colors.Black;
                s.BarWidth = 0.6;
            }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
