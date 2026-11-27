// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using SkiaSharp;

namespace MatPlotLibNet.Skia.Tests;

/// <summary>The room above an axes under matplotlib's tight layout is matplotlib's: the title's lift (its baseline <c>axes.titlepad</c>
/// above the spine, or, under the default style, whose <c>axes.titley</c> is None, its descent where that is deeper:
/// <c>Axes._update_title_position</c>) and the part of its box above the baseline, then the layout's pad. Printed with matplotlib 3.11.2,
/// one axes on 800 x 600 at 100 dpi, <c>ax.set_title("Tall", fontsize=pt)</c>, <c>fig.tight_layout()</c>: the top margin, 600 less the
/// top of <c>ax.bbox</c>. The text is measured as Agg measures it (the Skia package's glyph metrics), which the core's width table does
/// not.</summary>
public sealed class TitleRoomSkiaTests
{
    [Theory]
    [InlineData("v2", 120, 182.04)]
    [InlineData("v2", 40, 71.35)]
    [InlineData("v2", 24, 48.66)]
    [InlineData("classic", 120, 151.57)]
    [InlineData("classic", 40, 67.15)]
    [InlineData("classic", 24, 50.94)]
    public void TheTopMarginOfATitledAxes_UnderATightLayout_IsMatplotlibs(string style, double points, double matplotlib)
    {
        var theme = style.AsTheme();
        var figure = Plt.Create().WithSize(800, 600).WithTheme(theme).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).WithTitle("Tall", s => s with { FontSize = points * 100 / 72.0 }))
            .Build();
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);

        var plot = figure.PlotAreaOnSkia();

        Assert.InRange(plot.Y, matplotlib - 1, matplotlib + 1);
    }
}
