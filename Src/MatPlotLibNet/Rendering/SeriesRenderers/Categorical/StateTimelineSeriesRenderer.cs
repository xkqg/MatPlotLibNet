// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
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
            // From the start to the end across the full height: the rectangle between those corners, of positive width
            // on an X axis that runs backwards too.
            var segment = Rect.FromCorners(
                new Point(Transform.DataToPixel(seg.Start, 1.0).X, yTop),
                new Point(Transform.DataToPixel(seg.End, 0.0).X, yBottom));

            // A segment with an end that cannot be placed is skipped with its label.
            if (!segment.IsDrawable()) continue;

            // Draw the filled rectangle spanning the full plot height, in the fill the legend swatch shows the state in.
            Ctx.DrawRectangle(segment, seg.Fill(Context.Theme));

            // Centre the label text inside the rectangle
            if (!string.IsNullOrEmpty(seg.Label))
            {
                double textX = segment.Center.X;
                double textY = segment.Center.Y;
                Ctx.Draw(TextLabel.Of(seg.Label), Context.Theme.DataTextFontOn(seg.Color, series.TextSizeUnder(Context.Theme), GroundInk.Readable), new TextPlacement(new Point(textX, textY), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
            }
        }
    }
}
