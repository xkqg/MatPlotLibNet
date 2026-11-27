// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A polar axes stands in the centred square of its cell and its circle fills that square, as matplotlib draws it.
/// matplotlib 3.11, <c>PolarAxes.__init__</c>: <c>self.set_aspect('equal', adjustable='box', anchor='C')</c>, so the axes box
/// shrinks to the square of its cell, anchored at the centre, and the outermost radius stands on the edge of that box (the
/// 8 x 6 inch reference at 100 dpi: the box (100, 72, 620, 462) of the default subplot params becomes the square (179, 72,
/// 462, 462), the circle's radius 231). <c>PolarAxes.clear</c>: <c>self.title.set_y(1.05)</c>, so the title stands five hundredths
/// of the square above its top before the title's pad.</summary>
public class PolarSquareTests
{
    private static Axes PolarAxes(string? title = null) =>
        Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]);
            if (title is not null) ax.WithTitle(title);
        }).Build().SubPlots[0];

    [Theory]
    [InlineData(null)]
    [InlineData(2.0)]
    public void APolarAxes_IsTheCentredSquareOfItsCell(double? aspect)
    {
        var axes = PolarAxes();
        axes.Aspect = aspect;

        Assert.Equal(new Rect(179, 72, 462, 462), axes.FittedTo(new Rect(100, 72, 620, 462), Theme.MatplotlibV2));
        Assert.Equal(new Rect(10, 120, 300, 300), axes.FittedTo(new Rect(10, 20, 300, 500), Theme.Default));
    }

    [Fact]
    public void TheOutermostRadius_StandsOnTheEdgeOfTheSquare()
    {
        var transform = new PolarTransform(new Rect(179, 72, 462, 462), 1.0);

        Assert.Equal(231, transform.MaxRadius, 9);
        Assert.Equal(new Point(641, 303), transform.PolarToPixel(1.0, 0));
    }

    /// <summary>Under the library's own themes the title stands a twentieth of the square above it (the polar look's own rise); the
    /// matplotlib looks place it as matplotlib's set_title does (<see cref="PolarTitleTests"/>).</summary>
    [Fact]
    public void TheTitle_StandsATwentiethOfTheSquareAboveIt_AndTheGapAboveThat()
    {
        var square = new Rect(179, 72, 462, 462);
        var ctx = new RecordingRenderContext();
        new PolarAxesRenderer(PolarAxes("Rose"), square, ctx, Theme.Default).Render();

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == "Rose");

        Assert.Equal(72 - 0.05 * 462 - Theme.Default.TitleGapUnder, (title.Position).Y, 9);
    }

    /// <summary>The layout keeps the room the title rises into above the square, so under a theme whose margins are measured
    /// the title stays on the figure: the rise, the title and a gap below and above it.</summary>
    [Fact]
    public void TheLayout_KeepsTheTitlesRise_AboveTheSquare()
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Rose").PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])).Build();
        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();

        var plot = new ChartRenderer().ComputeLayout(figure, ctx).PlotAreas[0];

        double title = ctx.MeasureText("Rose", figure.SubPlots[0].TitleFont(figure.Theme)).Height;
        Assert.Equal(plot.Width, plot.Height, 9);
        Assert.True(plot.Y >= 0.05 * plot.Height + title + 2 * AxesTitleExtensions.TitleGap - 1e-9,
            $"the square starts at {plot.Y}, the title needs {0.05 * plot.Height + title + 2 * AxesTitleExtensions.TitleGap}");
    }
}
