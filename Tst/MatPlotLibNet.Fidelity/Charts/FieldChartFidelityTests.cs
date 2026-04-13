// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Field-family fidelity tests (quiver, streamplot, barbs, stem).</summary>
public class FieldChartFidelityTests : FidelityTest
{
    private static double[] Linspace(double start, double end, int n)
    {
        var arr = new double[n];
        double step = (end - start) / (n - 1);
        for (int i = 0; i < n; i++) arr[i] = start + i * step;
        return arr;
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.45, DeltaE = 55)]   // arrow AA + default blue color mismatch
    public void Quiver_RotationalField_MatchesMatplotlib(string themeId)
    {
        var xs = Linspace(-2, 2, 10);
        var ys = Linspace(-2, 2, 10);
        int n = 10 * 10;
        var x = new double[n];
        var y = new double[n];
        var u = new double[n];
        var v = new double[n];
        for (int iy = 0; iy < 10; iy++)
            for (int ix = 0; ix < 10; ix++)
            {
                int k = iy * 10 + ix;
                x[k] = xs[ix];
                y[k] = ys[iy];
                u[k] = -ys[iy];
                v[k] = xs[ix];
            }
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Quiver — rotational field")
                .Quiver(x, y, u, v))
            .Build();
        AssertFidelity(figure, "quiver");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 100, Ssim = 0.30, DeltaE = 80)]   // v1.1.4: streamline paths + AA produce irreducibly low SSIM; classic xmargin=0 widened plot area → SSIM 0.35→0.30
    public void Streamplot_VectorField_MatchesMatplotlib(string themeId)
    {
        int n = 40;
        var x = Linspace(-3, 3, n);
        var y = Linspace(-3, 3, n);
        var u = new double[n, n];
        var v = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
            {
                u[iy, ix] = -1 - x[ix] * x[ix] + y[iy];
                v[iy, ix] = 1 + x[ix] - y[iy] * y[iy];
            }
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Streamplot")
                .Streamplot(x, y, u, v))
            .Build();
        AssertFidelity(figure, "streamplot");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.50, DeltaE = 55)]
    public void Barbs_WindField_MatchesMatplotlib(string themeId)
    {
        var xs = Linspace(-2, 2, 8);
        var ys = Linspace(-2, 2, 8);
        int n = 64;
        var x = new double[n];
        var y = new double[n];
        var speed = new double[n];
        var direction = new double[n];
        for (int iy = 0; iy < 8; iy++)
            for (int ix = 0; ix < 8; ix++)
            {
                int k = iy * 8 + ix;
                x[k] = xs[ix];
                y[k] = ys[iy];
                double u = Math.Sin(xs[ix]) * 20;
                double vv = Math.Cos(ys[iy]) * 20;
                speed[k] = Math.Sqrt(u * u + vv * vv);
                direction[k] = Math.Atan2(vv, u) * 180.0 / Math.PI;
            }
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Barbs — wind field")
                .Barbs(x, y, speed, direction))
            .Build();
        AssertFidelity(figure, "barbs");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void Stem_Sine_MatchesMatplotlib(string themeId)
    {
        int n = 20;
        var x = Linspace(0, 2 * Math.PI, n);
        var y = x.Select(Math.Sin).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Stem — sin(x)")
                .Stem(x, y))
            .Build();
        AssertFidelity(figure, "stem");
    }
}
