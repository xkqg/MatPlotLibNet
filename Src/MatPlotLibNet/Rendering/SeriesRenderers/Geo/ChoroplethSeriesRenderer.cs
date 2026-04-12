// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ChoroplethSeries"/> instances, filling each GeoJSON feature
/// with a color derived from a data value mapped through a colormap.</summary>
internal sealed class ChoroplethSeriesRenderer : MapSeriesRenderer
{
    /// <inheritdoc />
    public ChoroplethSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public void Render(ChoroplethSeries series)
    {
        if (series.GeoData is null || series.Values is null) return;

        var features = ExtractFeatures(series.GeoData).ToArray();
        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        double vMin = series.VMin ?? (series.Values.Length > 0 ? series.Values.Min() : 0);
        double vMax = series.VMax ?? (series.Values.Length > 0 ? series.Values.Max() : 1);
        if (Math.Abs(vMax - vMin) < 1e-12) vMax = vMin + 1;

        for (int i = 0; i < features.Length && i < series.Values.Length; i++)
        {
            double t = norm.Normalize(series.Values[i], vMin, vMax);
            var fill = cmap.GetColor(Math.Clamp(t, 0.0, 1.0));
            RenderFeature(features[i], series.Projection, fill, series.EdgeColor, series.LineWidth);
        }
    }
}
