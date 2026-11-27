// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Benchmarks.LiveWall;

/// <summary>How big one live wall is: how many points the live trend holds, how the stacked panels are filled and how many
/// stat tiles sit on the row.</summary>
/// <param name="TrendPoints">Points per line on the live trend: three minutes at one point per quarter second is 720.</param>
/// <param name="Panels">How many stacked line panels the panel figure carries.</param>
/// <param name="LinesPerPanel">How many lines each panel draws.</param>
/// <param name="PointsPerLine">Points per line on a panel: three minutes at one point per half second is 360.</param>
public readonly record struct LiveWallShape(int TrendPoints, int Panels, int LinesPerPanel, int PointsPerLine)
{
    /// <summary>A wall of the size an operations dashboard draws: one live trend, seven panels of three lines, four
    /// log-scale panels, eleven tiles.</summary>
    public static LiveWallShape Operations => new(720, 7, 3, 360);

    /// <summary>The stat tiles on the row.</summary>
    public int Tiles => 11;

    /// <summary>The panels on the log-scale figure.</summary>
    public int LogPanels => 4;

    /// <summary>Points in a tile's sparkline.</summary>
    public int TilePoints => 60;

    /// <summary>Grid points along each side of the live 3-D surface.</summary>
    public int SurfaceSide => 40;
}

/// <summary>One line a rebuilt panel draws.</summary>
public readonly record struct WallLine(string Label, double[] X, double[] Y);

/// <summary>One stat tile: its number, its name and the sparkline under it.</summary>
public readonly record struct WallTile(string Label, double Value, double[] Trend);

/// <summary>
/// A live wall the way a real-time dashboard drives this library: ONE trend figure built once and fed a point per beat on
/// two streaming lines (two Y axes, a date axis, minor ticks, a grid, browser interaction), and three figures BUILT AGAIN
/// on every draw from the data of the moment (stacked line panels, log-scale panels, a row of stat tiles with
/// sparklines, a lit 3-D surface). Every draw ends in a whole SVG document, which is what a server pushes to its viewers.
/// </summary>
/// <remarks>The data is seeded, so two runs of the same shape draw the same pictures and their costs compare.</remarks>
public sealed class LiveWall
{
    /// <summary>How often the live trend gets a point.</summary>
    public static readonly TimeSpan Beat = TimeSpan.FromMilliseconds(250);

    private static readonly TimeSpan PanelStep = TimeSpan.FromMilliseconds(500);
    private static readonly Color GridInk = Color.FromHex("#39424A");
    private const double TenSecondsInDays = 10.0 / 86_400.0;

    private readonly LiveWallShape _shape;
    private readonly Random _random = new(42);
    private readonly StreamingLineSeries _delivered;
    private readonly StreamingLineSeries _traffic;
    private readonly Axes _trendAxes;
    private readonly WallLine[][] _panels;
    private readonly WallLine[][] _logPanels;
    private readonly WallTile[] _tiles;
    private DateTime _now;

    /// <summary>Builds the wall and fills the live trend with a full window, so the first draw is already the steady
    /// state.</summary>
    /// <param name="shape">How big the wall is.</param>
    /// <param name="start">The wall's clock at the first point.</param>
    public LiveWall(LiveWallShape shape, DateTime start)
    {
        _shape = shape;
        _now = start;
        var cycle = Theme.OpsNight.CycleColors;
        StreamingLineSeries? delivered = null;
        StreamingLineSeries? traffic = null;

        Trend = Plt.Create()
            .WithSize(1000, 300)
            .WithTheme(Theme.OpsNight)
            .WithTitle("Live trend")
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax =>
            {
                delivered = ax.StreamingPlot(shape.TrendPoints, s =>
                {
                    s.Label = "Delivered (msg)";
                    s.Color = cycle[0];
                });
                ax.SetYAxisColor(cycle[0])
                  .SetYLabel("Delivered (msg / beat)")
                  .SetXDateAxis()
                  .WithMinorTicks()
                  .SetXMargin(0)
                  .WithGrid(g => g with { Visible = true, Axis = GridAxis.Both, Color = GridInk })
                  .WithAddRightYAxis(right =>
                  {
                      traffic = right.StreamingPlot(shape.TrendPoints, s =>
                      {
                          s.Label = "Traffic (KiB)";
                          s.Color = cycle[1];
                      });
                      right.SetYAxisColor(cycle[1]).SetYLabel("Traffic (KiB / beat)");
                  })
                  .WithLegend(visible: false);
            })
            .WithSubPlotSpacing(sp => sp with { MarginRight = 100, MarginLeft = 110 })
            .Build();

        _delivered = delivered!;
        _traffic = traffic!;
        _trendAxes = Trend.SubPlots[0];
        _trendAxes.XAxis.MinorTicks = _trendAxes.XAxis.MinorTicks with { Spacing = TenSecondsInDays };
        for (var i = 0; i < shape.TrendPoints; i++)
        {
            Advance();
        }

        _panels = Lines(shape.Panels, shape.LinesPerPanel, low: 0, high: 5_000);
        _logPanels = Lines(shape.LogPanels, shape.LinesPerPanel, low: 1, high: 20_000);
        _tiles = new WallTile[shape.Tiles];
        for (var t = 0; t < _tiles.Length; t++)
        {
            var trend = new double[shape.TilePoints];
            for (var i = 0; i < trend.Length; i++)
            {
                trend[i] = 10 + (_random.NextDouble() * 20);
            }
            _tiles[t] = new WallTile($"Tile {t + 1}", trend[^1], trend);
        }
    }

    /// <summary>The live trend: built once, fed by <see cref="Advance"/>.</summary>
    public Figure Trend { get; }

    /// <summary>One beat: a new point on both live lines, the oldest falls out of the ring, and the x-axis is pinned to
    /// the window that now ends at the newest point.</summary>
    public void Advance()
    {
        _now += Beat;
        var x = _now.ToOADate();
        _delivered.AppendPoint(x, 800 + (_random.NextDouble() * 400));
        _traffic.AppendPoint(x, 30 + (_random.NextDouble() * 20));
        _trendAxes.XAxis.Min = (_now - (Beat * _shape.TrendPoints)).ToOADate();
        _trendAxes.XAxis.Max = x;
    }

    /// <summary>The stacked line panels, built from scratch, one panel per unit.</summary>
    /// <param name="legend">Where each panel puts its legend. The wall itself uses the top centre; <see cref="LegendPosition.Best"/>
    /// makes the layout look for the emptiest corner, which is the work the row of that name measures.</param>
    public Figure Panels(LegendPosition legend = LegendPosition.UpperCenter)
    {
        var builder = Plt.Create()
            .WithSize(1000, 190 * _shape.Panels)
            .WithTheme(Theme.OpsNight)
            .WithTitle($"Queues — {_shape.Panels * _shape.LinesPerPanel} series")
            .WithGridSpec(_shape.Panels, 1);
        for (var p = 0; p < _panels.Length; p++)
        {
            var lines = _panels[p];
            var title = $"Panel {p + 1}";
            builder = builder.AddSubPlot(GridPosition.Single(p, 0), ax =>
            {
                foreach (var line in lines)
                {
                    ax.Plot(line.X, line.Y, s => s.Label = line.Label);
                }
                ax.WithTitle(title)
                  .SetYLabel("entries")
                  .SetXDateAxis()
                  .WithGrid(g => g with { Visible = true, Axis = GridAxis.Both, Color = GridInk })
                  .WithLegend(l => l with { Position = legend, FontSize = 9, MarkerScale = 0.55, NCols = 4, FrameAlpha = 0.55 });
            });
        }
        return builder.Build();
    }

    /// <summary>The log-scale panels, built from scratch over a pinned window, each with a caption inside the axes.</summary>
    public Figure LogPanels()
    {
        var from = _now - (PanelStep * _shape.PointsPerLine);
        var builder = Plt.Create()
            .WithSize(1000, 230 * _shape.LogPanels)
            .WithTheme(Theme.OpsNight)
            .WithTitle("Latency — log scale")
            .WithGridSpec(_shape.LogPanels, 1);
        for (var p = 0; p < _logPanels.Length; p++)
        {
            var lines = _logPanels[p];
            var title = $"Latency {p + 1} (µs)";
            builder = builder.AddSubPlot(GridPosition.Single(p, 0), ax =>
            {
                foreach (var line in lines)
                {
                    ax.Plot(line.X, line.Y, s => s.Label = line.Label);
                }
                ax.WithTitle(title)
                  .SetYScale(AxisScale.Log)
                  .SetYLabel("µs (log)")
                  .SetXLim(from.ToOADate(), _now.ToOADate())
                  .SetXDateAxis()
                  .SetXMargin(0)
                  .Annotate("one closed window per point", 0.01, 0.04, a => a.Coordinates = AnnotationCoordinates.AxesFraction)
                  .WithGrid(g => g with { Visible = true, Axis = GridAxis.Both, Color = GridInk })
                  .WithLegend(l => l with { Position = LegendPosition.UpperCenter, FontSize = 9, MarkerScale = 0.55, NCols = 4, FrameAlpha = 0.55 });
            });
        }
        return builder.Build();
    }

    /// <summary>The row of stat tiles, each with its sparkline, built from scratch.</summary>
    public Figure Tiles()
    {
        var dashboard = Plt.OpsDashboard().WithWindow(_now, TimeSpan.FromMinutes(5));
        foreach (var tile in _tiles)
        {
            dashboard.AddTile(tile.Value, t =>
            {
                t.Label = tile.Label;
                t.Caption = "threshold 25 · +3.1\nnewest 2 000 frames";
                t.Format = "0.##";
                t.Trend = tile.Trend;
            });
        }
        return dashboard.Build()
            .WithTheme(Theme.OpsNight)
            .WithSize(1400, 230)
            .Build();
    }

    /// <summary>A lit 3-D surface over a square grid, built from scratch with rotation in the browser — the heaviest kind of
    /// figure a live wall can carry. Its heights are a ripple running outward from the centre, z = sin(r − t) / (1 + r/4),
    /// whose phase t is the wall's own clock, so every draw shows the wave a little further on.</summary>
    public Figure Surface()
    {
        var side = _shape.SurfaceSide;
        var x = new double[side];
        var y = new double[side];
        var z = new double[side, side];
        var half = (side - 1) / 2.0;
        for (var i = 0; i < side; i++)
        {
            x[i] = (i - half) / 2;
            y[i] = (i - half) / 2;
        }
        var phase = (_now - DateTime.UnixEpoch).TotalSeconds % (2 * Math.PI);
        for (var i = 0; i < side; i++)
        {
            for (var j = 0; j < side; j++)
            {
                var r = Math.Sqrt((x[i] * x[i]) + (y[j] * y[j]));
                z[i, j] = Math.Sin(r - phase) / (1 + (r / 4));
            }
        }
        return Plt.Create()
            .WithSize(800, 600)
            .WithTheme(Theme.OpsNight)
            .WithTitle("Live surface")
            .With3DRotation()
            .Surface(x, y, z)
            .WithLighting(0.5, 0.5, 1.0)
            .Build();
    }

    private WallLine[][] Lines(int panels, int linesPerPanel, double low, double high)
    {
        var result = new WallLine[panels][];
        for (var p = 0; p < panels; p++)
        {
            result[p] = new WallLine[linesPerPanel];
            for (var l = 0; l < linesPerPanel; l++)
            {
                var x = new double[_shape.PointsPerLine];
                var y = new double[_shape.PointsPerLine];
                for (var i = 0; i < x.Length; i++)
                {
                    x[i] = (_now - (PanelStep * (x.Length - i))).ToOADate();
                    y[i] = low + (_random.NextDouble() * (high - low));
                }
                result[p][l] = new WallLine($"lane {l + 1}", x, y);
            }
        }
        return result;
    }
}
