// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ErrorBarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ErrorBarSeriesRenderer : SeriesRenderer<ErrorBarSeries>
{
    /// <inheritdoc />
    public ErrorBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(ErrorBarSeries series, MarkObstacles.Builder marks)
    {
        var pixels = Pixels(series);
        for (int i = 0; i < pixels.Centre.Length; i++)
        {
            var center = new Point(pixels.X[i], pixels.Centre[i]);
            if (!center.IsDrawable()) continue;
            if (!Transform.LiesBeyond(center)) marks.AddPoint(center);
            if (series.ErrorEvery > 1 && i % series.ErrorEvery != 0) continue;
            var top = new Point(pixels.X[i], pixels.Top[i]);
            var bottom = new Point(pixels.X[i], pixels.Bottom[i]);
            if (Transform.CutSegment(bottom, top) is { } bar) marks.AddPath([bar.From, bar.To]);
            if (HorizontalBar(series, i) is { } across) marks.AddPath([across.From, across.To]);
        }
    }

    /// <summary>Where the points and the ends of their error bars stand in pixels: X, the point, its top and its bottom, a value
    /// a log axis cannot place a thousand decades beyond it, as matplotlib's log scale puts it.</summary>
    private readonly record struct ErrorBarPixels(double[] X, double[] Centre, double[] Top, double[] Bottom);

    /// <summary>Works the error bars' pixels out (batch-transformed, SIMD): the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    private ErrorBarPixels Pixels(ErrorBarSeries series)
    {
        int n = series.XData.Length;
        var topYData = new double[n];
        var botYData = new double[n];
        for (int i = 0; i < n; i++) { topYData[i] = series.YData[i] + series.YErrorHigh[i]; botYData[i] = series.YData[i] - series.YErrorLow[i]; }
        return new ErrorBarPixels(Transform.TransformXUnclipped(series.XData), Transform.TransformYUnclipped(series.YData),
            Transform.TransformYUnclipped(topYData), Transform.TransformYUnclipped(botYData));
    }

    /// <summary>The horizontal error bar of point <paramref name="i"/>, its ends placed as matplotlib's log scale places them,
    /// cut where it leaves the plot through the edge a log axis begins at (<see cref="DataTransform.CutSegment"/>):
    /// <see langword="null"/> when the series has none or nothing of it is on the plot.</summary>
    private PixelSegment? HorizontalBar(ErrorBarSeries series, int i) =>
        series.XErrorLow is { } low && series.XErrorHigh is { } high
            ? Transform.SegmentBetween(series.XData[i] - low[i], series.YData[i], series.XData[i] + high[i], series.YData[i])
            : null;

    /// <summary>An end of a horizontal error bar in pixels, where matplotlib's log scale places it.</summary>
    private Point EndOf(double x, double y) => Transform.DataToPixelUnclipped(x, y);

    /// <summary>Whether a cap stands at <paramref name="end"/>: an end on a number, not a thousand decades beyond a log axis,
    /// where matplotlib's cap is never seen.</summary>
    private bool HasCapAt(Point end) => end.IsDrawable() && !Transform.LiesBeyond(end);

    /// <inheritdoc />
    public override void Render(ErrorBarSeries series)
    {
        var color = series is { Color: null, WearsNeutral: true } ? Context.Theme.Neutral : ColorOf(series);
        int n = series.XData.Length;
        // An error bar has no dash pattern or marker of its own (its bars are solid, its point a dot), so the slot decides
        // only the width, which the bars and caps take unless the series names theirs.
        double lineWidth = Slot.LineWidthOf(series);
        double eLineWidth = series.ELineWidth ?? lineWidth;
        double capThick = series.CapThick ?? lineWidth;
        double cap = series.CapSizeUnder(Context.Theme);   // a cap that reaches nowhere is not drawn

        var (pxCenter, pyCenterArr, pyTop, pyBot) = Pixels(series);

        for (int i = 0; i < n; i++)
        {
            double cx = pxCenter[i];
            var center = new Point(cx, pyCenterArr[i]);

            // A point that is missing (NaN) is skipped with its error bars.
            if (!center.IsDrawable()) continue;

            // The data point's dot, unless it stands a thousand decades beyond a log axis, where matplotlib's is never seen.
            if (!Transform.LiesBeyond(center)) Ctx.DrawCircle(center, 3, ShapeStyle.Filled(color));

            // Honor ErrorEvery: only draw error bars at every N-th point
            if (series.ErrorEvery <= 1 || i % series.ErrorEvery == 0)
            {
                // The bar and each of its caps are marks of their own, as in matplotlib: a bar that runs to a value a log axis
                // cannot place is cut at the edge the axis begins at, and the cap at that end is never seen; a missing end skips
                // the bar and that end's cap. The other cap stays.
                var top = new Point(cx, pyTop[i]);
                var bottom = new Point(cx, pyBot[i]);
                if (Transform.CutSegment(bottom, top) is { } bar)
                    Ctx.DrawLine(bar.From, bar.To, StrokeStyle.Solid(color, eLineWidth));
                if (cap > 0 && HasCapAt(top))
                    Ctx.DrawLine(new Point(top.X - cap, top.Y), new Point(top.X + cap, top.Y), StrokeStyle.Solid(color, capThick));
                if (cap > 0 && HasCapAt(bottom))
                    Ctx.DrawLine(new Point(bottom.X - cap, bottom.Y), new Point(bottom.X + cap, bottom.Y), StrokeStyle.Solid(color, capThick));
                if (series.XErrorLow is { } low && series.XErrorHigh is { } high)
                {
                    var left = EndOf(series.XData[i] - low[i], series.YData[i]);
                    var right = EndOf(series.XData[i] + high[i], series.YData[i]);
                    if (Transform.CutSegment(left, right) is { } across)
                        Ctx.DrawLine(across.From, across.To, StrokeStyle.Solid(color, eLineWidth));
                    if (cap > 0 && HasCapAt(left))
                        Ctx.DrawLine(new Point(left.X, left.Y - cap), new Point(left.X, left.Y + cap), StrokeStyle.Solid(color, capThick));
                    if (cap > 0 && HasCapAt(right))
                        Ctx.DrawLine(new Point(right.X, right.Y - cap), new Point(right.X, right.Y + cap), StrokeStyle.Solid(color, capThick));
                }
            }
        }
    }
}
