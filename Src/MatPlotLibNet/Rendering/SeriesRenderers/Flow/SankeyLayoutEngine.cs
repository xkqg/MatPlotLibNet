// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers.Flow;

/// <summary>
/// Computes the pixel layout of a Sankey diagram: column assignment (BFS /
/// alignment), per-node flow values, greedy packing along the flow axis, and
/// iterative relaxation to minimise link crossings.
/// </summary>
/// <remarks>
/// Phase B.10 of the strict-90 floor plan (2026-04-20). Extracted from
/// <see cref="SankeySeriesRenderer"/> which previously owned both layout
/// and rendering in one class — a SRP violation. Now:
/// <list type="bullet">
///   <item><description><see cref="SankeyLayoutEngine"/> = layout (this class)</description></item>
///   <item><description><see cref="SankeySeriesRenderer"/> = drawing</description></item>
/// </list>
/// The renderer calls <see cref="Compute"/> once per render, then consumes
/// the returned <see cref="SankeyLayout"/> to draw links and nodes.
/// </remarks>
public sealed class SankeyLayoutEngine
{
    /// <summary>Computes the complete layout for a Sankey series within the given bounds.
    /// Returns <see langword="null"/> for degenerate inputs (empty series, unreachable layout).</summary>
    public SankeyLayout? Compute(SankeySeries series, Rect bounds)
    {
        int n = series.Nodes.Count;
        if (n == 0) return null;

        // 1. Column assignment
        var columns = ComputeColumns(series);
        int maxCol = columns.Max();

        // 2. Alignment post-processing
        ApplyAlignment(series, columns, maxCol);
        maxCol = columns.Max();

        // 3. Per-node total value
        var nodeValues = ComputeNodeValues(series);

        // 4. Group node indices by column and sort within each column by total value
        var colGroups = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            if (!colGroups.TryGetValue(columns[i], out var list))
                colGroups[columns[i]] = list = new List<int>();
            list.Add(i);
        }
        foreach (var kv in colGroups)
            kv.Value.Sort((a, b) => nodeValues[b].CompareTo(nodeValues[a]));

        // 5. Initial greedy packing along the flow axis
        bool vert = series.Orient == SankeyOrientation.Vertical;
        var nodeRects = new Rect[n];
        double primaryLen = vert ? bounds.Height : bounds.Width;
        double crossLen   = vert ? bounds.Width  : bounds.Height;
        double colStep = maxCol > 0 ? (primaryLen - series.NodeWidth) / maxCol : 0;
        double totalValuePerCol = ComputeMaxColumnValueSum(colGroups, nodeValues);
        double availableCross = Math.Max(1,
            crossLen - MaxPaddingSum(colGroups, series.NodePadding));
        double valueScale = totalValuePerCol > 0 ? availableCross / totalValuePerCol : 0;

        double primaryOrigin = vert ? bounds.Y : bounds.X;
        double crossOrigin   = vert ? bounds.X : bounds.Y;

        foreach (var (col, nodeIndices) in colGroups)
        {
            double primaryPos = primaryOrigin + col * colStep;
            double crossPos = crossOrigin;
            foreach (int idx in nodeIndices)
            {
                double size = Math.Max(1, nodeValues[idx] * valueScale);
                nodeRects[idx] = vert
                    ? new Rect(crossPos, primaryPos, size, series.NodeWidth)
                    : new Rect(primaryPos, crossPos, series.NodeWidth, size);
                crossPos += size + series.NodePadding;
            }
        }

        // 6. Iterative relaxation
        Relax(series, columns, colGroups, nodeRects, nodeValues, bounds, vert);

        return new SankeyLayout(columns, maxCol, nodeRects, nodeValues, vert);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Column assignment
    // ──────────────────────────────────────────────────────────────────────────

    private static int[] ComputeColumns(SankeySeries series)
    {
        int n = series.Nodes.Count;
        var cols = new int[n];
        var fromOverride = new bool[n];

        for (int i = 0; i < n; i++)
        {
            if (series.Nodes[i].Column is int c)
            {
                cols[i] = c;
                fromOverride[i] = true;
            }
            else
            {
                cols[i] = -1;
            }
        }

        var isTarget = new bool[n];
        foreach (var link in series.Links)
            isTarget[link.TargetIndex] = true;

        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (fromOverride[i])
            {
                queue.Enqueue(i);
            }
            else if (!isTarget[i])
            {
                cols[i] = 0;
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            int src = queue.Dequeue();
            foreach (var link in series.Links)
            {
                if (link.SourceIndex != src) continue;
                int tgt = link.TargetIndex;
                if (fromOverride[tgt]) continue;
                int nextCol = cols[src] + 1;
                if (cols[tgt] < nextCol)
                {
                    cols[tgt] = nextCol;
                    queue.Enqueue(tgt);
                }
            }
        }

        for (int i = 0; i < n; i++) if (cols[i] < 0) cols[i] = 0;
        return cols;
    }

    private static void ApplyAlignment(SankeySeries series, int[] columns, int maxCol)
    {
        if (series.NodeAlignment == SankeyNodeAlignment.Justify) return;

        int n = columns.Length;
        var latest = new int[n];
        for (int i = 0; i < n; i++) latest[i] = maxCol;

        for (int pass = 0; pass < n; pass++)
        {
            bool changed = false;
            foreach (var link in series.Links)
            {
                int candidate = latest[link.TargetIndex] - 1;
                if (candidate < latest[link.SourceIndex])
                {
                    latest[link.SourceIndex] = candidate;
                    changed = true;
                }
            }
            if (!changed) break;
        }
        for (int i = 0; i < n; i++) if (latest[i] < columns[i]) latest[i] = columns[i];

        for (int i = 0; i < n; i++)
        {
            columns[i] = series.NodeAlignment switch
            {
                SankeyNodeAlignment.Right  => latest[i],
                SankeyNodeAlignment.Center => (columns[i] + latest[i]) / 2,
                _                          => columns[i],
            };
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Node value aggregation
    // ──────────────────────────────────────────────────────────────────────────

    private static double[] ComputeNodeValues(SankeySeries series)
    {
        int n = series.Nodes.Count;
        var outVal = new double[n];
        var inVal = new double[n];
        foreach (var link in series.Links)
        {
            outVal[link.SourceIndex] += link.Value;
            inVal[link.TargetIndex] += link.Value;
        }
        var values = new double[n];
        for (int i = 0; i < n; i++)
            values[i] = Math.Max(outVal[i], inVal[i]);
        return values;
    }

    private static double ComputeMaxColumnValueSum(Dictionary<int, List<int>> colGroups, double[] values)
    {
        double max = 0;
        foreach (var group in colGroups.Values)
        {
            double sum = 0;
            foreach (int idx in group) sum += values[idx];
            if (sum > max) max = sum;
        }
        return max;
    }

    private static double MaxPaddingSum(Dictionary<int, List<int>> colGroups, double nodePadding)
    {
        int maxCount = 0;
        foreach (var group in colGroups.Values)
            if (group.Count > maxCount) maxCount = group.Count;
        return Math.Max(0, (maxCount - 1) * nodePadding);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Relaxation
    // ──────────────────────────────────────────────────────────────────────────

    private static void Relax(SankeySeries series, int[] columns,
        Dictionary<int, List<int>> colGroups, Rect[] nodeRects,
        double[] nodeValues, Rect bounds, bool vert)
    {
        if (series.Iterations <= 0) return;

        double alpha = 0.99;

        for (int iter = 0; iter < series.Iterations; iter++)
        {
            RelaxColumn(series, colGroups, nodeRects, upstream: true, alpha, vert);
            ResolveCollisions(colGroups, nodeRects, series.NodePadding, bounds, vert);
            RelaxColumn(series, colGroups, nodeRects, upstream: false, alpha, vert);
            ResolveCollisions(colGroups, nodeRects, series.NodePadding, bounds, vert);
            alpha *= 0.99;
        }
    }

    private static double CrossCentre(Rect r, bool vert) =>
        vert ? r.X + r.Width / 2 : r.Y + r.Height / 2;

    private static double CrossStart(Rect r, bool vert) => vert ? r.X : r.Y;

    private static double CrossSize(Rect r, bool vert) => vert ? r.Width : r.Height;

    private static Rect WithCrossStart(Rect r, double newStart, bool vert) =>
        vert ? new Rect(newStart, r.Y, r.Width, r.Height)
             : new Rect(r.X, newStart, r.Width, r.Height);

    private static void RelaxColumn(SankeySeries series,
        Dictionary<int, List<int>> colGroups, Rect[] nodeRects,
        bool upstream, double alpha, bool vert)
    {
        foreach (var (_, nodeIndices) in colGroups)
        {
            foreach (int idx in nodeIndices)
            {
                double weightedSum = 0;
                double weightTotal = 0;
                foreach (var link in series.Links)
                {
                    if (upstream && link.TargetIndex == idx)
                    {
                        weightedSum += CrossCentre(nodeRects[link.SourceIndex], vert) * link.Value;
                        weightTotal += link.Value;
                    }
                    else if (!upstream && link.SourceIndex == idx)
                    {
                        weightedSum += CrossCentre(nodeRects[link.TargetIndex], vert) * link.Value;
                        weightTotal += link.Value;
                    }
                }
                if (weightTotal == 0) continue;
                double desiredCentre = weightedSum / weightTotal;
                double currentCentre = CrossCentre(nodeRects[idx], vert);
                double shift = (desiredCentre - currentCentre) * alpha;
                double newStart = CrossStart(nodeRects[idx], vert) + shift;
                nodeRects[idx] = WithCrossStart(nodeRects[idx], newStart, vert);
            }
        }
    }

    private static void ResolveCollisions(Dictionary<int, List<int>> colGroups,
        Rect[] nodeRects, double nodePadding, Rect bounds, bool vert)
    {
        double crossMin = vert ? bounds.X : bounds.Y;
        double crossMax = vert ? bounds.X + bounds.Width : bounds.Y + bounds.Height;

        foreach (var (_, nodeIndices) in colGroups)
        {
            nodeIndices.Sort((a, b) =>
                CrossCentre(nodeRects[a], vert).CompareTo(CrossCentre(nodeRects[b], vert)));

            double cursor = crossMin;
            foreach (int idx in nodeIndices)
            {
                var r = nodeRects[idx];
                double newStart = Math.Max(CrossStart(r, vert), cursor);
                nodeRects[idx] = WithCrossStart(r, newStart, vert);
                cursor = newStart + CrossSize(r, vert) + nodePadding;
            }

            double overshoot = cursor - nodePadding - crossMax;
            if (overshoot > 0)
            {
                for (int k = nodeIndices.Count - 1; k >= 0; k--)
                {
                    int idx = nodeIndices[k];
                    var r = nodeRects[idx];
                    double newStart = CrossStart(r, vert) - overshoot;
                    nodeRects[idx] = WithCrossStart(r, newStart, vert);
                    if (k > 0)
                    {
                        int prev = nodeIndices[k - 1];
                        var pr = nodeRects[prev];
                        double maxAllowedPrevEnd = newStart - nodePadding;
                        double prevEnd = CrossStart(pr, vert) + CrossSize(pr, vert);
                        overshoot = prevEnd > maxAllowedPrevEnd ? prevEnd - maxAllowedPrevEnd : 0;
                    }
                }
            }
        }
    }
}
