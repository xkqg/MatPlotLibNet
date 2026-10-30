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
        {
            if (step.Bar.IsDrawable()) marks.AddBox(step.Bar);
            if (step.Target is { } mark && mark.From.CanDrawLineTo(mark.To)) marks.AddPath([mark.From, mark.To]);
        }
    }

    /// <summary>How far a target mark reaches past each side of its bar, in pixels.</summary>
    private const double TargetOverhangPx = 4;

    /// <summary>How thick a target mark is, in pixels.</summary>
    private const double TargetThickness = 2.5;

    /// <summary>The short line across a bar at the level its step was meant to reach.</summary>
    private readonly record struct TargetMark(Point From, Point To);

    /// <summary>One step of a waterfall: its bar, the colour it wears, the connector to the next step, when there is one, and the
    /// mark of its target, when it has one.</summary>
    private readonly record struct WaterfallStep(Rect Bar, Color Colour, Point? ConnectorFrom, Point? ConnectorTo, TargetMark? Target);

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
            var box = Transform.BoxOnFoot(slot, bar.Foot, bar.Top, bar.Rises, BarOrientation.Vertical);
            Point? from = null, to = null;
            if (i < bars.Length - 1)
            {
                from = Transform.DataToPixel(slot.Right, bar.Top);
                to = Transform.DataToPixel(series.SlotOf(i + 1).Left, bar.Top);
            }
            // The mark of the target: across the bar's own stretch of the axis, a little past it on both sides, at the absolute
            // level the target names. A level the axis cannot place (missing, or 0 on a log axis) gives a mark that is not drawn.
            TargetMark? target = null;
            if (series.Targets is { } levels && i < levels.Length)
            {
                var left = Transform.DataToPixel(slot.Left, levels[i]);
                var right = Transform.DataToPixel(slot.Right, levels[i]);
                target = new TargetMark(new Point(Math.Min(left.X, right.X) - TargetOverhangPx, left.Y), new Point(Math.Max(left.X, right.X) + TargetOverhangPx, right.Y));
            }

            yield return new WaterfallStep(box.Rect, color, from, to, target);
        }
    }

    /// <inheritdoc />
    public override void Render(WaterfallSeries series)
    {
        var targetStroke = new StrokeStyle(series.TargetColor ?? Context.Theme.ForegroundText, TargetThickness, LineStyle.Solid);
        foreach (var step in Steps(series))
        {
            if (step.Bar.IsDrawable())
                Ctx.DrawRectangle(step.Bar, new ShapeStyle(step.Colour, null, 0));
            // The mark of a target is drawn over its bar.
            if (step.Target is { } mark && mark.From.CanDrawLineTo(mark.To))
                Ctx.DrawLine(mark.From, mark.To, targetStroke);
            if (step.ConnectorFrom is { } ls && step.ConnectorTo is { } le && ls.CanDrawLineTo(le))
                Ctx.DrawLine(ls, le, new StrokeStyle(Context.Theme.Neutral, 0.5, LineStyle.Dashed));
        }
    }
}
