// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>An empty facet grid used to throw <see cref="DivideByZeroException"/> from the grid arithmetic
/// (measured 2026-08-30) — the least informative exception a caller can meet on a freshly booted wall. It is
/// refused with the reason instead.</summary>
public class FacetGridEmptyTests
{
    [Fact]
    public void ZeroCategories_AreRefusedWithTheReason()
    {
        var grid = new FacetGridFigure([], [], [], (ax, x, y) => ax.Plot(x, y));

        var ex = Assert.Throws<InvalidOperationException>(() => grid.Build());
        Assert.Contains("category", ex.Message);
    }

    [Fact]
    public void OneCategory_StillBuilds()
    {
        var grid = new FacetGridFigure([0, 1], [1, 2], ["a", "a"], (ax, x, y) => ax.Plot(x, y));

        Assert.Single(grid.Build().Build().SubPlots);
    }
}
