// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="ResidualSeries"/> as a scatter plot of regression residuals with an optional zero line.</summary>
internal sealed class ResidualSeriesRenderer : SeriesRenderer<ResidualSeries>
{
    /// <inheritdoc />
    public ResidualSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ResidualSeries series)
    {
        // The fit is made over the pairs that can be drawn: a pair with a missing value (NaN) or an infinity is left out,
        // and draws no marker (one NaN made every coefficient and every residual NaN).
        var pairs = series.XData.Data.DrawablePairs(series.YData.Data);
        if (pairs.X.Length == 0) return;

        double[] coeffs;
        try
        {
            coeffs = LeastSquares.PolyFit(pairs.X, pairs.Y, series.Degree);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Same reachable failure as RegressionSeriesRenderer (council fix B8): mismatched
            // XData/YData lengths throw IndexOutOfRangeException mid-fit (identical-X data does
            // NOT throw — the normal-equations solver degrades to a degenerate-but-valid fit for
            // singular matrices). Skip this series' render, but tell someone.
            ChartDiagnostics.Emit(new ChartDiagnostic(
                nameof(ResidualSeriesRenderer),
                $"Residual fit failed and was skipped: {ex.Message}",
                ex));
            return;
        }
        double[] predicted = LeastSquares.PolyEval(coeffs, pairs.X);
        Vec residuals = new Vec(pairs.Y) - new Vec(predicted);

        var color = ResolveColor(series.Color);
        double r = series.MarkerSize / 2.0;

        for (int i = 0; i < pairs.X.Length; i++)
        {
            var px = Transform.DataToPixel(pairs.X[i], residuals[i]);
            if (!px.IsDrawable()) continue;   // a residual the axis cannot place is skipped, as a marker is
            Ctx.DrawCircle(px, r, new ShapeStyle(color, null, 0));
        }

        if (series.ShowZeroLine)
        {
            var left = Transform.DataToPixel(Transform.DataXMin, 0);
            var right = Transform.DataToPixel(Transform.DataXMax, 0);
            Ctx.DrawLine(left, right, new StrokeStyle(color.WithAlpha(128), 1, Styling.LineStyle.Dashed));
        }
    }
}
