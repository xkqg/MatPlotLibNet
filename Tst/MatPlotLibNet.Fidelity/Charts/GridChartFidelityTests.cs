// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests;

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

    [Theory]
    [FidelityRows]
    // The lines stand on matplotlib's levels (levels=10 of the peaks surface, -0.8 to 1.2 in steps of 0.2), in the colours the theme's
    // map gives their levels; the labels' text is antialiased.
    [FidelityTolerance(Rms = 13, Ssim = 0.97, DeltaE = 3, V2Rms = 15)]
    public void ContourLines_Peaks_MatchesMatplotlib(string themeId)
    {
        int n = 100;
        var (x, y, z) = SampleSurface.PeaksOver(Linspace(-3, 3, n), Linspace(-3, 3, n));
        AssertFidelity(themeId, "contour_lines", "Contour lines — peaks function", ax => ax
            .Contour(x, y, z, s =>
            {
                // the reference's ax.clabel(cs, inline=True, fontsize=8): 8 points, 11.11 px at the 100 dpi it is drawn at
                s.ShowLabels = true;
                s.LabelFontSize = 8 * 100.0 / 72;
            }));
    }

    [Theory]
    [FidelityRows]
    // The reference's own points, its colour bar and its grid (every hexagon, MinCount 0, laid over the extent of the data); the
    // hexbin asks its axes for a tight range as matplotlib's does, so the hexagons, their colours and the colour bar's range are
    // matplotlib's, and what is left is the antialiasing of the hexagons' edges.
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3, V2Rms = 14)]
    public void Hexbin_Gaussian_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("hexbin");
        var x = points.Vector("x");
        var y = points.Vector("y");
        AssertFidelity(themeId, "hexbin", "Hexbin — 2000 samples", ax => ax
            .Hexbin(x, y, s => { s.ColorMap = ColorMaps.Viridis; s.MinCount = 0; })   // every hexagon of the grid, as with no mincnt
            .WithColorBar());   // the reference's fig.colorbar(hb)
    }

    [Theory]
    [FidelityRows]
    // The reference's points and colour bar (which takes its room from the axes as fig.colorbar's does): the cells are matplotlib's,
    // on the pixel grid as Agg snaps a QuadMesh, so no seam runs between them. Measured on Windows and in the noble container: classic
    // RMS 9.11 / 8.88, SSIM 0.9955 / 0.9952; v2 RMS 11.35 / 10.74, SSIM 0.9955 / 0.9954; colour distance 0.59 on both.
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 2, V2Rms = 13, V2Ssim = 0.98, V2DeltaE = 2)]
    public void Histogram2D_Gaussian_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("hist2d");
        var x = points.Vector("x");
        var y = points.Vector("y");
        AssertFidelity(themeId, "hist2d", "2D histogram", ax => ax
            .Histogram2D(x, y, 30, s => s.ColorMap = ColorMaps.Viridis)
            .WithColorBar());   // the reference's fig.colorbar(h[3])
    }

    [Theory]
    [FidelityRows]
    // The reference's mesh: pcolormesh(X, Y, Z) with X, Y and Z of one shape takes shading 'auto' -> 'nearest', a cell centred on
    // each of the 30 x 30 points, its edges half a step either side, so the mesh runs from -5/29 to 10 + 5/29. The colour bar
    // takes its room from the axes as fig.colorbar's does; the cells stand on the pixel grid as Agg snaps a QuadMesh. Measured on Windows
    // and in the noble container: classic RMS 8.24 / 7.63, SSIM 0.9966 / 0.9966; v2 RMS 13.69 / 12.90, SSIM 0.9947 / 0.9946; colour
    // distance 2.14 on both.
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 4, V2Rms = 15, V2Ssim = 0.98, V2DeltaE = 4)]
    public void Pcolormesh_SinCos_MatchesMatplotlib(string themeId)
    {
        int n = 30;
        var centres = Linspace(0, 10, n);
        double half = 10.0 / (n - 1) / 2;
        double[] edges = [.. Enumerable.Range(0, n + 1).Select(i => -half + i * 2 * half)];
        var z = new double[n, n];
        for (int iy = 0; iy < n; iy++)
            for (int ix = 0; ix < n; ix++)
                z[iy, ix] = Math.Sin(centres[ix]) * Math.Cos(centres[iy]);
        var x = edges;
        var y = edges;
        var theme = themeId.AsTheme();
        var figure = FidelityFigure(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Pcolormesh — sin(x)·cos(y)")
                .Pcolormesh(x, y, z, s => s.ColorMap = ColorMaps.Viridis)
                .WithColorBar())   // the reference's fig.colorbar(pc)
            .Build();
        AssertFidelity(figure, "pcolormesh");
    }

    [Theory]
    [FidelityRows]
    // Random noise. Measured on Windows and in the noble container, with the image on imshow's extent: classic RMS 13.33 / 13.16, SSIM
    // 0.9858 / 0.9859, colour distance 0 on both; v2 RMS 16.16 / 15.67, SSIM 0.9856 / 0.9853, colour distance 0 on both.
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 1, V2Rms = 13, V2Ssim = 0.98, V2DeltaE = 1)]
    public void Image_RandomValues_MatchesMatplotlib(string themeId)
    {
        double[,] data = ReferenceData.Load("image").Matrix("data");
        var figure = FidelityFigure(themeId)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Image 30×40 (viridis)")
                .Image(data, s => { s.ColorMap = ColorMaps.Viridis; s.Interpolation = "nearest"; })   // the reference pins nearest
                .SetAspectAuto())   // and aspect="auto": the cells fill the axes
            .Build();
        AssertFidelity(figure, "image");
    }

    [Theory]
    [FidelityRows]
    // No interpolation named, so the style's image.interpolation scales the wave: at 2.8 times its size across and 2.7 down, the
    // default style's 'auto' is Hanning and the classic style's is 'bilinear'. imshow stands the cells on their numbers, -0.5 to
    // 219.5 across and 169.5 down to -0.5, which the library does under the matplotlib themes. Shown under three times its size, the
    // wave's colours are interpolated (matplotlib's 'auto' interpolation stage), and the cells are one fill of the raster, so no seam runs
    // between them. Measured on Windows: classic RMS 12.53, SSIM 0.9902, colour distance 0.73; v2 RMS 16.22, SSIM 0.9912, colour distance
    // 0.73 (with the seams: 24.58 and 26.60).
    [FidelityTolerance(Rms = 14, Ssim = 0.98, DeltaE = 1, V2Rms = 18, V2Ssim = 0.98, V2DeltaE = 2)]
    public void Image_InterpolationOfTheStyle_MatchesMatplotlib(string themeId)
    {
        var data = new double[170, 220];
        for (int row = 0; row < 170; row++)
            for (int column = 0; column < 220; column++)
                data[row, column] = Math.Sin(column / 14.0) * Math.Cos(row / 11.0);   // the reference's np.sin(cols / 14) * np.cos(rows / 11)
        var figure = FidelityFigure(themeId)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Image 170×220, the style's interpolation")
                .Image(data, s => s.ColorMap = ColorMaps.Viridis)   // the reference names cmap="viridis" and no interpolation
                .SetAspectAuto())   // and aspect="auto": the cells fill the axes
            .Build();
        AssertFidelity(figure, "image_style");
    }

    [Theory]
    [FidelityRows]
    // The reference is matplotlib's specgram, which the library draws under the matplotlib themes: the decibels of the one-sided power
    // density of each window, an image on (first window's centre - half a step, last + half a step) by (0, 500) Hz with sticky edges,
    // scaled by the style's interpolation (classic bilinear), the view rounded out to ticks under the classic style. Measured on Windows
    // and in the noble container: classic RMS 9.95 / 9.57, SSIM 0.9646 / 0.9646; v2 RMS 10.27 / 9.55, SSIM 0.9967 / 0.9966; colour distance
    // 0 on both.
    [FidelityTolerance(Rms = 11, Ssim = 0.95, DeltaE = 1, V2Rms = 12, V2Ssim = 0.98, V2DeltaE = 1)]
    public void Spectrogram_Sine50Hz_MatchesMatplotlib(string themeId)
    {
        int fs = 1000;
        var signal = ReferenceData.Load("spectrogram").Vector("signal");
        var figure = FidelityFigure(themeId)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Spectrogram — 50 Hz + noise")
                .Spectrogram(signal, fs, s => s.ColorMap = ColorMaps.Viridis))   // the reference names cmap="viridis" and nothing else
            .Build();
        AssertFidelity(figure, "spectrogram");
    }

    [Theory]
    [FidelityRows]
    // The lines stand on matplotlib's levels (levels=10, 0 to 1 in steps of 0.1), as wide as matplotlib's lines.linewidth and
    // coloured through the theme's map, in the frame matplotlib draws, the extent of the points.
    [FidelityTolerance(Rms = 9, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2DeltaE = 4)]
    public void Tricontour_GaussianBump_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("tricontour");
        var x = points.Vector("x");
        var y = points.Vector("y");
        var z = points.Vector("z");
        AssertFidelity(themeId, "tricontour", "Tricontour — gaussian bump", ax => ax
            .Tricontour(x, y, z));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 16, Ssim = 0.92, DeltaE = 3, V2Rms = 18, V2DeltaE = 4)]   // the reference's colour bar, which takes its room from the axes as fig.colorbar's does
    public void Tripcolor_GaussianBump_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("tripcolor");
        var x = points.Vector("x");
        var y = points.Vector("y");
        var z = points.Vector("z");
        var theme = themeId.AsTheme();
        var figure = FidelityFigure(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Tripcolor — gaussian bump")
                .Tripcolor(x, y, z, s => s.ColorMap = ColorMaps.Viridis)
                .WithColorBar())
            .Build();
        AssertFidelity(figure, "tripcolor");
    }
}
