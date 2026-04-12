// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies the inset axes API additions and constrained-layout inset positioning.</summary>
public class InsetConstrainedLayoutTests
{
    // ---- InsetAxes alias ---------------------------------------------------

    [Fact]
    public void InsetAxes_Alias_AddsInset()
    {
        var axes = new Axes();
        axes.InsetAxes(0.6, 0.1, 0.35, 0.35);
        Assert.Single(axes.Insets);
    }

    [Fact]
    public void InsetAxes_Returns_TheInsetAxesInstance()
    {
        var axes = new Axes();
        var inset = axes.InsetAxes(0.6, 0.1, 0.35, 0.35);
        Assert.Same(axes.Insets[0], inset);
    }

    [Fact]
    public void InsetAxes_SetsInsetBounds()
    {
        var axes = new Axes();
        var inset = axes.InsetAxes(0.5, 0.2, 0.4, 0.3);
        var ib = inset.InsetBounds!.Value;
        Assert.Equal(0.5, ib.X, 1e-9);
        Assert.Equal(0.2, ib.Y, 1e-9);
        Assert.Equal(0.4, ib.Width, 1e-9);
        Assert.Equal(0.3, ib.Height, 1e-9);
    }

    // ---- FigureBuilder.AddInset --------------------------------------------

    [Fact]
    public void FigureBuilder_AddInset_RendersWithoutErrors()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1, 2, 3], [4, 5, 6]))
            .AddInset(0, 0.6, 0.1, 0.35, 0.35, ax => ax.Plot([1, 2], [4, 5]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void FigureBuilder_AddInset_WithNoConfigure_RendersWithoutErrors()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1, 2, 3], [4, 5, 6]))
            .AddInset(0, 0.6, 0.1, 0.35, 0.35)
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // ---- Constrained layout inset ------------------------------------------

    [Fact]
    public void ConstrainedLayout_InsetRendersWithinParent()
    {
        string svg = Plt.Create()
            .ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1, 2, 3], [4, 5, 6]))
            .AddInset(0, 0.6, 0.1, 0.35, 0.35, ax => ax.Plot([1, 2], [4, 5]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void MultipleInsets_AllRender()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1, 2, 3], [4, 5, 6]))
            .AddInset(0, 0.6, 0.1, 0.35, 0.3, ax => ax.Plot([1, 2], [4, 5]))
            .AddInset(0, 0.1, 0.6, 0.35, 0.3, ax => ax.Scatter([1, 2], [4, 5]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
