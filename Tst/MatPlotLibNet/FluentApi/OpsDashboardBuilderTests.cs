// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
}
