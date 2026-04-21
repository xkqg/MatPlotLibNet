// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="StackedAreaSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class StackedAreaSeriesRenderer : SeriesRenderer<StackedAreaSeries>
{
    /// <inheritdoc />
    public StackedAreaSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(StackedAreaSeries series)
    {
        int n = series.X.Length;
        int layers = series.YSets.Length;
        if (n == 0 || layers == 0) return;

        var cycleColors = Theme.Default.CycleColors;

        // Compute per-layer baselines using the chosen strategy
        var baselines = series.Baseline.ComputeFor(series.YSets, n);

        // Compute top edge (baseline + layer value) for each layer
        var tops = new double[layers][];
        for (int layer = 0; layer < layers; layer++)
        {
            tops[layer] = new double[n];
            for (int i = 0; i < n; i++)
            {
                double val = i < series.YSets[layer].Length ? series.YSets[layer][i] : 0;
                tops[layer][i] = baselines[layer][i] + val;
            }
        }

        // Precompute pixel X coordinates once for all layers (SIMD batch)
        var pxArr = Transform.TransformX(series.X);

        // Draw each layer as a filled polygon between baseline and top edge
        for (int layer = 0; layer < layers; layer++)
        {
            var color = cycleColors[layer % cycleColors.Length];
            var fillColor = ApplyAlpha(color, series.Alpha);

            var pyTop = Transform.TransformY(tops[layer]);
            var pyBot = Transform.TransformY(baselines[layer]);
            var polygon = new List<Point>(n * 2);

            // Top edge: left to right
            for (int i = 0; i < n; i++)
                polygon.Add(new Point(pxArr[i], pyTop[i]));

            // Bottom edge: right to left
            for (int i = n - 1; i >= 0; i--)
                polygon.Add(new Point(pxArr[i], pyBot[i]));

            Ctx.DrawPolygon(polygon, fillColor, null, 0);
        }
    }
}
