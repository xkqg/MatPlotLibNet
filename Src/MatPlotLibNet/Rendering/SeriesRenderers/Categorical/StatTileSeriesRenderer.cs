// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StatTileSeries"/> — a headline number with its label, an optional gap caption,
/// and an optional inline trend, filling the plot area it is given.
/// <para>A bare big number is a failed dashboard pattern: without a comparative, a reader cannot tell whether
/// it is good or bad. So the tile is drawn as four things — the value, the label, the gap in words, and the
/// trend that says which way it is going.</para></summary>
internal sealed class StatTileSeriesRenderer : SeriesRenderer<StatTileSeries>
{
    /// <summary>Vertical share of the tile given to the inline sparkline, measured from the bottom.</summary>
    private const double TrendShare = 0.22;

    /// <inheritdoc />
    public StatTileSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(StatTileSeries series)
    {
        var bounds = Area.PlotBounds;

        // A tile at rest wears the theme's NEUTRAL shade — never a colour from the series cycle. The cycler
        // exists to tell data series apart; a state mark is not a data series. Letting a resting tile take a
        // cycle colour is how a wall ends up in five cheerful hues, and then the one tile that actually needs
        // attention has nothing to stand out against. Colour appears only when the caller sets an accent, and
        // an accent means something is wrong.
        var color = series.AccentColor ?? Context.Theme.Alarm.Resting;

        // "No information" is a pattern, not a colour: a silent source is not a broken one.
        if (series.Hatch != HatchPattern.None)
        {
            Ctx.DrawRectangle(bounds, new ShapeStyle(Context.Theme.AxesBackground, null, 0)
            {
                Hatch = series.Hatch,
                HatchColor = series.HatchColor
            });
        }

        bool hasTrend = series.Trend is { Count: >= 2 };
        double bodyHeight = hasTrend ? bounds.Height * (1 - TrendShare) : bounds.Height;
        double cx = bounds.X + bounds.Width / 2;
        double cy = bounds.Y + bodyHeight / 2;

        Ctx.DrawText(series.FormattedValue, new Point(cx, cy),
            new Font { Size = 44, Weight = FontWeight.Bold, Color = color }, TextAlignment.Center);

        double below = cy + 30;
        if (!string.IsNullOrEmpty(series.Label))
        {
            Ctx.DrawText(series.Label, new Point(cx, below), new Font { Size = 14 }, TextAlignment.Center);
            below += 18;
        }

        // The gap line — the element that answers "is this good or bad" without anyone doing arithmetic.
        if (!string.IsNullOrEmpty(series.Caption))
        {
            Ctx.DrawText(series.Caption, new Point(cx, below),
                new Font { Size = 11, Color = color }, TextAlignment.Center);
        }

        if (hasTrend)
        {
            RenderTrend(series, bounds, color, bodyHeight);
        }
    }

    /// <summary>Draws the inline trend by DELEGATING to <see cref="SparklineSeriesRenderer"/> in a sub-area of
    /// the tile.
    /// <para>The sparkline renderer already knows how to draw an axis-less, frame-less line that fills the area
    /// it is handed — and <see cref="SeriesRenderer"/> exposes that area as an overridable seam precisely so a
    /// composite can hand it a smaller one. Re-implementing the polyline here would be a second copy of the
    /// same twelve lines, and the copy that nobody maintains is the one that drifts.</para></summary>
    private void RenderTrend(StatTileSeries series, Rect bounds, Color color, double bodyHeight)
    {
        double top = bounds.Y + bodyHeight;
        var strip = new Rect(bounds.X + bounds.Width * 0.1, top,
                             bounds.Width * 0.8, Math.Max(1, bounds.Height - bodyHeight - 4));

        var subContext = Context with { Area = new RenderArea(strip, Area.Context) };

        var sparkline = new SparklineSeries([.. series.Trend!]) { Color = series.TrendColor ?? color };
        new SparklineSeriesRenderer(subContext).Render(sparkline);
    }
}
