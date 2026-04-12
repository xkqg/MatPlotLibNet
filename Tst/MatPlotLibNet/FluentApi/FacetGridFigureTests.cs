// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="FacetGridFigure"/> — faceted one-panel-per-category preset.</summary>
public class FacetGridFigureTests
{
    private static readonly double[] X   = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
    private static readonly double[] Y   = [1.0, 2.0, 3.0, 1.5, 2.5, 3.5];
    private static readonly string[] Cat = ["A", "A", "B", "B", "C", "C"];

    [Fact]
    public void Build_ThreeCategories_HasThreePanels()
    {
        var fig = new FacetGridFigure(X, Y, Cat, (ax, x, y) => ax.Scatter(x, y)).Build().Build();
        Assert.Equal(3, fig.SubPlots.Count);
    }

    [Fact]
    public void Build_UsesMaxColsForRowCount()
    {
        // 5 categories, maxCols=3 → 2 rows → 5 panels (not 6; grid has 5 actual subplots)
        var x   = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var y   = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        string[] cat = ["A", "A", "B", "B", "C", "C", "D", "D", "E", "E"];
        var fig = new FacetGridFigure(x, y, cat, (ax, fx, fy) => ax.Scatter(fx, fy)) { MaxCols = 3 }
            .Build().Build();
        Assert.Equal(5, fig.SubPlots.Count);
    }

    [Fact]
    public void Build_InvokesPlotFuncPerPanel()
    {
        int callCount = 0;
        new FacetGridFigure(X, Y, Cat, (ax, x, y) => { callCount++; ax.Scatter(x, y); })
            .Build();   // FigureBuilder — sub-plots registered but not yet materialized
        // Build again to materialize
        _ = new FacetGridFigure(X, Y, Cat, (ax, x, y) => { callCount++; ax.Scatter(x, y); })
            .Build().Build();
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void Build_PlotFuncReceivesFilteredData()
    {
        double[]? capturedX = null;
        Action<AxesBuilder, double[], double[]> captureFirst = (ax, x, y) =>
        {
            // Capture the first invocation (category "A")
            capturedX ??= x;
            ax.Scatter(x, y);
        };
        _ = new FacetGridFigure(X, Y, Cat, captureFirst).Build().Build();
        // "A" occupies indices 0,1 → X values 1.0 and 2.0
        Assert.Equal(2, capturedX!.Length);
    }

    [Fact]
    public void Build_PanelTitleIsCategoryLabel()
    {
        var svg = new FacetGridFigure(X, Y, Cat, (ax, x, y) => ax.Scatter(x, y))
            .Build().ToSvg();
        Assert.Contains("A", svg);
        Assert.Contains("B", svg);
        Assert.Contains("C", svg);
    }

    [Fact]
    public void Build_WithHue_DoesNotAffectCategoryCount()
    {
        string[] hue = ["H1", "H2", "H1", "H2", "H1", "H2"];
        var fig = new FacetGridFigure(X, Y, Cat, (ax, x, y) => ax.Scatter(x, y)) { Hue = hue }
            .Build().Build();
        Assert.Equal(3, fig.SubPlots.Count);
    }
}
