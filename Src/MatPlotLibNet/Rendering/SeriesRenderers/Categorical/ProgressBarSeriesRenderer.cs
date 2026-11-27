// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ProgressBarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ProgressBarSeriesRenderer : SeriesRenderer<ProgressBarSeries>
{
    /// <inheritdoc />
    public ProgressBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ProgressBarSeries series)
    {
        var bounds = Area.PlotBounds;
        double barH = bounds.Height * series.BarHeight;
        // The track is centred in the plot along Y.
        var track = new Rect(bounds.X, Range1D.CentredIn(bounds.Y, bounds.Height, barH).Lo, bounds.Width, barH);
        Ctx.DrawRectangle(track, ShapeStyle.Filled(series.TrackColor));
        // A missing value has no bar and no percentage: the track alone is drawn.
        if (!series.Value.IsDrawable()) return;
        Ctx.DrawRectangle(track with { Width = bounds.Width * series.Value }, ShapeStyle.Filled(series.FillColor));
        // The percentage stands in the middle of the bar, on the fill once the fill reaches there and on the track before.
        var ground = series.Value >= 0.5 ? series.FillColor : series.TrackColor;
        Ctx.Draw(TextLabel.Of(series.ValueText), Context.Theme.DataTextFontOn(ground, series.TextSizeUnder(Context.Theme), GroundInk.Readable, FontWeight.Bold), new TextPlacement(new Point(bounds.Center.X, track.Center.Y), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
    }
}
