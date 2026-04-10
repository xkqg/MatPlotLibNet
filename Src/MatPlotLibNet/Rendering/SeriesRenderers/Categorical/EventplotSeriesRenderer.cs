// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders an <see cref="EventplotSeries"/> as rows of vertical tick lines at event positions.</summary>
internal sealed class EventplotSeriesRenderer : SeriesRenderer<EventplotSeries>
{
    public EventplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(EventplotSeries series)
    {
        if (series.Positions.Length == 0) return;

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
                Ctx.DrawLine(bottom, top, color, series.LineWidth, Styling.LineStyle.Solid);
            }
        }
    }
}
