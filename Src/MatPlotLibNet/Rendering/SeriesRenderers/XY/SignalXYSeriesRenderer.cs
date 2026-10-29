// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="SignalXYSeries"/> using O(log n) viewport slicing + LTTB downsampling.</summary>
internal sealed class SignalXYSeriesRenderer : SeriesRenderer<SignalXYSeries>
{
    /// <inheritdoc />
    public SignalXYSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The points of the signal in pixels: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/>
    /// hands to a legend.</summary>
    private Point[] Path(SignalXYSeries series)
    {
        var data = ApplyMonotonicDownsampling(series, series.MaxDisplayPoints);
        return Transform.TransformBatch(data.X, data.Y);
    }

    /// <inheritdoc />
    internal override void AddMarks(SignalXYSeries series, MarkObstacles.Builder marks) => AddPolyline(marks, Path(series));

    /// <inheritdoc />
    public override void Render(SignalXYSeries series)
    {
        var color = ResolveColor(series.Color);
        Ctx.DrawLines(Path(series), Slot.StrokeOf(color, series.LineWidth, series.LineStyle));
    }
}
