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

        for (int i = 0; i < series.Categories.Length; i++)
        {
            // Where the bar stands along its category axis: in its category's slot, or around its numeric x
            // (CategorySlot, the one rule the data range and the category labels read too).
            var slot = series.SlotOf(i);
            double barLeft = slot.Left, barRight = slot.Right;

            double baseline = series.Bottoms is { } bottoms ? bottoms[i] : 0;
            double value = series.Values[i];
            double end = baseline + value;
            // The bar stands on its foot and reaches its end. A foot the axis cannot place (0 on a log axis) is clipped to
            // the edge where the axis begins, as matplotlib clips it; a bar whose end, position or missing foot the axes
            // cannot place is skipped with its label. A rising bar's end is its top, a falling bar's its bottom. The bar is
            // the rectangle between the two corners (Rect.FromCorners), so it keeps a positive size on an axis that runs
            // backwards.
            bool rising = value >= 0;
            if (series.Orientation == BarOrientation.Vertical)
            {
                var tl = rising ? Transform.DataToPixel(barLeft, end) : Transform.DataToPixelClippedY(barLeft, baseline);
                var br = rising ? Transform.DataToPixelClippedY(barRight, baseline) : Transform.DataToPixel(barRight, end);
                var bar = Rect.FromCorners(tl, br);
                if (!bar.IsDrawable()) continue;
                Ctx.DrawRectangle(bar, new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = series.HatchColor });

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
                var tl = rising ? Transform.DataToPixelClippedX(baseline, barRight) : Transform.DataToPixel(end, barRight);
                var br = rising ? Transform.DataToPixel(end, barLeft) : Transform.DataToPixelClippedX(baseline, barLeft);
                var bar = Rect.FromCorners(tl, br);
                if (!bar.IsDrawable()) continue;
                Ctx.DrawRectangle(bar, new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = series.HatchColor });

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
                FontMetricsDefault.Current);
            var leaderColor = Context.Theme.ForegroundText;
            foreach (var p in placements)
            {
                if (p.LeaderLineStart is { } anchor)
                    CalloutBoxRenderer.DrawLeaderLine(Ctx, anchor, p.FinalPoint, leaderColor);
                Ctx.DrawText(p.Text, p.FinalPoint, p.Font, p.Alignment);
            }
        }
    }

    /// <summary>The label of a bar that names no format: up to six significant digits, trailing zeros dropped, an
    /// exponent only below 1e-4 or from 1e6 — matplotlib's <c>bar_label</c> default, <c>'%g'</c>. .NET's <c>G6</c> picks
    /// the same digits and switches to an exponent at the same place; it writes the exponent as <c>E+06</c> where
    /// <c>'%g'</c> writes <c>e+06</c>, so the marker is lowered.</summary>
    private const string DefaultLabelFormat = "G6";

    private static string FormatValue(double value, string? format) =>
        format is not null
            ? value.ToString(format, CultureInfo.InvariantCulture)
            : value.ToString(DefaultLabelFormat, CultureInfo.InvariantCulture).Replace('E', 'e');
}
