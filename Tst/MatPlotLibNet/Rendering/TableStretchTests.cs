// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>When a draw keeps where the subplot parameters put each subplot (<see cref="TableGrid.AnyStretchIn"/>), and how far a layout
/// stretched an axes from there (<see cref="FigureRanges.StretchOf"/>): matplotlib makes a table's row a share of the height its axes has
/// when <c>ax.table</c> is called (table.py:399-401), so a layout that moves the axes afterwards stretches the rows.</summary>
public class TableStretchTests
{
    private static Figure WithTable(Theme theme, Action<TableSeries>? configure = null)
    {
        var figure = new Figure { Theme = theme };
        var table = figure.AddSubPlot().Table([["1"]]);
        configure?.Invoke(table);
        return figure;
    }

    [Fact]
    public void ATableUnderMatplotlibsLook_Stretches() => Assert.True(TableGrid.AnyStretchIn(WithTable(Theme.MatplotlibV2)));

    [Fact]
    public void UnderTheLibrarysOwnLook_NoTableStretches() => Assert.False(TableGrid.AnyStretchIn(WithTable(Theme.Default)));

    [Fact]
    public void ATableGivenARowHeight_DoesNotStretch() =>
        Assert.False(TableGrid.AnyStretchIn(WithTable(Theme.MatplotlibV2, table => table.CellHeight = 20)));

    [Fact]
    public void AFigureWithNoTable_HasNothingToStretch()
    {
        var figure = new Figure { Theme = Theme.MatplotlibV2 };
        figure.AddSubPlot().Plot([0.0, 1.0], [0.0, 1.0]);

        Assert.False(TableGrid.AnyStretchIn(figure));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]   // an inset four deep is not drawn (ChartRenderer.DrawWithInsets draws three deep)
    public void ATableOnAnInset_StretchesWhereTheInsetIsDrawn(int depth, bool stretches)
    {
        var figure = new Figure { Theme = Theme.MatplotlibV2 };
        var axes = figure.AddSubPlot();
        for (int i = 0; i < depth; i++) axes = axes.AddInset(0.1, 0.1, 0.5, 0.5);
        axes.Table([["1"]]);

        Assert.Equal(stretches, TableGrid.AnyStretchIn(figure));
    }

    [Fact]
    public void ADrawThatKeepsNoPlaces_StretchesNothing()
    {
        var figure = WithTable(Theme.MatplotlibV2);

        Assert.Equal(1, new FigureRanges(figure).StretchOf(figure.SubPlots[0], 500));
        Assert.False(new FigureRanges(figure).Stretches);
    }

    [Fact]
    public void ASubplot_IsStretchedByItsHeightOverItsHeightAtTheSubplotParameters()
    {
        var figure = WithTable(Theme.MatplotlibV2);
        var ranges = new FigureRanges(figure) { MadeOn = [new Rect(80, 58, 496, 400)] };

        Assert.True(ranges.Stretches);
        Assert.Equal(1.1, ranges.StretchOf(figure.SubPlots[0], 440), 12);
    }

    [Fact]
    public void AnAxesThatIsNoSubplot_OrStoodAtNoHeight_IsNotStretched()
    {
        var figure = WithTable(Theme.MatplotlibV2);
        var inset = figure.SubPlots[0].AddInset(0.1, 0.1, 0.5, 0.5);

        Assert.Equal(1, new FigureRanges(figure) { MadeOn = [new Rect(80, 58, 496, 400)] }.StretchOf(inset, 440));
        Assert.Equal(1, new FigureRanges(figure) { MadeOn = [new Rect(80, 58, 496, 0)] }.StretchOf(figure.SubPlots[0], 440));
    }
}
