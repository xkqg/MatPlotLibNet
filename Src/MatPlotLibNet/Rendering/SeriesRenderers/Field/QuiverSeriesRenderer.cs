// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="QuiverSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class QuiverSeriesRenderer : SeriesRenderer<QuiverSeries>
{
    /// <inheritdoc />
    public QuiverSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(QuiverSeries series)
    {
        var color = ColorOf(series);
        if (series.AutoScaledUnder(Context.Theme))
        {
            RenderAutoscaled(series, color);
            return;
        }

        // An arrow given a scale is lines of the library's own look: a line's resting width under every theme.
        var stroke = Slot.StrokeOf(color, LineWidthRule.Own);
        for (int i = 0; i < series.XData.Length; i++)
        {
            var start = Transform.DataToPixel(series.XData[i], series.YData[i]);
            var end = Transform.DataToPixel(series.XData[i] + series.UData[i] * series.Scale, series.YData[i] + series.VData[i] * series.Scale);
            if (!start.CanDrawLineTo(end)) continue;   // an arrow with an end that cannot be placed is skipped, head and all
            DrawArrow(start, end, series.ArrowHeadSize, stroke);
        }
    }

    /// <summary>The arrows as matplotlib's quiver draws them given no scale, each the polygon <see cref="QuiverArrow"/> makes: a vector
    /// of length 1 spans <see cref="QuiverSeries.AutoScaleShare"/> of the width of the axes, a shaft is
    /// <see cref="QuiverSeries.AutoScaleWidth"/> of it across, and an arrow points along its vector as it stands on the screen
    /// (<c>angles='uv'</c>), up where the vector's V is positive. It is filled with the quiver's colour and has no edge
    /// (<c>linewidths=(0,)</c>). A vector that is not a number is masked and draws nothing, and nothing is drawn when every vector is
    /// zero long, which has no scale. The length is no part of the axes' range, so an arrow can stand out past the plot area, which
    /// matplotlib's collection clips to the axes: the series pass cuts it off there (<c>QuiverSeries.MarksReachPastItsRange</c>).</summary>
    private void RenderAutoscaled(QuiverSeries series, Color color)
    {
        double share = series.AutoScaleShare();
        if (share <= 0) return;
        double plotWidth = Area.PlotBounds.Width;
        double width = series.AutoScaleWidth() * plotWidth;
        var style = ShapeStyle.Filled(color);
        for (int i = 0; i < series.XData.Length; i++)
        {
            double u = series.UData[i], v = series.VData[i];
            if (!double.IsFinite(u) || !double.IsFinite(v)) continue;
            var tail = Transform.DataToPixel(series.XData[i], series.YData[i]);
            if (!tail.IsDrawable()) continue;
            double length = new Point(u, v).Length;
            // np.angle(U + iV): a vector of no length points along X, as matplotlib's does.
            double cos = length > 0 ? u / length : 1, sin = length > 0 ? v / length : 0;
            Ctx.DrawPolygon(QuiverArrow.Place(tail, length * share * plotWidth / width, width, cos, sin), style);
        }
    }

    /// <summary>A shaft from <paramref name="start"/> to <paramref name="end"/> and the two strokes of its head (<see cref="ArrowHead.QuiverHead"/>),
    /// each a share of the shaft's length long.</summary>
    private void DrawArrow(Point start, Point end, double headSize, StrokeStyle stroke)
    {
        Ctx.DrawLine(start, end, stroke);
        double length = start.HeadingTo(end).Length;
        if (length < 1e-6) return;
        ArrowHead.QuiverHead.Draw(Ctx, start, end, length * headSize, stroke);
    }
}
