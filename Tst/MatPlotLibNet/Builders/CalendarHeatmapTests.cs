// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The calendar heatmap in one call: the days of the dates laid out as seven weekday rows by week columns
/// (<see cref="CalendarGrid"/>), drawn as ONE heatmap with the week start in the top row, the weekdays named on the Y axis,
/// each month named under the column that holds its 1st, a colour bar, and a blank (not a zero) for every day without a
/// value. The verb sets no axis limits.</summary>
public class CalendarHeatmapTests
{
    private static DateTime[] Days(DateTime first, DateTime last) =>
        [.. Enumerable.Range(0, (last - first).Days + 1).Select(d => first.AddDays(d))];

    private static readonly DateTime[] Year2025 = Days(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

    /// <summary>A count per day, made by integer arithmetic, 0 to 8.</summary>
    private static double[] Counts(DateTime[] days) => [.. days.Select(d => (double)(d.DayOfYear % 9))];

    /// <summary>Every week start.</summary>
    public static TheoryData<DayOfWeek> WeekStarts { get; } = new(Enum.GetValues<DayOfWeek>());

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    // ---- reading the picture ---------------------------------------------------------------------------------------

    /// <summary>The plot area in pixels.</summary>
    private readonly record struct Area(double X, double Y, double W, double H);

    /// <summary>A cell as drawn: its rectangle in pixels, and whether it is blank (transparent).</summary>
    private readonly record struct Box(double X, double Y, double W, double H, bool Blank);

    /// <summary>A tick label as drawn: its text and where its anchor stands.</summary>
    private readonly record struct Label(string Text, double X, double Y);

    private static string Svg(Action<AxesBuilder> configure, int width = 1100, int height = 300, bool legendToggle = false)
    {
        var figure = Plt.Create().WithSize(width, height).AddSubPlot(1, 1, 1, configure).Build();
        figure.EnableLegendToggle = legendToggle;
        return figure.ToSvg();
    }

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The plot area: the first background-coloured rectangle that is not the figure's.</summary>
    private static Area PlotArea(string svg)
    {
        var m = Regex.Match(svg, """<rect x="([1-9][0-9.]*)" y="([0-9.]+)" width="([0-9.]+)" height="([0-9.]+)" fill="#FFFFFF" """.TrimEnd());
        return new Area(Num(m.Groups[1].Value), Num(m.Groups[2].Value), Num(m.Groups[3].Value), Num(m.Groups[4].Value));
    }

    /// <summary>The heatmap's cells, row by row from the top: the rectangles drawn directly in the figure after the figure's
    /// and the plot area's backgrounds (the colour bar's are inside its own group).</summary>
    private static Box[] Cells(string svg) =>
        [.. XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "rect").Skip(2).Select(r => new Box(
            Num((string)r.Attribute("x")!), Num((string)r.Attribute("y")!), Num((string)r.Attribute("width")!),
            Num((string)r.Attribute("height")!), (string?)r.Attribute("fill-opacity") == "0"))];

    private static Label[] TickLabels(string svg, string anchor) =>
        [.. XDocument.Parse(svg).Root!.Elements()
            .Where(e => e.Name.LocalName == "text" && (string?)e.Attribute("text-anchor") == anchor)
            .Select(e => new Label(e.Value, Num((string)e.Attribute("x")!), Num((string)e.Attribute("y")!)))];

    /// <summary>The names of the Y axis' ticks, from the top down.</summary>
    private static string[] YNamesFromTheTop(Axis axis) =>
        [.. axis.TickLocator!.Locate(0, CalendarGrid.DaysPerWeek).OrderDescending().Select(axis.TickFormatter!.Format)];

    // ---- one heatmap of the calendar grid --------------------------------------------------------------------------

    /// <summary>The whole chart is ONE heatmap whose cells are the calendar grid of the dates: seven weekday rows by 53 week
    /// columns for 2025, and nothing else on the axes.</summary>
    [Fact]
    public void TheChart_IsOneHeatmapOfTheCalendarGrid_AndNothingElse()
    {
        var axes = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025)));

        var heatmap = Assert.IsType<HeatmapSeries>(Assert.Single(axes.Series));
        Assert.Empty(axes.SecondarySeries);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Equal(CalendarGrid.Of(Year2025, Counts(Year2025), DayOfWeek.Monday).Cells, heatmap.Data);
        Assert.Equal(7, heatmap.Data.GetLength(0));
        Assert.Equal(53, heatmap.Data.GetLength(1));
    }

    /// <summary>The week starts on Monday unless the caller names another day.</summary>
    [Fact]
    public void TheWeek_StartsOnMonday_UnlessTheCallerNamesAnotherDay()
    {
        var byDefault = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025)));
        var fromSunday = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025), DayOfWeek.Sunday));

        Assert.Equal(CalendarGrid.Of(Year2025, Counts(Year2025), DayOfWeek.Monday).Cells, ((HeatmapSeries)byDefault.Series[0]).Data);
        Assert.Equal(CalendarGrid.Of(Year2025, Counts(Year2025), DayOfWeek.Sunday).Cells, ((HeatmapSeries)fromSunday.Series[0]).Data);
        Assert.Equal("Mon", YNamesFromTheTop(byDefault.YAxis)[0]);
        Assert.Equal("Sun", YNamesFromTheTop(fromSunday.YAxis)[0]);
    }

    /// <summary>All seven weekdays are named on the Y axis, one tick on the centre of each row, the week start at the top
    /// (a heatmap draws its row 0 at the top while the Y axis counts upward), whatever day the week starts on.</summary>
    [Theory]
    [MemberData(nameof(WeekStarts))]
    public void EveryWeekday_IsNamedOnItsRow_TheWeekStartAtTheTop(DayOfWeek weekStart)
    {
        var axis = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025), weekStart)).YAxis;

        var start = new DateTime(2025, 3, 2);
        while (start.DayOfWeek != weekStart) start = start.AddDays(1);
        Assert.Equal([0.5, 1.5, 2.5, 3.5, 4.5, 5.5, 6.5], axis.TickLocator!.Locate(0, 7));
        Assert.Equal([.. Enumerable.Range(0, 7).Select(r => start.AddDays(r).ToString("ddd", CultureInfo.InvariantCulture))], YNamesFromTheTop(axis));
    }

    /// <summary>On the picture: with weeks from Monday a Monday's cell is the top row and the name "Mon" stands level with
    /// it at the top of the Y axis; with weeks from Sunday the Monday is the second row and "Sun" stands on top.</summary>
    [Theory]
    [InlineData(DayOfWeek.Monday, 0, "Mon")]
    [InlineData(DayOfWeek.Sunday, 1, "Sun")]
    public void AMondaysCell_IsTheRowOfItsPlaceInTheWeek_UnderItsName(DayOfWeek weekStart, int mondayRow, string topName)
    {
        var monday = new DateTime(2025, 3, 3);

        string svg = Svg(ax => ax.CalendarHeatmap([monday], [5], weekStart));

        var plot = PlotArea(svg);
        var cells = Cells(svg);
        double rowHeight = plot.H / 7;
        Assert.Equal(7, cells.Length);
        var coloured = Assert.Single(cells, c => !c.Blank);
        Assert.Equal(plot.Y + mondayRow * rowHeight, coloured.Y, 9);
        Assert.Equal(rowHeight, coloured.H, 9);

        var names = TickLabels(svg, "end").OrderBy(l => l.Y).ToArray();
        Assert.Equal(topName, names[0].Text);
        var mon = Assert.Single(names, l => l.Text == "Mon");
        Assert.InRange(mon.Y, coloured.Y, coloured.Y + coloured.H);
    }

    /// <summary>Each month is named under the column that holds its 1st, at the column's centre, in a year of 53 columns and
    /// in one of 54 (2012 starts on a Sunday and has 366 days), and the cell of that 1st is in that very column.</summary>
    [Theory]
    [InlineData(2025, 53)]
    [InlineData(2012, 54)]
    public void EveryMonth_IsNamedUnderTheColumnThatHoldsItsFirst(int year, int columns)
    {
        var days = Days(new DateTime(year, 1, 1), new DateTime(year, 12, 31));
        double[] values = [.. days.Select(_ => 1.0)];
        var grid = CalendarGrid.Of(days, values, DayOfWeek.Monday);

        string svg = Svg(ax => ax.CalendarHeatmap(days, values));

        var plot = PlotArea(svg);
        var cells = Cells(svg);
        double columnWidth = plot.W / columns, rowHeight = plot.H / 7;
        Assert.Equal(7 * columns, cells.Length);
        var months = TickLabels(svg, "middle");
        Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[..12], months.Select(m => m.Text));
        for (int m = 0; m < 12; m++)
        {
            var first = new DateTime(year, m + 1, 1);
            int column = grid.ColumnOf(first), row = grid.RowOf(first);
            Assert.Equal(plot.X + (column + 0.5) * columnWidth, months[m].X, 6);
            Assert.True(months[m].Y > plot.Y + plot.H, $"'{months[m].Text}' stands at {months[m].Y}, not under the plot ending at {plot.Y + plot.H}");
            var cell = cells[row * columns + column];
            Assert.False(cell.Blank);
            Assert.Equal(plot.X + column * columnWidth, cell.X, 6);
            Assert.Equal(plot.Y + row * rowHeight, cell.Y, 6);
        }
    }

    // ---- blank days ------------------------------------------------------------------------------------------------

    /// <summary>The days of the first week before January 1 and of the last week after December 31 are drawn blank
    /// (transparent), not as zeros: 2025 starts and ends on a Wednesday, so six cells of 371 are blank.</summary>
    [Fact]
    public void TheDaysBeforeTheFirstDate_AndAfterTheLast_AreDrawnBlank()
    {
        var cells = Cells(Svg(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025))));

        Assert.Equal(371, cells.Length);
        int[] blank = [.. cells.Select((c, i) => c.Blank ? i : -1).Where(i => i >= 0)];
        // Row-major, 53 columns: Monday and Tuesday of column 0, Thursday to Sunday of column 52.
        Assert.Equal([0, 53, 3 * 53 + 52, 4 * 53 + 52, 5 * 53 + 52, 6 * 53 + 52], blank);
    }

    /// <summary>A missing value (NaN) is a blank day; a zero is a day with a value and wears the bottom of the scale.</summary>
    [Fact]
    public void AMissingValue_IsABlankDay_AndAZeroIsNot()
    {
        DateTime[] dates = [new(2025, 3, 3), new(2025, 3, 4), new(2025, 3, 5)];

        var cells = Cells(Svg(ax => ax.CalendarHeatmap(dates, [double.NaN, 0, 4])));

        Assert.True(cells[0].Blank);
        Assert.False(cells[1].Blank);
        Assert.False(cells[2].Blank);
    }

    /// <summary>Two values on one day are drawn as their sum, whatever their time of day.</summary>
    [Fact]
    public void TwoValuesOnOneDay_AreDrawnAsTheirSum()
    {
        DateTime[] dates = [new(2025, 3, 3, 8, 0, 0), new(2025, 3, 3, 20, 0, 0), new(2025, 3, 4)];

        var heatmap = (HeatmapSeries)AxesOf(ax => ax.CalendarHeatmap(dates, [1, 2, 3])).Series[0];

        Assert.Equal(3, heatmap.Data[0, 0]);
        Assert.Equal(3, heatmap.Data[1, 0]);
    }

    // ---- what else the verb sets, and what it leaves alone ------------------------------------------------------------

    /// <summary>The chart has a colour bar: the cells are read against it.</summary>
    [Fact]
    public void TheChart_HasAColourBar()
    {
        var axes = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025)));

        Assert.True(axes.ColorBar?.Visible);
        Assert.Contains("""<g class="colorbar" """.TrimEnd(), Svg(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025))));
    }

    /// <summary>A colour bar set up before the call stays as it was set up.</summary>
    [Fact]
    public void AColourBarOfYourOwn_StaysYours()
    {
        var axes = AxesOf(ax => ax.WithColorBar(cb => cb with { Label = "Commits" }).CalendarHeatmap(Year2025, Counts(Year2025)));

        Assert.Equal("Commits", axes.ColorBar!.Label);
        Assert.True(axes.ColorBar.Visible);
    }

    /// <summary>The verb sets no axis limits: a limit clips every series on the axes and overwrites the caller's own. The
    /// heatmap's own range already spans its cells.</summary>
    [Fact]
    public void TheVerb_SetsNoAxisLimits()
    {
        var axes = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025)));

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }

    /// <summary>The chart lists nothing in a legend: its one heatmap has no label, and the colour bar is its key.</summary>
    [Fact]
    public void TheChart_ListsNothingInALegend()
    {
        string svg = Svg(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025)).WithLegend(), legendToggle: true);

        Assert.DoesNotContain("data-legend-index=", svg);
    }

    /// <summary>The callback styles the one heatmap, once, after the verb has built it (its cells are there), so what it
    /// sets wins.</summary>
    [Fact]
    public void TheCallback_StylesTheOneHeatmap_OnceAfterItIsBuilt()
    {
        int calls = 0, columnsSeen = 0;
        var axes = AxesOf(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025), configure: s =>
        {
            calls++;
            columnsSeen = s.Data.GetLength(1);
            s.ColorMap = SequentialColorMaps.Greens;
        }));

        Assert.Equal(1, calls);
        Assert.Equal(53, columnsSeen);
        Assert.Same(SequentialColorMaps.Greens, ((HeatmapSeries)axes.Series[0]).ColorMap);
    }

    /// <summary>Written to JSON the chart is its parts (a heatmap with its blank days, the named ticks, the colour bar); read
    /// back it draws the same picture and names the same days and months.</summary>
    [Fact]
    public void ReadBackFromJson_DrawsTheSameCalendar_BlankDaysAndNamesIncluded()
    {
        double[] values = Counts(Year2025);
        values[40] = double.NaN;
        var figure = Plt.Create().WithSize(1100, 300).AddSubPlot(1, 1, 1, ax => ax.CalendarHeatmap(Year2025, values)).Build();

        var back = ChartServices.Serializer.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], YNamesFromTheTop(back.SubPlots[0].YAxis));
        Assert.True(back.SubPlots[0].ColorBar?.Visible);
    }

    /// <summary>The verb draws exactly the picture of the hand chain of public building blocks it replaces (a heatmap of the
    /// grid, the weekday names read from the top down on the row centres, the month names on the centres of the columns of
    /// their 1st, and a colour bar), with and without a missing day.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDrew(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("calendar").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.CalendarByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>The cells tile the plot area at any figure size: every cell is the plot's width over the number of week
    /// columns wide and its height over seven high, so a cell is square only at the figure size that makes those two equal
    /// (measured, not promised: the verb sets no aspect).</summary>
    [Theory]
    [InlineData(1100, 300)]
    [InlineData(800, 500)]
    public void TheCells_TileThePlotArea(int width, int height)
    {
        string svg = Svg(ax => ax.CalendarHeatmap(Year2025, Counts(Year2025)), width, height);

        var plot = PlotArea(svg);
        var cells = Cells(svg);
        Assert.All(cells, c =>
        {
            Assert.Equal(plot.W / 53, c.W, 9);
            Assert.Equal(plot.H / 7, c.H, 9);
        });
        Assert.Equal(plot.X, cells.Min(c => c.X), 9);
        Assert.Equal(plot.X + plot.W, cells.Max(c => c.X + c.W), 6);
        Assert.Equal(plot.Y + plot.H, cells.Max(c => c.Y + c.H), 6);
    }

    // ---- what is refused -------------------------------------------------------------------------------------------

    /// <summary>Every refusal names the parameter: no axes, no dates, no values, no dates at all, values that do not pair up
    /// with the dates, a week start that is not a day of the week.</summary>
    [Fact]
    public void EveryRefusal_NamesTheParameter()
    {
        DateTime[] two = [new(2025, 1, 1), new(2025, 1, 2)];

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).CalendarHeatmap(two, [1, 2])).ParamName);
        Assert.Equal("dates", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.CalendarHeatmap(null!, [1]))).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.CalendarHeatmap(two, null!))).ParamName);
        Assert.Equal("dates", Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.CalendarHeatmap([], []))).ParamName);
        Assert.Equal("values", Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.CalendarHeatmap(two, [1]))).ParamName);
        Assert.Equal("firstDayOfWeek",
            Assert.Throws<ArgumentOutOfRangeException>(() => AxesOf(ax => ax.CalendarHeatmap(two, [1, 2], (DayOfWeek)7))).ParamName);
    }

    /// <summary>A refused call leaves the axes as it was: no heatmap, no ticks, no colour bar.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = AxesOf(ax => Assert.Throws<ArgumentException>(() => ax.CalendarHeatmap([new DateTime(2025, 1, 1)], [1, 2])));

        Assert.Empty(axes.Series);
        Assert.Null(axes.YAxis.TickLocator);
        Assert.Null(axes.XAxis.TickFormatter);
        Assert.Null(axes.ColorBar);
    }
}
