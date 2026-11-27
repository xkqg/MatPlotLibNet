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
    internal override void AddMarks(ResidualSeries series, MarkObstacles.Builder marks)
    {
        if (Residuals(series, reportFailure: false) is not { } residuals) return;
        foreach (var point in residuals) marks.AddPoint(point);
    }

    /// <summary>The pixel of every residual that can be drawn: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    /// <param name="series">The series to fit.</param>
    /// <param name="reportFailure">Whether a fit that fails is reported (the draw reports it; the legend's pass only skips).</param>
    /// <returns>The pixels; <see langword="null"/> when there is nothing to fit or the fit fails.</returns>
    private List<Point>? Residuals(ResidualSeries series, bool reportFailure)
    {
        // The fit is made over the pairs that can be drawn: a pair with a missing value (NaN) or an infinity is left out,
        // and draws no marker (one NaN made every coefficient and every residual NaN).
        var pairs = series.XData.Data.DrawablePairs(series.YData.Data);
        if (pairs.X.Length == 0) return null;

        Vec residuals;
        try
        {
            double[] coeffs = LeastSquares.PolyFit(pairs.X, pairs.Y, series.Degree);
            double[] predicted = LeastSquares.PolyEval(coeffs, pairs.X);
            residuals = new Vec(pairs.Y) - new Vec(predicted);
        }
        catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
        {
            // Same reachable failure as RegressionSeriesRenderer (council fix B8): mismatched
            // XData/YData lengths throw IndexOutOfRangeException mid-fit, or ArgumentException where the
            // residual is taken against the prediction (identical-X data does NOT throw — the
            // normal-equations solver degrades to a degenerate-but-valid fit for singular matrices).
            // Skip this series' render, but tell someone.
            if (reportFailure)
                ChartDiagnostics.Emit(new ChartDiagnostic(
                    nameof(ResidualSeriesRenderer),
                    $"Residual fit failed and was skipped: {ex.Message}",
                    ex));
            return null;
        }

        var points = new List<Point>(pairs.X.Length);
        for (int i = 0; i < pairs.X.Length; i++)
        {
            var px = Transform.DataToPixel(pairs.X[i], residuals[i]);
            if (px.IsDrawable()) points.Add(px);   // a residual the axis cannot place is skipped, as a marker is
        }
        return points;
    }

    /// <inheritdoc />
    public override void Render(ResidualSeries series)
    {
        if (Residuals(series, reportFailure: true) is not { } points) return;

        var theme = Context.Theme;
        var color = ColorOf(series);
        double r = series.DiameterUnder(theme, Slot) / 2.0;
        // seaborn's regression scatter: its colour at its opacity on the face and, where the dot look outlines it, on an edge as wide as
        // the theme's marker edge (lines.markeredgewidth).
        var face = color.WithOpacityGiven(series.OpacityUnder(theme));
        var dot = theme.Looks.Dots.RegressionEdged && theme.Looks.Markers.LineEdge is { } edge
            ? ShapeStyle.Edged(face, face, edge.EdgeWidth)
            : ShapeStyle.Filled(face);
        foreach (var point in points)
            Ctx.DrawCircle(point, r, dot);

        // The zero line is seaborn's ax.axhline(0): across the plot from the left edge to the right whatever the X axis' scale,
        // at the height of 0, placed as a horizontal reference line is (the height of its value at the low end of the X range), in the
        // theme's guide line (SeabornVerbLook.ResidualZeroLine). A log Y axis puts 0 a thousand decades below the plot and a limit the
        // caller set can leave it above or below: then it is not drawn.
        if (series.ShowZeroLine && Transform.DataToPixel(Transform.UnscaledXMin, 0).Y is var zeroY && zeroY.IsDrawable()
            && Transform.PlotBounds.HorizontalAcross(zeroY) is { } zero)
            Ctx.DrawLine(zero.From, zero.To, theme.Looks.Verbs.ResidualZeroLine.StrokeIn(Slot, color));
    }
}
