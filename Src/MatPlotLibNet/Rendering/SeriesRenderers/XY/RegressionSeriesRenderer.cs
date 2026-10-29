// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="RegressionSeries"/> as a fitted polynomial line with optional confidence band.</summary>
internal sealed class RegressionSeriesRenderer : SeriesRenderer<RegressionSeries>
{
    private const int NumEvalPoints = 100;

    /// <inheritdoc />
    public RegressionSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(RegressionSeries series, MarkObstacles.Builder marks)
    {
        if (Pixels(series, reportFailure: false) is not { } pixels) return;
        if (pixels.Band is not null) marks.AddPath(pixels.Band);
        AddPolyline(marks, pixels.Line);
    }

    /// <summary>The fitted line in pixels, and the outline of its confidence band when it shows one.</summary>
    private readonly record struct RegressionPixels(List<Point>? Band, List<Point> Line);

    /// <summary>Fits the series and works its pixels out: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    /// <param name="series">The series to fit.</param>
    /// <param name="reportFailure">Whether a fit that fails is reported (the draw reports it; the legend's pass only skips).</param>
    /// <returns>The pixels; <see langword="null"/> when there is nothing to fit or the fit fails.</returns>
    private RegressionPixels? Pixels(RegressionSeries series, bool reportFailure)
    {
        // The fit is made over the pairs that can be drawn: a pair with a missing value (NaN) or an infinity is left out,
        // as seaborn drops it (one NaN made every coefficient NaN and the line was not drawn at all).
        var pairs = series.XData.DrawablePairs(series.YData);
        if (pairs.X.Length < 2) return null;

        int degree = Math.Clamp(series.Degree, 0, 10);
        double[] coeff;
        try
        {
            coeff = LeastSquares.PolyFit(pairs.X, pairs.Y, degree);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // LeastSquares.PolyFit documents ArgumentException/ArgumentOutOfRangeException for
            // empty data or an out-of-range degree; IndexOutOfRangeException is the reachable
            // failure at this call site when XData/YData lengths disagree (identical-X data does
            // NOT throw — the normal-equations solver degrades to a degenerate-but-valid fit for
            // singular matrices). Either way: skip this series' regression line, but tell someone.
            if (reportFailure)
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(RegressionSeriesRenderer),
                    $"Regression fit failed and was skipped: {ex.Message}",
                    ex));
            return null;
        }

        double xMin = pairs.X.Min(), xMax = pairs.X.Max();
        double step = (xMax - xMin) / (NumEvalPoints - 1);
        double[] evalX = Enumerable.Range(0, NumEvalPoints).Select(i => xMin + step * i).ToArray();
        double[] evalY = LeastSquares.PolyEval(coeff, evalX);

        List<Point>? band = null;
        if (series.ShowConfidence)
        {
            var interval = LeastSquares.ConfidenceBand(pairs.X, pairs.Y, coeff, evalX, series.ConfidenceLevel);
            band = new List<Point>(NumEvalPoints * 2);
            for (int i = 0; i < NumEvalPoints; i++)
                band.Add(Transform.DataToPixel(evalX[i], interval.Upper[i]));
            for (int i = NumEvalPoints - 1; i >= 0; i--)
                band.Add(Transform.DataToPixel(evalX[i], interval.Lower[i]));
        }

        var line = new List<Point>(NumEvalPoints);
        for (int i = 0; i < NumEvalPoints; i++)
            line.Add(Transform.DataToPixel(evalX[i], evalY[i]));
        return new RegressionPixels(band, line);
    }

    /// <inheritdoc />
    public override void Render(RegressionSeries series)
    {
        if (Pixels(series, reportFailure: true) is not { } pixels) return;

        var color = ResolveColor(series.Color);
        if (pixels.Band is not null)
        {
            var bandColor = series.BandColor ?? color;
            Ctx.DrawPolygon(pixels.Band, new ShapeStyle(ApplyAlpha(bandColor, series.BandAlpha), null, 0));
        }
        Ctx.DrawLines(pixels.Line, Slot.StrokeOf(color, series.LineWidth, series.LineStyle, RegressionSeries.RestingLineWidth));
    }
}
