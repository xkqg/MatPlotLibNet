// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class ScatterSeriesRenderer : SeriesRenderer<ScatterSeries>
{
    public ScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

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
            double size = series.Sizes is not null ? Math.Sqrt(series.Sizes[i]) : Math.Sqrt(series.MarkerSize);
            var c = ResolvePointColor(series, i, defaultColor, normalizer, cMin, cMax);
            var edgeColor = series.EdgeColors is not null && i < series.EdgeColors.Length ? series.EdgeColors[i] : (Color?)null;
            var edgeWidth = series.LineWidths is not null && i < series.LineWidths.Length ? series.LineWidths[i] : 0.0;
            Ctx.DrawCircle(pts[i], size / 2, c, edgeColor, edgeWidth);
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
