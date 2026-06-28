// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StateTimelineSeries"/> instances onto an <see cref="IRenderContext"/> —
/// one filled rectangle per state segment spanning [Start, End] in X across the full plot height,
/// with the segment label centred inside the rectangle.</summary>
internal sealed class StateTimelineSeriesRenderer : SeriesRenderer<StateTimelineSeries>
{
    /// <inheritdoc />
    public StateTimelineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(StateTimelineSeries series)
    {
        if (series.Segments.Count == 0) return;

        var bounds = Area.PlotBounds;
        double yTop    = bounds.Y;
        double yBottom = bounds.Y + bounds.Height;

        foreach (var seg in series.Segments)
        {
            var left  = Transform.DataToPixel(seg.Start, 1.0);
            var right = Transform.DataToPixel(seg.End,   0.0);

            double x = left.X;
            double w = right.X - left.X;
            double y = yTop;
            double h = yBottom - yTop;

            // Draw the filled rectangle spanning the full plot height
            Ctx.DrawRectangle(new Rect(x, y, w, h), seg.Color, null, 0);

            // Centre the label text inside the rectangle
            if (!string.IsNullOrEmpty(seg.Label))
            {
                double textX = x + w / 2.0;
                double textY = y + h / 2.0;
                Ctx.DrawText(seg.Label, new Point(textX, textY),
                    new Font { Size = 11 }, TextAlignment.Center);
            }
        }
    }
}
