// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The gap between the subplots of a figure under the matplotlib themes. matplotlib 3.11.2 keeps <c>figure.subplot.wspace</c>
/// and <c>figure.subplot.hspace</c> at 0.2 in both styles, and <c>GridSpecBase.get_grid_positions</c> takes them as a share of the
/// average size of a cell: <c>cell_w = tot_width / (ncols + wspace * (ncols - 1))</c>, <c>sep_w = wspace * cell_w</c> (rows the same
/// with <c>hspace</c>). At 8 x 6 inches and 100 dpi, <c>fig.subplots(1, 2)</c> leaves the axes 100 to 381.818 and 438.182 to 720
/// (a gap of 56.364, not the 40 pixels the library's own themes rest at), and <c>fig.subplots(2, 2)</c> the rows 72 to 282 and
/// 324 to 534 under the default style, 60 to 278.182 and 321.818 to 540 under the classic one.</summary>
public sealed class SubplotGapTests
{
    private const double Within = 1e-3;

    private static FigureBuilder Grid(Theme theme, int rows, int cols)
    {
        var builder = Plt.Create().WithSize(800, 600).WithTheme(theme);
        for (int i = 1; i <= rows * cols; i++)
            builder.AddSubPlot(rows, cols, i, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]));
        return builder;
    }

    private static IReadOnlyList<Rect> PlotsOf(Figure figure) => new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas;

    private static void AssertRect(double left, double top, double right, double bottom, Rect actual, string what)
    {
        Assert.True(Math.Abs(left - actual.X) < Within, $"{what}: left {actual.X}, matplotlib {left}");
        Assert.True(Math.Abs(top - actual.Y) < Within, $"{what}: top {actual.Y}, matplotlib {top}");
        Assert.True(Math.Abs(right - (actual.X + actual.Width)) < Within, $"{what}: right {actual.X + actual.Width}, matplotlib {right}");
        Assert.True(Math.Abs(bottom - (actual.Y + actual.Height)) < Within, $"{what}: bottom {actual.Y + actual.Height}, matplotlib {bottom}");
    }

    /// <summary>The gap is wspace of the average axes width: 0.2 of 281.818, whatever the style.</summary>
    [Theory]
    [InlineData("classic", 60.0, 540.0)]
    [InlineData("v2", 72.0, 534.0)]
    public void TwoColumns_AreSeparatedByWspaceOfTheAverageWidth(string themeId, double top, double bottom)
    {
        var plots = PlotsOf(Grid(themeId.AsTheme(), 1, 2).Build());

        AssertRect(100, top, 381.818, bottom, plots[0], "the first plot");
        AssertRect(438.182, top, 720, bottom, plots[1], "the second plot");
    }

    /// <summary>Rows are separated by hspace of the average height the same way.</summary>
    [Theory]
    [InlineData("classic", 60.0, 278.182, 321.818, 540.0)]
    [InlineData("v2", 72.0, 282.0, 324.0, 534.0)]
    public void TwoRows_AreSeparatedByHspaceOfTheAverageHeight(string themeId, double top1, double bottom1, double top2, double bottom2)
    {
        var plots = PlotsOf(Grid(themeId.AsTheme(), 2, 2).Build());

        AssertRect(100, top1, 381.818, bottom1, plots[0], "row 1, column 1");
        AssertRect(438.182, top1, 720, bottom1, plots[1], "row 1, column 2");
        AssertRect(100, top2, 381.818, bottom2, plots[2], "row 2, column 1");
        AssertRect(438.182, top2, 720, bottom2, plots[3], "row 2, column 2");
    }

    /// <summary>With width ratios the gap is still wspace of the average cell: <c>add_gridspec(1, 3, width_ratios=[2, 1, 1])</c>
    /// leaves 100 to 373.529, 410 to 546.765 and 583.235 to 720 (a gap of 36.47 = 0.2 * 620 / 3.4).</summary>
    [Fact]
    public void WidthRatios_ShareTheWidthLeftAfterTheGaps()
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .WithGridSpec(1, 3, widthRatios: [2, 1, 1])
            .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot([0.0, 10.0], [0.0, 10.0]))
            .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Plot([0.0, 10.0], [0.0, 10.0]))
            .AddSubPlot(GridPosition.Single(0, 2), ax => ax.Plot([0.0, 10.0], [0.0, 10.0]))
            .Build();

        var plots = PlotsOf(figure);

        AssertRect(100, 72, 373.529, 534, plots[0], "the wide column");
        AssertRect(410, 72, 546.765, 534, plots[1], "the second column");
        AssertRect(583.235, 72, 720, 534, plots[2], "the third column");
    }

    /// <summary>An axes that spans the columns takes the gap in with it: <c>add_gridspec(2, 2)</c> with <c>gs[0, :]</c> and
    /// <c>gs[1, 0]</c> leaves the first 100 to 720 over rows 72 to 282, the second 100 to 381.818 over rows 324 to 534.</summary>
    [Fact]
    public void ASpanningAxes_TakesTheGapsItSpans()
    {
        var figure = Plt.Create().WithSize(800, 600).WithTheme(Theme.MatplotlibV2)
            .WithGridSpec(2, 2)
            .AddSubPlot(new GridPosition(0, 1, 0, 2), ax => ax.Plot([0.0, 10.0], [0.0, 10.0]))
            .AddSubPlot(GridPosition.Single(1, 0), ax => ax.Plot([0.0, 10.0], [0.0, 10.0]))
            .Build();

        var plots = PlotsOf(figure);

        AssertRect(100, 72, 720, 282, plots[0], "the spanning axes");
        AssertRect(100, 324, 381.818, 534, plots[1], "the one below it");
    }

    /// <summary>A gap that the axes' own labels need more room for still widens: the layout keeps what is drawn off its neighbour,
    /// which matplotlib's fixed spacing does not, so the resting gap is a floor and not a cap.</summary>
    [Fact]
    public void ANeighbourWhoseLabelsNeedMore_StillWidensTheGap()
    {
        var figure = Plt.Create().WithSize(300, 300).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [100000.0, 200000.0]))
            .Build();

        var plots = PlotsOf(figure);

        // 0.2 of the average width at 300 pixels is 0.2 * 232.5 / 2.2 = 21 pixels; the second axes' tick labels need more.
        double gap = plots[1].X - (plots[0].X + plots[0].Width);
        Assert.True(gap > 21.1, $"the gap is {gap}");
    }

    /// <summary>The library's own themes carry no gap fraction: their gap rests at the 40 pixels of a new spacing (widened by what
    /// the neighbours draw), the same as when that spacing is named, whatever the number of columns, as it always did.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void TheLibrarysTheme_KeepsItsRestingGap(int columns)
    {
        var builder = Plt.Create().WithSize(800, 600).WithTheme(Theme.Default);
        for (int i = 1; i <= columns; i++) builder.AddSubPlot(1, columns, i, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]));

        var resting = PlotsOf(builder.Build());
        var named = PlotsOf(builder.WithSubPlotSpacing(s => s with { HorizontalGap = 40 }).Build());

        for (int i = 0; i < columns; i++) Assert.Equal(named[i], resting[i]);
        Assert.True(resting[1].X - (resting[0].X + resting[0].Width) < 45, "a gap of about 40 pixels, not a share of the axes");
    }

    /// <summary>A spacing the author set is not a theme's: its pixel gaps stand under a matplotlib theme too.</summary>
    [Fact]
    public void ASpacingTheAuthorSet_KeepsItsPixelGap()
    {
        var figure = Grid(Theme.MatplotlibV2, 1, 2).WithSubPlotSpacing(s => s with { HorizontalGap = 80 }).Build();

        var plots = PlotsOf(figure);

        Assert.Equal(80, plots[1].X - (plots[0].X + plots[0].Width), 6);
    }

    /// <summary>The figure written and read back lays out the same: the gap fractions travel in the theme's JSON.</summary>
    [Fact]
    public void TheGapFractions_SurviveTheJson()
    {
        var figure = Grid(Theme.MatplotlibV2, 1, 2).Build();

        var back = figure.RoundTripped();

        Assert.Equal(0.2, back.Theme.DefaultSpacing!.HorizontalGapFraction);
        Assert.Equal(0.2, back.Theme.DefaultSpacing!.VerticalGapFraction);
        Assert.Equal(PlotsOf(figure)[1].X, PlotsOf(back)[1].X, 6);
    }
}
