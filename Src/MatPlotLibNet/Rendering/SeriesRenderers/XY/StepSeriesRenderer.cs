// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StepSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class StepSeriesRenderer : SeriesRenderer<StepSeries>
{
    /// <inheritdoc />
    public StepSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>What a step stands on in pixels: the points of its path, cut where it leaves the plot towards a value a log axis
    /// cannot place, and the pixel of every sample it keeps (such a value a thousand decades beyond the axis).</summary>
    private readonly record struct StepPixels(IReadOnlyList<Point> Path, double[] X, double[] Y);

    /// <summary>Works the step's pixels out: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend.</summary>
    private StepPixels Pixels(StepSeries series)
    {
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        if (data.X.Length == 0) return new StepPixels([], [], []);
        // The corners are a stepped line's (SteppedBy); a value a log axis cannot place stands where matplotlib's log scale puts it,
        // so a step to it drops straight to the edge, and the markers sit on the data points themselves.
        var drawn = data.SteppedBy(series.DrawStyle);
        return new StepPixels(Transform.TransformPath(drawn.X, drawn.Y),
            Transform.TransformXUnclipped(data.X), Transform.TransformYUnclipped(data.Y));
    }

    /// <inheritdoc />
    internal override void AddMarks(StepSeries series, MarkObstacles.Builder marks) =>
        AddPolyline(marks, Pixels(series).Path);

    /// <inheritdoc />
    public override void Render(StepSeries series)
    {
        var color = ColorOf(series);
        var (pts, pxArr, pyArr) = Pixels(series);
        int n = pxArr.Length;
        if (n == 0) return;
        DrawSeriesLine(pts, series, color);

        // The marker the step names, or its slot's, sits on each data point (not on the corners of the steps). A step has
        // no marker size of its own: it takes the one a new line holds.
        if (Slot.LineMarkerOf(series.Marker) is { } style and not MarkerStyle.None)
        {
            var marker = MarkerShape.Of(style, Context.Theme);
            double markerSize = LineSeries.RestingMarkerSize.PointsInPixels;
            var markerStyle = Slot.LineMarkerStyleOf(style, color, null, null, null);
            for (int i = 0; i < n; i++)
            {
                var point = new Point(pxArr[i], pyArr[i]);
                // A marker at a point the axes cannot place is skipped, as a line's is: masked, or beyond a log axis.
                if (!point.IsDrawable() || Transform.LiesBeyond(point)) continue;
                marker.Draw(Ctx, point, markerSize, markerStyle);
            }
        }
    }

}
