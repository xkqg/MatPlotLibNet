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
            marks.AddPoint(center);
            if (series.ErrorEvery > 1 && i % series.ErrorEvery != 0) continue;
            var top = new Point(pixels.X[i], pixels.Top[i]);
            var bottom = new Point(pixels.X[i], pixels.Bottom[i]);
            if (bottom.CanDrawLineTo(top)) marks.AddPath([bottom, top]);
            if (series.XErrorLow is not null && series.XErrorHigh is not null)
            {
                var left = Transform.DataToPixel(series.XData[i] - series.XErrorLow[i], series.YData[i]);
                var right = Transform.DataToPixel(series.XData[i] + series.XErrorHigh[i], series.YData[i]);
                if (left.CanDrawLineTo(right)) marks.AddPath([left, right]);
            }
        }
    }

    /// <summary>Where the points and the ends of their error bars stand in pixels: X, the point, its top and its bottom.</summary>
    private readonly record struct ErrorBarPixels(double[] X, double[] Centre, double[] Top, double[] Bottom);

    /// <summary>Works the error bars' pixels out (batch-transformed, SIMD): the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    private ErrorBarPixels Pixels(ErrorBarSeries series)
    {
        int n = series.XData.Length;
        var topYData = new double[n];
        var botYData = new double[n];
        for (int i = 0; i < n; i++) { topYData[i] = series.YData[i] + series.YErrorHigh[i]; botYData[i] = series.YData[i] - series.YErrorLow[i]; }
        return new ErrorBarPixels(Transform.TransformX(series.XData), Transform.TransformY(series.YData),
            Transform.TransformY(topYData), Transform.TransformY(botYData));
    }

    /// <inheritdoc />
    public override void Render(ErrorBarSeries series)
    {
        var color = ResolveColor(series.Color);
        int n = series.XData.Length;
        // An error bar has no dash pattern or marker of its own (its bars are solid, its point a dot), so the slot decides
        // only the width, which the bars and caps take unless the series names theirs.
        double lineWidth = Slot.LineWidthOf(series.LineWidth);
        double eLineWidth = series.ELineWidth ?? lineWidth;
        double capThick = series.CapThick ?? lineWidth;

        var (pxCenter, pyCenterArr, pyTop, pyBot) = Pixels(series);

        for (int i = 0; i < n; i++)
        {
            double cx = pxCenter[i];
            var center = new Point(cx, pyCenterArr[i]);

            // A point the axes cannot place is skipped with its error bars.
            if (!center.IsDrawable()) continue;

            // Draw the data point marker always
            Ctx.DrawCircle(center, 3, new ShapeStyle(color, null, 0));

            // Honor ErrorEvery: only draw error bars at every N-th point
            if (series.ErrorEvery <= 1 || i % series.ErrorEvery == 0)
            {
                // The bar and each of its caps are marks of their own, as in matplotlib: an end that cannot be placed
                // (a missing error, a value a log axis masks) skips the bar and that end's cap; the other cap stays.
                var top = new Point(cx, pyTop[i]);
                var bottom = new Point(cx, pyBot[i]);
                if (bottom.CanDrawLineTo(top))
                    Ctx.DrawLine(bottom, top, new StrokeStyle(color, eLineWidth, LineStyle.Solid));
                if (top.IsDrawable())
                    Ctx.DrawLine(new Point(top.X - series.CapSize, top.Y), new Point(top.X + series.CapSize, top.Y), new StrokeStyle(color, capThick, LineStyle.Solid));
                if (bottom.IsDrawable())
                    Ctx.DrawLine(new Point(bottom.X - series.CapSize, bottom.Y), new Point(bottom.X + series.CapSize, bottom.Y), new StrokeStyle(color, capThick, LineStyle.Solid));
                if (series.XErrorLow is not null && series.XErrorHigh is not null)
                {
                    var left = Transform.DataToPixel(series.XData[i] - series.XErrorLow[i], series.YData[i]);
                    var right = Transform.DataToPixel(series.XData[i] + series.XErrorHigh[i], series.YData[i]);
                    if (left.CanDrawLineTo(right))
                        Ctx.DrawLine(left, right, new StrokeStyle(color, eLineWidth, LineStyle.Solid));
                    if (left.IsDrawable())
                        Ctx.DrawLine(new Point(left.X, left.Y - series.CapSize), new Point(left.X, left.Y + series.CapSize), new StrokeStyle(color, capThick, LineStyle.Solid));
                    if (right.IsDrawable())
                        Ctx.DrawLine(new Point(right.X, right.Y - series.CapSize), new Point(right.X, right.Y + series.CapSize), new StrokeStyle(color, capThick, LineStyle.Solid));
                }
            }
        }
    }
}
