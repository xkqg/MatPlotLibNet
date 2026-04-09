// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class StackedAreaSeriesRenderer : SeriesRenderer<StackedAreaSeries>
{
    public StackedAreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(StackedAreaSeries series)
    {
        int n = series.X.Length;
        int layers = series.YSets.Length;
        if (n == 0 || layers == 0) return;

        var cycleColors = Theme.Default.CycleColors;

        // Compute cumulative Y values for each layer
        var cumulative = new double[layers][];
        for (int layer = 0; layer < layers; layer++)
        {
            cumulative[layer] = new double[n];
            for (int i = 0; i < n; i++)
            {
                double prev = layer > 0 ? cumulative[layer - 1][i] : 0;
                double val = i < series.YSets[layer].Length ? series.YSets[layer][i] : 0;
                cumulative[layer][i] = prev + val;
            }
        }

        // Draw each layer as a filled polygon between consecutive cumulative curves
        for (int layer = 0; layer < layers; layer++)
        {
            var color = cycleColors[layer % cycleColors.Length];
            var fillColor = color.WithAlpha((byte)(series.Alpha * 255));

            var polygon = new List<Point>(n * 2);

            // Top edge: left to right along this layer's cumulative curve
            for (int i = 0; i < n; i++)
                polygon.Add(Transform.DataToPixel(series.X[i], cumulative[layer][i]));

            // Bottom edge: right to left along previous layer's cumulative curve (or y=0)
            for (int i = n - 1; i >= 0; i--)
            {
                double bottom = layer > 0 ? cumulative[layer - 1][i] : 0;
                polygon.Add(Transform.DataToPixel(series.X[i], bottom));
            }

            Ctx.DrawPolygon(polygon, fillColor, null, 0);
        }
    }
}
