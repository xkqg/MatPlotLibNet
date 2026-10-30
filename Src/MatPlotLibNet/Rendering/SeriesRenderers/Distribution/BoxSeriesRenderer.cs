// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BoxSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BoxSeriesRenderer : SeriesRenderer<BoxSeries>
{
    /// <inheritdoc />
    public BoxSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(BoxSeries series, MarkObstacles.Builder marks)
    {
        foreach (var box in Boxes(series))
        {
            if (box.Box.IsDrawable()) marks.AddBox(box.Box);
            AddSegment(marks, box.Median.From, box.Median.To);
            AddSegment(marks, box.WhiskerUp.From, box.WhiskerUp.To);
            AddSegment(marks, box.WhiskerDown.From, box.WhiskerDown.To);
            if (box.Mean is { } mean && mean.IsDrawable()) marks.AddPoint(mean);
        }
    }

    /// <summary>One box as it stands on the plot: its rectangle, its median and whiskers, and its mean when it shows one.</summary>
    private readonly record struct BoxPixels(Rect Box, PixelSegment Median, PixelSegment WhiskerUp, PixelSegment WhiskerDown, Point? Mean);

    /// <summary>The boxes in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<BoxPixels> Boxes(BoxSeries series)
    {
        double halfW = series.HalfWidth;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            // The box is drawn from the observations that are there: a missing one (NaN) and an infinity are left out,
            // as they are left out of the range.
            var data = series.Datasets[i].SortedDrawableValues();
            if (data.Length == 0) continue;
            double pos = series.Positions.PositionOf(i);
            double q1 = data.Percentile(25), median = data.Percentile(50), q3 = data.Percentile(75);
            double iqr = q3 - q1;
            double whisLo = Math.Max(data[0], q1 - series.Whis * iqr);
            double whisHi = Math.Min(data[^1], q3 + series.Whis * iqr);
            double? mean = series.ShowMeans ? data.Average() : null;

            // The box and each whisker stand on their lower statistic and reach the upper one, as a bar stands on its
            // foot: a lower statistic the axis cannot place (0 or below on a log axis) is clipped to the edge where the
            // axis begins, as matplotlib clips it, and a part whose upper statistic it cannot place lies wholly beyond the
            // axis and is skipped. The median and the mean stand at one value: skipped when it cannot be placed. The box is
            // the rectangle between its two corners (Rect.FromCorners), of positive size on an axis that runs backwards.
            if (series.Vert)
            {
                yield return new BoxPixels(
                    Transform.BoxOnFoot(CategorySlot.Around(pos, 2 * halfW), q1, q3, rising: true, BarOrientation.Vertical).Rect,
                    new PixelSegment(Transform.DataToPixel(pos - halfW, median), Transform.DataToPixel(pos + halfW, median)),
                    new PixelSegment(Transform.DataToPixelClippedY(pos, q3), Transform.DataToPixel(pos, whisHi)),
                    new PixelSegment(Transform.DataToPixel(pos, q1), Transform.DataToPixelClippedY(pos, whisLo)),
                    mean is { } m ? Transform.DataToPixel(pos, m) : null);
            }
            else
            {
                // Horizontal orientation — swap axes.
                yield return new BoxPixels(
                    Transform.BoxOnFoot(CategorySlot.Around(pos, 2 * halfW), q1, q3, rising: true, BarOrientation.Horizontal).Rect,
                    new PixelSegment(Transform.DataToPixel(median, pos - halfW), Transform.DataToPixel(median, pos + halfW)),
                    new PixelSegment(Transform.DataToPixelClippedX(q3, pos), Transform.DataToPixel(whisHi, pos)),
                    new PixelSegment(Transform.DataToPixel(q1, pos), Transform.DataToPixelClippedX(whisLo, pos)),
                    mean is { } m ? Transform.DataToPixel(m, pos) : null);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(BoxSeries series)
    {
        var color = ResolveColor(series.Color);
        var boxStyle = new ShapeStyle(null, color, 1.5);
        var medianStroke = new StrokeStyle(series.MedianColor ?? Colors.Red, 2, LineStyle.Solid);
        var whiskerStroke = new StrokeStyle(color, 1, LineStyle.Solid);
        foreach (var box in Boxes(series))
        {
            DrawBox(box.Box, boxStyle);
            DrawSegment(box.Median.From, box.Median.To, medianStroke);
            DrawSegment(box.WhiskerUp.From, box.WhiskerUp.To, whiskerStroke);
            DrawSegment(box.WhiskerDown.From, box.WhiskerDown.To, whiskerStroke);
            if (box.Mean is { } mean) DrawMean(mean);
        }
    }

    /// <summary>Draws the box when it can be drawn: a box whose third quartile the axes cannot place is skipped.</summary>
    private void DrawBox(Rect box, ShapeStyle style)
    {
        if (box.IsDrawable()) Ctx.DrawRectangle(box, style);
    }

    /// <summary>Draws the mean's marker when the axes can place it.</summary>
    private void DrawMean(Point at)
    {
        if (at.IsDrawable()) Ctx.DrawCircle(at, 4, new ShapeStyle(Colors.Green, null, 0));
    }
}
