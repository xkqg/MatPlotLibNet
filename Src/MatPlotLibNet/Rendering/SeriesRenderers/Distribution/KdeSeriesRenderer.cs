// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="KdeSeries"/> as a smooth Gaussian KDE density curve with optional fill.</summary>
internal sealed class KdeSeriesRenderer : SeriesRenderer<KdeSeries>
{
    /// <inheritdoc />
    public KdeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(KdeSeries series)
    {
        if (series.Data.Length == 0) return;

        var sorted = series.Data.OrderBy(v => v).ToArray();
        double bw = series.Bandwidth ?? GaussianKde.SilvermanBandwidth(sorted);
        var (xs, density) = GaussianKde.Evaluate(sorted, bw);

        var color = ResolveColor(series.Color);

        var points = new List<Point>(xs.Length);
        for (int i = 0; i < xs.Length; i++)
            points.Add(Transform.DataToPixel(xs[i], density[i]));

        if (series.Fill)
        {
            var polygon = new List<Point>(points.Count + 2);
            polygon.AddRange(points);
            polygon.Add(Transform.DataToPixel(xs[^1], 0));
            polygon.Add(Transform.DataToPixel(xs[0], 0));
            Ctx.DrawPolygon(polygon, ApplyAlpha(color, series.Alpha), null, 0);
        }

        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);
    }
}
