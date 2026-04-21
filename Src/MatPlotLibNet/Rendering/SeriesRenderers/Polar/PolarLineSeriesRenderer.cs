// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarLineSeries"/> as connected lines in polar coordinates.</summary>
internal sealed class PolarLineSeriesRenderer : PolarTransformRenderer<PolarLineSeries>
{
    /// <inheritdoc />
    public PolarLineSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PolarLineSeries series)
    {
        var color = ResolveColor(series.Color);
        var bounds = Area.PlotBounds;
        var transform = PrepareTransform(series.R, bounds);

        var points = new List<Point>(series.R.Length);
        for (int i = 0; i < series.R.Length; i++)
            points.Add(transform.PolarToPixel(series.R[i], series.Theta[i]));

        if (points.Count > 1)
            Ctx.DrawLines(points, color, series.LineWidth, series.LineStyle);
    }
}
