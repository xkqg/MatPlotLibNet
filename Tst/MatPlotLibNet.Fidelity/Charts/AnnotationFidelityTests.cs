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
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.55, DeltaE = 30)]   // AA grey text vs matplotlib black
    public void Annotate_WithoutAFont_MatchesMatplotlib(string themeId)
    {
        double[] x = Enumerable.Range(0, 50).Select(i => i * 10.0 / 49).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Annotation text size")
                .Plot(x, x)
                .Annotate("peak", 5, 5)
                .Annotate("start", 1, 1)
                .SetXLabel("x")
                .SetYLabel("y"))
            .Build();
        AssertFidelity(figure, "annotate_default");
    }
}
