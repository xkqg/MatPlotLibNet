// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>The control room is the figure the data table has to be right for: a row of tiles, timelines on a
/// time window, a trend panel. Measured on the cookbook's own dashboard before these tests existed, the tables
/// were three anonymous one-row grids for three tiles, a timeline whose start and end printed as OLE day
/// numbers, and a trend whose 30-second samples collapsed onto the same minute — a reader who cannot see the
/// wall would have known less than the wall shows. Each test here says what that reader is owed.</summary>
public class OpsDashboardDataTablesTests
{
    private static readonly DateTime Now = new(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc);

    private static double At(int secondsBeforeNow) => Now.AddSeconds(-secondsBeforeNow).ToOADate();

    private static Figure Dashboard()
    {
        var theme = Theme.OpsNight;
        double[] t = [At(300), At(270), At(240), At(210), At(180)];
        return Plt.OpsDashboard()
            .WithTitle("Synapse - federation")
            .WithWindow(Now, TimeSpan.FromMinutes(5))
            .AddTile(15, tile => { tile.Label = "Buses"; tile.Caption = "all 15 normal"; })
            .AddTile(28.1, tile =>
            {
                tile.Label = "RFx p99";
                tile.Target = 25;
                tile.Caption = "target 25 ms · +3.1 over";
                tile.Trend = [24.1, 24.8, 25.3, 26, 27.2, 28.1];
            })
            .AddTile(0, tile => { tile.Label = "Exchange"; tile.Caption = "no contact"; tile.Hatch = HatchPattern.ForwardDiagonal; })
            .AddTimeline(
                [
                    new StateSegment(At(300), At(180), "normal", theme.Alarm.Resting),
                    new StateSegment(At(180), At(100), "degraded", theme.Alarm.Warning),
                    new StateSegment(At(100), At(0), "normal", theme.Alarm.Resting),
                ], timeline => timeline.Label = "Exchange feed")
            .AddTrend(t, [2310, 2380, 2450, 2290, 2510], s => s.Label = "msg/s")
            .Build()
            .WithTheme(theme)
            .Build();
    }

    /// <summary>A tile row is ONE table — a row per tile — not a one-row table per tile. Every tile sits in its
    /// own subplot, which is a layout fact, not a data fact; the reader wants the row the operator sees.</summary>
    [Fact]
    public void ATileRow_IsOneTable_WithARowPerTile()
    {
        var tiles = Dashboard().ToDataTables()[0];

        Assert.Equal("Synapse - federation", tiles.Caption);
        Assert.Equal(["label", "value", "target", "caption", "trend"], tiles.Columns.Select(c => c.Header));
        Assert.Equal(["Buses", "RFx p99", "Exchange"], tiles.Rows.Select(r => r[0].Text));
    }

    /// <summary>A tile carries value, target, caption and trend; a bare number is the failed pattern the tile
    /// exists to avoid, and the table must not reintroduce it.</summary>
    [Fact]
    public void ATile_TablesItsTargetCaptionAndTrend()
    {
        var row = Dashboard().ToDataTables()[0].Rows[1];

        Assert.Equal(28.1, row[1].Number);
        Assert.Equal(25.0, row[2].Number);
        Assert.Equal("target 25 ms · +3.1 over", row[3].Text);
        Assert.Equal("24.1, 24.8, 25.3, 26, 27.2, 28.1", row[4].Text);
    }

    /// <summary>What a tile does not have is empty, not zero and not a fake: an empty cell is the honest shape.</summary>
    [Fact]
    public void ATileWithoutATargetOrTrend_LeavesThoseCellsEmpty()
    {
        var row = Dashboard().ToDataTables()[0].Rows[0];

        Assert.Equal(DataCellKind.Empty, row[2].Kind);
        Assert.Equal("all 15 normal", row[3].Text);
        Assert.Equal(DataCellKind.Empty, row[4].Kind);
    }

    /// <summary>A timeline on a time window is read in time: its start and end are OLE dates because the axis
    /// is a date axis, and the table must say 07:58:20, not 46277.332.</summary>
    [Fact]
    public void ATimelineOnTheWindow_ReadsStartAndEndAsTimes()
    {
        var timeline = Dashboard().ToDataTables()[1];

        Assert.Equal(["state", "start", "end"], timeline.Columns.Select(c => c.Header));
        Assert.Equal(DataColumnKind.Date, timeline.Columns[1].Kind);
        Assert.Equal(DataColumnKind.Date, timeline.Columns[2].Kind);
        Assert.Contains("| degraded | 2026-09-12 07:57 | 2026-09-12 07:58:20 |", timeline.ToMarkdown());
    }

    /// <summary>A subplot with one series is captioned with that series' label when the label is not already a
    /// column header — otherwise five tables under one dashboard read as five copies of the same heading.</summary>
    [Fact]
    public void ASingleSeriesSubplot_IsCaptionedWithItsSeriesLabel()
    {
        var tables = Dashboard().ToDataTables();

        Assert.Equal("Synapse - federation — Exchange feed", tables[1].Caption);
        // The trend's label IS its value header, so the caption does not say it twice.
        Assert.Equal("Synapse - federation", tables[2].Caption);
    }

    /// <summary>The trend's x is time, its samples are 30 seconds apart, and the table keeps the seconds; a
    /// column that says 07:55 twice has lost the one thing that told the two rows apart. The column keeps the
    /// name the series gave it — the ops window sets a date axis and no label, and inventing "time" here would
    /// be the table naming something the chart does not.</summary>
    [Fact]
    public void ATrendOnTheWindow_KeepsTheSeconds()
    {
        var trend = Dashboard().ToDataTables()[2];

        Assert.Equal(["x", "msg/s"], trend.Columns.Select(c => c.Header));
        Assert.Equal(DataColumnKind.Date, trend.Columns[0].Kind);
        Assert.Equal(["2026-09-12 07:55", "2026-09-12 07:55:30", "2026-09-12 07:56"],
            trend.Rows.Take(3).Select(r => ChartDataTable.CellText(r[0], trend.Columns[0].Kind)));
    }

    /// <summary>A subplot that names itself keeps its own table. The united row drops the per-table suffix,
    /// which for an untitled tile is nothing and for a titled subplot would be the only thing that said which
    /// panel a number came from.</summary>
    [Fact]
    public void TitledOneRowSubplots_AreNotSwallowedIntoARow()
    {
        var figure = Plt.Create().WithTitle("Two panels")
            .AddSubPlot(2, 1, 1, ax => { ax.WithTitle("North"); ax.AddSeries(new StatTileSeries(5) { Label = "Rate" }); })
            .AddSubPlot(2, 1, 2, ax => { ax.WithTitle("South"); ax.AddSeries(new StatTileSeries(9) { Label = "Rate" }); })
            .Build();

        var tables = figure.ToDataTables();

        Assert.Equal(2, tables.Count);
        Assert.Equal(["Two panels — North — Rate", "Two panels — South — Rate"], tables.Select(t => t.Caption));
    }

    /// <summary>Two categorical one-row series on one axes — two bullet graphs — are one table with two rows,
    /// because each row already names itself; two tables would name nothing more.</summary>
    [Fact]
    public void TwoBulletGraphsOnOneAxes_AreOneTable()
    {
        var figure = Plt.Create().WithTitle("Targets")
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.AddSeries(new BulletGraphSeries(5) { Label = "Latency", Target = 8 });
                ax.AddSeries(new BulletGraphSeries(3) { Label = "Errors" });
            })
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(["label", "value", "target"], table.Columns.Select(c => c.Header));
        Assert.Equal(["Latency", "Errors"], table.Rows.Select(r => r[0].Text));
        Assert.Equal(DataCellKind.Empty, table.Rows[1][2].Kind);
    }
}
