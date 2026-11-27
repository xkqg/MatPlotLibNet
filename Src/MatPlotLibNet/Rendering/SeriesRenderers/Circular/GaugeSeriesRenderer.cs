// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="GaugeSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class GaugeSeriesRenderer : SeriesRenderer<GaugeSeries>
{
    /// <summary>How far in from the dial's rim a band reaches, as a share of the radius.</summary>
    private const double BandInnerShare = 0.7;

    /// <summary>How far below the hub the baseline of the value stands, in pixels.</summary>
    private const double ValueBelowHub = 20;

    /// <inheritdoc />
    public GaugeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(GaugeSeries series)
    {
        var bounds = Area.PlotBounds;
        var frame = RoundFrame.In(bounds, RoundFrame.DiscShare, 0, RoundFrame.DialCentreDown);
        double cx = frame.Centre.X, cy = frame.Centre.Y;
        double radius = frame.Radius;
        double range = series.Max - series.Min;
        // A scale that cannot be laid out (a missing or infinite end, or no span) has nothing to draw on.
        if (!range.IsPositiveFinite()) return;
        var ranges = series.Ranges ?? [new(60, Colors.Green), new(80, Colors.Amber), new(100, Colors.Red)];

        double prevAngle = Math.PI;
        foreach (var (threshold, color) in ranges)
        {
            // A band whose upper end is missing has no place on the dial: it is skipped, as a pie's missing slice is.
            if (threshold.IsMissing()) continue;
            double frac = LinearNormalizer.Instance.Normalize(threshold, series.Min, series.Max);
            double endAngle = Math.PI - frac * Math.PI;
            Ctx.DrawPolygon(RingWedge.CounterClockwise(frame.Centre, radius * BandInnerShare, radius, prevAngle, endAngle).Polygon(), ShapeStyle.Filled(color));
            prevAngle = endAngle;
        }
        // A value that cannot be placed (missing, or infinite) has no needle, hub or number: the bands alone are drawn.
        if (!series.Value.IsDrawable()) return;
        double valFrac = LinearNormalizer.Instance.Normalize(series.Value, series.Min, series.Max);
        double needleAngle = Math.PI - valFrac * Math.PI;
        Ctx.DrawLine(new Point(cx, cy), new Point(cx, cy).AtAngleCounterClockwise(radius * 0.85, needleAngle), Slot.StrokeOf(series.NeedleColor, LineWidthRule.OwnMark));
        Ctx.DrawCircle(new Point(cx, cy), 4, ShapeStyle.Filled(series.NeedleColor));
        Ctx.Draw(TextLabel.Of(series.Value.InvariantText("G5")), Context.Theme.DataTextFont(series.TextSizeUnder(Context.Theme), weight: FontWeight.Bold), new TextPlacement(new Point(cx, cy + ValueBelowHub), TextAlignment.Center));
    }
}
