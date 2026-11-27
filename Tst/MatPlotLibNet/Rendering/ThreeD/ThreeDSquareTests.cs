// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Where a 3-D axes stands: matplotlib's <c>Axes3D.apply_aspect</c> shrinks each subplot's own position, its cell of the
/// grid under the figure's subplot params, to a square anchored at its centre. matplotlib 3.11.2 prints, for an 800 x 600
/// figure, the active position (100+79, 72, 462) under the default style and (170, 60, 480) under the classic one, and for
/// subplots 2,2,1 and 2,2,4 two different squares.</summary>
public sealed class ThreeDSquareTests
{
    private static readonly double[] Two = [0.0, 1.0];

    /// <summary>The square of every 3-D scene in the SVG, in the order they are drawn.</summary>
    private static List<Rect> Squares(Figure figure)
    {
        var svg = figure.ToSvg();
        return [.. svg.Xml().Descendants()
            .Where(e => e.Attribute("data-distance") is not null)
            .Select(e => new Rect(e.Number("data-plot-x"), e.Number("data-plot-y"), e.Number("data-plot-w"), e.Number("data-plot-h")))];
    }

    /// <summary>The centred square inside <paramref name="cell"/>.</summary>
    private static Rect SquareIn(Rect cell)
    {
        double side = Math.Min(cell.Width, cell.Height);
        return new Rect(cell.X + (cell.Width - side) / 2, cell.Y + (cell.Height - side) / 2, side, side);
    }

    private static void AssertRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
        Assert.Equal(expected.Width, actual.Width, 9);
        Assert.Equal(expected.Height, actual.Height, 9);
    }

    [Fact]
    public void EachSubplotOfAGrid_StandsInTheSquareOfItsOwnCell()
    {
        // The cells of a 2 x 2 grid in 800 x 600 under the default theme, as matplotlib's GridSpec leaves them (wspace and hspace
        // 0.2 of the average cell): (100, 72, 281.818, 210) and (438.182, 324, 281.818, 210).
        var figure = Plt.Create().WithSize(800, 600).WithBrowserInteraction()
            .AddSubPlot(2, 2, 1, ax => ax.Scatter3D(Two, Two, Two))
            .AddSubPlot(2, 2, 4, ax => ax.Scatter3D(Two, Two, Two))
            .Build();

        var squares = Squares(figure);

        Assert.Equal(2, squares.Count);
        AssertRect(SquareIn(new Rect(100, 72, 620 / 2.2, 462 / 2.2)), squares[0]);
        AssertRect(SquareIn(new Rect(100 + 620 / 2.2 * 1.2, 72 + 462 / 2.2 * 1.2, 620 / 2.2, 462 / 2.2)), squares[1]);
    }

    [Fact]
    public void TheLayout_HandsTheSquareOut_SoTheHitTestAndTheDrawingAgree()
    {
        var figure = Plt.Create().WithSize(800, 600).WithBrowserInteraction()
            .AddSubPlot(2, 2, 1, ax => ax.Scatter3D(Two, Two, Two))
            .AddSubPlot(2, 2, 4, ax => ax.Scatter3D(Two, Two, Two))
            .Build();

        var plotAreas = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;

        Assert.Equal(Squares(figure), plotAreas);
    }

    [Theory]
    [InlineData("v2", 179, 72, 462)]
    [InlineData("classic", 170, 60, 480)]
    public void ASingleSubplot_StandsWhereMatplotlibsActivePositionIs(string theme, double x, double y, double side)
    {
        var figure = Plt.Create().WithSize(800, 600).WithBrowserInteraction()
            .WithTheme(theme.AsTheme())
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(Two, Two, Two))
            .Build();

        AssertRect(new Rect(x, y, side, side), Squares(figure).Single());
    }

    /// <summary>matplotlib draws a 3-D axes' background patch at its active position, the square: under the classic style the
    /// white of the axes spans x 170..650 of an 800 x 600 figure, the grey figure around it.</summary>
    [Fact]
    public void TheAxesBackground_FillsTheSquare_NotTheCell()
    {
        var figure = Plt.Create().WithSize(800, 600).WithBrowserInteraction().WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(Two, Two, Two))
            .Build();
        var recording = new RecordingRenderContext();

        new ChartRenderer().Render(figure, recording);

        var axesBackground = recording.Of<DrawRectangleCall>()
            .Where(args => args.Fill == figure.Theme.AxesBackground)
            .Select(args => args.Rect)
            .First();
        AssertRect(Squares(figure).Single(), axesBackground);
    }

    [Fact]
    public void ATallCell_PutsTheSquareInItsMiddle()
    {
        // The default theme's margins in 400 x 900: 0.125 and 0.1 of the width, 0.12 and 0.11 of the height.
        var cell = new Rect(50, 108, 400 - 50 - 40, 900 - 108 - 99);
        var figure = Plt.Create().WithSize(400, 900).WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(Two, Two, Two))
            .Build();

        AssertRect(SquareIn(cell), Squares(figure).Single());
    }
}
