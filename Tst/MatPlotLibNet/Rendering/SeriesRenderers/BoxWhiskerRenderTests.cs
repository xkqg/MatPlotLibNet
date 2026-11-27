// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A whisker stops at the most extreme observation inside its reach (<see cref="MatPlotLibNet.Models.Series.BoxSeries.Whis"/>,
/// matplotlib's <c>whis</c>), so an outlier beyond the fence leaves the whisker where the clean data would end it. The library ended it
/// at the fence, a value no observation holds: with an outlier at 100 a box of 1 to 5 grew a whisker to 8.5.</summary>
public sealed class BoxWhiskerRenderTests
{
    private static (XDocument Svg, Rect Plot) Draw(Theme theme, double[] values)
    {
        var figure = Plt.Create().WithSize(400, 300).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot([values], b => b.Positions = [0]).SetYLim(0, 10).SetXLim(-1, 1).WithGrid(g => g with { Visible = false })).Build();
        var plot = figure.FirstPlotArea();
        return (figure.ToSvg().Xml(), plot);
    }

    /// <summary>The top of the whisker above the box: the smallest y among the vertical lines of the chart that are not an axis tick.</summary>
    private static double TopOfTheWhisker(XDocument svg, Rect plot) =>
        svg.Each("line").Where(e => Math.Abs(e.Number("x1") - e.Number("x2")) < 1e-6
                && Math.Abs(e.Number("x1") - (plot.X + plot.Width / 2)) < 1.0 && Math.Abs(e.Number("y1") - e.Number("y2")) > 1)
            .Min(e => Math.Min(e.Number("y1"), e.Number("y2")));

    private static double PixelOf(Rect plot, double value) => plot.Y + plot.Height * (1 - value / 10.0);

    [Theory]
    [InlineData("v2")]
    [InlineData("default")]
    public void AnOutlierBeyondTheFence_LeavesTheWhiskerAtTheLastObservationInside(string themeName)
    {
        var theme = themeName.AsTheme();

        var (svg, plot) = Draw(theme, [1, 2, 3, 4, 5, 100]);   // fence at 8.5, the last observation inside it is 5

        Assert.Equal(PixelOf(plot, 5), TopOfTheWhisker(svg, plot), 1);
    }

    [Fact]
    public void WithNoOutlier_TheWhiskerReachesTheLargestObservation()
    {
        var (svg, plot) = Draw(Theme.Default, [1, 2, 3, 4, 5, 6, 7, 8, 9]);

        Assert.Equal(PixelOf(plot, 9), TopOfTheWhisker(svg, plot), 1);
    }
}
