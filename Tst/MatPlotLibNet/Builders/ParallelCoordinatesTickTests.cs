// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Every dimension of a parallel-coordinates chart reads in its own units, so its axis line carries ticks: the
/// values the locator picks over the column's own range, each a short mark on the column line with its number to the left,
/// at the height that value is drawn at. The Y axis itself stays tickless, since each column is scaled on its own.</summary>
public class ParallelCoordinatesTickTests
{
    private static readonly string[] Dimensions = ["length", "width", "height"];

    // Column ranges: length 1..3, width 10..30, height 0..100.
    private static readonly BarGroup[] Rows =
    [
        new("dog", [2, 30, 0]),
        new("cat", [1, 10, 100]),
        new("dog", [3, 20, 50]),
    ];

    private static Figure Of(string[] dimensions, IReadOnlyList<BarGroup> rows) => Plt.Create().WithSize(700, 450)
        .AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(dimensions, rows)).Build();

    private static IReadOnlyList<Annotation> Ticks(Figure figure) => figure.SubPlots[0].Annotations;

    private static double Centre(int column) => column + 0.5;

    private static string Text(double value) => value.ToString("G6", CultureInfo.InvariantCulture);

    private static double[] Expected(double low, double high) =>
        [.. new AutoLocator().Locate(low, high).Where(v => v >= low - 1e-9 * (high - low) && v <= high + 1e-9 * (high - low))];

    [Fact]
    public void TheHeightColumn_IsTickedAtTheNiceNumbersOfItsOwnRange()
    {
        var ticks = Ticks(Of(Dimensions, Rows)).Where(a => a.X == Centre(2)).ToArray();

        Assert.Equal(["0", "20", "40", "60", "80", "100"], ticks.Select(a => a.Text));
        Assert.Equal([0.0, 0.2, 0.4, 0.6, 0.8, 1.0], ticks.Select(a => a.Y).Select(y => Math.Round(y, 9)));
    }

    [Theory]
    [InlineData(0, 1.0, 3.0)]
    [InlineData(1, 10.0, 30.0)]
    [InlineData(2, 0.0, 100.0)]
    public void EveryColumn_IsTickedFromItsOwnRange_NotAnotherColumns(int column, double low, double high)
    {
        var ticks = Ticks(Of(Dimensions, Rows)).Where(a => a.X == Centre(column)).ToArray();

        double[] values = Expected(low, high);
        Assert.Equal(values.Select(Text), ticks.Select(a => a.Text));
        Assert.Equal(values.Select(v => Math.Round((v - low) / (high - low), 9)), ticks.Select(a => Math.Round(a.Y, 9)));
    }

    [Fact]
    public void ATick_IsAShortMarkOnTheColumnLine_WithItsNumberToTheLeft()
    {
        var tick = Ticks(Of(Dimensions, Rows)).First();

        Assert.Equal(tick.X, tick.ArrowTargetX);
        Assert.Equal(tick.Y, tick.ArrowTargetY);
        Assert.Equal(ArrowStyle.Simple, tick.ArrowStyle);
        Assert.Equal(TextAlignment.Right, tick.Alignment);
        Assert.Equal(TextVerticalAlignment.Center, tick.VerticalAlignment);
        Assert.True(tick.OffsetX < 0, "the number stands left of the column line");
    }

    [Fact]
    public void ATickMark_EndsAtThePixelItsValueIsDrawnAt()
    {
        var figure = Of(Dimensions, Rows);
        var plot = new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];
        var range = figure.SubPlots[0].ComputeDataRanges(figure.Theme);
        var recording = new RecordingRenderContext();

        new ChartRenderer().Render(figure, recording);

        // The mark of the tick at 60 on the height column: a connection path whose end is on the column line at that height.
        double columnX = plot.X + (Centre(2) - range.XMin) / (range.XMax - range.XMin) * plot.Width;
        double tickY = plot.Y + plot.Height * (1 - (0.6 - range.YMin) / (range.YMax - range.YMin));
        var ends = new List<Point>();
        foreach (var call in recording.OfKind("DrawPath"))
        {
            dynamic args = call.Args!;
            var segments = (PathSegment[])args.segments;
            if (segments.Length > 0 && segments[^1] is LineToSegment line) ends.Add(line.Point);
        }

        Assert.Contains(ends, end => Math.Abs(end.X - columnX) < 1e-6 && Math.Abs(end.Y - tickY) < 1e-6);
    }

    [Fact]
    public void AColumnWhoseValuesAreAllTheSame_HasOneTick_AtTheMiddle()
    {
        var ticks = Ticks(Of(["a", "b"], [new BarGroup("x", [5, 1]), new BarGroup("x", [5, 2])])).Where(a => a.X == Centre(0)).ToArray();

        var only = Assert.Single(ticks);
        Assert.Equal("5", only.Text);
        Assert.Equal(0.5, only.Y);
    }

    [Fact]
    public void AColumnWithNothingToDraw_HasNoTick()
    {
        var ticks = Ticks(Of(["a", "b"], [new BarGroup("x", [double.NaN, 1]), new BarGroup("x", [double.NaN, 2])]));

        Assert.DoesNotContain(ticks, a => a.X == Centre(0));
        Assert.Contains(ticks, a => a.X == Centre(1));
    }

    [Fact]
    public void AMissingValue_IsNoPartOfTheRangeTheTicksAreChosenOver()
    {
        // Without the missing row the height column runs 0..50; with the 100 gone the ticks stop at 50.
        BarGroup[] rows = [new("dog", [2, 30, 0]), new("cat", [1, 10, double.NaN]), new("dog", [3, 20, 50])];

        var ticks = Ticks(Of(Dimensions, rows)).Where(a => a.X == Centre(2)).ToArray();

        Assert.Equal(Expected(0, 50).Select(Text), ticks.Select(a => a.Text));
    }

    [Fact]
    public void TheClassesAndTheirLegend_AreAsTheyWere()
    {
        var axes = Of(Dimensions, Rows).SubPlots[0];

        Assert.Equal(2, axes.Series.OfType<LineSeries>().Count());
        Assert.Equal(3, axes.ReferenceLines.Count);
    }

    [Fact]
    public void TheYAxis_StillCarriesNoTicks()
    {
        Assert.Empty(Of(Dimensions, Rows).SubPlots[0].YAxis.TickLocator!.Locate(-10, 10));
    }
}
