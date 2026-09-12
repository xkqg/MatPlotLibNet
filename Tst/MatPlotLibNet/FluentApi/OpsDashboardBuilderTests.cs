// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="OpsDashboardBuilder"/> — the composition of a control-room screen.</summary>
public class OpsDashboardBuilderTests
{
    private static readonly DateTime Now = new(2026, 7, 12, 14, 30, 0, DateTimeKind.Utc);

    private static double[] Clock(int seconds) =>
        [.. Enumerable.Range(0, seconds).Select(i => Now.AddSeconds(i - seconds).ToOADate())];

    /// <summary>Tiles, timelines and the trend panel each get their own subplot.</summary>
    [Fact]
    public void EachElement_GetsItsOwnPanel()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTile(187)
            .AddTimeline([new StateSegment(0, 1, "Up", Colors.Tab10Green)])
            .AddTrend([0, 1], [1, 2])
            .Build()
            .Build();

        Assert.Equal(2 + 1 + 1, figure.SubPlots.Count);
    }

    /// <summary>A dashboard with only tiles is legitimate — the tile row is the resting page.</summary>
    [Fact]
    public void TilesAlone_AreEnough()
    {
        var figure = Plt.OpsDashboard().AddTile(15).Build().Build();

        Assert.Single(figure.SubPlots);
    }

    /// <summary>A dashboard with nothing on its top row has nothing to say, and says so.</summary>
    [Fact]
    public void WithoutTiles_ItRefusesToBuild()
    {
        Assert.Throws<InvalidOperationException>(() => Plt.OpsDashboard().Build());
    }

    /// <summary>The window is pinned to EXACT bounds on every time panel — not rounded outward to a nice
    /// number. Rounding the bounds is what makes a rolling axis stand still and then jump a whole step; pinning
    /// them is what turns that lurch into a glide.</summary>
    [Fact]
    public void TheWindow_PinsExactBoundsOnEveryTimePanel()
    {
        var span = TimeSpan.FromMinutes(1);

        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTimeline([new StateSegment(Now.AddMinutes(-1).ToOADate(), Now.ToOADate(), "Up", Colors.Gray)])
            .AddTrend(Clock(60), [.. Enumerable.Repeat(1.0, 60)])
            .WithWindow(Now, span)
            .Build()
            .Build();

        // subplot 0 is the tile; 1 is the timeline; 2 is the trend
        foreach (var axes in figure.SubPlots.Skip(1))
        {
            Assert.Equal((Now - span).ToOADate(), axes.XAxis.Min!.Value, 9);
            Assert.Equal(Now.ToOADate(), axes.XAxis.Max!.Value, 9);
        }
    }

    /// <summary>An ops window is minutes wide, so its ticks must read as TIME. The window used to install a
    /// fixed <c>yyyy-MM-dd</c> format, which printed the same date on every tick of a five-minute screen —
    /// an axis that says nothing. The granularity now follows the window: minutes and hours read HH:mm,
    /// seconds HH:mm:ss, and a multi-day window still reads as dates.</summary>
    [Fact]
    public void TheWindowsTicks_ReadAsTIMEOnAMinutesWideScreen()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTrend(Clock(60), [.. Enumerable.Repeat(1.0, 60)])
            .WithWindow(Now, TimeSpan.FromMinutes(5))
            .Build()
            .Build();

        var trend = figure.SubPlots[^1];
        double[] ticks = trend.XAxis.TickLocator!.Locate(trend.XAxis.Min!.Value, trend.XAxis.Max!.Value);
        string first = trend.XAxis.TickFormatter!.Format(ticks[0]);
        string last = trend.XAxis.TickFormatter!.Format(ticks[^1]);

        Assert.Contains(":", first);            // a time, not a bare date
        Assert.NotEqual(first, last);           // ...and the ticks differ across the window
    }

    /// <summary>The trend panel and the timeline rows share ONE window. If each scaled itself, the rows would
    /// drift apart by a few pixels and an operator reading a fault across them would line up the wrong instants.</summary>
    [Fact]
    public void EveryTimePanel_SharesTheSameWindow()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTimeline([new StateSegment(Now.AddMinutes(-1).ToOADate(), Now.ToOADate(), "Up", Colors.Gray)])
            .AddTimeline([new StateSegment(Now.AddSeconds(-30).ToOADate(), Now.ToOADate(), "Up", Colors.Gray)])
            .AddTrend(Clock(60), [.. Enumerable.Repeat(1.0, 60)])
            .WithWindow(Now, TimeSpan.FromMinutes(1))
            .Build()
            .Build();

        var windows = figure.SubPlots.Skip(1)
            .Select(a => (a.XAxis.Min, a.XAxis.Max))
            .Distinct()
            .ToList();

        Assert.Single(windows);
    }

    /// <summary>Without a window the panels are left to auto-scale — the builder does not invent one, because
    /// it has no clock to invent it from.</summary>
    [Fact]
    public void WithoutAWindow_NoBoundsArePinned()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTrend([0, 1], [1, 2])
            .Build()
            .Build();

        Assert.Null(figure.SubPlots[1].XAxis.Min);
    }

    /// <summary>The normal band is shaded behind the traces, so a deviation is visible without reading the axis.</summary>
    [Fact]
    public void TheNormalBand_IsShadedBehindTheTraces()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTrend([0, 1], [1, 2])
            .WithNormalBand(2200, 2700)
            .Build()
            .Build();

        Assert.Single(figure.SubPlots[1].Spans);
    }

    /// <summary>The title reaches the figure, and the trend configuration callback runs.</summary>
    [Fact]
    public void TitleAndTrendConfiguration_AreApplied()
    {
        string svg = Plt.OpsDashboard()
            .WithTitle("Synapse")
            .AddTile(15)
            .AddTrend([0, 1], [1, 2])
            .ConfigureTrend(ax => ax.SetYLabel("Messages / s"))
            .Build()
            .ToSvg();

        Assert.Contains("Synapse", svg);
        Assert.Contains("Messages / s", svg);
    }

    /// <summary>The library never reads a wall clock: the same builder inputs produce a byte-identical figure
    /// no matter when it is built. A charting library that calls DateTime.Now cannot be tested, cannot replay
    /// history, and cannot render a dashboard for any moment but this one.</summary>
    [Fact]
    public void TheSameInputs_AlwaysProduceTheSameFigure()
    {
        string First() => Plt.OpsDashboard()
            .AddTile(15, t => t.Label = "Buses")
            .AddTrend(Clock(30), [.. Enumerable.Range(0, 30).Select(i => (double)i)])
            .WithWindow(Now, TimeSpan.FromSeconds(30))
            .Build()
            .ToSvg();

        Assert.Equal(First(), First());
    }
    /// <summary>A tile row is EIGHT TILES WIDE and wraps at eight (owner 2026-08-30: <i>"maximum 8 tegels op een
    /// row"</i>; again 2026-09-02 on a nine-tile wall: <i>"wat we hadden is 8 tegels / row"</i>). A row FILLS
    /// before the next one starts — the balanced wrap (9 as 5+4) was tried and rejected: the figure it produces
    /// is narrower than the wall it hangs on, and a page that fits the SVG to its own width then scales that
    /// narrower figure UP, so every card comes out bigger than the one size a card is supposed to have.</summary>
    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(8, 8, 1)]
    [InlineData(9, 8, 2)]
    [InlineData(15, 8, 2)]
    [InlineData(16, 8, 2)]
    [InlineData(17, 8, 3)]
    public void ATileRow_IsEightWide_AndFillsBeforeItWraps(int tiles, int expectedPerRow, int expectedRows)
    {
        var dashboard = Plt.OpsDashboard();
        for (var i = 0; i < tiles; i++)
        {
            dashboard.AddTile(i);
        }

        var figure = dashboard.Build().Build();

        var placed = figure.SubPlots.Where(a => a.GridPosition is not null).Select(a => a.GridPosition!.Value).ToList();
        Assert.Equal(tiles, placed.Count);
        Assert.All(placed, p => Assert.True(p.ColEnd - p.ColStart == 1));
        Assert.Equal(expectedRows, placed.Select(p => p.RowStart).Distinct().Count());
        Assert.Equal(expectedPerRow, placed.Count(p => p.RowStart == 0));
        Assert.True(placed.Max(p => p.ColEnd) <= OpsDashboardBuilder.MaxTilesPerRow);
        // Every row is filled left to right, and no row is wider than the first.
        foreach (var row in placed.GroupBy(p => p.RowStart))
        {
            Assert.Equal(Enumerable.Range(0, row.Count()).ToArray(), row.Select(p => p.ColStart).OrderBy(c => c).ToArray());
        }
    }

    /// <summary>A wrapped tile row grows the FIGURE — the second row of tiles gets its own height instead of
    /// halving the first one's, which is what makes a tile's inline sparkline unreadable.</summary>
    [Fact]
    public void EachExtraTileRow_AddsItsOwnHeight()
    {
        static double Height(int tiles)
        {
            var dashboard = Plt.OpsDashboard();
            for (var i = 0; i < tiles; i++)
            {
                dashboard.AddTile(i);
            }
            return dashboard.Build().Build().Height;
        }

        var one = Height(8);
        var two = Height(9);
        var three = Height(17);

        Assert.True(two > one, "a second tile row needs its own height");
        Assert.Equal(two - one, three - two, 3); // each further row costs exactly the same
    }

    /// <summary>The tiles sit CLOSE together (owner 2026-08-30: <i>"tussen space tussen de tegels mag kleiner"</i>) —
    /// a tile is a card, and the white gutter between cards is what pushes a fifteen-tile wall off the screen.</summary>
    [Fact]
    public void TheTiles_SitTighterThanTheDefaultSubplotGap()
    {
        var figure = Plt.OpsDashboard().AddTile(1).AddTile(2).Build().Build();

        Assert.True(figure.Spacing.HorizontalGap < new MatPlotLibNet.Models.SubPlotSpacing().HorizontalGap,
            "the ops wall tightens the gutter it inherits from the generic figure default");
        Assert.Equal(OpsDashboardBuilder.TileGap, figure.Spacing.HorizontalGap);
    }

    /// <summary>The gutter the tiles are DRAWN with, not the one the builder declared. The test above pins the
    /// value on the figure; this one pins what survives <see cref="FigureBuilder.TightLayout"/>, which recomputes
    /// every gap from what each axes needs for its tick labels. A tile hides its axes entirely
    /// (<c>HideAllAxes</c>), so it needs none of that room — and until this was pinned it was charged for it
    /// anyway: measured 2026-09-01 on the nine-tile wall, cards 218 pt wide with a 57,5 pt gutter, five times the
    /// declared 12. Owner, twice: <i>"de tegels mogen wat breder en de tussenruimte van de tegels mag kleiner"</i>.</summary>
    [Fact]
    public void TheTilesAreDrawn_WithTheGutterTheBuilderDeclared_NotTheOneTicksWouldNeed()
    {
        var figure = Plt.OpsDashboard().AddTile(1).AddTile(2).AddTile(3).Build().WithSize(1400, 260).Build();

        var computed = new MatPlotLibNet.Rendering.Layout.ConstrainedLayoutEngine()
            .Compute(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext());

        Assert.Equal(OpsDashboardBuilder.TileGap, computed.HorizontalGap);
    }

    // ── the topology panel ─────────────────────────────────────────────────────
    //
    // A tile row says WHAT is wrong. A topology panel says WHERE it is wrong and what sits downstream of it.
    // The row it lands on is the whole risk: the trend is the last row and takes the tallest ratio, so a panel
    // appended after it would take that ratio and leave the trend at zero — a panel that renders, occupies no
    // height, and reports no error. The topology therefore sits BETWEEN the timelines and the trend.

    private static readonly IReadOnlyList<GraphNode> Services =
    [
        new("gateway", Label: "gateway"),
        new("orders", Label: "orders"),
        new("payments", Label: "payments", ColorScalar: 0.95),
    ];

    private static readonly IReadOnlyList<GraphEdge> Calls =
    [
        new("gateway", "orders", 2.0, IsDirected: true),
        new("orders", "payments", 1.0, IsDirected: true),
    ];

    [Fact]
    public void ATopologyPanel_GetsItsOwnRow()
    {
        var figure = Plt.OpsDashboard().AddTile(15).AddTopology(Services, Calls).Build().Build();

        Assert.Equal(2, figure.SubPlots.Count);
        Assert.Equal(2, figure.GridSpec!.Rows);
        Assert.Single(figure.SubPlots[1].Series.OfType<NetworkGraphSeries>());
    }

    [Fact]
    public void ATopologyPanel_SitsBetweenTheTimelinesAndTheTrend()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTimeline([new StateSegment(0, 1, "Up", Colors.Tab10Green)])
            .AddTopology(Services, Calls)
            .AddTrend([0, 1], [1, 2])
            .Build()
            .Build();

        Assert.Equal(4, figure.SubPlots.Count);
        Assert.Single(figure.SubPlots[1].Series.OfType<StateTimelineSeries>());
        Assert.Single(figure.SubPlots[2].Series.OfType<NetworkGraphSeries>());
        Assert.Single(figure.SubPlots[3].Series.OfType<LineSeries>());
    }

    [Fact]
    public void ATopologyPanel_DoesNotTakeTheTrendsHeight()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTimeline([new StateSegment(0, 1, "Up", Colors.Tab10Green)])
            .AddTopology(Services, Calls)
            .AddTrend([0, 1], [1, 2])
            .Build()
            .Build();

        var ratios = figure.GridSpec!.HeightRatios!;

        Assert.Equal(4, ratios.Length);
        Assert.All(ratios, ratio => Assert.True(ratio > 0, "every row is given a height: " + string.Join(", ", ratios)));
        Assert.Equal(ratios[2], ratios[3]);   // a panel is a panel, whether it draws a graph or a trace
    }

    [Fact]
    public void ATopologyPanel_HasNoCoordinatesToRead()
    {
        // The numbers on the axes of a service map mean nothing: they are whatever the layout happened to produce.
        var figure = Plt.OpsDashboard().AddTile(15).AddTopology(Services, Calls).Build().Build();

        var panel = figure.SubPlots[1];

        Assert.False(panel.Spines.Left.Visible);
        Assert.False(panel.Spines.Bottom.Visible);
        Assert.False(panel.XAxis.MajorTicks.Visible);
        Assert.False(panel.YAxis.MajorTicks.Visible);
    }

    [Fact]
    public void ATopologyPanel_MakesTheFigureTaller()
    {
        double without = Plt.OpsDashboard().AddTile(15).Build().Build().Height;
        double with = Plt.OpsDashboard().AddTile(15).AddTopology(Services, Calls).Build().Build().Height;

        Assert.True(with > without, $"a panel needs room: {without} -> {with}");
    }

    [Fact]
    public void TheCaller_ConfiguresTheGraphItself()
    {
        // A map that moves between refreshes is a map an operator has to learn again every time, so the seed is
        // the caller's to fix — the builder does not choose it for them.
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTopology(Services, Calls, s => { s.Layout = GraphLayout.ForceDirected; s.LayoutSeed = 42; })
            .Build()
            .Build();

        var graph = figure.SubPlots[1].Series.OfType<NetworkGraphSeries>().Single();

        Assert.Equal(GraphLayout.ForceDirected, graph.Layout);
        Assert.Equal(42, graph.LayoutSeed);
    }

    [Fact]
    public void TwoTopologyPanels_EachGetTheirOwnRow()
    {
        var figure = Plt.OpsDashboard()
            .AddTile(15)
            .AddTopology(Services, Calls)
            .AddTopology(Services, Calls)
            .AddTrend([0, 1], [1, 2])
            .Build()
            .Build();

        Assert.Equal(4, figure.SubPlots.Count);
        Assert.Equal(4, figure.GridSpec!.HeightRatios!.Length);
        Assert.Equal(2, figure.SubPlots.Count(ax => ax.Series.OfType<NetworkGraphSeries>().Any()));
    }
}
