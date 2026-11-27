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
    private readonly record struct LayerPolygon(int Layer, IReadOnlyList<Point> Polygon);

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
        var pxArr = Transform.TransformXUnclipped(series.X);

        for (int layer = 0; layer < layers; layer++)
        {
            // A layer stands on the one below it, the first on its baseline (0 for a plain stack): the baseline is what the
            // stack stands on, clipped to the edge where a log axis begins as matplotlib clips it. A top, and the floor that is
            // the top of the layer below, is a curve: a value a log axis cannot place lies a thousand decades below it, so the
            // layer's edge dives to the bottom edge there and the fill is cut at it.
            var pyTop = Transform.TransformYUnclipped(tops[layer]);
            var pyBot = layer == 0 ? Transform.TransformYClipped(baselines[layer]) : Transform.TransformYUnclipped(baselines[layer]);

            // A gap breaks the layer, as matplotlib's fill_between breaks at a masked point: a missing value or an
            // infinity in x, in this layer or in one stacked under it. Each unbroken run is filled on its own; without a gap
            // the one run is the whole layer.
            foreach (var run in pxArr.DrawableIndexRuns(pyTop, pyBot))
            {
                var polygon = new List<Point>(run.Count * 2);

                // Top edge: left to right
                for (int i = run.StartInclusive; i < run.EndExclusive; i++)
                    polygon.Add(new Point(pxArr[i], pyTop[i]));

                // Bottom edge: right to left
                for (int i = run.EndExclusive - 1; i >= run.StartInclusive; i--)
                    polygon.Add(new Point(pxArr[i], pyBot[i]));

                yield return new LayerPolygon(layer, Transform.CutBeyondTheStart(polygon, closed: true));
            }
        }
    }

    /// <inheritdoc />
    public override void Render(StackedAreaSeries series)
    {
        // Each layer is a filled polygon between its baseline and its top edge. Under the matplotlib themes it is drawn as matplotlib's
        // stackplot draws its fill_between layers: opaque unless an alpha was given, edged as every patch is (Theme.PatchEdgeOf:
        // black 1 pt under classic, none under v2). Under the library's own themes at the stack's alpha, with no edge.
        var theme = Context.Theme;
        double alpha = series.OpacityUnder(theme);
        var edges = theme.PatchEdgesOf(series);
        var edge = edges.Of();
        var hatching = series.Hatching;
        foreach (var layer in Layers(series))
        {
            var fillColor = PartColor(layer.Layer).WithOpacityGiven(alpha);
            Ctx.DrawPolygon(layer.Polygon, edges.StyleOf(fillColor, edge, hatching));
        }
    }
}
