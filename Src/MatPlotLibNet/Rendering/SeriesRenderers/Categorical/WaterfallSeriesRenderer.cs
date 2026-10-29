// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="WaterfallSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class WaterfallSeriesRenderer : SeriesRenderer<WaterfallSeries>
{
    /// <inheritdoc />
    public WaterfallSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(WaterfallSeries series, MarkObstacles.Builder marks)
    {
        foreach (var step in Steps(series))
            if (step.Bar.IsDrawable()) marks.AddBox(step.Bar);
    }

    /// <summary>One step of a waterfall: its bar, the colour it wears and the connector to the next step, when there is one.</summary>
    private readonly record struct WaterfallStep(Rect Bar, Color Colour, Point? ConnectorFrom, Point? ConnectorTo);

    /// <summary>The steps in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend.</summary>
    private IEnumerable<WaterfallStep> Steps(WaterfallSeries series)
    {
        var bars = series.Bars();
        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];
            // The bar stands centred in its step's slot, where the axis writes the step's name.
            var slot = series.SlotOf(i);
            // A total wears its own colour, a step the colour of its direction.
            var color = bar.IsTotal ? series.TotalColor : bar.Rises ? series.IncreaseColor : series.DecreaseColor;
            // A bar stands on its foot (the total before a step, zero for a total) and reaches its top. A foot the axis
            // cannot place (0 on a log axis) is clipped to the edge where the axis begins, as matplotlib clips it; a bar
            // whose top the axes cannot place is skipped. A missing step is not known, and neither is any total after it
            // (a NaN carries on through the running sum) until a total states a level, so those steps and their
            // connectors go too.
            var tl = bar.Rises ? Transform.DataToPixel(slot.Left, bar.Top) : Transform.DataToPixelClippedY(slot.Left, bar.Foot);
            var br = bar.Rises ? Transform.DataToPixelClippedY(slot.Right, bar.Foot) : Transform.DataToPixel(slot.Right, bar.Top);
            Point? from = null, to = null;
            if (i < bars.Length - 1)
            {
                from = Transform.DataToPixel(slot.Right, bar.Top);
                to = Transform.DataToPixel(series.SlotOf(i + 1).Left, bar.Top);
            }
            yield return new WaterfallStep(Rect.FromCorners(tl, br), color, from, to);
        }
    }

    /// <inheritdoc />
    public override void Render(WaterfallSeries series)
    {
        foreach (var step in Steps(series))
        {
            if (step.Bar.IsDrawable())
                Ctx.DrawRectangle(step.Bar, new ShapeStyle(step.Colour, null, 0));
            if (step.ConnectorFrom is { } ls && step.ConnectorTo is { } le && ls.CanDrawLineTo(le))
                Ctx.DrawLine(ls, le, new StrokeStyle(Colors.Gray, 0.5, LineStyle.Dashed));
        }
    }
}
