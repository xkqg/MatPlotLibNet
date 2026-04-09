// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class GanttSeriesRenderer : SeriesRenderer<GanttSeries>
{
    public GanttSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(GanttSeries series)
    {
        var color = ResolveColor(series.Color);
        double halfH = series.BarHeight / 2;
        for (int i = 0; i < series.Tasks.Length; i++)
        {
            var tl = Transform.DataToPixel(series.Starts[i], i + halfH);
            var br = Transform.DataToPixel(series.Ends[i], i - halfH);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
        }
    }
}
