// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SwarmplotSeries"/> using beeswarm layout for non-overlapping dots per category.</summary>
internal sealed class SwarmplotSeriesRenderer : SeriesRenderer<SwarmplotSeries>
{
    public SwarmplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(SwarmplotSeries series)
    {
        if (series.Datasets.Length == 0) return;

        var color = ResolveColor(series.Color);
        byte alpha255 = (byte)Math.Round(Math.Clamp(series.Alpha, 0.0, 1.0) * 255);
        var dotColor = color.WithAlpha(alpha255);
        double r = series.MarkerSize / 2.0;

        // Estimate markerRadius in data units from pixel radius
        double dataPerPixelX = Math.Abs(Transform.DataXMax - Transform.DataXMin) /
                               Math.Max(1, Area.PlotBounds.Width);
        double markerRadiusData = r * dataPerPixelX;

        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var sorted = series.Datasets[i].ToArray();
            Array.Sort(sorted);
            double[] xPositions = BeeswarmLayout.Compute(sorted, markerRadiusData, i);

            for (int j = 0; j < sorted.Length; j++)
            {
                var px = Transform.DataToPixel(xPositions[j], sorted[j]);
                Ctx.DrawCircle(px, r, dotColor, null, 0);
            }
        }
    }
}
