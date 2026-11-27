// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>
/// Phase 5 3D-family fidelity tests (scatter3d, bar3d, surface, wireframe, stem3d).
/// Expect wider tolerances — camera projection / depth sort / face shading diverge from matplotlib.
/// </summary>
public class ThreeDChartFidelityTests : FidelityTest
{
    private static double[] Linspace(double start, double end, int n)
    {
        var arr = new double[n];
        double step = (end - start) / (n - 1);
        for (int i = 0; i < n; i++) arr[i] = start + i * step;
        return arr;
    }


    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 19, Ssim = 0.93, DeltaE = 4, V2Rms = 17, V2Ssim = 0.94, V2DeltaE = 3)]   // camera projection
    public void Scatter3D_Random_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("scatter3d");
        double[] x = points.Vector("x");
        double[] y = points.Vector("y");
        double[] z = points.Vector("z");
        AssertFidelity(themeId, "scatter3d", "Scatter 3D", ax => ax
            .Scatter3D(x, y, z)
            .WithCamera(elevation: 30, azimuth: -60));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 17, Ssim = 0.94, DeltaE = 2, V2Rms = 19, V2Ssim = 0.93, V2DeltaE = 3)]   // 3D bar face shading + projection angle differences
    public void Bar3D_Grid_MatchesMatplotlib(string themeId)
    {
        var (x, y) = Meshgrid([0.0, 1.0, 2.0, 3.0], [0.0, 1.0, 2.0, 3.0]);
        double[] z = [.. Enumerable.Range(1, 16).Select(k => (double)k)];
        AssertFidelity(themeId, "bar3d", "Bar 3D", ax => ax
            .Bar3D(x, y, z, s => s.BarWidth = 0.8)   // the reference's bars: ax.bar3d(x, y, 0, 0.8, 0.8, dz)
            .WithCamera(elevation: 30, azimuth: -60));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 19, Ssim = 0.92, DeltaE = 3, V2Rms = 16, V2Ssim = 0.94)]   // surface colormap + shading
    public void Surface_SinR_MatchesMatplotlib(string themeId)
    {
        int n = 40;
        var (x, y, z) = SampleSurface.SinR(Linspace(-3, 3, n), Linspace(-3, 3, n));
        AssertFidelity(themeId, "surface", "Surface — sin(r)", ax => ax
            .Surface(x, y, z, s => s.ColorMap = ColorMaps.Viridis)
            .WithCamera(elevation: 30, azimuth: -60));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 19, Ssim = 0.92, DeltaE = 2, V2Rms = 17, V2Ssim = 0.94, V2DeltaE = 3)]
    public void Wireframe_SinR_MatchesMatplotlib(string themeId)
    {
        int n = 20;
        var (x, y, z) = SampleSurface.SinR(Linspace(-3, 3, n), Linspace(-3, 3, n));
        AssertFidelity(themeId, "wireframe", "Wireframe — sin(r)", ax => ax
            .Wireframe(x, y, z)
            .WithCamera(elevation: 30, azimuth: -60));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 23, Ssim = 0.90, DeltaE = 3, V2Rms = 22, V2Ssim = 0.93)]   // the reference's stems are thin lines and the line through their feet thin and cyan (classic); the library draws them 2.5 px wide, the line through the feet as blue as the stems
    public void Stem3D_Spiral_MatchesMatplotlib(string themeId)
    {
        int n = 20;
        var theta = Linspace(0, 2 * Math.PI, n);
        var x = theta.Select(Math.Cos).ToArray();
        var y = theta.Select(Math.Sin).ToArray();
        var z = theta;
        AssertFidelity(themeId, "stem3d", "Stem 3D — spiral", ax => ax
            .Stem3D(x, y, z)
            .WithCamera(elevation: 30, azimuth: -60));
    }
}
