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
        // Collect the last TailLength valid (x, y) pixel points.
        int tail = series.TailLength;
        var points = new List<Point>(tail);

        for (int t = rsRatio.Length - 1; t >= 0 && points.Count < tail; t--)
        {
            if (double.IsNaN(rsRatio[t]) || double.IsNaN(rsMom[t])) continue;
            points.Insert(0, Transform.DataToPixel(rsRatio[t], rsMom[t]));
        }

        if (points.Count == 0) return;

        // Draw fading tail segments (oldest to newest, alpha 0.2 → 1.0).
        for (int i = 0; i < points.Count - 1; i++)
        {
            double alpha = 0.2 + 0.8 * ((double)i / (points.Count - 1));
            Ctx.SetOpacity(alpha);
            Ctx.DrawLine(points[i], points[i + 1], color, TailThickness, LineStyle.Solid);
        }

        // Draw head dot at full opacity.
        Ctx.SetOpacity(1.0);
        var head = points[^1];
        Ctx.DrawCircle(head, HeadRadius, color, Colors.Black, strokeThickness: 0.5);

        // Label to the right of head.
        Ctx.DrawText(label, new Point(head.X + HeadRadius + 2.0, head.Y),
            new Font { Family = "sans-serif", Size = LabelFontSize, Color = Colors.Black },
            TextAlignment.Left);
    }
}
