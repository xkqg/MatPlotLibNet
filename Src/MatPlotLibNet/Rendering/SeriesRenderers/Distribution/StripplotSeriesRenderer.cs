// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="StripplotSeries"/> as randomly jittered dots per category.</summary>
internal sealed class StripplotSeriesRenderer : SeriesRenderer<StripplotSeries>
{
    public StripplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(StripplotSeries series)
    {
        if (series.Datasets.Length == 0) return;

        var color = ResolveColor(series.Color);
        byte alpha255 = (byte)Math.Round(Math.Clamp(series.Alpha, 0.0, 1.0) * 255);
        var dotColor = color.WithAlpha(alpha255);

        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i];
            // Deterministic seed per category to keep output reproducible
            var rng = new Random(data.Length * 31 + i);

            foreach (double value in data)
            {
                double jitter = (rng.NextDouble() * 2 - 1) * series.Jitter;
                var px = Transform.DataToPixel(i + jitter, value);
                Ctx.DrawCircle(px, series.MarkerSize / 2.0, dotColor, null, 0);
            }
        }
    }
}
