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

        // Precompute pixel X coordinates once for all layers (SIMD batch)
        var pxArr = Transform.TransformX(series.X);
        double pyZero = Transform.TransformY([0.0])[0];

        // Draw each layer as a filled polygon between consecutive cumulative curves
        for (int layer = 0; layer < layers; layer++)
        {
            var color = cycleColors[layer % cycleColors.Length];
            var fillColor = color.WithAlpha((byte)(series.Alpha * 255));

            var pyTop = Transform.TransformY(cumulative[layer]);
            var polygon = new List<Point>(n * 2);

            // Top edge: left to right (SIMD-transformed)
            for (int i = 0; i < n; i++)
                polygon.Add(new Point(pxArr[i], pyTop[i]));

            // Bottom edge: right to left
            if (layer > 0)
            {
                var pyBot = Transform.TransformY(cumulative[layer - 1]);
                for (int i = n - 1; i >= 0; i--)
                    polygon.Add(new Point(pxArr[i], pyBot[i]));
            }
            else
            {
                for (int i = n - 1; i >= 0; i--)
                    polygon.Add(new Point(pxArr[i], pyZero));
            }

            Ctx.DrawPolygon(polygon, fillColor, null, 0);
        }
    }
}
