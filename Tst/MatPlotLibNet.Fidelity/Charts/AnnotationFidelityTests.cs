// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Annotation text size: matplotlib draws an annotation at the size of its tick labels.</summary>
public class AnnotationFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3)]   // AA grey text vs matplotlib black
    public void Annotate_WithoutAFont_MatchesMatplotlib(string themeId)
    {
        double[] x = Enumerable.Range(0, 50).Select(i => i * 10.0 / 49).ToArray();
        AssertFidelity(themeId, "annotate_default", "Annotation text size", ax => ax
            .Plot(x, x)
            .Annotate("peak", 5, 5)
            .Annotate("start", 1, 1)
            .SetXLabel("x")
            .SetYLabel("y"));
    }
}
