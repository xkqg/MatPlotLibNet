// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>The ternary chart against mpltern (the matplotlib extension for ternary plots): ten compositions in the triangle, the
/// three corner labels, the grid and the ticks at fifths. The reference is made by <c>tools/mpl_reference/generate.py</c>
/// (<c>ternary_scatter</c>), with the top, left and right values given in the same order as <c>TernaryScatter</c> takes them.</summary>
public class TernaryFidelityTests : FidelityTest
{
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 40, Ssim = 0.74, DeltaE = 105)]   // measured on Windows, ubuntu noble and resolute (RMS 25.9 and 19.1, SSIM 0.84 and 0.92, dE 14 and 60 for classic and v2), plus a margin of 10 percent (dE from the v2 value over 1.7, RMS over 1.5, SSIM less 0.10)
    public void TernaryScatter_MatchesMpltern(string themeId)
    {
        double[] top = [0.80, 0.55, 0.30, 0.15, 0.10, 0.40, 0.25, 0.60, 0.35, 0.50];
        double[] left = [0.10, 0.20, 0.50, 0.30, 0.15, 0.20, 0.35, 0.10, 0.45, 0.25];
        double[] right = [.. top.Select((t, i) => 1.0 - t - left[i])];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("ternary scatter")
                .SetYLabel("Top")
                .SetZLabel("Left")
                .SetXLabel("Right")
                .TernaryScatter(top, left, right))
            .Build();
        AssertFidelity(figure, "ternary_scatter");
    }
}
