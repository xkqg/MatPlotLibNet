// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SankeySeries"/> as node rectangles connected by curved bezier links.</summary>
internal sealed class SankeySeriesRenderer : SeriesRenderer<SankeySeries>
{
    public SankeySeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(SankeySeries series)
    {
        var bounds = Context.Area.PlotBounds;
        if (series.Nodes.Count == 0) return;

        // Compute node columns (simple: source nodes left, target nodes right)
        int nodeCount = series.Nodes.Count;
        var columns = ComputeColumns(series);
        int maxCol = columns.Max();

        // Compute node positions
        double colWidth = maxCol > 0 ? (bounds.Width - series.NodeWidth) / maxCol : bounds.Width;
        var nodeValues = new double[nodeCount];
        foreach (var link in series.Links)
        {
            nodeValues[link.SourceIndex] += link.Value;
            nodeValues[link.TargetIndex] += link.Value;
        }

        // Group nodes by column
        var colGroups = new Dictionary<int, List<int>>();
        for (int i = 0; i < nodeCount; i++)
        {
            int col = columns[i];
            if (!colGroups.ContainsKey(col)) colGroups[col] = new List<int>();
            colGroups[col].Add(i);
        }

        // Position nodes
        var nodeRects = new Rect[nodeCount];
        foreach (var (col, nodeIndices) in colGroups)
        {
            double totalValue = nodeIndices.Sum(i => nodeValues[i]);
            double availableHeight = bounds.Height - (nodeIndices.Count - 1) * series.NodePadding;
            double x = bounds.X + col * colWidth;
            double y = bounds.Y;

            foreach (int idx in nodeIndices)
            {
                double h = totalValue > 0 ? availableHeight * (nodeValues[idx] / totalValue) : availableHeight / nodeIndices.Count;
                nodeRects[idx] = new Rect(x, y, series.NodeWidth, Math.Max(1, h));
                y += h + series.NodePadding;
            }
        }

        // Draw links (bezier curves)
        var sourceOffsets = new double[nodeCount];
        var targetOffsets = new double[nodeCount];

        foreach (var link in series.Links)
        {
            var srcRect = nodeRects[link.SourceIndex];
            var tgtRect = nodeRects[link.TargetIndex];

            double srcHeight = srcRect.Height * (link.Value / nodeValues[link.SourceIndex]);
            double tgtHeight = tgtRect.Height * (link.Value / nodeValues[link.TargetIndex]);

            double srcY = srcRect.Y + sourceOffsets[link.SourceIndex];
            double tgtY = tgtRect.Y + targetOffsets[link.TargetIndex];

            double srcX = srcRect.X + series.NodeWidth;
            double tgtX = tgtRect.X;
            double midX = (srcX + tgtX) / 2;

            var srcColor = series.Nodes[link.SourceIndex].Color ?? ResolveColor(null);
            var linkColor = ApplyAlpha(srcColor, series.LinkAlpha);

            // Top curve
            var segments = new List<PathSegment>
            {
                new MoveToSegment(new Point(srcX, srcY)),
                new BezierSegment(new Point(midX, srcY), new Point(midX, tgtY), new Point(tgtX, tgtY)),
                new LineToSegment(new Point(tgtX, tgtY + tgtHeight)),
                new BezierSegment(new Point(midX, tgtY + tgtHeight), new Point(midX, srcY + srcHeight), new Point(srcX, srcY + srcHeight)),
                new CloseSegment()
            };
            Ctx.DrawPath(segments, linkColor, null, 0);

            sourceOffsets[link.SourceIndex] += srcHeight;
            targetOffsets[link.TargetIndex] += tgtHeight;
        }

        // Draw nodes
        for (int i = 0; i < nodeCount; i++)
        {
            var color = series.Nodes[i].Color ?? ResolveColor(null);
            Ctx.DrawRectangle(nodeRects[i], color, null, 0);

            // Label
            var font = new Font { Size = 10 };
            double labelX = nodeRects[i].X + series.NodeWidth + 4;
            if (columns[i] == maxCol)
                labelX = nodeRects[i].X - 4;

            Ctx.DrawText(series.Nodes[i].Label,
                new Point(labelX, nodeRects[i].Y + nodeRects[i].Height / 2 + 4),
                font, columns[i] == maxCol ? TextAlignment.Right : TextAlignment.Left);
        }
    }

    private static int[] ComputeColumns(SankeySeries series)
    {
        int n = series.Nodes.Count;
        var cols = new int[n];
        var visited = new bool[n];

        // Find source nodes (not targeted by any link)
        var isTarget = new bool[n];
        foreach (var link in series.Links)
            isTarget[link.TargetIndex] = true;

        // BFS from sources
        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (!isTarget[i])
            {
                cols[i] = 0;
                visited[i] = true;
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            int src = queue.Dequeue();
            foreach (var link in series.Links)
            {
                if (link.SourceIndex == src && !visited[link.TargetIndex])
                {
                    cols[link.TargetIndex] = cols[src] + 1;
                    visited[link.TargetIndex] = true;
                    queue.Enqueue(link.TargetIndex);
                }
            }
        }

        return cols;
    }
}
