// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="PairPlotFigure"/>: the faceted figure of a pair plot, which is the <see cref="PairGridSeries"/>.</summary>
public class PairPlotFigureTests
{
    private static readonly double[][] Two   = [[1.0, 2.0, 3.0], [3.0, 2.0, 1.0]];
    private static readonly double[][] Three = [[1.0, 2.0, 3.0], [3.0, 2.0, 1.0], [1.0, 3.0, 2.0]];

    private static PairGridSeries GridOf(PairPlotFigure figure) =>
        Assert.IsType<PairGridSeries>(Assert.Single(Assert.Single(figure.Build().Build().SubPlots).Series));

    /// <summary>A pair plot is the pair grid series over its columns, on one axes 200 pixels a variable square.</summary>
    [Fact]
    public void Build_IsOneAxesHoldingThePairGridOfItsColumns()
    {
        var figure = new PairPlotFigure(Three).Build().Build();

        var grid = Assert.IsType<PairGridSeries>(Assert.Single(Assert.Single(figure.SubPlots).Series));
        Assert.Same(Three, grid.Variables);
        Assert.Equal((600.0, 600.0), (figure.Width, figure.Height));
    }

    /// <summary>The diagonal is a histogram of the bins asked for, the cells off it scatters.</summary>
    [Fact]
    public void Build_DiagonalIsAHistogramOfItsBins_OffItAScatter()
    {
        var grid = GridOf(new PairPlotFigure(Three) { Bins = 7 });

        Assert.Equal(PairGridDiagonalKind.Histogram, grid.DiagonalKind);
        Assert.Equal(PairGridOffDiagonalKind.Scatter, grid.OffDiagonalKind);
        Assert.Equal(7, grid.DiagonalBins);
    }

    /// <summary>The column names name the variables, drawn under the grid and beside it.</summary>
    [Fact]
    public void Build_WithColumnNames_NamesTheVariables()
    {
        string[] names = ["Alpha", "Beta", "Gamma"];
        var figure = new PairPlotFigure(Three) { ColumnNames = names };

        Assert.Same(names, GridOf(figure).Labels);
        Assert.Contains(">Alpha<", figure.Build().ToSvg(), StringComparison.Ordinal);
    }

    /// <summary>A hue label per observation splits the grid by hue: one group per label in the order first seen, named by the
    /// label, in the palette given.</summary>
    [Fact]
    public void Build_WithHue_SplitsTheGridByHueInTheOrderFirstSeen()
    {
        Color[] palette = [Colors.Red, Colors.Blue];
        var grid = GridOf(new PairPlotFigure(Two) { Hue = ["B", "A", "B"], Palette = palette });

        Assert.Equal([0, 1, 0], grid.HueGroups!);
        Assert.Equal(["B", "A"], grid.HueLabels!);
        Assert.Same(palette, grid.HuePalette);
    }

    /// <summary>With no hue the grid is one colour, and with no palette it takes the theme's cycle.</summary>
    [Fact]
    public void Build_NoHue_NoPalette_IsOneGroupInTheThemesColours()
    {
        var grid = GridOf(new PairPlotFigure(Two));

        Assert.Null(grid.HueGroups);
        Assert.Null(grid.HueLabels);
        Assert.Null(grid.HuePalette);
    }

    /// <summary>A pair plot of no column draws an empty figure, as it always did.</summary>
    [Fact]
    public void Build_NoColumns_DrawsNoAxes() =>
        Assert.Empty(new PairPlotFigure([]).Build().Build().SubPlots);
}
