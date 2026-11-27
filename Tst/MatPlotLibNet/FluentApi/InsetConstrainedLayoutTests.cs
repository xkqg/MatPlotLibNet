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

    // ---- Constrained layout inset ------------------------------------------

}
