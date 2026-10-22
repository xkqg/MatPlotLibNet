// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A heatmap's cells stand where their data coordinates are, like every other series: column <c>c</c> covers X from
/// <c>c</c> to <c>c + 1</c>, and row <c>r</c> covers Y from <c>rows - r - 1</c> to <c>rows - r</c> (row 0 on top). The axes'
/// tick labels, grid and month names all follow the axes' range, so a cell placed by the plot area alone ends up under
/// the wrong label as soon as the range is not the grid's own (a limit set by hand, a zoom).</summary>
public class HeatmapAxesRangeTests
{
    private const double Tolerance = 1e-6;

    private static readonly Regex RectRx = new(
        """<rect x="(?<x>[-0-9.E]+)" y="(?<y>[-0-9.E]+)" width="(?<w>[-0-9.E]+)" height="(?<h>[-0-9.E]+)" fill="(?<fill>#[0-9A-Fa-f]{6,8})" />""",
        RegexOptions.Compiled);

    private static readonly Regex MonthRx = new(
        """<text x="(?<x>[-0-9.E]+)" y="[-0-9.E]+"[^>]*>(?<name>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)</text>""",
        RegexOptions.Compiled);

    private readonly record struct Box(double X, double Y, double W, double H, string Fill)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
        public double CentreX => X + W / 2;
    }

    private static List<Box> Boxes(string svg) =>
        [.. RectRx.Matches(svg).Select(m => new Box(Num(m, "x"), Num(m, "y"), Num(m, "w"), Num(m, "h"), m.Groups["fill"].Value))];

    private static double Num(Match m, string group) => double.Parse(m.Groups[group].Value, CultureInfo.InvariantCulture);

    /// <summary>The plot area: the axes background, the second rectangle of the picture (after the figure's own).</summary>
    private static Box Plot(List<Box> boxes) => boxes[1];

    /// <summary>The cells of a plain heatmap on the default theme: every rectangle that is not white (the figure and the
    /// plot area are, and no cell of the data used here is).</summary>
    private static List<Box> Cells(List<Box> boxes) => [.. boxes.Where(b => b.Fill != "#FFFFFF")];

    private const int Rows = 3, Cols = 4;

    private static double[,] Grid()
    {
        var data = new double[Rows, Cols];
        for (int r = 0; r < Rows; r++) for (int c = 0; c < Cols; c++) data[r, c] = r * Cols + c;
        return data;
    }

    private static Figure HeatmapWith(Func<AxesBuilder, AxesBuilder> limits) =>
        Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => limits(ax.Heatmap(Grid()))).Build();

    /// <summary>Row r's top edge is Y = Rows - r, its bottom edge Y = Rows - r - 1; column c spans X = c to c + 1.</summary>
    private static Box ExpectedCell(Box plot, int r, int c, double xMin, double xMax, double yMin, double yMax)
    {
        double left = plot.X + (c - xMin) / (xMax - xMin) * plot.W;
        double right = plot.X + (c + 1 - xMin) / (xMax - xMin) * plot.W;
        double top = plot.Y + (yMax - (Rows - r)) / (yMax - yMin) * plot.H;
        double bottom = plot.Y + (yMax - (Rows - r - 1)) / (yMax - yMin) * plot.H;
        return new Box(Math.Min(left, right), Math.Min(top, bottom), Math.Abs(right - left), Math.Abs(bottom - top), "");
    }

    private static void AssertCellsStandAtTheirData(Figure figure, double xMin, double xMax, double yMin, double yMax)
    {
        var boxes = Boxes(figure.ToSvg());
        var plot = Plot(boxes);
        var cells = Cells(boxes);
        Assert.Equal(Rows * Cols, cells.Count);
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var want = ExpectedCell(plot, r, c, xMin, xMax, yMin, yMax);
                // Cells are written row by row, left to right in the data (a reversed range does not reorder them).
                var got = cells[r * Cols + c];
                Assert.True(Math.Abs(got.X - want.X) < Tolerance && Math.Abs(got.Y - want.Y) < Tolerance
                            && Math.Abs(got.W - want.W) < Tolerance && Math.Abs(got.H - want.H) < Tolerance,
                    $"row {r}, column {c}: the cell is at ({got.X:F4}, {got.Y:F4}) {got.W:F4} x {got.H:F4}; its data puts it at ({want.X:F4}, {want.Y:F4}) {want.W:F4} x {want.H:F4}.");
            }
        }
    }

    // ---- the grid's own range: the cells tile the plot ------------------------------------------------------------

    [Fact]
    public void AtTheGridsOwnRange_TheCellsTileThePlotAreaExactly()
    {
        var boxes = Boxes(HeatmapWith(ax => ax).ToSvg());
        var plot = Plot(boxes);
        var cells = Cells(boxes);

        Assert.Equal(Rows * Cols, cells.Count);
        Assert.True(Math.Abs(cells.Min(c => c.X) - plot.X) < Tolerance, "the first column starts at the plot's left edge");
        Assert.True(Math.Abs(cells.Max(c => c.Right) - plot.Right) < Tolerance, "the last column ends at the plot's right edge");
        Assert.True(Math.Abs(cells.Min(c => c.Y) - plot.Y) < Tolerance, "row 0 starts at the plot's top edge");
        Assert.True(Math.Abs(cells.Max(c => c.Bottom) - plot.Bottom) < Tolerance, "the last row ends at the plot's bottom edge");
        AssertCellsStandAtTheirData(HeatmapWith(ax => ax), 0, Cols, 0, Rows);
    }

    // ---- a limit set by hand ---------------------------------------------------------------------------------------

    [Fact]
    public void AnXLimit_PlacesEachColumnAtItsDataCoordinates()
    {
        AssertCellsStandAtTheirData(HeatmapWith(ax => ax.SetXLim(1, 3)), 1, 3, 0, Rows);
    }

    [Fact]
    public void AYLimit_PlacesEachRowAtItsDataCoordinates()
    {
        AssertCellsStandAtTheirData(HeatmapWith(ax => ax.SetYLim(0.5, 2.5)), 0, Cols, 0.5, 2.5);
    }

    [Fact]
    public void AReversedXLimit_PutsTheFirstColumnAtTheRight()
    {
        AssertCellsStandAtTheirData(HeatmapWith(ax => ax.SetXLim(4, 0)), 4, 0, 0, Rows);
    }

    [Fact]
    public void AZoomEvent_MovesTheCellsWithTheLimitsItRewrites()
    {
        var figure = HeatmapWith(ax => ax);
        new ZoomEvent("chart", 0, 1, 3, 0.5, 2.5).ApplyTo(figure);

        AssertCellsStandAtTheirData(figure, 1, 3, 0.5, 2.5);
    }

    // ---- a cell the axis cannot place -------------------------------------------------------------------------------

    [Fact]
    public void OnALogAxis_ACellWhoseEdgeCannotBePlaced_IsLeftOut()
    {
        // Column 0 reaches back to X = 0, which a log axis has no place for; columns 1 to 3 stand on 1 to 4.
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.Heatmap(Grid()).SetXScale(AxisScale.Log).SetXLim(1, 4)).Build();

        string svg = figure.ToSvg();
        var cells = Cells(Boxes(svg));

        Assert.DoesNotContain("NaN", svg);
        Assert.Equal(Rows * (Cols - 1), cells.Count);
    }

    [Fact]
    public void AnEmptyMatrix_DrawsNoCell()
    {
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.Heatmap(new double[0, 0])).Build();

        Assert.Empty(Cells(Boxes(figure.ToSvg())));
    }

    // ---- the names stand under their cells -------------------------------------------------------------------------

    private static readonly DateTime[] Year = [.. Enumerable.Range(0, 365).Select(d => new DateTime(2025, 1, 1).AddDays(d))];

    private static readonly double[] Ones = [.. Enumerable.Repeat(1.0, 365)];

    private static Figure Calendar(Func<AxesBuilder, AxesBuilder> limits) =>
        Plt.Create().WithSize(1100, 300).AddSubPlot(1, 1, 1, ax => limits(ax.CalendarHeatmap(Year, Ones))).Build();

    /// <summary>The calendar's month names against its cells: each name stands at the centre of a column of cells, and that
    /// column holds the month's 1st (the names are ticks on the column centres). The cells are the rectangles a 7th of the
    /// plot's height tall that are as wide as one column in the drawn range.</summary>
    private static void AssertEachMonthNameStandsOverACellColumn(Figure figure, double columnsShown)
    {
        string svg = figure.ToSvg();
        var boxes = Boxes(svg);
        var plot = Plot(boxes);
        double columnWidth = plot.W / columnsShown;
        var cells = boxes.Where(b => Math.Abs(b.W - columnWidth) < 1e-4 && Math.Abs(b.H - plot.H / 7) < 1e-4).ToList();
        var names = MonthRx.Matches(svg).Select(m => (Name: m.Groups["name"].Value, X: Num(m, "x"))).ToList();

        Assert.True(names.Count > 0, "the calendar names no month");
        Assert.True(cells.Count > 0,
            $"no cell is {columnWidth:F4} px wide and {plot.H / 7:F4} px tall, the size {columnsShown} columns across the plot give: the cells ignore the range the axes show.");
        foreach (var (name, x) in names)
        {
            Assert.True(cells.Any(cell => Math.Abs(cell.CentreX - x) < Tolerance),
                $"'{name}' is written at x = {x:F4}, over no column of cells (columns are {columnWidth:F4} wide and start at {cells.Min(c => c.X):F4}).");
        }
    }

    [Fact]
    public void AtTheCalendarsOwnRange_EachMonthNameStandsOverAColumnOfCells()
    {
        AssertEachMonthNameStandsOverACellColumn(Calendar(ax => ax), 53);
    }

    [Fact]
    public void WhenTheCalendarIsZoomed_EachMonthNameStillStandsOverAColumnOfCells()
    {
        AssertEachMonthNameStandsOverACellColumn(Calendar(ax => ax.SetXLim(10, 20)), 10);
    }

    [Fact]
    public void WhenTheCalendarIsZoomedByAnEvent_EachMonthNameStillStandsOverAColumnOfCells()
    {
        var figure = Calendar(ax => ax);
        new ZoomEvent("chart", 0, 10, 20, 0, 7).ApplyTo(figure);

        AssertEachMonthNameStandsOverACellColumn(figure, 10);
    }
}
