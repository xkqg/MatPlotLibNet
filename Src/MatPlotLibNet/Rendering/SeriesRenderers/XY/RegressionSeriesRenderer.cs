// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="RegressionSeries"/> as a fitted polynomial line with optional confidence band.</summary>
internal sealed class RegressionSeriesRenderer : SeriesRenderer<RegressionSeries>
{
    private const int NumEvalPoints = 100;

    /// <inheritdoc />
    public RegressionSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(RegressionSeries series)
    {
        if (series.XData.Length < 2) return;

        int degree = Math.Clamp(series.Degree, 0, 10);
        double[] coeff;
        try { coeff = LeastSquares.PolyFit(series.XData, series.YData, degree); }
        catch { return; }

        double xMin = series.XData.Min(), xMax = series.XData.Max();
        double step = (xMax - xMin) / (NumEvalPoints - 1);
        double[] evalX = Enumerable.Range(0, NumEvalPoints).Select(i => xMin + step * i).ToArray();
        double[] evalY = LeastSquares.PolyEval(coeff, evalX);

        var color = ResolveColor(series.Color);

        if (series.ShowConfidence)
        {
            var (upper, lower) = LeastSquares.ConfidenceBand(series.XData, series.YData, coeff, evalX, series.ConfidenceLevel);
            var bandColor = series.BandColor ?? color;

            var polygon = new List<Point>(NumEvalPoints * 2);
            for (int i = 0; i < NumEvalPoints; i++)
                polygon.Add(Transform.DataToPixel(evalX[i], upper[i]));
            for (int i = NumEvalPoints - 1; i >= 0; i--)
                polygon.Add(Transform.DataToPixel(evalX[i], lower[i]));
            Ctx.DrawPolygon(polygon, ApplyAlpha(bandColor, series.BandAlpha), null, 0);
        }

        var linePoints = new List<Point>(NumEvalPoints);
        for (int i = 0; i < NumEvalPoints; i++)
            linePoints.Add(Transform.DataToPixel(evalX[i], evalY[i]));
        Ctx.DrawLines(linePoints, color, series.LineWidth, series.LineStyle);
    }
}
