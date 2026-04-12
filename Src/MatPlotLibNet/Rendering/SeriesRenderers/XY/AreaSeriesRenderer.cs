// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="AreaSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class AreaSeriesRenderer : SeriesRenderer<AreaSeries>
{
    /// <inheritdoc />
    public AreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(AreaSeries series)
    {
        var color = ResolveColor(series.Color);
        var fillColor = series.FillColor ?? ApplyAlpha(color, series.Alpha);
        var data = ApplyDownsampling(series.XData, series.YData, series.MaxDisplayPoints);
        int n = data.X.Length;
        if (n == 0) return;

        // Apply step interpolation to the top edge if requested
        var top = DrawStyleInterpolation.Apply(data.X, data.Y, series.StepMode);

        // Batch-transform the top edge; reuse pixel X for the reverse bottom edge
        var topPts = Transform.TransformBatch(top.X, top.Y);
        var pxX = Transform.TransformX(top.X);
        var polygon = new List<Point>(topPts.Length * 2);
        polygon.AddRange(topPts);

        if (series.YData2 is not null)
        {
            var bot = DrawStyleInterpolation.Apply(data.X, series.YData2, series.StepMode);
            var botPxX = Transform.TransformX(bot.X);
            var botPxY = Transform.TransformY(bot.Y);
            for (int i = bot.X.Length - 1; i >= 0; i--) polygon.Add(new Point(botPxX[i], botPxY[i]));
        }
        else
        {
            double pyZero = Transform.TransformY([0.0])[0];
            for (int i = pxX.Length - 1; i >= 0; i--) polygon.Add(new Point(pxX[i], pyZero));
        }

        Ctx.DrawPolygon(polygon, fillColor, null, 0);

        // Draw boundary with EdgeColor override if set
        var edgeColor = series.EdgeColor ?? color;
        Ctx.DrawLines(new List<Point>(topPts), edgeColor, series.LineWidth, series.LineStyle);
    }

}
