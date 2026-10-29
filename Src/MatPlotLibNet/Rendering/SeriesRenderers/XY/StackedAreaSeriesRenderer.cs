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
    internal override void AddMarks(StackedAreaSeries series, MarkObstacles.Builder marks)
    {
        foreach (var layer in Layers(series)) marks.AddPath(layer.Polygon);
    }

    /// <summary>One filled run of one layer: the layer's place in the stack and the polygon's vertices in pixels.</summary>
    private readonly record struct LayerPolygon(int Layer, List<Point> Polygon);

    /// <summary>The polygons of every layer: the one geometry <see cref="Render"/> fills and <see cref="AddMarks"/> hands to a
    /// legend.</summary>
    private IEnumerable<LayerPolygon> Layers(StackedAreaSeries series)
    {
        int n = series.X.Length;
        int layers = series.YSets.Length;
        if (n == 0 || layers == 0) yield break;

        // Compute per-layer baselines using the chosen strategy, and each layer's top edge on its baseline
        var baselines = series.Baseline.ComputeFor(series.YSets, n);
        var tops = series.LayerTops(baselines);

        // Precompute pixel X coordinates once for all layers (SIMD batch)
        var pxArr = Transform.TransformX(series.X);

        for (int layer = 0; layer < layers; layer++)
        {
            // A layer stands on the one below it, the first on its baseline (0 for a plain stack): a floor the axis cannot
            // place (0 on a log axis) is clipped to the edge where the axis begins, as matplotlib clips it.
            var pyTop = Transform.TransformY(tops[layer]);
            var pyBot = Transform.TransformYClipped(baselines[layer]);

            // A gap breaks the layer, as matplotlib's fill_between breaks at a masked point: a missing value or an
            // infinity in x, in this layer or in one stacked under it, or a top the axes cannot place. Each unbroken run
            // is filled on its own; without a gap the one run is the whole layer.
            foreach (var run in pxArr.DrawableIndexRuns(pyTop, pyBot))
            {
                var polygon = new List<Point>(run.Count * 2);

                // Top edge: left to right
                for (int i = run.StartInclusive; i < run.EndExclusive; i++)
                    polygon.Add(new Point(pxArr[i], pyTop[i]));

                // Bottom edge: right to left
                for (int i = run.EndExclusive - 1; i >= run.StartInclusive; i--)
                    polygon.Add(new Point(pxArr[i], pyBot[i]));

                yield return new LayerPolygon(layer, polygon);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(StackedAreaSeries series)
    {
        // Each layer is a filled polygon between its baseline and its top edge.
        foreach (var layer in Layers(series))
        {
            var fillColor = ApplyAlpha(PartColor(layer.Layer), series.Alpha);
            Ctx.DrawPolygon(layer.Polygon, new ShapeStyle(fillColor, null, 0) { Hatch = series.Hatch, HatchColor = series.HatchColor });
        }
    }
}
