// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TreemapSeries"/> as nested rectangles using a slice-and-dice layout.</summary>
internal sealed class TreemapSeriesRenderer : SeriesRenderer<TreemapSeries>
{
    /// <inheritdoc />
    public TreemapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(TreemapSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        RenderNode(series.Root, bounds, series, 0);
    }

    private void RenderNode(TreeNode node, Rect bounds, TreemapSeries series, int depth)
    {
        if (node.Children.Count == 0)
        {
            // Leaf: draw rectangle
            var color = node.Color ?? ResolveColor(null);
            Ctx.DrawRectangle(bounds, color, Colors.White, 1);
            if (series.ShowLabels && bounds.Width > 20 && bounds.Height > 14)
                Ctx.DrawText(node.Label, new Point(bounds.X + 4, bounds.Y + 14),
                    new Font { Size = 10 }, TextAlignment.Left);
            return;
        }

        double total = node.TotalValue;
        if (total <= 0) return;

        // Squarified layout: alternate horizontal/vertical splits
        bool horizontal = bounds.Width >= bounds.Height;
        double offset = 0;

        foreach (var child in node.Children)
        {
            double fraction = child.TotalValue / total;
            Rect childBounds;
            double pad = series.Padding;

            if (horizontal)
            {
                double w = bounds.Width * fraction;
                childBounds = new Rect(bounds.X + offset + pad / 2, bounds.Y + pad / 2, Math.Max(1, w - pad), Math.Max(1, bounds.Height - pad));
                offset += w;
            }
            else
            {
                double h = bounds.Height * fraction;
                childBounds = new Rect(bounds.X + pad / 2, bounds.Y + offset + pad / 2, Math.Max(1, bounds.Width - pad), Math.Max(1, h - pad));
                offset += h;
            }

            RenderNode(child, childBounds, series, depth + 1);
        }
    }
}
