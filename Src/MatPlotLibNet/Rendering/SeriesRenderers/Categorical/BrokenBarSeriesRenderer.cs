// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="BrokenBarSeries"/> as horizontal bars with gaps per row.</summary>
internal sealed class BrokenBarSeriesRenderer : SeriesRenderer<BrokenBarSeries>
{
    /// <inheritdoc />
    public BrokenBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(BrokenBarSeries series)
    {
        if (series.Ranges.Length == 0) return;

        var color = ResolveColor(series.Color);
        double halfH = series.BarHeight / 2.0;

        for (int i = 0; i < series.Ranges.Length; i++)
        {
            foreach (var (start, width) in series.Ranges[i])
            {
                var tl = Transform.DataToPixel(start, i + halfH);
                var br = Transform.DataToPixel(start + width, i - halfH);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
            }
        }
    }
}
