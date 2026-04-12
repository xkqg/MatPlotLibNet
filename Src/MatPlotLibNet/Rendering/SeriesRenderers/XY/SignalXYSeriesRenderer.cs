// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="SignalXYSeries"/> using O(log n) viewport slicing + LTTB downsampling.</summary>
internal sealed class SignalXYSeriesRenderer : SeriesRenderer<SignalXYSeries>
{
    /// <inheritdoc />
    public SignalXYSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SignalXYSeries series)
    {
        var color = ResolveColor(series.Color);
        var data  = ApplyMonotonicDownsampling(series, series.MaxDisplayPoints);
        var points = new List<Point>(Transform.TransformBatch(data.X, data.Y));
        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);
    }
}
