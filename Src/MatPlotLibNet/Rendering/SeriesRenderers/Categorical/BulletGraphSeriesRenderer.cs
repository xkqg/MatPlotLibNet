// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;
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

        Render(series, series.Orientation.RunsAlongX, bounds, upper, ink, tick);
    }

    /// <summary>Draws the bands, the measure and the target along the way the graph runs: along X (horizontal, from the left edge) or
    /// along Y (vertical, from the bottom edge up). Every place along it is where its value's share of the upper bound stands from where
    /// values start (<c>ValueStartAlong</c>), the way a value grows on the screen (<c>ValueGrowthAlong</c>); every place across it is a
    /// share of the strip.</summary>
    private void Render(BulletGraphSeries series, bool alongX, Rect bounds, double upper, Color ink, Color tick)
    {
        double start = bounds.ValueStartAlong(alongX), growth = Rect.ValueGrowthAlong(alongX), length = bounds.LengthAlong(alongX);
        double acrossStart = bounds.StartAlong(!alongX), acrossLength = bounds.LengthAlong(!alongX);
        double At(double value) => start + growth * (Fraction(value, upper) * length);

        double prev = 0;
        foreach (var band in series.Bands ?? [])
        {
            if (band.Threshold.IsMissing()) continue;
            double from = At(prev), to = At(band.Threshold);
            // The band runs between the two thresholds, the same rule in either orientation: a band out of order covers the stretch
            // between them there too, where it was drawn with no width.
            Ctx.DrawRectangle(Rect.Along(alongX, Math.Min(from, to), Math.Abs(to - from), acrossStart, acrossLength),
                ShapeStyle.Filled(band.Color));
            prev = band.Threshold;
        }

        // A measure that cannot be placed (missing, or infinite) has no bar; the bands and the target are still drawn.
        if (series.Value.IsDrawable())
        {
            double reach = Fraction(series.Value, upper) * length, thickness = acrossLength * BarThickness;
            var across = Range1D.Around(bounds.CentreAlong(!alongX), thickness);
            Ctx.DrawRectangle(Rect.Along(alongX, Math.Min(start, start + growth * reach), reach, across.Lo, thickness),
                ShapeStyle.Filled(ink));
        }

        if (series.Target is { } target && target.IsDrawable())
        {
            double at = At(target);
            Ctx.DrawLine(Point.Along(alongX, at, acrossStart + acrossLength * 0.15),
                         Point.Along(alongX, at, acrossStart + acrossLength * 0.85),
                         Slot.StrokeOf(tick, LineWidthRule.OwnMark));
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
                    string.Invariant($"Bullet graph bands are not in ascending order (band {i} threshold {bands[i].Threshold} ") +
                    string.Invariant($"is below band {i - 1} threshold {bands[i - 1].Threshold}); the bands overpaint each other ") +
                    "and the qualitative ranges read backwards.",
                    null));
                return;
            }
        }
    }

    private static double Fraction(double value, double upper) => LinearNormalizer.Instance.Normalize(value, 0, upper);
}
