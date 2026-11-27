// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

internal static class TestFigures
{
    internal static Figure SingleLine(double[]? x = null, double[]? y = null)
        => Plt.Create().Plot(x ?? [1.0, 2.0, 3.0], y ?? [4.0, 5.0, 6.0]).Build();

    internal static Figure Empty() => Plt.Create().Build();

    /// <summary>The SVG of a two-point line that <paramref name="with"/> sets a figure-level flag on.</summary>
    /// <param name="with">Switches a flag of the figure on (returns the builder it is given for a figure with none).</param>
    internal static string TwoPointSvg(Func<FigureBuilder, FigureBuilder> with) => with(Plt.Create()).Plot([1.0, 2.0], [3.0, 4.0]).ToSvg();

    /// <summary>The figure a test of one verb draws: 800 by 400 pixels with one subplot that <paramref name="verb"/> fills.</summary>
    /// <param name="verb">The call of the verb on the subplot.</param>
    internal static Figure OfOneVerb(Action<AxesBuilder> verb) => Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, verb).Build();
}
