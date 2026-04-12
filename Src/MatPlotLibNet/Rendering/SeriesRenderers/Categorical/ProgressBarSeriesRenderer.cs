// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
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
        double y = bounds.Y + (bounds.Height - barH) / 2;
        Ctx.DrawRectangle(new Rect(bounds.X, y, bounds.Width, barH), series.TrackColor, null, 0);
        Ctx.DrawRectangle(new Rect(bounds.X, y, bounds.Width * series.Value, barH), series.FillColor, null, 0);
        Ctx.DrawText($"{series.Value:P0}", new Point(bounds.X + bounds.Width / 2, y + barH / 2 + 4), new Font { Size = 11, Weight = FontWeight.Bold }, TextAlignment.Center);
    }
}
