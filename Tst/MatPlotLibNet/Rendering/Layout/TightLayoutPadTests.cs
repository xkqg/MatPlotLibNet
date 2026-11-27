// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The pad a tight layout keeps. Under the matplotlib themes it is matplotlib's <c>tight_layout</c> pad, 1.08 of the theme's
/// font size, at every edge of the figure and once between two subplots (<c>_auto_adjust_subplotpars</c>: <c>margin_left =
/// hspaces[:, 0].max() + pad_inch</c>, <c>hspace = hspaces[:, 1:-1].max() + hpad_inch</c>, <c>w_pad</c> defaulting to <c>pad</c>).
/// Printed with matplotlib 3.11.2 for two axes side by side, each with a Y label and labels on the right as well: under the default
/// style the left margin is the overhang 51.17 plus 15.0, the right margin 31.72 plus 15.0, the gap 31.72 + 51.17 + 15.0 = 97.89;
/// under the classic style the pad is 18.0. The library's own themes keep their pads.</summary>
public sealed class TightLayoutPadTests
{
    /// <summary>Two axes side by side, each with a Y label and its Y tick labels on the right as well: every margin and the gap is past
    /// what the axes draw there (labels five digits wide), above the floors the tight layout keeps.</summary>
    private static Figure Pair(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme)
        .AddSubPlot(1, 2, 1, Panel)
        .AddSubPlot(1, 2, 2, Panel)
        .TightLayout().Build();

    private static void Panel(AxesBuilder ax) => ax.Plot([0.0, 25.0, 50.0], [0.0, 25_000.0, 50_000.0]).SetYLabel("y").WithYTicksMirrored();

    [Theory]
    [InlineData("v2", 10 * 1.08 * 100 / 72)]
    [InlineData("classic", 12 * 1.08 * 100 / 72)]
    public void ATightLayoutUnderTheMatplotlibThemes_KeepsTheirPad_AtTheEdgeAndOnceBetweenTwoSubplots(string id, double pad)
    {
        var theme = id.AsTheme();

        var mirrored = new ConstrainedLayoutEngine().Compute(Pair(theme), new SvgRenderContext());
        Assert.Equal(mirrored.MarginLeft + mirrored.MarginRight - pad, mirrored.HorizontalGap, 9);
    }

    /// <summary>The pad holds ink off an edge: two rows of two axes that draw nothing past any spine keep the gaps their caller
    /// declared, narrower than the pad, under every theme. A pad alone is no need.</summary>
    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    [InlineData("default")]
    public void AxesThatDrawNothingPastTheirSpines_KeepTheDeclaredGaps_EvenBelowThePad(string id)
    {
        var theme = id.AsTheme();
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(2, 2, 1, Bare).AddSubPlot(2, 2, 2, Bare).AddSubPlot(2, 2, 3, Bare).AddSubPlot(2, 2, 4, Bare)
            .TightLayout().WithSubPlotSpacing(sp => sp with { HorizontalGap = 3, VerticalGap = 3 }).Build();

        var computed = new ConstrainedLayoutEngine().Compute(figure, new SvgRenderContext());

        Assert.Equal(3, computed.HorizontalGap);
        Assert.Equal(3, computed.VerticalGap);
    }

    private static void Bare(AxesBuilder ax) => ax.Plot([0.0, 1.0], [0.0, 1.0]).HideAllAxes();

    [Fact]
    public void ATightLayoutUnderALibraryTheme_KeepsTheLibrarysPads_OneOnEachSideOfAGap()
    {
        var mirrored = new ConstrainedLayoutEngine().Compute(Pair(Theme.Default), new SvgRenderContext());
        Assert.True(mirrored.MarginLeft > new SubPlotSpacing().MarginLeft, "precondition: the left margin is past its floor");
        Assert.Equal(mirrored.MarginLeft + mirrored.MarginRight, mirrored.HorizontalGap, 9);
    }
}
