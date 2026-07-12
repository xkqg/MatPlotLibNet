// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BulletGraphSeries"/> — the qualitative bands behind, the feature bar over them,
/// the target as a perpendicular tick.
/// <para>Painted back-to-front on purpose: the bands are context and must never obscure the measure, and the
/// tick is the thing the eye should land on last and remember first.</para></summary>
internal sealed class BulletGraphSeriesRenderer : SeriesRenderer<BulletGraphSeries>
{
    /// <summary>Fraction of the plot's cross-axis the feature bar occupies. The bands fill the strip; the bar
    /// is deliberately thinner so the band it sits in stays readable around it — that contrast IS the design.</summary>
    private const double BarThickness = 0.34;

    /// <inheritdoc />
    public BulletGraphSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(BulletGraphSeries series)
    {
        var bounds = Area.PlotBounds;

        // No guard on a non-positive upper bound: BulletGraphSeries.UpperBound already substitutes 1 for one,
        // so the branch could never fire. A defensive arm that cannot execute is not safety — it is dead code
        // that dilutes the coverage of the arms that can.
        double upper = series.UpperBound;

        WarnIfBandsAreNotAscending(series);

        Color ink = series.BarColor ?? Context.Theme.ForegroundText;
        Color tick = series.TargetColor ?? Context.Theme.ForegroundText;

        if (series.Orientation == Orientation.Horizontal)
        {
            RenderHorizontal(series, bounds, upper, ink, tick);
        }
        else
        {
            RenderVertical(series, bounds, upper, ink, tick);
        }
    }

    private void RenderHorizontal(BulletGraphSeries series, Rect bounds, double upper, Color ink, Color tick)
    {
        double cy = bounds.Y + bounds.Height / 2;
        double barH = bounds.Height * BarThickness;

        double prev = 0;
        foreach (var band in series.Bands ?? [])
        {
            double x0 = bounds.X + Fraction(prev, upper) * bounds.Width;
            double x1 = bounds.X + Fraction(band.Threshold, upper) * bounds.Width;
            Ctx.DrawRectangle(new Rect(x0, bounds.Y, Math.Max(0, x1 - x0), bounds.Height),
                new ShapeStyle(band.Color, null, 0));
            prev = band.Threshold;
        }

        double barW = Fraction(series.Value, upper) * bounds.Width;
        Ctx.DrawRectangle(new Rect(bounds.X, cy - barH / 2, barW, barH), new ShapeStyle(ink, null, 0));

        if (series.Target is { } target)
        {
            double tx = bounds.X + Fraction(target, upper) * bounds.Width;
            Ctx.DrawLine(new Point(tx, bounds.Y + bounds.Height * 0.15),
                         new Point(tx, bounds.Y + bounds.Height * 0.85),
                         new StrokeStyle(tick, 2.5, LineStyle.Solid));
        }
    }

    private void RenderVertical(BulletGraphSeries series, Rect bounds, double upper, Color ink, Color tick)
    {
        double cx = bounds.X + bounds.Width / 2;
        double barW = bounds.Width * BarThickness;
        double bottom = bounds.Y + bounds.Height;

        double prev = 0;
        foreach (var band in series.Bands ?? [])
        {
            double y0 = bottom - Fraction(prev, upper) * bounds.Height;
            double y1 = bottom - Fraction(band.Threshold, upper) * bounds.Height;
            Ctx.DrawRectangle(new Rect(bounds.X, Math.Min(y0, y1), bounds.Width, Math.Abs(y0 - y1)),
                new ShapeStyle(band.Color, null, 0));
            prev = band.Threshold;
        }

        double barH = Fraction(series.Value, upper) * bounds.Height;
        Ctx.DrawRectangle(new Rect(cx - barW / 2, bottom - barH, barW, barH), new ShapeStyle(ink, null, 0));

        if (series.Target is { } target)
        {
            double ty = bottom - Fraction(target, upper) * bounds.Height;
            Ctx.DrawLine(new Point(bounds.X + bounds.Width * 0.15, ty),
                         new Point(bounds.X + bounds.Width * 0.85, ty),
                         new StrokeStyle(tick, 2.5, LineStyle.Solid));
        }
    }

    /// <summary>Bands whose thresholds do not ascend would paint over each other and read backwards — a chart
    /// that lies quietly. The series still draws what it was handed, but the caller hears about it: a silent
    /// wrong picture is worse than a loud one.</summary>
    private static void WarnIfBandsAreNotAscending(BulletGraphSeries series)
    {
        var bands = series.Bands;
        if (bands is null || bands.Count < 2)
        {
            return;
        }

        for (int i = 1; i < bands.Count; i++)
        {
            if (bands[i].Threshold < bands[i - 1].Threshold)
            {
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(BulletGraphSeriesRenderer),
                    $"Bullet graph bands are not in ascending order (band {i} threshold {bands[i].Threshold} " +
                    $"is below band {i - 1} threshold {bands[i - 1].Threshold}); the bands overpaint each other " +
                    "and the qualitative ranges read backwards.",
                    null));
                return;
            }
        }
    }

    private static double Fraction(double value, double upper) => Math.Clamp(value / upper, 0, 1);
}
