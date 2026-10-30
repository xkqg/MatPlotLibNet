// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TernaryLineSeries"/> as one polyline through the compositions in the order given.</summary>
internal sealed class TernaryLineSeriesRenderer : SeriesRenderer<TernaryLineSeries>
{
    /// <inheritdoc />
    public TernaryLineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(TernaryLineSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(TernaryLineSeries series)
    {
        var color = ResolveColor(series.Color);
        DrawPolyline(Context.PixelsOf(series), Slot.StrokeOf(color, series.LineWidth, series.LineStyle));
    }
}
