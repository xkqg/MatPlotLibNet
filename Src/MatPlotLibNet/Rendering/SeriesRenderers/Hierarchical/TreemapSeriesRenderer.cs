// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>
/// Renders a <see cref="TreemapSeries"/> using the squarified treemap algorithm
/// (Bruls, Huijse, van Wijk 1999). Leaves whose nodes have no explicit colour
/// are coloured by their index through the series colormap (default viridis) so
/// flat trees don't render as a single-colour block.
/// </summary>
internal sealed class TreemapSeriesRenderer : SeriesRenderer<TreemapSeries>
{
    /// <inheritdoc />
    public TreemapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(TreemapSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        RenderNode(series.Root, bounds, series, cmap,
            depth: 0, indexInParent: 0, siblingCount: 1, nodeId: "0", parentId: "");
    }

    /// <summary>
    /// Renders one node and recurses into its children. <paramref name="nodeId"/> is a
    /// deterministic path-based identifier (root = "0", first child of root = "0.0", etc.)
    /// used by <c>SvgTreemapDrilldownScript</c> to navigate the hierarchy on click. Path IDs
    /// are emitted as <c>data-treemap-*</c> attributes on every rect regardless of whether
    /// interactivity is enabled — the cost is 3 attributes per rect (~60 bytes each) and the
    /// drilldown JS is only loaded when <see cref="Models.Figure.EnableTreemapDrilldown"/>
    /// is set, so non-interactive renders carry harmless unused attributes.
    /// </summary>
    private void RenderNode(TreeNode node, Rect bounds, TreemapSeries series,
        IColorMap cmap, int depth, int indexInParent, int siblingCount,
        string nodeId, string parentId)
    {
        if (node.Children.Count == 0)
        {
            // Leaf: fill with node colour, or cmap sample at the sibling fraction.
            var color = node.Color
                ?? cmap.GetColor(siblingCount > 1 ? indexInParent / (double)(siblingCount - 1) : 0.5);
            Ctx.SetNextElementData("treemap-node", nodeId);
            Ctx.SetNextElementData("treemap-depth", depth.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Ctx.SetNextElementData("treemap-parent", parentId);
            Ctx.DrawRectangle(bounds, color, Colors.White, 1);
            if (series.ShowLabels && !string.IsNullOrEmpty(node.Label))
            {
                // Measure before drawing so labels only appear when they actually fit. This
                // replaces the old fixed 20×14 threshold with a font-metrics–driven check
                // that honours the caller's theme font size.
                var font = new Font { Size = 10, Color = Colors.White };
                var size = ChartServices.FontMetrics.Measure(node.Label, font);
                if (size.Width + 8 <= bounds.Width && size.Height + 4 <= bounds.Height)
                    Ctx.DrawText(node.Label, new Point(bounds.X + 4, bounds.Y + 14),
                        font, TextAlignment.Left);
            }
            return;
        }

        // Interior node: emit its own rect (as an invisible hit target with the same
        // bounds) so clicking a group cell drills into it rather than the child rect.
        // Browser click events on an SVG group are unreliable, so we use an explicit
        // transparent rect with pointer-events. We only emit this when the node actually
        // has children — leaves are drawn above.
        Ctx.SetNextElementData("treemap-node", nodeId);
        Ctx.SetNextElementData("treemap-depth", depth.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Ctx.SetNextElementData("treemap-parent", parentId);
        // Invisible hit rect: zero stroke, zero fill alpha. Drilldown script still picks it up.
        Ctx.DrawRectangle(bounds, Colors.Black.WithAlpha(0), null, 0);

        // Squarify the children into the remaining bounds.
        var children = node.Children.OrderByDescending(c => c.TotalValue).ToList();
        double total = children.Sum(c => c.TotalValue);
        if (total <= 0) return;

        var rects = Squarify(children, bounds, total, series.Padding);
        for (int i = 0; i < children.Count; i++)
        {
            string childId = nodeId + "." + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            RenderNode(children[i], rects[i], series, cmap,
                depth + 1, i, children.Count, childId, nodeId);
        }
    }

    /// <summary>Squarified treemap layout (Bruls et al. 1999). Produces rectangles with
    /// aspect ratios close to 1, much better than naive slice-and-dice for flat trees.</summary>
    private static Rect[] Squarify(IReadOnlyList<TreeNode> nodes, Rect bounds, double total, double pad)
    {
        var rects = new Rect[nodes.Count];

        // Scale values to the available pixel area.
        double area = bounds.Width * bounds.Height;
        double scale = area / total;
        double[] sizes = new double[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
            sizes[i] = nodes[i].TotalValue * scale;

        // Running cursor + "row" accumulator. Direction: lay out rows along the shorter side.
        var row = new List<int>();
        double x = bounds.X, y = bounds.Y;
        double w = bounds.Width, h = bounds.Height;
        int index = 0;

        while (index < nodes.Count)
        {
            double shorter = Math.Min(w, h);
            // Try adding the next node to the current row — if aspect ratio gets worse, emit row.
            double rowSum = row.Sum(i => sizes[i]);
            int cand = index;
            double candSum = rowSum + sizes[cand];
            if (row.Count == 0 || Worst(row, rowSum, shorter, sizes) >= Worst(Append(row, cand), candSum, shorter, sizes))
            {
                row.Add(cand);
                index++;
                continue;
            }

            // Emit the current row perpendicular to the shorter side.
            EmitRow(row, sizes, ref x, ref y, ref w, ref h, rects, pad);
            row.Clear();
        }
        // Emit leftover row.
        if (row.Count > 0)
            EmitRow(row, sizes, ref x, ref y, ref w, ref h, rects, pad);

        return rects;
    }

    private static double Worst(IReadOnlyList<int> row, double sum, double shorter, double[] sizes)
    {
        if (row.Count == 0 || sum <= 0) return double.PositiveInfinity;
        double max = double.MinValue, min = double.MaxValue;
        foreach (int i in row)
        {
            if (sizes[i] > max) max = sizes[i];
            if (sizes[i] < min) min = sizes[i];
        }
        double s2 = sum * sum;
        double side2 = shorter * shorter;
        return Math.Max(side2 * max / s2, s2 / (side2 * min));
    }

    private static int[] Append(List<int> row, int extra)
    {
        var arr = new int[row.Count + 1];
        for (int i = 0; i < row.Count; i++) arr[i] = row[i];
        arr[^1] = extra;
        return arr;
    }

    private static void EmitRow(List<int> row, double[] sizes,
        ref double x, ref double y, ref double w, ref double h, Rect[] rects, double pad)
    {
        if (row.Count == 0) return;
        double rowSum = 0;
        foreach (int i in row) rowSum += sizes[i];
        if (rowSum <= 0) return;

        bool horizontalStrip = w >= h;   // lay out along the shorter side
        double stripWidth = rowSum / (horizontalStrip ? h : w);

        if (horizontalStrip)
        {
            double cursorY = y;
            foreach (int i in row)
            {
                double cellH = sizes[i] / stripWidth;
                rects[i] = new Rect(x + pad / 2, cursorY + pad / 2,
                    Math.Max(1, stripWidth - pad), Math.Max(1, cellH - pad));
                cursorY += cellH;
            }
            x += stripWidth;
            w -= stripWidth;
        }
        else
        {
            double cursorX = x;
            foreach (int i in row)
            {
                double cellW = sizes[i] / stripWidth;
                rects[i] = new Rect(cursorX + pad / 2, y + pad / 2,
                    Math.Max(1, cellW - pad), Math.Max(1, stripWidth - pad));
                cursorX += cellW;
            }
            y += stripWidth;
            h -= stripWidth;
        }
    }
}
