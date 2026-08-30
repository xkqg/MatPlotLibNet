// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>Composes a single-screen operations dashboard: a row of KPI tiles, a stack of state timelines, and
/// a shared trend panel — all sharing one time window.
///
/// <para><b>Why a builder and not another template method.</b> The concepts here keep arriving: tiles,
/// timelines, trends, a window, a normal band. Each new one added to a positional signature ripples through
/// every call site that ever used it, and a five-parameter call is already unreadable at the point of use. A
/// composition API is exactly where the fluent canon earns its keep — you add a step, and nothing that existed
/// before it changes.</para>
///
/// <para><b>One window, one owner.</b> <see cref="WithWindow"/> pins the same time span on the trend panel and
/// on every timeline row. That is not a convenience: if each panel scales itself, the rows drift apart by a few
/// pixels and an operator reading a fault across them lines up the wrong instants. The window has a single
/// owner, and it is this builder.</para>
///
/// <para><b>The caller owns the clock.</b> The window is <c>(end, span)</c>, not <c>(span)</c> — the library
/// never reads a wall clock. A charting library that calls <c>DateTime.Now</c> cannot be tested deterministically,
/// cannot replay history, and cannot render a dashboard for a moment other than this one.</para></summary>
public sealed class OpsDashboardBuilder
{
    private readonly List<OpsTileSpec> _tiles = [];
    private readonly List<OpsTimelineSpec> _timelines = [];
    private readonly List<OpsTrendSpec> _trends = [];

    /// <summary>The widest a tile row gets before it wraps onto another row. Eight is what a control-room screen
    /// reads at a glance; past it the tiles narrow until the number inside stops being scannable.</summary>
    public const int MaxTilesPerRow = 8;

    /// <summary>The gutter between two tiles, in points — tighter than the generic figure default, which is sized
    /// for axes that carry tick labels between them.</summary>
    public const double TileGap = 12;

    /// <summary>What a further tile row costs the figure, in points (the tile row's own height, 220, is in the base).</summary>
    private const double TileRowHeight = 150;

    private string? _title;
    private DateTime? _windowEnd;
    private TimeSpan _windowSpan;
    private (double Low, double High)? _band;
    private Action<AxesBuilder>? _configureTrend;

    /// <summary>Sets the figure title.</summary>
    /// <param name="title">The title.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Pins the rolling time window shared by the trend panel and every timeline row.
    /// <para>The <b>caller</b> supplies the end instant; the library never reads a clock of its own. Pass
    /// <c>DateTime.UtcNow</c> from a live dashboard, or any past instant to render history — the figure is the
    /// same either way, which is what makes it testable and replayable.</para></summary>
    /// <param name="end">The right edge of the window — "now", as the caller understands it.</param>
    /// <param name="span">How much history the window shows.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder WithWindow(DateTime end, TimeSpan span)
    {
        _windowEnd = end;
        _windowSpan = span;
        return this;
    }

    /// <summary>Shades the normal operating band behind the trend traces, so a deviation is visible without
    /// anyone reading the axis.</summary>
    /// <param name="low">Lower bound of the normal band.</param>
    /// <param name="high">Upper bound of the normal band.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder WithNormalBand(double low, double high)
    {
        _band = (low, high);
        return this;
    }

    /// <summary>Adds a KPI tile to the top row.</summary>
    /// <param name="value">The headline value.</param>
    /// <param name="configure">Optional configuration of the tile.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder AddTile(double value, Action<StatTileSeries>? configure = null)
    {
        _tiles.Add(new OpsTileSpec(value, configure));
        return this;
    }

    /// <summary>Adds a state-timeline row beneath the tiles.</summary>
    /// <param name="segments">The row's coloured state segments.</param>
    /// <param name="configure">Optional configuration of the timeline.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder AddTimeline(IReadOnlyList<StateSegment> segments,
        Action<StateTimelineSeries>? configure = null)
    {
        _timelines.Add(new OpsTimelineSpec(segments, configure));
        return this;
    }

    /// <summary>Adds a trace to the shared trend panel.</summary>
    /// <param name="x">X values — clock instants as OLE Automation dates when a window is pinned.</param>
    /// <param name="y">Y values.</param>
    /// <param name="configure">Optional configuration of the line.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder AddTrend(double[] x, double[] y, Action<LineSeries>? configure = null)
    {
        _trends.Add(new OpsTrendSpec(x, y, configure));
        return this;
    }

    /// <summary>Further configures the trend axes after the traces are added.</summary>
    /// <param name="configure">The configuration to apply.</param>
    /// <returns>This builder for chaining.</returns>
    public OpsDashboardBuilder ConfigureTrend(Action<AxesBuilder> configure)
    {
        _configureTrend = configure;
        return this;
    }

    /// <summary>Composes the dashboard.</summary>
    /// <returns>A <see cref="FigureBuilder"/> the caller can theme, size and render.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no tile has been added — a dashboard with
    /// nothing on its top row has nothing to say.</exception>
    public FigureBuilder Build()
    {
        if (_tiles.Count == 0)
        {
            throw new InvalidOperationException(
                "An ops dashboard needs at least one tile: the tile row is what an operator reads first.");
        }

        // A tile row has a maximum WIDTH: past it the row wraps, BALANCED (9 tiles read as 5+4, never 8+1 — a lone
        // tile on a second row is a layout accident an operator reads as a category). Owner 2026-08-30, on a
        // fifteen-tile wall: "maximum 8 tegels op een row".
        int tileRows = ((_tiles.Count - 1) / MaxTilesPerRow) + 1;
        int columns = ((_tiles.Count - 1) / tileRows) + 1;
        int trendRows = _trends.Count > 0 ? 1 : 0;
        int rows = tileRows + _timelines.Count + trendRows;

        var heightRatios = new double[rows];
        for (int i = 0; i < tileRows; i++)
        {
            heightRatios[i] = 0.8;
        }

        for (int i = 0; i < _timelines.Count; i++)
        {
            heightRatios[tileRows + i] = 1.0;
        }

        if (trendRows > 0)
        {
            heightRatios[^1] = 1.6;
        }

        var figure = Plt.Create()
            .WithSize(1200, 220 + ((tileRows - 1) * TileRowHeight) + (_timelines.Count * 90) + (trendRows * 320))
            .WithGridSpec(rows, columns, heightRatios: heightRatios)
            .TightLayout()
            // A tile is a CARD, and the gutter between cards is what pushes a wide wall off the screen. The generic
            // figure gap is sized for axes with tick labels between them; tiles carry none (owner: "tussen space
            // tussen de tegels mag kleiner").
            .WithSubPlotSpacing(sp => sp with { HorizontalGap = TileGap });

        if (_title is not null)
        {
            figure.WithTitle(_title);
        }

        for (int i = 0; i < _tiles.Count; i++)
        {
            var spec = _tiles[i];
            int column = i % columns;
            int tileRow = i / columns;
            figure.AddSubPlot(GridPosition.Single(tileRow, column), ax =>
            {
                ax.StatTile(spec.Value, spec.Configure);
                ax.HideAllAxes();
                ax.WithLegend(visible: false);
            });
        }

        for (int i = 0; i < _timelines.Count; i++)
        {
            var spec = _timelines[i];
            int row = tileRows + i;
            figure.AddSubPlot(new GridPosition(row, row + 1, 0, columns), ax =>
            {
                var timeline = ax.StateTimeline(spec.Segments, spec.Configure);
                PinWindow(ax);
                ax.HideTopSpine();
                ax.HideRightSpine();
                ax.WithLegend(visible: false);
            });
        }

        if (trendRows > 0)
        {
            int row = rows - 1;
            figure.AddSubPlot(new GridPosition(row, row + 1, 0, columns), ax =>
            {
                if (_band is { } band)
                {
                    ax.AxHSpan(band.Low, band.High, s => s.Alpha = 0.08);
                }

                foreach (var trend in _trends)
                {
                    ax.Plot(trend.X, trend.Y, trend.Configure);
                }

                PinWindow(ax);
                ax.WithLegend();
                _configureTrend?.Invoke(ax);
            });
        }

        return figure;
    }

    /// <summary>Pins the shared window on a time axis: EXACT bounds, ROUND ticks.
    /// <para>The distinction is the whole reason a rolling chart reads well or badly. The ticks should sit on
    /// round clock instants and glide leftwards. The <i>bounds</i> must not be rounded — an auto axis expands
    /// them outward to the nearest nice number, which makes the axis stand perfectly still while the data grows
    /// into it and then jump a whole step at once. Pinning the bounds and letting the locator round only the
    /// ticks is what turns that lurch into a glide.</para></summary>
    private void PinWindow(AxesBuilder ax)
    {
        if (_windowEnd is not { } end)
        {
            return;
        }

        ax.SetXLim((end - _windowSpan).ToOADate(), end.ToOADate());
        // The granularity follows the WINDOW: an ops screen is minutes wide, and a fixed yyyy-MM-dd printed the
        // same date on every tick of it — an axis that says nothing. The auto locator/formatter pair reads the
        // visible range and picks minutes, hours or days accordingly.
        ax.SetXDateAxis();
    }

    private readonly record struct OpsTileSpec(double Value, Action<StatTileSeries>? Configure);

    private readonly record struct OpsTimelineSpec(
        IReadOnlyList<StateSegment> Segments,
        Action<StateTimelineSeries>? Configure);

    private readonly record struct OpsTrendSpec(double[] X, double[] Y, Action<LineSeries>? Configure);
}
