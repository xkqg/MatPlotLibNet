// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>A calendar of events: every day of the span a neutral cell, and each event a bar inside the cell of its day, the
/// events of a day side by side, one series for each kind of event.</summary>
public class CalendarEventsTests
{
    private static readonly DateTime[] Dates =
    [
        new(2025, 3, 3), new(2025, 3, 3), new(2025, 3, 12), new(2025, 4, 1), new(2025, 3, 12), new(2025, 3, 3),
    ];

    private static readonly string[] Kinds = ["deploy", "incident", "deploy", "incident", "incident", "deploy"];

    /// <summary>The call chain the <c>CalendarEvents</c> verb replaces, written by hand with the public building blocks and the
    /// numbers worked out by hand: five week columns by seven days, a cell of 0 for each of the thirty days from March 3
    /// (Monday, row 0, column 0) to April 1 (Tuesday, row 1, column 4) and blank after; the marks are bars lifted onto the foot
    /// of their cell's row (row 0 stands 6 to 7, row 1 5 to 6, a tenth clear of each edge), 0.8 tall; the busiest day has two
    /// events, so every mark is 0.8 / 2 * 0.9 wide; the two events of March 3 stand at a quarter and three quarters of the inner
    /// width of column 0, the one of April 1 in the middle of column 4. The weekdays are named on the row centres from the top down, April on column 4. The
    /// verb must draw exactly this picture. It lives here, not with the shared fixtures, because the neutral-grey flag of a
    /// heatmap is internal and the other test projects that read those fixtures cannot see it.</summary>
    private static FigureBuilder ByHand()
    {
        var cells = new double[7, 5];
        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < 5; c++) cells[r, c] = c < 4 || r < 2 ? 0 : double.NaN;
        }

        var labels = new string[5];
        labels[4] = "Apr";
        // The numbers of the layout, written the way they are worked out: a cell is inset 0.1 on every edge, the inner width 0.8
        // is shared by the events of a day, and every mark is a ninth tenth of the narrowest stripe.
        double inner = 1 - 2 * 0.1, stripe = inner / 2, markWidth = inner / 2 * 0.9;
        return Plt.Create().WithSize(900, 300).AddSubPlot(1, 1, 1, ax =>
        {
            ax.AddSeries(new BarSeries(["2025-03-03", "2025-04-01"], [inner, inner])
            {
                XCoordinate = [0 + 0.1 + 0.5 * stripe, 4 + 0.1 + 0.5 * inner], StackBaseline = [6 + 0.1, 5 + 0.1], BarWidth = markWidth, Label = "deploy",
            });
            ax.AddSeries(new BarSeries(["2025-03-03"], [inner]) { XCoordinate = [0 + 0.1 + 1.5 * stripe], StackBaseline = [6 + 0.1], BarWidth = markWidth, Label = "incident" });
            ax.AddSeries(new HeatmapSeries(cells) { FirstCellDate = new DateTime(2025, 3, 3), WearsNeutral = true, ZOrder = -1 });
            ax.SetXTickLocator(new FixedLocator([4.5]))
              .SetXTickFormatter(new CategoryFormatter(labels))
              .SetYTickLocator(new FixedLocator([.. Enumerable.Range(0, 7).Select(r => r + 0.5)]))
              .SetYTickFormatter(new CategoryFormatter(["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"], reversed: true))
              .SetAspect(1);
        });
    }

    private static Figure Of(Action<CalendarEventsParts>? configure = null, DayOfWeek start = DayOfWeek.Monday, Theme? theme = null, bool legend = false)
    {
        var builder = Plt.Create().WithSize(900, 300);
        if (theme is not null) builder = builder.WithTheme(theme);
        // The geometry tests read the marks off the plot: the legend would add its own swatches among them.
        return builder.AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(Dates, Kinds, start, configure).WithLegend(visible: legend)).Build();
    }

    [Fact]
    public void TheChart_IsABarSeriesForEachKind_InOrderOfFirstAppearance_AndOneHeatmapOfDays()
    {
        var axes = Of().SubPlots[0];

        var bars = axes.Series.OfType<BarSeries>().ToArray();
        Assert.Equal(["deploy", "incident"], bars.Select(bar => bar.Label));
        Assert.Equal([3, 3], bars.Select(bar => bar.Values.Length));
        Assert.Single(axes.Series.OfType<HeatmapSeries>());
    }

    [Fact]
    public void TheDays_AreACellEachOverTheSpan_AndBlankOutsideIt()
    {
        var grid = CalendarGrid.Of(Dates, new double[Dates.Length], DayOfWeek.Monday);

        var cells = Of().SubPlots[0].Series.OfType<HeatmapSeries>().Single().Data;

        Assert.Equal(7, cells.GetLength(0));
        Assert.Equal(grid.Columns, cells.GetLength(1));
        int inSpan = 0;
        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < grid.Columns; c++)
            {
                bool day = grid.DateAt(r, c) is not null;
                Assert.Equal(day, !double.IsNaN(cells[r, c]));
                if (day) inSpan++;
            }
        }

        Assert.Equal((Dates.Max() - Dates.Min()).Days + 1, inSpan);
    }

    [Fact]
    public void EveryMark_StandsInsideTheCellOfItsDay()
    {
        var figure = Of();
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);

        var rects = recording.OfKind("DrawRectangle").Select(call => { dynamic a = call.Args!; return ((Rect)a.rect, (Color?)a.fill); }).ToArray();
        var cells = rects.Where(r => r.Item2 is { A: 64 }).Select(r => r.Item1).ToArray();
        // Opaque rectangles narrower than a day: the marks (the figure and axes backgrounds are opaque too, and wider).
        var marks = rects.Where(r => r.Item2 is { A: 255 } && r.Item1.Width < cells.Min(cell => cell.Width)).Select(r => r.Item1).ToArray();

        Assert.NotEmpty(cells);
        Assert.Equal(6, marks.Length);
        Assert.All(marks, mark => Assert.Contains(cells, cell =>
            mark.X >= cell.X - 1e-6 && mark.X + mark.Width <= cell.X + cell.Width + 1e-6
            && mark.Y >= cell.Y - 1e-6 && mark.Y + mark.Height <= cell.Y + cell.Height + 1e-6));
    }

    [Fact]
    public void TwoEventsOnADay_AreTwoDisjointStripes_AndThreeAreThree()
    {
        var figure = Of();
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var marks = recording.OfKind("DrawRectangle").Select(call => { dynamic a = call.Args!; return ((Rect)a.rect, (Color?)a.fill); })
            .Where(r => r.Item2 is { A: 255 } && r.Item1.Width < 20).Select(r => r.Item1).ToArray();

        // March 3 has three events and March 12 two: marks that share a row are, with the one on April 1 in another row, the
        // stripes of a day (the three on March 3 and the two on March 12 stand in the same weekday row but different weeks).
        var byDay = marks.GroupBy(m => Math.Round(m.Y, 6)).Select(g => g.OrderBy(m => m.X).ToArray()).ToArray();

        Assert.Contains(byDay, stripes => stripes.Length == 3);
        Assert.Contains(byDay, stripes => stripes.Length == 2);
        foreach (var stripes in byDay)
        {
            for (int i = 1; i < stripes.Length; i++)
            {
                Assert.True(stripes[i - 1].X + stripes[i - 1].Width < stripes[i].X, "disjoint stripes");
            }
        }
    }

    [Fact]
    public void TheTable_OfAKind_IsOneRowPerEvent_UnderTheDaysIsoDate()
    {
        var deploy = Of().SubPlots[0].Series.OfType<BarSeries>().First(bar => bar.Label == "deploy");

        var table = deploy.ToDataTable()!;

        Assert.Equal(["category", "from", "to"], table.Columns.Select(c => c.Header));
        Assert.Equal(["2025-03-03", "2025-03-12", "2025-03-03"], table.Rows.Select(row => row[0].Text));
    }

    [Fact]
    public void TheLegend_NamesTheKinds_AndTheDaysAreNotInIt()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(Dates, Kinds).WithLegend()).Build().SubPlots[0];

        Assert.Equal(["deploy", "incident"], axes.LegendEntries(directLabelsDrawn: false).Select(entry => entry.Label));
    }

    [Fact]
    public void AnEventWithNoKind_IsDrawnAndHasNoLegendEntry()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents([new DateTime(2025, 1, 1), new DateTime(2025, 1, 2)], ["a", null!]).WithLegend()).Build().SubPlots[0];

        Assert.Equal(["a"], axes.LegendEntries(directLabelsDrawn: false).Select(entry => entry.Label));
        Assert.Equal(2, axes.Series.OfType<BarSeries>().Count());
    }

    [Fact]
    public void TheDays_WearTheThemesNeutralGrey_ThroughATranslucentFill()
    {
        foreach (var theme in new[] { Theme.MatplotlibV2, Theme.Dark })
        {
            var recording = new RecordingRenderContext();
            new ChartRenderer().Render(Of(theme: theme), recording);

            var days = recording.OfKind("DrawRectangle").Select(call => { dynamic a = call.Args!; return (Color?)a.fill; })
                .Where(fill => fill is { A: 64 }).Select(fill => fill!.Value).ToArray();

            Assert.Equal(30, days.Length);   // March 3 to April 1
            Assert.All(days, day => Assert.Equal((theme.Neutral.R, theme.Neutral.G, theme.Neutral.B), (day.R, day.G, day.B)));
        }
    }

    [Fact]
    public void ThePlot_HasSquareCells_AndSetsNoAxisLimits()
    {
        var axes = Of().SubPlots[0];

        Assert.Equal(1.0, axes.Aspect);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }

    [Fact]
    public void TheCallback_GetsTheDaysAndTheKinds_AfterTheVerbBuiltThem()
    {
        CalendarEventsParts seen = default;
        var figure = Of(parts =>
        {
            seen = parts;
            parts.Kinds[0].Color = Color.FromHex("#112233");
        });

        Assert.Equal(2, seen.Kinds.Count);
        Assert.Same(figure.SubPlots[0].Series.OfType<HeatmapSeries>().Single(), seen.Days);
        Assert.Equal(Color.FromHex("#112233"), figure.SubPlots[0].Series.OfType<BarSeries>().First().Color);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of();

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Theory]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Saturday)]
    public void AnyWeekStart_PlacesEveryEventInItsOwnCell(DayOfWeek start)
    {
        var figure = Of(start: start);
        var grid = CalendarGrid.Of(Dates, new double[Dates.Length], start);

        foreach (var bar in figure.SubPlots[0].Series.OfType<BarSeries>())
        {
            for (int i = 0; i < bar.Values.Length; i++)
            {
                var day = DateTime.Parse(bar.Categories[i], System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(grid.ColumnOf(day), (int)Math.Floor(bar.XCoordinate![i]));
            }
        }
    }

    /// <summary>The verb draws exactly the picture of the hand chain of public building blocks it replaces, the numbers of
    /// which are worked out by hand.</summary>
    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ByHand().ToSvg(), ComposedChartShapes.Named("calendar-events").Build(false).ToSvg());
    }

    [Fact]
    public void TheRefusals_NameTheParameter()
    {
        Assert.Equal("dates", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(null!, ["a"])).Build()).ParamName);
        Assert.Equal("kinds", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(Dates, null!)).Build()).ParamName);
        Assert.Equal("kinds", Assert.Throws<ArgumentException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(Dates, ["a"])).Build()).ParamName);
        Assert.Equal("dates", Assert.Throws<ArgumentException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents([], [])).Build()).ParamName);
        Assert.Equal("firstDayOfWeek", Assert.Throws<ArgumentOutOfRangeException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.CalendarEvents(Dates, Kinds, (DayOfWeek)9)).Build()).ParamName);
    }
}
