// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Polar-family fidelity tests (polar scatter, polar bar, polar heatmap).</summary>
public class PolarChartFidelityTests : FidelityTest
{
    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.50, DeltaE = 55)]   // polar grid labels + different RNG
    public void PolarScatter_RandomPoints_MatchesMatplotlib()
    {
        var rng = new Random(42);
        int n = 60;
        var theta = Enumerable.Range(0, n).Select(_ => rng.NextDouble() * 2 * Math.PI).ToArray();
        var r     = Enumerable.Range(0, n).Select(_ => rng.NextDouble()).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Polar scatter")
                .PolarScatter(r, theta))
            .Build();
        AssertFidelity(figure, "polar_scatter");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 85, Ssim = 0.55, DeltaE = 80)]   // our blue wedge fill is ~25% lighter; no dark saturated blue in our top-5
    public void PolarBar_Wedges_MatchesMatplotlib()
    {
        int n = 12;
        var theta = Enumerable.Range(0, n).Select(i => i * 2 * Math.PI / n).ToArray();
        double[] r = [3, 5, 2, 6, 4, 7, 3, 5, 4, 6, 3, 5];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Polar bar")
                .PolarBar(r, theta))
            .Build();
        AssertFidelity(figure, "polar_bar");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 85, Ssim = 0.50, DeltaE = 90)]   // different RNG → different cell colors; viridis extremes drift
    public void PolarHeatmap_Random_MatchesMatplotlib()
    {
        var rng = new Random(42);
        int nr = 10, ntheta = 24;
        var data = new double[nr, ntheta];
        for (int ir = 0; ir < nr; ir++)
            for (int it = 0; it < ntheta; it++)
                data[ir, it] = rng.NextDouble();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Polar heatmap")
                .PolarHeatmap(data, ntheta, nr, s => s.ColorMap = ColorMaps.Viridis))
            .Build();
        AssertFidelity(figure, "polar_heatmap");
    }
}
