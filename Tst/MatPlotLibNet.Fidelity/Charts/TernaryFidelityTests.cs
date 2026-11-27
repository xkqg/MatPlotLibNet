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
    [FidelityRows]
    [FidelityTolerance(Rms = 19, Ssim = 0.91, DeltaE = 4, V2Rms = 20, V2Ssim = 0.92, V2DeltaE = 4)]
    public void TernaryScatter_MatchesMpltern(string themeId)
    {
        double[] top = [0.80, 0.55, 0.30, 0.15, 0.10, 0.40, 0.25, 0.60, 0.35, 0.50];
        double[] left = [0.10, 0.20, 0.50, 0.30, 0.15, 0.20, 0.35, 0.10, 0.45, 0.25];
        double[] right = [.. top.Select((t, i) => 1.0 - t - left[i])];
        AssertFidelity(themeId, "ternary_scatter", "ternary scatter", ax => ax
            .SetYLabel("Top")
            .SetZLabel("Left")
            .SetXLabel("Right")
            .TernaryScatter(top, left, right));
    }
}
