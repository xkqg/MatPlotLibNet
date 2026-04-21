// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BarSeriesRenderer : SeriesRenderer<BarSeries>
{
    /// <inheritdoc />
    public BarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(BarSeries series)
    {
        var baseColor = ResolveColor(series.Color);
        var fillColor = ApplyAlpha(baseColor, series.Alpha);
        var df = Context.Theme.DefaultFont;
        var labelFont = new Font { Family = df.Family, Size = df.Size, Color = df.Color };
        double edgeWidth = series.LineWidth > 0 ? series.LineWidth : (series.EdgeColor.HasValue ? 1 : 0);
        Color? edgeColor = series.LineWidth > 0 ? (series.EdgeColor ?? baseColor) : series.EdgeColor;

        // Collect label candidates during the bar loop; placing them in one batch after all
        // bars draw lets LabelLayoutEngine shift the few that would otherwise overlap
        // neighbouring bars (dense category axes, narrow bars, long labels).
        var labelCandidates = series.ShowLabels ? new List<LabelCandidate>() : null;

        bool useNumericX = series.XCoordinate is { Length: > 0 };
        for (int i = 0; i < series.Categories.Length; i++)
        {
            // Default (categorical): bar i occupies slot [i, i+1]. Centered body at i+0.5.
            // Numeric X (XCoordinate set): bar center placed at XCoordinate[i], regardless of slot.
            double slotStart = useNumericX ? series.XCoordinate![i] - 0.5 : i;
            double effectiveBarWidth = series.BarGroupWidth ?? series.BarWidth;
            double halfW = effectiveBarWidth / 2;
            double barLeft, barRight;
            if (useNumericX)
            {
                double center = series.XCoordinate![i] + series.BarGroupOffset;
                barLeft = center - halfW;
                barRight = center + halfW;
            }
            else if (series.Align == BarAlignment.Edge)
            {
                barLeft = slotStart + series.BarGroupOffset;
                barRight = slotStart + series.BarGroupOffset + effectiveBarWidth;
            }
            else
            {
                double center = slotStart + 0.5 + series.BarGroupOffset;
                barLeft = center - halfW;
                barRight = center + halfW;
            }

            double baseline = series.StackBaseline is not null ? series.StackBaseline[i] : 0;
            if (series.Orientation == BarOrientation.Vertical)
            {
                var tl = Transform.DataToPixel(barLeft, baseline + Math.Max(series.Values[i], 0));
                var br = Transform.DataToPixel(barRight, baseline + Math.Min(series.Values[i], 0));
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), fillColor, edgeColor, edgeWidth);

                if (labelCandidates is not null)
                {
                    string label = FormatValue(series.Values[i], series.LabelFormat);
                    double barCenterX = (tl.X + br.X) / 2;
                    labelCandidates.Add(new LabelCandidate(
                        new Point(barCenterX, tl.Y - 4), label, labelFont, TextAlignment.Center));
                }
            }
            else
            {
                var tl = Transform.DataToPixel(baseline + Math.Min(series.Values[i], 0), barRight);
                var br = Transform.DataToPixel(baseline + Math.Max(series.Values[i], 0), barLeft);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), fillColor, edgeColor, edgeWidth);

                if (labelCandidates is not null)
                {
                    string label = FormatValue(series.Values[i], series.LabelFormat);
                    double barCenterY = (tl.Y + br.Y) / 2;
                    labelCandidates.Add(new LabelCandidate(
                        new Point(br.X + 4, barCenterY + 4), label, labelFont, TextAlignment.Left));
                }
            }
        }

        if (labelCandidates is { Count: > 0 })
        {
            var placements = LabelLayoutEngine.Place(
                labelCandidates,
                Context!.Area.PlotBounds,
                ChartServices.FontMetrics);
            var leaderColor = Context.Theme.ForegroundText;
            foreach (var p in placements)
            {
                if (p.LeaderLineStart is { } anchor)
                    CalloutBoxRenderer.DrawLeaderLine(Ctx, anchor, p.FinalPoint, leaderColor);
                Ctx.DrawText(p.Text, p.FinalPoint, p.Font, p.Alignment);
            }
        }
    }

    private static string FormatValue(double value, string? format) =>
        format is not null
            ? value.ToString(format, CultureInfo.InvariantCulture)
            : value.ToString("G4", CultureInfo.InvariantCulture);
}
