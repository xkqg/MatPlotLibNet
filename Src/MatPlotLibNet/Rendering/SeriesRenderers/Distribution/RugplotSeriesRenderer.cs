// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="RugplotSeries"/> as short vertical tick marks along the X axis.</summary>
internal sealed class RugplotSeriesRenderer : SeriesRenderer<RugplotSeries>
{
    /// <inheritdoc />
    public RugplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(RugplotSeries series)
    {
        if (series.Data.Length == 0) return;

        var color = ResolveColor(series.Color);
        var tickColor = ApplyAlpha(color, series.Alpha);

        double yDataMin = Transform.DataYMin;
        double yDataMax = Transform.DataYMax;
        double yRange = Math.Abs(yDataMax - yDataMin);
        double tickHeightData = series.Height * yRange;

        foreach (double value in series.Data.Data)
        {
            var bottom = Transform.DataToPixel(value, yDataMin);
            var top = Transform.DataToPixel(value, yDataMin + tickHeightData);
            Ctx.DrawLine(bottom, top, tickColor, series.LineWidth, Styling.LineStyle.Solid);
        }
    }
}
