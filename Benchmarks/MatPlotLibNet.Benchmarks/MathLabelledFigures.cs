// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks;

/// <summary>The figures of the math-label rows (<c>MathTextBenchmarks</c> draws them as SVG text, <c>MathTextSkiaBenchmarks</c>
/// as pictures and as SVG outlines; the test suite of the benchmark program compiles this file in without either class): four
/// panels whose titles, axis labels and legend entries hold math, under a figure's own theme (matplotlib v2) with no layout engine,
/// the matplotlib v2 theme in a tight layout, and the matplotlib classic theme.</summary>
public sealed class MathLabelledFigures
{
    /// <summary>Five labels of the kind axes and legends carry: four with math, one without.</summary>
    public static readonly string[] Labels =
    [
        @"Latency ($\mu$s)",
        @"$\alpha_i^2 + \beta_i$",
        @"$\frac{\Delta y}{\Delta x}$",
        @"$\sqrt{x^2 + y^2}$",
        "Plain axis label with no math at all",
    ];

    private int _tick;

    /// <summary>The four panels under a figure's own theme, with no layout engine.</summary>
    public Figure Default { get; } = Panels(Plt.Create()).Build();

    /// <summary>The four panels under the matplotlib v2 theme in a tight layout.</summary>
    public Figure V2 { get; } = Panels(Plt.Create().WithTheme(Theme.MatplotlibV2).TightLayout()).Build();

    /// <summary>The four panels under the matplotlib classic theme, whose math is set in its own font set.</summary>
    public Figure Classic { get; } = Panels(Plt.Create().WithTheme(Theme.MatplotlibClassic)).Build();

    /// <summary>The v2 figure with each panel's title moved on to the next label, so every panel is laid out and drawn again.</summary>
    public Figure NextV2()
    {
        int shift = _tick++ & 1;
        for (int i = 0; i < V2.SubPlots.Count; i++) V2.SubPlots[i].Title = Labels[(i + shift) % Labels.Length];
        return V2;
    }

    private static FigureBuilder Panels(FigureBuilder figure)
    {
        var x = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var y = x.Select(v => v * v / 50).ToArray();
        var grid = figure.WithSize(800, 600).WithTitle(Labels[1]);
        for (int i = 0; i < 4; i++)
        {
            int panel = i;
            grid = grid.AddSubPlot(2, 2, i + 1, ax => ax
                .Plot(x, y, line => line.Label = Labels[(panel + 2) % Labels.Length])
                .WithTitle(Labels[panel])
                .SetXLabel(Labels[(panel + 1) % Labels.Length])
                .SetYLabel(Labels[(panel + 3) % Labels.Length])
                .WithLegend());
        }
        return grid;
    }
}
