// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class EcdfSeriesRenderer : SeriesRenderer<EcdfSeries>
{
    public EcdfSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(EcdfSeries series)
    {
        var color = ResolveColor(series.Color);
        int n = series.SortedX.Length;
        if (n == 0) return;

        var pts = new List<Point>();

        // Start at (SortedX[0], 0)
        pts.Add(Transform.DataToPixel(series.SortedX[0], 0));

        for (int i = 0; i < n; i++)
        {
            // Horizontal line to (SortedX[i], previous CDF value)
            double prevY = i > 0 ? series.CdfY[i - 1] : 0;
            pts.Add(Transform.DataToPixel(series.SortedX[i], prevY));

            // Vertical line to (SortedX[i], CdfY[i])
            pts.Add(Transform.DataToPixel(series.SortedX[i], series.CdfY[i]));
        }

        Ctx.DrawLines(pts, color, series.LineWidth, series.LineStyle);
    }
}
