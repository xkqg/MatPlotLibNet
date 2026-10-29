// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders an <see cref="EventplotSeries"/> as rows of vertical tick lines at event positions.</summary>
internal sealed class EventplotSeriesRenderer : SeriesRenderer<EventplotSeries>
{
    /// <inheritdoc />
    public EventplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(EventplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var tick in Ticks(series)) marks.AddPath([tick.Line.From, tick.Line.To]);
    }

    /// <summary>One event's tick: where it stands and the colour its row wears.</summary>
    private readonly record struct EventTick(PixelSegment Line, Color Colour);

    /// <summary>The ticks that can be drawn: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a
    /// legend. An event that cannot be placed draws no tick.</summary>
    private IEnumerable<EventTick> Ticks(EventplotSeries series)
    {
        double halfLen = series.LineLength / 2.0;
        for (int i = 0; i < series.Positions.Length; i++)
        {
            var color = series.Colors is not null && i < series.Colors.Length
                ? series.Colors[i]
                : ResolveColor(null);

            foreach (double pos in series.Positions[i])
            {
                var bottom = Transform.DataToPixel(pos, i - halfLen);
                var top = Transform.DataToPixel(pos, i + halfLen);
                if (bottom.CanDrawLineTo(top)) yield return new EventTick(new PixelSegment(bottom, top), color);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(EventplotSeries series)
    {
        foreach (var tick in Ticks(series))
            Ctx.DrawLine(tick.Line.From, tick.Line.To, new StrokeStyle(tick.Colour, series.LineWidth, Styling.LineStyle.Solid));
    }
}
