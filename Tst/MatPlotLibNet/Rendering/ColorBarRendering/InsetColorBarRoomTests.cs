// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ColorBarRendering;

/// <summary>An inset's colour bar takes its room from the rectangle the inset is given, not from the part of it a shape is fitted
/// into. matplotlib 3.11.2: <c>fig.colorbar(im, ax=inset)</c> on an <c>inset_axes</c> (no subplot spec) runs
/// <c>make_axes</c> over the inset's original position, its bounds; the inset is moved to 0.80 of that box and a shape fixed by an
/// aspect is fitted inside that part against the bar (<c>panchor</c>), while the bar is as tall as the whole box. Measured: an
/// inset of 100 x 300 pixels at (162, 118.2) with <c>imshow</c> of a 6 x 6 matrix and a bar: the inset is an 80 pixel square
/// against the bar, the bar 15 wide and 300 tall.</summary>
public sealed class InsetColorBarRoomTests
{
    // The v2 theme's single subplot: (100, 72, 620, 462). An inset 100 x 300 pixels at 0.1 of the way in on both sides.
    private static readonly Rect Parent = new(100, 72, 620, 462);
    private static readonly Rect InsetCell = new(Parent.X + 0.1 * Parent.Width, Parent.Y + 0.1 * Parent.Height, 100, 300);

    private static Figure FigureWithInset(Func<AxesBuilder, AxesBuilder> inset, Theme theme) =>
        Plt.Create().WithSize(800, 600).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0])
                .AddInset(0.1, 0.1, InsetCell.Width / Parent.Width, InsetCell.Height / Parent.Height, i => inset(i)))
            .Build();

    private static List<Rect> RectanglesDrawn(Figure figure)
    {
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);
        return [.. ctx.Rects()];
    }

    /// <summary>A shape fitted in the inset's part leaves the bar as tall as the inset's rectangle, in a slot of 0.15 of its width.</summary>
    [Fact]
    public void AnInsetWithAnAspect_GetsItsBarAgainstItsOwnRectangle()
    {
        var figure = FigureWithInset(i => i.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetAspect(1).WithColorBar(), Theme.MatplotlibV2);

        var rectangles = RectanglesDrawn(figure);

        Assert.True(rectangles.Any(r => r.IsCloseTo(new Rect(InsetCell.X + 0.85 * InsetCell.Width, InsetCell.Y, 15, 300), 1e-3)),
            $"no bar of the inset's height was drawn; drawn: {string.Join(", ", rectangles.Where(r => r.Width < 30 && r.Height > 20))}");
    }

    /// <summary>An inset with no aspect fills its part: the cell is the rectangle and the bar stands as it did.</summary>
    [Fact]
    public void AnInsetWithNoAspect_KeepsItsBar()
    {
        var figure = FigureWithInset(i => i.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).WithColorBar(), Theme.MatplotlibV2);

        Assert.Contains(RectanglesDrawn(figure), r => r.IsCloseTo(new Rect(InsetCell.X + 0.85 * InsetCell.Width, InsetCell.Y, 15, 300), 1e-3));
    }

    /// <summary>The library's own themes keep the bar beside the inset's plot, 20 pixels thick, and leave the slot empty.</summary>
    [Fact]
    public void UnderTheLibrarysThemes_TheBarStandsBesideThePlot()
    {
        var figure = FigureWithInset(i => i.Heatmap(new double[,] { { 1, 2 }, { 3, 4 } }).SetAspect(1).WithColorBar(), Theme.Default);

        var rectangles = RectanglesDrawn(figure);

        Assert.DoesNotContain(rectangles, r => r.IsCloseTo(new Rect(InsetCell.X + 0.85 * InsetCell.Width, InsetCell.Y, 15, 300), 1e-3));
        Assert.Contains(rectangles, r => r.Width is > 19.9 and < 20.1 && r.Height > 20);
    }
}
