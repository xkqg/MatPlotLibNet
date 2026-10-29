// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="SignalSeries"/> using O(1) viewport slicing + LTTB downsampling.</summary>
internal sealed class SignalSeriesRenderer : SeriesRenderer<SignalSeries>
{
    /// <inheritdoc />
    public SignalSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The points of the signal in pixels: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/>
    /// hands to a legend.</summary>
    private Point[] Path(SignalSeries series)
    {
        var data = ApplyMonotonicDownsampling(series, series.MaxDisplayPoints);
        return Transform.TransformBatch(data.X, data.Y);
    }

    /// <inheritdoc />
    internal override void AddMarks(SignalSeries series, MarkObstacles.Builder marks) => AddPolyline(marks, Path(series));

    /// <inheritdoc />
    public override void Render(SignalSeries series)
    {
        var color = ResolveColor(series.Color);
        Ctx.DrawLines(Path(series), Slot.StrokeOf(color, series.LineWidth, series.LineStyle));
    }
}
