// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="RugplotSeries"/> as short vertical tick marks along the X axis.</summary>
internal sealed class RugplotSeriesRenderer : SeriesRenderer<RugplotSeries>
{
    /// <inheritdoc />
    public RugplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(RugplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var tick in Ticks(series)) marks.AddPath([tick.From, tick.To]);
    }

    /// <summary>The ticks that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend. An observation that cannot be placed draws no tick.</summary>
    private IEnumerable<PixelSegment> Ticks(RugplotSeries series)
    {
        double yDataMin = Transform.DataYMin;
        double yDataMax = Transform.DataYMax;
        // Signed: DataYMin is the bottom edge as drawn, so on a reversed axis the tick rises towards a smaller value.
        double tickHeightData = series.Height * (yDataMax - yDataMin);

        foreach (double value in series.Data.Data)
        {
            var bottom = Transform.DataToPixel(value, yDataMin);
            var top = Transform.DataToPixel(value, yDataMin + tickHeightData);
            if (bottom.CanDrawLineTo(top)) yield return new PixelSegment(bottom, top);
        }
    }

    /// <inheritdoc />
    public override void Render(RugplotSeries series)
    {
        var color = ResolveColor(series.Color);
        var tickColor = ApplyAlpha(color, series.Alpha);
        foreach (var tick in Ticks(series))
            Ctx.DrawLine(tick.From, tick.To, new StrokeStyle(tickColor, series.LineWidth, Styling.LineStyle.Solid));
    }
}
