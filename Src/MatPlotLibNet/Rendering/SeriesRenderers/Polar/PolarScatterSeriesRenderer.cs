// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarScatterSeries"/> as markers in polar coordinates.</summary>
internal sealed class PolarScatterSeriesRenderer : PolarTransformRenderer<PolarScatterSeries>
{
    /// <inheritdoc />
    public PolarScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PolarScatterSeries series)
    {
        var color = ResolveColor(series.Color);
        var bounds = Area.PlotBounds;
        var transform = PrepareTransform(series.R, bounds);

        for (int i = 0; i < series.R.Length; i++)
        {
            // A point at a radius or an angle that cannot be placed is skipped, as a cartesian marker is.
            var pt = transform.PolarToPixel(series.R[i], series.Theta[i]);
            if (!pt.IsDrawable()) continue;
            Ctx.DrawCircle(pt, series.MarkerSize / 2, new ShapeStyle(color, null, 0));
        }
    }
}
