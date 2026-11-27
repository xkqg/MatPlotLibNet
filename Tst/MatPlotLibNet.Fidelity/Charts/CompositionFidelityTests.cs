// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>
/// v1.1.3 composition fidelity tests — multi-subplot layouts that exercise figure-level
/// suptitle reservation, interior y-label gutter computation, and legend mathtext parsing.
///
/// These were the user-reported failure modes that motivated the v1.1.3 release; each
/// test here is a permanent regression guard.
/// </summary>
public class CompositionFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 48, Ssim = 0.64, DeltaE = 3, V2Rms = 56, V2Ssim = 0.53, V2DeltaE = 4)]   // mathtext legend + thin lines + 2-axis layout
    public void MathText_TwoSubplots_WithSupTitle_Legend_MatchesMatplotlib(string themeId)
    {
        var t = new double[500];
        var decay = new double[500];
        var noise = new double[500];
        for (int i = 0; i < 500; i++)
        {
            t[i] = i * 50.0 / 499.0;
            decay[i] = Math.Exp(-t[i] * 0.08) * Math.Cos(t[i] * 0.4);
            noise[i] = Math.Sin(t[i] * 1.3) * 0.3;
        }

        var figure = FidelityFigure(themeId)
            // As the reference composes it: fig.suptitle(..., fontsize=14, fontweight="bold") and fig.tight_layout(rect=[0, 0, 1, 0.93]).
            .WithTitle("$\\alpha$ decay and $\\beta$ noise — $\\omega = 0.4$ rad/ms",
                style => style with { FontSize = 14 * 100.0 / 72, FontWeight = FontWeight.Bold })
            .AddSubPlot(1, 2, 1, ax => ax
                .WithTitle("R$^{2}$ = 0.97")
                .SetXLabel("$\\Delta t$ (ms)")
                .SetYLabel("$\\sigma$ (normalised)")
                .Plot(t, decay, line => { line.Color = Colors.Tab10Blue; line.Label = "$\\alpha$ decay"; })
                .WithLegend(LegendPosition.UpperRight))
            .AddSubPlot(1, 2, 2, ax => ax
                .WithTitle("Noise — $\\mu \\pm 2\\sigma$")
                .SetXLabel("$\\Delta t$ (ms)")
                .SetYLabel("Amplitude")
                .Plot(t, noise, line => { line.Color = Colors.Tab10Orange; line.Label = "$\\beta$ noise"; })
                .WithLegend(LegendPosition.UpperRight))
            .TightLayout(new LayoutRect(0, 0, 1, 0.93))
            .Build();

        AssertFidelity(figure, "comp_mathtext_two_subplots");
    }
}
