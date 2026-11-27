// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Numerics;

/// <summary>One child of a treemap node and the cell the squarified layout gives it.</summary>
/// <param name="Node">The child.</param>
/// <param name="Bounds">Where it is drawn, its padding taken off.</param>
internal readonly record struct SquarifiedCell(TreeNode Node, Rect Bounds);

/// <summary>The squarified treemap layout (Bruls, Huijse and van Wijk): the children of a node, largest first, fill its area in
/// rows laid along the shorter side, and a row takes the next child only while that keeps its cells closer to squares.</summary>
internal static class SquarifiedLayout
{
    /// <summary>The cells of the children of <paramref name="node"/> inside <paramref name="bounds"/>: largest first (children of
    /// equal value in the order they are stored), each sized in proportion to its total value and shrunk by half of
    /// <paramref name="pad"/> on every side, never below one pixel. Children whose total is not a positive number (nothing in
    /// them, a negative sum, a value that is not a number or an infinite one) have no area to share and get no cell, the rule
    /// a sunburst's partition follows.</summary>
    /// <param name="node">The node whose children are laid out.</param>
    /// <param name="bounds">The area the children share.</param>
    /// <param name="pad">The gap left between neighbouring cells, in pixels.</param>
    /// <param name="fromBottom">Whether the layout starts at the bottom left, as squarify's does in a Y that runs up: the same layout turned
    /// over in <paramref name="bounds"/>, each cell keeping its size and its padding.</param>
    /// <returns>One cell per child, largest first; none for a leaf or for children worth nothing.</returns>
    internal static SquarifiedCell[] Of(TreeNode node, Rect bounds, double pad, bool fromBottom = false)
    {
        var children = node.Children;
        int n = children.Count;
        var values = new double[n];
        for (int i = 0; i < n; i++) values[i] = children[i].TotalValue;

        int[] order = values.DescendingOrder();

        var sizes = new double[n];
        double total = 0;
        for (int k = 0; k < n; k++)
        {
            sizes[k] = values[order[k]];
            total += sizes[k];
        }

        if (!total.IsPositiveFinite()) return [];

        var cells = new SquarifiedCell[n];
        double scale = bounds.Width * bounds.Height / total;
        for (int k = 0; k < n; k++)
        {
            sizes[k] *= scale;
            cells[k] = new SquarifiedCell(children[order[k]], default);
        }

        var cursor = new Cursor(bounds);
        int start = 0, next = 0;
        double rowSum = 0, rowMax = double.MinValue, rowMin = double.MaxValue;
        while (next < n)
        {
            double size = sizes[next];
            double withSum = rowSum + size;
            double withMax = size > rowMax ? size : rowMax, withMin = size < rowMin ? size : rowMin;
            double shorter = Math.Min(cursor.W, cursor.H);
            if (next == start || Worst(rowSum, rowMax, rowMin, shorter) >= Worst(withSum, withMax, withMin, shorter))
            {
                rowSum = withSum;
                rowMax = withMax;
                rowMin = withMin;
                next++;
                continue;
            }

            cursor.Emit(cells, sizes, start, next, rowSum, pad);
            start = next;
            rowSum = 0;
            rowMax = double.MinValue;
            rowMin = double.MaxValue;
        }

        if (next > start) cursor.Emit(cells, sizes, start, next, rowSum, pad);
        if (fromBottom)
        {
            for (int k = 0; k < n; k++)
            {
                var at = cells[k].Bounds;
                cells[k] = cells[k] with { Bounds = at with { Y = bounds.Y + bounds.Height - (at.Y - bounds.Y) - at.Height } };
            }
        }

        return cells;
    }

    /// <summary>The worst aspect ratio of a row of cells summing to <paramref name="sum"/>, its largest
    /// <paramref name="max"/> and its smallest <paramref name="min"/>, laid along a side of <paramref name="shorter"/>.</summary>
    private static double Worst(double sum, double max, double min, double shorter)
    {
        if (sum <= 0) return double.PositiveInfinity;
        double s2 = sum * sum;
        double side2 = shorter * shorter;
        return Math.Max(side2 * max / s2, s2 / (side2 * min));
    }

    /// <summary>The part of the area no row has taken yet.</summary>
    private struct Cursor(Rect bounds)
    {
        private double _x = bounds.X, _y = bounds.Y;

        internal double W = bounds.Width, H = bounds.Height;

        /// <summary>Lays the cells <paramref name="start"/> up to <paramref name="end"/> in one strip along the shorter side and
        /// takes the strip off the area. A row worth nothing is not laid.</summary>
        internal void Emit(SquarifiedCell[] cells, double[] sizes, int start, int end, double rowSum, double pad)
        {
            if (rowSum <= 0) return;

            bool horizontalStrip = W >= H;
            double stripWidth = rowSum / (horizontalStrip ? H : W);
            if (horizontalStrip)
            {
                double cursorY = _y;
                for (int k = start; k < end; k++)
                {
                    double cellH = sizes[k] / stripWidth;
                    cells[k] = cells[k] with
                    {
                        Bounds = new Rect(_x + pad / 2, cursorY + pad / 2, Math.Max(1, stripWidth - pad), Math.Max(1, cellH - pad)),
                    };
                    cursorY += cellH;
                }

                _x += stripWidth;
                W -= stripWidth;
            }
            else
            {
                double cursorX = _x;
                for (int k = start; k < end; k++)
                {
                    double cellW = sizes[k] / stripWidth;
                    cells[k] = cells[k] with
                    {
                        Bounds = new Rect(cursorX + pad / 2, _y + pad / 2, Math.Max(1, cellW - pad), Math.Max(1, stripWidth - pad)),
                    };
                    cursorX += cellW;
                }

                _y += stripWidth;
                H -= stripWidth;
            }
        }
    }
}
