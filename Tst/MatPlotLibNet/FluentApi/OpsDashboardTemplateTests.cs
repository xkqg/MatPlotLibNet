// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Dashboard;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="FigureTemplates.OpsDashboard"/> composition and output.</summary>
public class OpsDashboardTemplateTests
{
    private static readonly OpsTile[] Tiles =
    [
        new("Messages/s", 2400, "0"),
        new("Lag", 0.3, "0.0' s'"),
        new("Active", 11, "0' of '12"),
        new("Errors", 0, "0"),
        new("Dropped/s", 0, "0")
    ];

    private static readonly StateSegment[] Segments =
    [
        new(0, 30, "Up", Colors.Tab10Green),
        new(30, 45, "Degraded", Colors.Tab10Orange),
        new(45, 120, "Up", Colors.Tab10Green)
    ];

    private static readonly OpsStateTimeline[] Timelines =
    [
        new("Bus", Segments),
        new("Exchange", Segments)
    ];

    private static readonly double[] X = [0.0, 1, 2, 3, 4];
    private static readonly double[] Y1 = [10.0, 12, 11, 14, 13];
    private static readonly double[] Y2 = [9.0, 11, 10, 13, 12];

    private static readonly OpsTrendLine[] TrendLines =
    [
        new("Publish", X, Y1),
        new("Consume", X, Y2)
    ];

    /// <summary>OpsDashboard returns a non-null FigureBuilder.</summary>
    [Fact]
    public void OpsDashboard_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines);
        Assert.NotNull(builder);
    }

    /// <summary>OpsDashboard produces the expected number of subplots.</summary>
    [Fact]
    public void OpsDashboard_HasExpectedSubplotCount()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines).Build();
        // 5 tiles + 2 timelines + 1 trend panel = 8
        Assert.Equal(8, figure.SubPlots.Count);
    }

    /// <summary>OpsDashboard without timelines or trends still builds.</summary>
    [Fact]
    public void OpsDashboard_Minimal_HasOnlyTiles()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles).Build();
        Assert.Equal(Tiles.Length, figure.SubPlots.Count);
    }

    /// <summary>Timelines without trend lines: the trend row is omitted entirely.</summary>
    [Fact]
    public void OpsDashboard_TimelinesWithoutTrends_OmitsTrendRow()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, Timelines).Build();

        Assert.Equal(Tiles.Length + Timelines.Length, figure.SubPlots.Count);
        Assert.Empty(figure.SubPlots.SelectMany(ax => ax.Series.OfType<LineSeries>()));
        Assert.Equal(Timelines.Length, figure.SubPlots.Sum(ax => ax.Series.OfType<StateTimelineSeries>().Count()));
    }

    /// <summary>Trend lines without timelines: the trend row sits directly under the tiles.</summary>
    [Fact]
    public void OpsDashboard_TrendsWithoutTimelines_HasTilesAndTrendRow()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, timelines: null, trendLines: TrendLines).Build();

        Assert.Equal(Tiles.Length + 1, figure.SubPlots.Count);
        Assert.Empty(figure.SubPlots.SelectMany(ax => ax.Series.OfType<StateTimelineSeries>()));
        Assert.Equal(TrendLines.Length, figure.SubPlots.Sum(ax => ax.Series.OfType<LineSeries>().Count()));
    }

    /// <summary>Tile label and format reach the StatTileSeries; without a threshold the accent stays unset.</summary>
    [Fact]
    public void OpsDashboard_PropagatesTileLabelAndFormat_AndLeavesAccentUnset()
    {
        var figure = FigureTemplates.OpsDashboard([new OpsTile("Lag", 0.3, "0.0' s'")]).Build();
        var statTile = figure.SubPlots[0].Series.OfType<StatTileSeries>().Single();

        Assert.Equal("Lag", statTile.Label);
        Assert.Equal("0.0' s'", statTile.Format);
        Assert.Null(statTile.AccentColor);
    }

    /// <summary>The tile row contains StatTileSeries instances.</summary>
    [Fact]
    public void OpsDashboard_ContainsStatTiles()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines).Build();
        int tileCount = figure.SubPlots.Sum(ax => ax.Series.OfType<StatTileSeries>().Count());
        Assert.Equal(Tiles.Length, tileCount);
    }

    /// <summary>The timeline rows contain StateTimelineSeries instances.</summary>
    [Fact]
    public void OpsDashboard_ContainsStateTimelines()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines).Build();
        int timelineCount = figure.SubPlots.Sum(ax => ax.Series.OfType<StateTimelineSeries>().Count());
        Assert.Equal(Timelines.Length, timelineCount);
    }

    /// <summary>The trend panel contains LineSeries instances.</summary>
    [Fact]
    public void OpsDashboard_ContainsTrendLines()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines).Build();
        int lineCount = figure.SubPlots.Sum(ax => ax.Series.OfType<LineSeries>().Count());
        Assert.Equal(TrendLines.Length, lineCount);
    }

    /// <summary>The title is applied when provided.</summary>
    [Fact]
    public void OpsDashboard_SetsTitle()
    {
        var figure = FigureTemplates.OpsDashboard(Tiles, title: "Bus Dashboard").Build();
        Assert.Equal("Bus Dashboard", figure.Title);
    }

    /// <summary>Accent threshold maps a value to a colour.</summary>
    [Fact]
    public void OpsDashboard_AppliesAccentThreshold()
    {
        var tile = new OpsTile("Errors", 5, "0")
        {
            AccentThreshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 1, 3)
        };

        var figure = FigureTemplates.OpsDashboard([tile]).Build();
        var statTile = figure.SubPlots[0].Series.OfType<StatTileSeries>().Single();
        Assert.Equal(Colors.Red, statTile.AccentColor);
    }

    /// <summary>Threshold helper returns the OK colour below the warning value.</summary>
    [Fact]
    public void OpsTile_Threshold_BelowWarning_ReturnsOk()
    {
        var threshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 1, 3);
        Assert.Null(threshold(0));
    }

    /// <summary>A non-null OK colour is returned below the warning value (not silently dropped).</summary>
    [Fact]
    public void OpsTile_Threshold_BelowWarning_ReturnsSuppliedOkColour()
    {
        var threshold = OpsTile.Threshold(Colors.Tab10Green, Colors.Orange, Colors.Red, 1, 3);
        Assert.Equal(Colors.Tab10Green, threshold(0));
    }

    /// <summary>Threshold helper returns the warning colour between warning and critical.</summary>
    [Fact]
    public void OpsTile_Threshold_BetweenWarningAndCritical_ReturnsWarning()
    {
        var threshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 1, 3);
        Assert.Equal(Colors.Orange, threshold(1));
        Assert.Equal(Colors.Orange, threshold(2));
    }

    /// <summary>Threshold helper returns the critical colour at or above the critical value.</summary>
    [Fact]
    public void OpsTile_Threshold_AtOrAboveCritical_ReturnsCritical()
    {
        var threshold = OpsTile.Threshold(null, Colors.Orange, Colors.Red, 1, 3);
        Assert.Equal(Colors.Red, threshold(3));
        Assert.Equal(Colors.Red, threshold(5));
    }

    /// <summary>The optional trend configuration callback is invoked.</summary>
    [Fact]
    public void OpsDashboard_ConfigureTrend_IsInvoked()
    {
        string svg = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines,
            configureTrend: ax => ax.SetYLabel("Throughput")).ToSvg();
        Assert.Contains("Throughput", svg);
    }

    /// <summary>OpsDashboard renders to valid SVG containing the title and labels.</summary>
    [Fact]
    public void OpsDashboard_RendersToValidSvg()
    {
        string svg = FigureTemplates.OpsDashboard(Tiles, Timelines, TrendLines, title: "Ops").ToSvg();
        Assert.Contains("<svg", svg);
        Assert.Contains("Ops", svg);
        Assert.Contains("Messages/s", svg);
        Assert.Contains("Publish", svg);
    }

    /// <summary>OpsDashboard throws when no tiles are supplied.</summary>
    [Fact]
    public void OpsDashboard_Throws_WhenTilesEmpty()
    {
        Assert.Throws<ArgumentException>(() => FigureTemplates.OpsDashboard([]));
    }

    /// <summary>OpsDashboard throws the precise null-argument exception when tiles is null.</summary>
    [Fact]
    public void OpsDashboard_Throws_WhenTilesNull()
    {
        Assert.Throws<ArgumentNullException>(() => FigureTemplates.OpsDashboard(null!));
    }
}
