// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarLineSeries"/> as connected lines in polar coordinates.</summary>
internal sealed class PolarLineSeriesRenderer : SeriesRenderer<PolarLineSeries>
{
    /// <inheritdoc />
    public PolarLineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PolarLineSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(PolarLineSeries series)
    {
        var color = ResolveColor(series.Color);
        var transform = Context.PolarTransformFor(series);

        var points = new List<Point>(series.R.Length);
        for (int i = 0; i < series.R.Length; i++)
            points.Add(transform.PolarToPixel(series.R[i], series.Theta[i]));

        // Broken at a point that cannot be placed, as a cartesian line is.
        DrawPolyline(points, Slot.StrokeOf(color, series.LineWidth, series.LineStyle));
    }
}
