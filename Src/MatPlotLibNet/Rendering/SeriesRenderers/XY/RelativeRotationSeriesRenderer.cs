// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="RelativeRotationSeries"/> as a 2D scatter of (RS-Ratio, RS-Momentum)
/// per asset with a fading tail, optional quadrant fills, and a 100/100 crosshair.</summary>
/// <remarks>Quadrant fills use the StockCharts canonical palette (green/yellow/red/blue at low
/// opacity). The fading tail alpha ramps from 0.2 (oldest) to 1.0 (most recent head).
/// One <see cref="IRenderContext.DrawLine"/> call per tail segment so each segment can have
/// its own alpha. <see cref="IRenderContext.SetOpacity"/> is reset to 1.0 after each asset.</remarks>
internal sealed class RelativeRotationSeriesRenderer : SeriesRenderer<RelativeRotationSeries>
{
    // Quadrant fill colours (very low alpha so the series remains readable on top).
    private static readonly Color LeadingColor   = new(0,   180, 0,   20);  // +/+ green
    private static readonly Color WeakeningColor = new(230, 200, 0,   20);  // +/- yellow
    private static readonly Color LaggingColor   = new(200, 0,   0,   20);  // -/- red
    private static readonly Color ImprovingColor = new(0,   100, 200, 20);  // -/+ blue
    private static readonly Color CrosshairColor = new(100, 100, 100, 120);

    private const double CrosshairThickness = 0.5;
    private const double HeadRadius         = 5.0;
    private const double TailThickness      = 1.2;
    private const double LabelFontSize      = 9.0;

    // ENB radius: ENB=1 → ~1.5px, ENB=5 → ~7.5px (ENB * sqrt(7/π) ≈ ENB * 1.49).
    private static double EnbToRadius(double enb) => Math.Max(1.5, enb * 1.5);
    // Absorption colormap: low absorption (safe) = green, high (panic) = red.
    private static readonly IColorMap AbsorptionCmap =
        new ReversedColorMap(DivergingColorMaps.RdYlGn);

    /// <inheritdoc />
    public RelativeRotationSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(RelativeRotationSeries series)
    {
        var rsData = series.ComputeRsData();

        // Determine the visible window: union of last TailLength valid points across assets.
        // Axes scaling is driven by ComputeDataRange; we just paint inside the current area.

        if (series.ShowQuadrantGrid)
            DrawQuadrantGrid();

        var cmap = series.ColorMap ?? QualitativeColorMaps.Tab10;

        for (int a = 0; a < rsData.Length; a++)
        {
            var (rsRatio, rsMom) = rsData[a];
            double hue = rsData.Length > 1 ? (double)a / (rsData.Length - 1) : 0.0;
            var assetColor = cmap.GetColor(Math.Clamp(hue, 0.0, 1.0));
            DrawAsset(series, rsRatio, rsMom, assetColor, series.AssetLabels[a]);
        }
    }

    private void DrawQuadrantGrid()
    {
        // Convert the centrepoint (100, 100) to pixel space.
        var center = Transform.DataToPixel(100.0, 100.0);

        // Draw four faint quadrant fills.
        var b = Area.PlotBounds;

        // Leading: top-right (+/+)
        Ctx.DrawRectangle(
            new Rect(center.X, b.Y, b.Right - center.X, center.Y - b.Y),
            LeadingColor, null, 0);

        // Weakening: bottom-right (+/-)
        Ctx.DrawRectangle(
            new Rect(center.X, center.Y, b.Right - center.X, b.Bottom - center.Y),
            WeakeningColor, null, 0);

        // Lagging: bottom-left (-/-)
        Ctx.DrawRectangle(
            new Rect(b.X, center.Y, center.X - b.X, b.Bottom - center.Y),
            LaggingColor, null, 0);

        // Improving: top-left (-/+)
        Ctx.DrawRectangle(
            new Rect(b.X, b.Y, center.X - b.X, center.Y - b.Y),
            ImprovingColor, null, 0);

        // Crosshair lines.
        Ctx.DrawLine(
            new Point(b.X, center.Y), new Point(b.Right, center.Y),
            CrosshairColor, CrosshairThickness, LineStyle.Dashed);
        Ctx.DrawLine(
            new Point(center.X, b.Y), new Point(center.X, b.Bottom),
            CrosshairColor, CrosshairThickness, LineStyle.Dashed);
    }

    private void DrawAsset(
        RelativeRotationSeries series,
        double[] rsRatio, double[] rsMom,
        Color color, string label)
    {
        // Collect the last TailLength valid (pixel, time-index) pairs, oldest first.
        int tail = series.TailLength;
        var points = new List<(Point Pixel, int T)>(tail);

        for (int t = rsRatio.Length - 1; t >= 0 && points.Count < tail; t--)
        {
            if (double.IsNaN(rsRatio[t]) || double.IsNaN(rsMom[t])) continue;
            points.Insert(0, (Transform.DataToPixel(rsRatio[t], rsMom[t]), t));
        }

        if (points.Count == 0) return;

        bool hasAbsorption = series.AbsorptionRatioPerBar is not null;
        bool hasEnb        = series.EnbPerBar is not null;

        if (hasAbsorption || hasEnb)
        {
            // ── Overlay mode: gray ghost trail + per-point colored dots ────────
            // Gray ghost trail (fading).
            for (int i = 0; i < points.Count - 1; i++)
            {
                double alpha = 0.2 + 0.8 * ((double)i / (points.Count - 1));
                Ctx.SetOpacity(alpha);
                Ctx.DrawLine(points[i].Pixel, points[i + 1].Pixel,
                    new Color(160, 160, 160), TailThickness, LineStyle.Solid);
            }

            // Per-point dots.
            bool isLast = false;
            for (int i = 0; i < points.Count; i++)
            {
                isLast = i == points.Count - 1;
                var (pixel, t) = points[i];
                double alpha = isLast ? 1.0 : 0.2 + 0.8 * ((double)i / (points.Count - 1));
                Ctx.SetOpacity(alpha);

                Color fill = color;
                if (hasAbsorption)
                {
                    double ratio = Math.Clamp(series.AbsorptionRatioPerBar![t], 0.0, 1.0);
                    fill = AbsorptionCmap.GetColor(ratio);
                }

                double radius = hasEnb
                    ? EnbToRadius(series.EnbPerBar![t]) * (isLast ? 1.5 : 1.0)
                    : HeadRadius * (isLast ? 1.0 : 0.6);

                double stroke = isLast ? 1.5 : 0.5;
                Ctx.DrawCircle(pixel, radius, fill, color, strokeThickness: stroke);
            }
        }
        else
        {
            // ── Default mode: fading trail lines + head dot ──────────────────
            for (int i = 0; i < points.Count - 1; i++)
            {
                double alpha = 0.2 + 0.8 * ((double)i / (points.Count - 1));
                Ctx.SetOpacity(alpha);
                Ctx.DrawLine(points[i].Pixel, points[i + 1].Pixel, color, TailThickness, LineStyle.Solid);
            }
            Ctx.SetOpacity(1.0);
            Ctx.DrawCircle(points[^1].Pixel, HeadRadius, color, Colors.Black, strokeThickness: 0.5);
        }

        // Label always at head, full opacity.
        Ctx.SetOpacity(1.0);
        var head = points[^1].Pixel;
        Ctx.DrawText(label, new Point(head.X + HeadRadius + 2.0, head.Y),
            new Font { Family = "sans-serif", Size = LabelFontSize, Color = Colors.Black },
            TextAlignment.Left);
    }
}
