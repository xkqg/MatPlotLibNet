// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Grid-family fidelity tests (contour, hexbin, hist2d, pcolormesh, image, spectrogram, tricontour, tripcolor).</summary>
public class GridChartFidelityTests : FidelityTest
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
    [FidelityTolerance(Rms = 90, Ssim = 0.55, DeltaE = 55)]   // contour labels + AA text
    public void ContourLines_Peaks_MatchesMatplotlib(string themeId)
    {
        int n = 100;
        var x = Linspace(-3, 3, n);
        var y = Linspace(-3, 3, n);
        var z = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
                z[iy, ix] = (1 - x[ix] / 2 + Math.Pow(x[ix], 5) + Math.Pow(y[iy], 3))
                             * Math.Exp(-x[ix] * x[ix] - y[iy] * y[iy]);
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Contour lines — peaks function")
                .Contour(x, y, z))
            .Build();
        AssertFidelity(figure, "contour_lines");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 110, Ssim = 0.35, DeltaE = 55)]   // different RNG → different cell occupancy; sparse cells lower SSIM
    public void Hexbin_Gaussian_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        int n = 2000;
        var x = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var y = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Hexbin — 2000 samples")
                .Hexbin(x, y, s => s.ColorMap = ColorMaps.Viridis))
            .Build();
        AssertFidelity(figure, "hexbin");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.50, DeltaE = 55)]   // different RNG
    public void Histogram2D_Gaussian_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        int n = 5000;
        var x = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var y = Enumerable.Range(0, n).Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("2D histogram")
                .Histogram2D(x, y, 30, s => s.ColorMap = ColorMaps.Viridis))
            .Build();
        AssertFidelity(figure, "hist2d");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 75, Ssim = 0.40, DeltaE = 55)]   // half-cell spatial offset vs matplotlib (corner vs center) lowers SSIM; ΔE=17 confirms colormap is correct
    public void Pcolormesh_SinCos_MatchesMatplotlib(string themeId)
    {
        int n = 30;
        var x = Linspace(0, 10, n);
        var y = Linspace(0, 10, n);
        // Our renderer uses corner convention (X,Y size n, C size n-1 × n-1);
        // matplotlib auto-drops the last row/col when sizes match, giving the same output.
        var z = new double[n - 1, n - 1];
        for (int iy = 0; iy < n - 1; iy++)
            for (int ix = 0; ix < n - 1; ix++)
                z[iy, ix] = Math.Sin(x[ix]) * Math.Cos(y[iy]);
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Pcolormesh — sin(x)·cos(y)")
                .Pcolormesh(x, y, z, s => s.ColorMap = ColorMaps.Viridis))
            .Build();
        AssertFidelity(figure, "pcolormesh");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.30, DeltaE = 60)]   // random noise: SSIM is inherently low (no structure)
    public void Image_RandomRgb_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        var data = new double[30, 40];
        for (int r = 0; r < 30; r++)
            for (int c = 0; c < 40; c++)
                data[r, c] = rng.NextDouble();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("RGB image")
                .Image(data))
            .Build();
        AssertFidelity(figure, "image");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 100, Ssim = 0.40, DeltaE = 80)]   // FFT-of-random-noise differs by RNG
    public void Spectrogram_Sine50Hz_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        int fs = 1000;
        int n = 2000;
        var signal = new double[n];
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)fs;
            signal[i] = Math.Sin(2 * Math.PI * 50 * t) + 0.5 * NextGaussian(rng);
        }
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Spectrogram — 50 Hz + noise")
                .Spectrogram(signal, fs))
            .Build();
        AssertFidelity(figure, "spectrogram");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.50, DeltaE = 55)]   // different RNG scatter positions
    public void Tricontour_GaussianBump_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        int n = 200;
        var x = Enumerable.Range(0, n).Select(_ => (rng.NextDouble() * 6 - 3)).ToArray();
        var y = Enumerable.Range(0, n).Select(_ => (rng.NextDouble() * 6 - 3)).ToArray();
        var z = x.Zip(y, (xi, yi) => Math.Exp(-(xi * xi + yi * yi) / 2)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Tricontour — gaussian bump")
                .Tricontour(x, y, z))
            .Build();
        AssertFidelity(figure, "tricontour");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.50, DeltaE = 55)]   // different RNG
    public void Tripcolor_GaussianBump_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        int n = 200;
        var x = Enumerable.Range(0, n).Select(_ => (rng.NextDouble() * 6 - 3)).ToArray();
        var y = Enumerable.Range(0, n).Select(_ => (rng.NextDouble() * 6 - 3)).ToArray();
        var z = x.Zip(y, (xi, yi) => Math.Exp(-(xi * xi + yi * yi) / 2)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Tripcolor — gaussian bump")
                .Tripcolor(x, y, z, s => s.ColorMap = ColorMaps.Viridis))
            .Build();
        AssertFidelity(figure, "tripcolor");
    }
}
