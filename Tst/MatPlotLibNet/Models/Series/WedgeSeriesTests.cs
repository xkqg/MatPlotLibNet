// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>What a chart of the slices of a whole holds and answers whether it is a pie (<see cref="PieSeries"/>) or a donut
/// (<see cref="DonutSeries"/>), written once in <see cref="WedgeSeries"/> and held here for both kinds.</summary>
public class WedgeSeriesTests
{
    /// <summary>The two kinds of a chart of slices, by the name each is written under.</summary>
    public static TheoryData<string> Kinds => ["pie", "donut"];

    private static WedgeSeries Make(string kind, double[] sizes) => kind == "pie" ? new PieSeries(sizes) : new DonutSeries(sizes);

    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheSlices_AreHeldAsGiven_AndStartAtTheTop(string kind)
    {
        double[] sizes = [1.0, 2.0, 3.0];
        var series = Make(kind, sizes);

        Assert.Same(sizes, series.Sizes);
        Assert.Null(series.Labels);
        Assert.Null(series.Colors);
        Assert.Equal(90, series.StartAngle);
        Assert.Equal(3, series.ColorSlotSpan);
        Assert.True(series.DrawsWithoutAxes);
        Assert.Equal(default, series.ComputeDataRange(null!));
    }

    /// <summary>The table is a row per slice: its name, or "slice N" where it has none, and its size.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheTable_IsARowPerSlice(string kind)
    {
        var series = Make(kind, [1.0, 2.0]);
        series.Labels = ["first"];

        var table = series.ToDataTable()!;

        Assert.Equal(["label", "size"], table.Columns.Select(c => c.Header));
        Assert.Equal(["first", "slice 2"], table.Rows.Select(row => row[0].Text));
        Assert.Equal([1.0, 2.0], table.Rows.Select(row => row[1].Number));
    }

    /// <summary>The legend has an entry for each named slice, in its wedge's colour; a chart whose slices are not named has the series'
    /// own name as its entry.</summary>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void TheLegend_NamesEachNamedSlice(string kind)
    {
        var named = Make(kind, [1.0, 2.0, 3.0]);
        named.Labels = ["a", null!, "c"];
        named.Colors = [Colors.Red, Colors.Green, Colors.Blue];
        var unnamed = Make(kind, [1.0, 2.0]);
        unnamed.Label = "whole";

        Assert.Equal(["a", "c"], named.LegendPartsUnder(Theme.Default).Select(part => part.Label));
        Assert.Equal(["whole"], unnamed.LegendPartsUnder(Theme.Default).Select(part => part.Label));
    }

    /// <summary>matplotlib draws a donut as a pie whose wedges have a width (<c>pie(..., wedgeprops={'width': w})</c>), so its slices
    /// follow the pie's one direction rule (<c>counterclock</c>): the library's pie goes clockwise from the top unless it is told
    /// <c>CounterClockwise</c>, and so does its donut. A first slice of a quarter stands up and to the right clockwise, up and to the left
    /// counter-clockwise.</summary>
    [Theory]
    [InlineData("pie", false, 1)]
    [InlineData("pie", true, -1)]
    [InlineData("donut", false, 1)]
    [InlineData("donut", true, -1)]
    public void TheSlices_GoClockwiseFromTheTop_UnlessToldCounterClockwise(string kind, bool counterClockwise, int side)
    {
        var figure = Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax =>
        {
            if (kind == "pie") ax.Pie([1.0, 3.0], configure: s => s.CounterClockwise = counterClockwise);
            else ax.Donut([1.0, 3.0], configure: s => s.CounterClockwise = counterClockwise);
        }).Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var first = ctx.Of<DrawPathCall>().First().Segments;
        var arc = first.OfType<ArcSegment>().FirstOrDefault();
        var centre = arc?.Center ?? ((MoveToSegment)first[0]).Point;
        var points = first.SelectMany(segment => segment switch
        {
            MoveToSegment move => [move.Point],
            LineToSegment line => [line.Point],
            ArcSegment a => new[] { a.Center.AtAngle(a.RadiusX, a.StartAngle.ToRadians()), a.Center.AtAngle(a.RadiusX, a.EndAngle.ToRadians()) },
            _ => [],
        }).ToList();

        Assert.Equal(side, Math.Sign(points.Average(p => p.X) - centre.X));
        Assert.True(points.Average(p => p.Y) < centre.Y);
    }
}
