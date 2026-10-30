// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders an <see cref="IcicleSeries"/>: one row of cells for each level of the hierarchy under the root, the cells of
/// a node side by side under it, each as wide as its share of the node's width (<see cref="TreeNodePartitionExtensions"/>, the
/// one rule every hierarchical layout partitions by). The rows share the plot area's height equally, and a cell is as tall as one row.</summary>
internal sealed class IcicleSeriesRenderer : SeriesRenderer<IcicleSeries>
{
    /// <summary>The point size of a cell's label.</summary>
    private const double LabelSize = 12;

    /// <summary>The pixels between a cell's edge and its label.</summary>
    private const double LabelPad = 4;

    /// <inheritdoc />
    public IcicleSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>One cell: the node it stands for, where it stands and the colour it wears.</summary>
    private readonly record struct IcicleCell(TreeNode Node, Rect Box, Color Color);

    /// <summary>The cells in the order they are drawn: a node before the nodes under it, siblings in the order they are stored.
    /// A cell's colour is its node's own, otherwise the node's value through the colour map, otherwise the colour map at the
    /// node's place among its siblings — the rule a treemap colours by.</summary>
    private IEnumerable<IcicleCell> Cells(IcicleSeries series)
    {
        var bounds = Area.PlotBounds;
        int levels = series.Root.Levels();
        if (levels == 0 || !(bounds.Width > 0) || !(bounds.Height > 0)) yield break;

        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var range = series.ColorRange();
        double rowHeight = bounds.Height / levels;
        foreach (var cell in Under(series.Root, bounds.X, bounds.Width, 0)) yield return cell;

        IEnumerable<IcicleCell> Under(TreeNode parent, double x, double width, int depth)
        {
            var shares = parent.Shares();
            for (int i = 0; i < shares.Count; i++)
            {
                var (child, share) = shares[i];
                double cellWidth = width * share;
                var colour = child.Color
                    ?? series.ColorFromValue(child, cmap, range.Lo, range.Hi)
                    ?? cmap.GetColor(i.ColormapFraction(shares.Count));
                yield return new IcicleCell(child, new Rect(x, bounds.Y + depth * rowHeight, cellWidth, rowHeight), colour);
                foreach (var below in Under(child, x, cellWidth, depth + 1)) yield return below;
                x += cellWidth;
            }
        }
    }

    /// <inheritdoc />
    public override void Render(IcicleSeries series)
    {
        var font = new Font { Size = LabelSize, Color = Colors.White };
        foreach (var cell in Cells(series))
        {
            Ctx.DrawRectangle(cell.Box, new ShapeStyle(cell.Color, Colors.White, 1) { Hatch = cell.Node.Hatch, HatchColor = cell.Node.HatchColor });

            // A label is drawn where it fits its cell; a cell too narrow to hold its name stays unnamed.
            if (series.ShowLabels && !string.IsNullOrEmpty(cell.Node.Label)
                && cell.Box.Height >= LabelSize + 2 * LabelPad
                && Ctx.MeasureText(cell.Node.Label, font).Width <= cell.Box.Width - 2 * LabelPad)
            {
                Ctx.DrawText(cell.Node.Label, new Point(cell.Box.X + LabelPad, cell.Box.Y + LabelSize + 2), font, TextAlignment.Left);
            }
        }
    }
}
