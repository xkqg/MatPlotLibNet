// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="ResidualSeries"/> as a scatter plot of regression residuals with an optional zero line.</summary>
internal sealed class ResidualSeriesRenderer : SeriesRenderer<ResidualSeries>
{
    /// <inheritdoc />
    public ResidualSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ResidualSeries series)
    {
        if (series.XData.Length == 0) return;

        double[] coeffs = LeastSquares.PolyFit(series.XData, series.YData, series.Degree);
        double[] predicted = LeastSquares.PolyEval(coeffs, series.XData);
        Vec residuals = series.YData - new Vec(predicted);

        var color = ResolveColor(series.Color);
        double r = series.MarkerSize / 2.0;

        for (int i = 0; i < series.XData.Length; i++)
        {
            var px = Transform.DataToPixel(series.XData[i], residuals[i]);
            Ctx.DrawCircle(px, r, color, null, 0);
        }

        if (series.ShowZeroLine)
        {
            var left = Transform.DataToPixel(Transform.DataXMin, 0);
            var right = Transform.DataToPixel(Transform.DataXMax, 0);
            Ctx.DrawLine(left, right, color.WithAlpha(128), 1, Styling.LineStyle.Dashed);
        }
    }
}
