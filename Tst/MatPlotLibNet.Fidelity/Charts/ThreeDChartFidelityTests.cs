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

    private static double NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.45, DeltaE = 80)]   // camera projection + different RNG
    public void Scatter3D_Random_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        int n = 60;
        var x = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var y = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var z = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Scatter 3D")
                .Scatter3D(x, y, z)
                .WithCamera(elevation: 30, azimuth: -60))
            .Build();
        AssertFidelity(figure, "scatter3d");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 120, Ssim = 0.35, DeltaE = 90)]   // 3D bar face shading + projection angle differences
    public void Bar3D_Grid_MatchesMatplotlib(string themeId)
    {
        int n = 16;
        var x = new double[n];
        var y = new double[n];
        var z = new double[n];
        for (int iy = 0; iy < 4; iy++)
            for (int ix = 0; ix < 4; ix++)
            {
                int k = iy * 4 + ix;
                x[k] = ix;
                y[k] = iy;
                z[k] = k + 1;
            }
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Bar 3D")
                .Bar3D(x, y, z)
                .WithCamera(elevation: 30, azimuth: -60))
            .Build();
        AssertFidelity(figure, "bar3d");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 100, Ssim = 0.40, DeltaE = 90)]   // surface colormap + shading
    public void Surface_SinR_MatchesMatplotlib(string themeId)
    {
        int n = 40;
        var x = Linspace(-3, 3, n);
        var y = Linspace(-3, 3, n);
        var z = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
                z[iy, ix] = Math.Sin(Math.Sqrt(x[ix] * x[ix] + y[iy] * y[iy]));
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Surface — sin(r)")
                .Surface(x, y, z, s => s.ColorMap = ColorMaps.Viridis)
                .WithCamera(elevation: 30, azimuth: -60))
            .Build();
        AssertFidelity(figure, "surface");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 100, Ssim = 0.40, DeltaE = 80)]
    public void Wireframe_SinR_MatchesMatplotlib(string themeId)
    {
        int n = 20;
        var x = Linspace(-3, 3, n);
        var y = Linspace(-3, 3, n);
        var z = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
                z[iy, ix] = Math.Sin(Math.Sqrt(x[ix] * x[ix] + y[iy] * y[iy]));
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Wireframe — sin(r)")
                .Wireframe(x, y, z)
                .WithCamera(elevation: 30, azimuth: -60))
            .Build();
        AssertFidelity(figure, "wireframe");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.45, DeltaE = 80)]
    public void Stem3D_Spiral_MatchesMatplotlib(string themeId)
    {
        int n = 20;
        var theta = Linspace(0, 2 * Math.PI, n);
        var x = theta.Select(Math.Cos).ToArray();
        var y = theta.Select(Math.Sin).ToArray();
        var z = theta;
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Stem 3D — spiral")
                .Stem3D(x, y, z)
                .WithCamera(elevation: 30, azimuth: -60))
            .Build();
        AssertFidelity(figure, "stem3d");
    }
}
