// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarHeatmapSeries"/> as wedge/sector cells on a polar grid.</summary>
internal sealed class PolarHeatmapSeriesRenderer : SeriesRenderer<PolarHeatmapSeries>
{
    /// <inheritdoc />
    public PolarHeatmapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PolarHeatmapSeries series)
    {
        var transform = PolarTransform.For(Context, series);

        var scale = series.ColorScaleUnder(Context.Theme);

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
                var fillColor = scale.ColorOf(val);

                Ctx.DrawPolygon(transform.WedgeOf(innerR, outerR, startAngle, endAngle).Polygon(), ShapeStyle.Filled(fillColor));
            }
        }
    }
}
