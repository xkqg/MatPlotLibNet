// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>matplotlib's <c>matshow</c>: a matrix shown as a grid of cells, one square for each value, the first row on top,
/// the columns numbered along the top and the rows down the left, whole numbers only, with a colour bar. The cells are
/// square because the plot has an aspect of one, so they stay square at any figure size.</summary>
public class MatShowTests
{
    private static double[,] Matrix(int rows, int columns)
    {
        var matrix = new double[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++) matrix[r, c] = r * columns + c;
        }

        return matrix;
    }

    private static Figure Of(int width, int height, int rows, int columns, Action<HeatmapSeries>? configure = null) => Plt.Create().WithSize(width, height)
        .AddSubPlot(1, 1, 1, ax => ax.MatShow(Matrix(rows, columns), configure))
        .Build();

    private static List<(string Text, Point At)> Labels(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var labels = new List<(string, Point)>();
        foreach (var call in recording.OfKind("DrawText"))
        {
            dynamic args = call.Args!;
            labels.Add(((string)args.text, (Point)args.position));
        }

        return labels;
    }

    [Theory]
    [InlineData(600, 600, 10, 10)]
    [InlineData(500, 350, 6, 12)]
    [InlineData(900, 500, 20, 5)]
    public void TheCells_AreSquare_AtAnyFigureSize(int width, int height, int rows, int columns)
    {
        var figure = Of(width, height, rows, columns);
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);

        Assert.Equal((range.XMax - range.XMin) / plot.Width, (range.YMax - range.YMin) / plot.Height, 9);
    }

    [Fact]
    public void TheChart_IsOneHeatmap_WithAnAspectOfOne()
    {
        var figure = Of(500, 400, 4, 5);

        Assert.IsType<HeatmapSeries>(Assert.Single(figure.SubPlots[0].Series));
        Assert.Equal(1.0, figure.SubPlots[0].Aspect);
    }

    [Fact]
    public void ASmallMatrix_NumbersEveryRowAndColumn_FromZero()
    {
        var labels = Labels(Of(500, 400, 3, 4)).Select(label => label.Text).ToArray();

        foreach (string index in new[] { "0", "1", "2", "3" }) Assert.Contains(index, labels);
    }

    [Fact]
    public void ALargeMatrix_NumbersWholeRoundNumbersOnly()
    {
        var labels = Labels(Of(900, 700, 40, 40)).Select(label => label.Text)
            .Where(text => int.TryParse(text, out _)).Select(int.Parse).Distinct().Order().ToArray();

        Assert.Contains(0, labels);
        Assert.All(labels, number => Assert.Equal(0, number % 5));
        Assert.DoesNotContain(1, labels);
        Assert.True(labels.Length <= 20, "a thinned set of ticks, not one for every row and column");
    }

    [Fact]
    public void TheFirstRow_IsOnTop_AndTheRowsAreNumberedDownTheLeft()
    {
        var figure = Of(500, 400, 3, 4);
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

        var leftOfThePlot = Labels(figure).Where(label => label.At.X < plot.X && label.At.Y > plot.Y - 2 && label.At.Y < plot.Y + plot.Height + 8
                                                        && int.TryParse(label.Text, out int n) && n <= 2).ToList();

        var zero = leftOfThePlot.First(label => label.Text == "0");
        var two = leftOfThePlot.First(label => label.Text == "2");
        Assert.True(zero.At.Y < two.At.Y, "row 0 is above row 2");
    }

    [Fact]
    public void TheColumns_AreNumberedAlongTheTop()
    {
        var figure = Of(500, 400, 3, 4);
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

        var above = Labels(figure).Where(label => label.At.Y < plot.Y && int.TryParse(label.Text, out _)).Select(label => label.Text).ToArray();

        Assert.Contains("3", above);
        Assert.Contains("0", above);
    }

    [Fact]
    public void ThereIsAColourBar()
    {
        Assert.NotNull(Of(500, 400, 3, 4).SubPlots[0].ColorBar);
    }

    [Fact]
    public void TheHeatmapIsStyledByConfigure_AfterTheVerb()
    {
        var figure = Of(500, 400, 3, 4, heatmap => heatmap.ShowLabels = true);

        Assert.True(figure.SubPlots[0].Series.OfType<HeatmapSeries>().Single().ShowLabels);
    }

    [Fact]
    public void ATwoWayMatrix_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of(500, 400, 3, 4);

        string svg = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(figure.ToSvg(), svg);
    }

    /// <summary>The verb draws exactly the picture of the hand chain of public building blocks it replaces, with and without a
    /// missing value.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws(bool missing)
    {
        Assert.Equal(ComposedChartShapes.MatShowByHand(missing).ToSvg(), ComposedChartShapes.Named("matshow").Build(missing).ToSvg());
    }

    [Fact]
    public void ANullMatrix_IsRefusedByName()
    {
        Assert.Equal("matrix", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.MatShow(null!)).Build()).ParamName);
    }

    [Fact]
    public void AMatrixWithNoCells_IsRefusedByName()
    {
        var ex = Assert.Throws<ArgumentException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.MatShow(new double[0, 3])).Build());

        Assert.Equal("matrix", ex.ParamName);
    }
}
