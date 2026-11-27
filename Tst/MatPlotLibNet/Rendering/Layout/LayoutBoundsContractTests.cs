// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The bounds a tight or a constrained layout holds its margins and gaps in. The library's own are twelve numbers, a floor
/// and a cap for each margin and each gap, the cap lifted by what is measured there. matplotlib's <c>tight_layout</c> keeps none: in
/// <c>_auto_adjust_subplotpars</c> (matplotlib 3.11.2, read with python) a margin is what the axes draw past their position and the
/// pad, <c>margin_top = max(vspaces[0, :].max(), 0) + pad_inch/fig_height_inch</c>, and a gap the same, <c>hspace =
/// hspaces[:, 1:-1].max() + hpad_inch / fig_width_inch</c>, with no floor and no cap. Printed with python for one axes on an 800 by 600
/// figure at 100 dpi whose title is 120 points: the top margin is 182.04 px under the default style and 151.57 px under the classic
/// one, past the library's cap of 120 px.</summary>
public sealed class LayoutBoundsContractTests
{
    /// <summary>The top margins matplotlib 3.11.2 prints for the figure of <see cref="TallTitle"/> under <c>tight_layout()</c>, in
    /// pixels.</summary>
    private const double MatplotlibV2TopMargin = 182.04;
    private const double MatplotlibClassicTopMargin = 151.57;

    /// <summary>One axes on an 800 by 600 figure under a tight layout, its title 120 points (at 100 dpi, in pixels).</summary>
    private static Figure TallTitle(Theme theme) => Plt.Create().WithSize(800, 600).WithTheme(theme).TightLayout()
        .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).WithTitle("Tall", style => style with { FontSize = 120 * 100 / 72.0 }))
        .Build();

    [Fact]
    public void TheLibrarysBounds_AreItsTwelveNumbers()
    {
        var bounds = LayoutBounds.Library;

        Assert.Equal(new MarginBounds(30, 120), bounds.Margins.Left);
        Assert.Equal(new MarginBounds(30, 100), bounds.Margins.Bottom);
        Assert.Equal(new MarginBounds(20, 120), bounds.Margins.Top);
        Assert.Equal(new MarginBounds(10, 140), bounds.Margins.Right);
        Assert.Equal(new MarginBounds(0, 150), bounds.ColumnGap);
        Assert.Equal(new MarginBounds(0, 120), bounds.RowGap);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void ATightLayoutUnderTheMatplotlibThemes_KeepsNoBound(string id)
    {
        var theme = id.AsTheme();

        Assert.Equal(LayoutBounds.None, LayoutPads.Of(TallTitle(theme)).Bounds);
    }

    [Fact]
    public void EveryOtherLayout_KeepsTheLibrarysBounds()
    {
        var line = (Func<FigureBuilder, FigureBuilder>)(b => b.AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])));

        Assert.Equal(LayoutBounds.Library, LayoutPads.Of(line(Plt.Create().WithTheme(Theme.Default).TightLayout()).Build()).Bounds);
        Assert.Equal(LayoutBounds.Library, LayoutPads.Of(line(Plt.Create().WithTheme(Theme.MatplotlibV2).ConstrainedLayout()).Build()).Bounds);
        Assert.Equal(LayoutBounds.Library, LayoutPads.Of(line(Plt.Create().WithTheme(Theme.MatplotlibV2)).Build()).Bounds);
    }

    /// <summary>A title taller than the library's cap keeps its whole room under matplotlib's tight layout, as matplotlib keeps it:
    /// the top margin is what the axes needs above its plot, past the cap as matplotlib's is. The numbers meet where the text is measured as
    /// Agg measures it (the Skia package's glyph metrics: <c>TitleRoomSkiaTests</c> holds 182.04 and 151.57); the core's width table has no
    /// glyphs to measure the title's box by.</summary>
    [Theory]
    [InlineData("v2", MatplotlibV2TopMargin)]
    [InlineData("classic", MatplotlibClassicTopMargin)]
    public void ATitleTallerThanTheLibrarysCap_KeepsItsWholeRoom_UnderMatplotlibsTightLayout(string id, double matplotlib)
    {
        var figure = TallTitle(id.AsTheme());
        var ctx = new SvgRenderContext();

        var spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);

        var needs = new AxesEdgeNeeds(LayoutPads.Of(figure)).MeasureEnds(figure.SubPlots[0], figure, ctx, default);
        Assert.True(matplotlib > LayoutBounds.Library.Margins.Top.Cap, "precondition: matplotlib's margin is past the library's cap");
        Assert.Equal(needs.Start, spacing.MarginTop, 9);
        Assert.True(spacing.MarginTop > LayoutBounds.Library.Margins.Top.Cap, $"the top margin {spacing.MarginTop} is held at the cap");
    }

    /// <summary>The library's own themes keep their cap: the same title is cut to it.</summary>
    [Fact]
    public void UnderALibraryTheme_TheSameTitle_IsHeldAtTheCap()
    {
        var spacing = new ConstrainedLayoutEngine().Compute(TallTitle(Theme.Default), new SvgRenderContext());

        Assert.Equal(LayoutBounds.Library.Margins.Top.Cap, spacing.MarginTop, 9);
    }
}
