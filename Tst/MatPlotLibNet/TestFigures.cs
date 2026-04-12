// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests;

internal static class TestFigures
{
    internal static Figure SingleLine(double[]? x = null, double[]? y = null)
        => Plt.Create().Plot(x ?? [1.0, 2.0, 3.0], y ?? [4.0, 5.0, 6.0]).Build();

    internal static Figure SingleScatter(double[]? x = null, double[]? y = null)
        => Plt.Create().Scatter(x ?? [1.0, 2.0, 3.0], y ?? [4.0, 5.0, 6.0]).Build();

    internal static Figure SingleBar(string[]? cats = null, double[]? vals = null)
        => Plt.Create().Bar(cats ?? ["A", "B", "C"], vals ?? [10.0, 20.0, 15.0]).Build();

    internal static Figure Empty() => Plt.Create().Build();

    internal static Figure WithTitle(string title)
        => Plt.Create().WithTitle(title).Plot([1.0, 2.0], [3.0, 4.0]).Build();
}
