// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The grid a figure lays its subplots out on: how many rows and columns, the cell each axes takes, which edges of the
/// grid that cell touches, and the plot area of each axes under a spacing.</summary>
public class FigureGridTests
{
    private static readonly SubPlotSpacing Spacing = new() { MarginLeft = 60, MarginRight = 20, MarginTop = 40, MarginBottom = 50, HorizontalGap = 40, VerticalGap = 30 };

    private static Figure Bare(int count)
    {
        var figure = new Figure { Width = 800, Height = 600 };
        for (int i = 0; i < count; i++) figure.AddSubPlot();
        return figure;
    }

    // ---- the edge the figure's colour bar stands at ------------------------------------------------------------------------

    /// <summary>On a grid of two rows and two columns a bar beside the subplots has the last column on its edge, a bar below them the
    /// bottom row.</summary>
    [Theory]
    [InlineData(true, new[] { false, true, false, true })]
    [InlineData(false, new[] { false, false, true, true })]
    public void TheEdgeOfTheFiguresBar_IsTheLastColumnBesideAndTheBottomRowBelow(bool beside, bool[] onEdge)
    {
        var figure = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 2 };
        for (int i = 0; i < 4; i++) figure.AddSubPlot(gs, GridPosition.Single(i / 2, i % 2));
        var bar = new ColorBar { Visible = true, Orientation = beside ? ColorBarOrientation.Vertical : ColorBarOrientation.Horizontal };

        var grid = new FigureGrid(figure);

        Assert.Equal(onEdge, Enumerable.Range(0, 4).Select(i => grid.StandsAtBarEdge(i, bar)).ToArray());
    }

    // ---- rows and columns -----------------------------------------------------------------------------------------------

    [Fact]
    public void AFigureWithAGridSpec_HasItsRowsAndColumns()
    {
        var figure = new Figure();
        figure.AddSubPlot(new GridSpec { Rows = 3, Cols = 4 }, GridPosition.Single(0, 0));

        var grid = new FigureGrid(figure);

        Assert.Equal((3, 4), (grid.Rows, grid.Cols));
    }

    [Fact]
    public void AxesWithNoGrid_StandInOneRow()
    {
        var grid = new FigureGrid(Bare(3));

        Assert.Equal((1, 3), (grid.Rows, grid.Cols));
    }

    [Fact]
    public void AxesThatNameAGrid_TakeTheLargestTheyName()
    {
        var figure = new Figure();
        figure.AddSubPlot(2, 3, 1);
        figure.AddSubPlot(4, 2, 1);
        figure.AddSubPlot();

        var grid = new FigureGrid(figure);

        Assert.Equal((4, 3), (grid.Rows, grid.Cols));
    }

    // ---- the cell of each axes ------------------------------------------------------------------------------------------

    [Fact]
    public void AnAxesInAGridSpec_TakesItsPosition_OrTheFirstCellWithoutOne()
    {
        var figure = new Figure();
        var gs = new GridSpec { Rows = 2, Cols = 2 };
        figure.AddSubPlot(gs, new GridPosition(1, 2, 0, 2));
        figure.AddSubPlot(2, 2, 4);

        var grid = new FigureGrid(figure);

        Assert.Equal(new GridPosition(1, 2, 0, 2), grid.CellOf(0));
        Assert.Equal(GridPosition.Single(0, 0), grid.CellOf(1));
    }

    [Fact]
    public void AnAxesThatNamesAnIndex_StandsAtIt_AndOneWithoutStandsAtItsTurn()
    {
        var figure = new Figure();
        figure.AddSubPlot(2, 2, 4);
        figure.AddSubPlot();

        var grid = new FigureGrid(figure);

        Assert.Equal(GridPosition.Single(1, 1), grid.CellOf(0));
        Assert.Equal(GridPosition.Single(0, 1), grid.CellOf(1));
    }

    // ---- plot areas -----------------------------------------------------------------------------------------------------

    [Fact]
    public void AxesWithNoGrid_ShareTheWidthInEqualCells()
    {
        var rects = new FigureGrid(Bare(2)).PlotRects(40, Spacing);

        // 800 - 60 - 20 = 720 wide, less one 40 px gap: two cells of 340.
        Assert.Equal(new Rect(60, 40, 340, 510), rects[0]);
        Assert.Equal(new Rect(440, 40, 340, 510), rects[1]);
    }

    [Fact]
    public void AGridSpecWithRatios_SpansCellsAndTheGapsBetweenThem()
    {
        var figure = new Figure { Width = 800, Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 2, WidthRatios = [1, 3], HeightRatios = [2, 1] };
        figure.AddSubPlot(gs, new GridPosition(0, 2, 0, 1));
        figure.AddSubPlot(gs, new GridPosition(0, 1, 0, 2));

        var rects = new FigureGrid(figure).PlotRects(40, Spacing);

        // Columns: (720 - 40) split 1:3 = 170 and 510; rows: (510 - 30) split 2:1 = 320 and 160.
        Assert.Equal(new Rect(60, 40, 170, 320 + 30 + 160), rects[0]);
        Assert.Equal(new Rect(60, 40, 170 + 40 + 510, 320), rects[1]);
    }

    [Fact]
    public void AnAxesWithAnAspect_IsFittedInsideItsCell()
    {
        var figure = Bare(1);
        figure.SubPlots[0].Plot([0.0, 1.0], [0.0, 1.0]);
        figure.SubPlots[0].Aspect = 1;

        var rect = new FigureGrid(figure).PlotRects(40, Spacing)[0];

        Assert.Equal(510, rect.Width, 9);
        Assert.Equal(510, rect.Height, 9);
    }

    [Fact]
    public void TheRendererLayout_IsTheGridsPlotAreas()
    {
        var figure = Bare(3);

        Assert.Equal(new FigureGrid(figure).PlotRects(70, Spacing), new ChartRenderer().ComputeSubPlotLayout(figure, 70, Spacing));
    }

    // ---- the edges a cell touches ---------------------------------------------------------------------------------------

    [Fact]
    public void ACellInsideTheGrid_TouchesNoEdge()
    {
        var figure = new Figure();
        for (int i = 1; i <= 9; i++) figure.AddSubPlot(3, 3, i);

        var grid = new FigureGrid(figure);

        Assert.Equal(new Sides<bool>(Left: false, Bottom: false, Top: false, Right: false), grid.EdgeOf(4));
        Assert.Equal(new Sides<bool>(Left: true, Bottom: false, Top: true, Right: false), grid.EdgeOf(0));
        Assert.Equal(new Sides<bool>(Left: false, Bottom: true, Top: false, Right: true), grid.EdgeOf(8));
    }

    [Fact]
    public void ACellsHeight_IsItsShareOfTheFigure()
    {
        var figure = new Figure { Height = 600 };
        var gs = new GridSpec { Rows = 3, Cols = 1 };
        figure.AddSubPlot(gs, new GridPosition(0, 2, 0, 1));

        Assert.Equal(400, new FigureGrid(figure).CellHeight(0), 9);
    }

    // ---- the layout reads the cell the drawing uses ---------------------------------------------------------------------

    /// <summary>Two axes added with no grid stand side by side, so the first does not touch the right edge and the second
    /// does not touch the left: what the first draws past its right spine and the second past its left goes to the gap
    /// between them, not to the figure's margins.</summary>
    [Fact]
    public void TwoAxesWithNoGrid_EachTouchOnlyTheirOwnSide()
    {
        var grid = new FigureGrid(Bare(2));

        Assert.Equal(new Sides<bool>(Left: true, Bottom: true, Top: true, Right: false), grid.EdgeOf(0));
        Assert.Equal(new Sides<bool>(Left: false, Bottom: true, Top: true, Right: true), grid.EdgeOf(1));
    }

    /// <summary>An axes in a grid spec with no position of its own is drawn in the first cell, so its edges and its height
    /// are those of the first cell.</summary>
    [Fact]
    public void AnAxesInAGridSpecWithNoPosition_TouchesTheEdgesOfTheFirstCell()
    {
        var figure = new Figure { Height = 600 };
        var gs = new GridSpec { Rows = 2, Cols = 2 };
        figure.AddSubPlot(gs, GridPosition.Single(1, 1));
        figure.AddSubPlot(2, 2, 4);

        var grid = new FigureGrid(figure);

        Assert.Equal(new Sides<bool>(Left: true, Bottom: false, Top: true, Right: false), grid.EdgeOf(1));
        Assert.Equal(300, grid.CellHeight(1), 9);
    }

    /// <summary>An axes with no grid among axes that name one stands at its turn, and touches the edges of that cell.</summary>
    [Fact]
    public void AnAxesWithNoGridAmongAxesThatNameOne_TouchesTheEdgesOfItsTurn()
    {
        var figure = new Figure();
        figure.AddSubPlot(2, 2, 1);
        figure.AddSubPlot();

        Assert.Equal(new Sides<bool>(Left: false, Bottom: false, Top: true, Right: true), new FigureGrid(figure).EdgeOf(1));
    }

    /// <summary>The figure is laid out by where its axes are drawn: two axes added with no grid get the plot areas
    /// <c>subplot(1, 2, 1)</c> and <c>subplot(1, 2, 2)</c> get, margins and gap included, under the tight layout too.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoAxesWithNoGrid_AreLaidOutAsOneRowOfTwo(bool tight)
    {
        double[] x = [1, 2, 3];
        double[] wide = [125000, 980000, 455000];
        Figure Make(bool named)
        {
            var figure = new Figure { Width = 800, Height = 400, Spacing = new SubPlotSpacing { TightLayout = tight } };
            (named ? figure.AddSubPlot(1, 2, 1) : figure.AddSubPlot()).Plot(x, x);
            var second = named ? figure.AddSubPlot(1, 2, 2) : figure.AddSubPlot();
            second.Plot(x, wide);
            second.YAxis.Label = "a wide label";
            return figure;
        }

        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        Assert.Equal(new ChartRenderer().ComputeLayout(Make(named: true), ctx).PlotAreas,
                     new ChartRenderer().ComputeLayout(Make(named: false), ctx).PlotAreas);
    }

    /// <summary>An axes in a grid spec with no position is laid out as the axes the first cell names.</summary>
    [Fact]
    public void AnAxesInAGridSpecWithNoPosition_IsLaidOutAsTheFirstCell()
    {
        double[] x = [1, 2, 3];
        Figure Make(bool positioned)
        {
            var figure = new Figure { Width = 800, Height = 600, Spacing = new SubPlotSpacing { TightLayout = true } };
            var gs = new GridSpec { Rows = 2, Cols = 2 };
            figure.AddSubPlot(gs, GridPosition.Single(1, 1)).Plot(x, x);
            var first = positioned ? figure.AddSubPlot(gs, GridPosition.Single(0, 0)) : figure.AddSubPlot(2, 2, 4);
            first.Plot(x, [125000, 980000, 455000]);
            first.XAxis.Label = "x";
            return figure;
        }

        var ctx = new MatPlotLibNet.Rendering.Svg.SvgRenderContext();
        Assert.Equal(new ChartRenderer().ComputeLayout(Make(positioned: true), ctx).PlotAreas,
                     new ChartRenderer().ComputeLayout(Make(positioned: false), ctx).PlotAreas);
    }
}
