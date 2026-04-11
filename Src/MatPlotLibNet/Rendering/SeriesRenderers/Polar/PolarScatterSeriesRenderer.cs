// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarScatterSeries"/> as markers in polar coordinates.</summary>
internal sealed class PolarScatterSeriesRenderer : SeriesRenderer<PolarScatterSeries>
{
    /// <inheritdoc />
    public PolarScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PolarScatterSeries series)
    {
        var color = ResolveColor(series.Color);
        var bounds = Area.PlotBounds;
        double rMax = series.R.Length > 0 ? series.R.Max() : 1;
        var transform = new PolarTransform(bounds, rMax);

        for (int i = 0; i < series.R.Length; i++)
        {
            var pt = transform.PolarToPixel(series.R[i], series.Theta[i]);
            Ctx.DrawCircle(pt, series.MarkerSize / 2, color, null, 0);
        }
    }
}
