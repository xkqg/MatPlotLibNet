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
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 15, Ssim = 0.97, DeltaE = 3, V2Rms = 12, V2Ssim = 0.98)]   // polar grid labels
    public void PolarScatter_RandomPoints_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("polar_scatter");
        var theta = points.Vector("theta");
        var r     = points.Vector("r");
        AssertFidelity(themeId, "polar_scatter", "Polar scatter", ax => ax
            .PolarScatter(r, theta));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 5, V2Rms = 8, V2Ssim = 0.98, V2DeltaE = 3)]
    public void PolarBar_Wedges_MatchesMatplotlib(string themeId)
    {
        int n = 12;
        var theta = Enumerable.Range(0, n).Select(i => i * 2 * Math.PI / n).ToArray();
        double[] r = [3, 5, 2, 6, 4, 7, 3, 5, 4, 6, 3, 5];
        AssertFidelity(themeId, "polar_bar", "Polar bar", ax => ax
            // The reference's bars are 2 pi / 12 wide: ax.bar(theta, r, width=width).
            .PolarBar(r, theta, b => b.BarWidth = 2 * Math.PI / n));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 16, Ssim = 0.96, DeltaE = 3, V2Rms = 15, V2Ssim = 0.97)]   // the cells, the spoke labels and the title are the reference's (classic RMS 15.73, v2 16.23); what remains is the antialiased text and the outline
    public void PolarHeatmap_Random_MatchesMatplotlib(string themeId)
    {
        // The reference's Z is indexed [radial bin, angular bin] (pcolormesh(T, R, Z) over a meshgrid of 24 angles and 10 radii); the series
        // reads [angular bin, radial bin], so the matrix is turned: with it as it stood, the cells of the first ten angles held the data
        // and the other fourteen angles of the disc stayed empty.
        double[,] z = ReferenceData.Load("polar_heatmap").Matrix("data");
        int nr = z.GetLength(0), ntheta = z.GetLength(1);
        var data = new double[ntheta, nr];
        for (int t = 0; t < ntheta; t++)
        for (int r = 0; r < nr; r++)
            data[t, r] = z[r, t];
        AssertFidelity(themeId, "polar_heatmap", "Polar heatmap", ax => ax
            .PolarHeatmap(data, ntheta, nr, s => s.ColorMap = ColorMaps.Viridis));
    }
}
