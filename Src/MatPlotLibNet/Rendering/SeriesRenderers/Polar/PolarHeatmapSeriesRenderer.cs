// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarHeatmapSeries"/> as wedge/sector cells on a polar grid.</summary>
internal sealed class PolarHeatmapSeriesRenderer : SeriesRenderer<PolarHeatmapSeries>
{
    private const int ArcSegments = 12;

    /// <inheritdoc />
    public PolarHeatmapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PolarHeatmapSeries series)
    {
        var bounds = Area.PlotBounds;
        var transform = new PolarTransform(bounds, series.RMax);

        var (min, max) = series.GetColorBarRange();
        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        double thetaStep = 2 * Math.PI / series.ThetaBins;
        double rStep = series.RMax / series.RBins;

        for (int tIdx = 0; tIdx < series.ThetaBins; tIdx++)
        {
            double startAngle = tIdx * thetaStep;
            double endAngle = startAngle + thetaStep;

            for (int rIdx = 0; rIdx < series.RBins; rIdx++)
            {
                double val = (tIdx < series.Data.GetLength(0) && rIdx < series.Data.GetLength(1))
                    ? series.Data[tIdx, rIdx]
                    : 0.0;

                double innerR = rIdx * rStep;
                double outerR = innerR + rStep;
                double normalized = norm.Normalize(val, min, max);
                var fillColor = cmap.GetColor(normalized);

                var pts = BuildWedge(transform, startAngle, endAngle, innerR, outerR);
                Ctx.DrawPolygon(pts, fillColor, fillColor, 0);
            }
        }
    }

    private static List<Point> BuildWedge(
        PolarTransform transform,
        double startAngle, double endAngle,
        double innerR, double outerR)
    {
        var pts = new List<Point>(ArcSegments * 2 + 4);

        // Outer arc: startAngle → endAngle
        for (int s = 0; s <= ArcSegments; s++)
        {
            double a = startAngle + (endAngle - startAngle) * s / ArcSegments;
            pts.Add(transform.PolarToPixel(outerR, a));
        }

        // Inner arc: endAngle → startAngle (reversed)
        for (int s = ArcSegments; s >= 0; s--)
        {
            double a = startAngle + (endAngle - startAngle) * s / ArcSegments;
            pts.Add(transform.PolarToPixel(innerR, a));
        }

        return pts;
    }
}
