// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ScatterSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ScatterSeriesRenderer : SeriesRenderer<ScatterSeries>
{
    /// <inheritdoc />
    public ScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ScatterSeries series)
    {
        var defaultColor = ResolveColor(series.Color);

        // When C is set, skip viewport culling — colors are index-tied to data array
        XYData data;
        if (series.C is not null)
            data = new XYData(series.XData, series.YData);
        else
            data = series.MaxDisplayPoints.HasValue
                ? ViewportCuller.Cull(series.XData, series.YData, Transform.DataXMin, Transform.DataXMax)
                : new XYData(series.XData, series.YData);

        // Pre-compute C normalization bounds once
        double cMin = 0, cMax = 1;
        if (series.C is not null)
        {
            cMin = series.VMin ?? series.C.Min();
            cMax = series.VMax ?? series.C.Max();
        }
        var normalizer = series.Normalizer ?? LinearNormalizer.Instance;

        var pts = Transform.TransformBatch(data.X, data.Y);
        for (int i = 0; i < pts.Length; i++)
        {
            BeginTooltip($"x={data.X[i]:G5}, y={data.Y[i]:G5}");
            // matplotlib scatter: s is marker AREA in points².  radius = sqrt(s/π) × (dpi/72).
            // At 100 DPI, (100/72) ≈ 1.389 — converts pt to px at the standard render DPI.
            double s_pt2  = series.Sizes is not null ? series.Sizes[i] : series.MarkerSize;
            double radius  = Math.Sqrt(s_pt2 / Math.PI) * (100.0 / 72.0);
            var c = ResolvePointColor(series, i, defaultColor, normalizer, cMin, cMax);
            var edgeColor = series.EdgeColors is not null && i < series.EdgeColors.Length ? series.EdgeColors[i] : (Color?)null;
            var edgeWidth = series.LineWidths is not null && i < series.LineWidths.Length ? series.LineWidths[i] : 0.0;
            if (series.Marker == MarkerStyle.Square)
                Ctx.DrawRectangle(new Rect(pts[i].X - radius, pts[i].Y - radius, 2 * radius, 2 * radius), c, edgeColor, edgeWidth);
            else
                Ctx.DrawCircle(pts[i], radius, c, edgeColor, edgeWidth);
            EndTooltip();
        }
    }

    private static Color ResolvePointColor(ScatterSeries series, int i, Color defaultColor,
        INormalizer normalizer, double cMin, double cMax)
    {
        // Priority: Colors[] > C+ColorMap > uniform Color
        if (series.Colors is not null && i < series.Colors.Length)
            return series.Colors[i];
        if (series.C is not null && i < series.C.Length && series.ColorMap is not null)
            return series.ColorMap.GetColor(normalizer.Normalize(series.C[i], cMin, cMax));
        return defaultColor;
    }
}
