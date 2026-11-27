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

    /// <summary>The fitted line in pixels, and the outline of its confidence band when it shows one, each cut where it leaves the
    /// plot towards a value a log axis cannot place.</summary>
    private readonly record struct RegressionPixels(IReadOnlyList<Point>? Band, IReadOnlyList<Point> Line);

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
        catch (IndexOutOfRangeException ex)
        {
            // PolyFit's refusals (no data, a degree outside 0..10) cannot reach it here; X data longer than its Y reads past the Y
            // (identical X does not throw: the solver degrades to a valid fit). Skip this series' line, but tell someone.
            if (reportFailure)
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(RegressionSeriesRenderer),
                    $"Regression fit failed and was skipped: {ex.Message}",
                    ex));
            return null;
        }

        double xMin = pairs.X.Min(), xMax = pairs.X.Max();
        double[] evalX = new EqualWidthBins(xMin, xMax, NumEvalPoints - 1).Edges();
        double[] evalY = LeastSquares.PolyEval(coeff, evalX);

        IReadOnlyList<Point>? band = null;
        if (series.ShowConfidence)
        {
            var interval = LeastSquares.ConfidenceBand(pairs.X, pairs.Y, coeff, evalX, series.ConfidenceLevel);
            var outline = new List<Point>(NumEvalPoints * 2);
            for (int i = 0; i < NumEvalPoints; i++)
                outline.Add(Transform.DataToPixelUnclipped(evalX[i], interval.Upper[i]));
            for (int i = NumEvalPoints - 1; i >= 0; i--)
                outline.Add(Transform.DataToPixelUnclipped(evalX[i], interval.Lower[i]));
            band = Transform.CutBeyondTheStart(outline, closed: true);
        }

        return new RegressionPixels(band, Transform.TransformPath(evalX, evalY));
    }

    /// <inheritdoc />
    public override void Render(RegressionSeries series)
    {
        if (Pixels(series, reportFailure: true) is not { } pixels) return;

        var color = ColorOf(series);
        if (pixels.Band is not null)
        {
            var bandColor = series.BandColor ?? color;
            Ctx.DrawPolygon(pixels.Band, ShapeStyle.Filled(bandColor.WithOpacityGiven(series.BandAlpha)));
        }
        DrawSeriesLine(pixels.Line, series, color);
    }
}
