// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
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
        // Bar value labels inherit the theme's default font so they pick up MatplotlibV2/Classic
        // sizes (matplotlib uses font.size = 10 pt for v2). Falls back to a system 11-px font
        // if no theme is in scope.
        var themeFont = Context?.Theme?.DefaultFont;
        var labelFont = themeFont is not null
            ? new Font { Family = themeFont.Family, Size = themeFont.Size, Color = themeFont.Color }
            : new Font { Family = "sans-serif", Size = 11 };
        double edgeWidth = series.LineWidth > 0 ? series.LineWidth : (series.EdgeColor.HasValue ? 1 : 0);
        Color? edgeColor = series.LineWidth > 0 ? (series.EdgeColor ?? baseColor) : series.EdgeColor;

        for (int i = 0; i < series.Categories.Length; i++)
        {
            // Bar i occupies slot [i, i+1].
            // Center alignment: body centered at i+0.5.
            // Edge alignment: bar left edge at i, right edge at i + barWidth.
            double slotStart = i;
            double effectiveBarWidth = series.BarGroupWidth ?? series.BarWidth;
            double halfW = effectiveBarWidth / 2;
            double barLeft, barRight;
            if (series.Align == BarAlignment.Edge)
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

                if (series.ShowLabels)
                {
                    string label = FormatValue(series.Values[i], series.LabelFormat);
                    double barCenterX = (tl.X + br.X) / 2;
                    Ctx.DrawText(label, new Point(barCenterX, tl.Y - 4), labelFont, TextAlignment.Center);
                }
            }
            else
            {
                var tl = Transform.DataToPixel(baseline + Math.Min(series.Values[i], 0), barRight);
                var br = Transform.DataToPixel(baseline + Math.Max(series.Values[i], 0), barLeft);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), fillColor, edgeColor, edgeWidth);

                if (series.ShowLabels)
                {
                    string label = FormatValue(series.Values[i], series.LabelFormat);
                    double barCenterY = (tl.Y + br.Y) / 2;
                    Ctx.DrawText(label, new Point(br.X + 4, barCenterY + 4), labelFont, TextAlignment.Left);
                }
            }
        }
    }

    private static string FormatValue(double value, string? format) =>
        format is not null
            ? value.ToString(format, CultureInfo.InvariantCulture)
            : value.ToString("G4", CultureInfo.InvariantCulture);
}
