// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="SignalSeries"/> using O(1) viewport slicing + LTTB downsampling.</summary>
internal sealed class SignalSeriesRenderer : SeriesRenderer<SignalSeries>
{
    /// <inheritdoc />
    public SignalSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SignalSeries series)
    {
        var color = ResolveColor(series.Color);
        var data  = ApplyMonotonicDownsampling(series, series.MaxDisplayPoints);
        var points = new List<Point>(Transform.TransformBatch(data.X, data.Y));
        Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);
    }
}
