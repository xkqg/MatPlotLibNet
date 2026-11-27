// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders an <see cref="IcicleSeries"/>: one row of cells for each level of the hierarchy under the root, the cells of
/// a node side by side under it, each as wide as its share of the node's width (<see cref="TreeNodePartitionExtensions"/>, the
/// one rule every hierarchical layout partitions by). The rows share the plot area's height equally, and a cell is as tall as one row.</summary>
internal sealed class IcicleSeriesRenderer : SeriesRenderer<IcicleSeries>
{

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

        var scale = series.ColorScaleUnder(Context.Theme);
        double rowHeight = bounds.Height / levels;
        foreach (var cell in Under(series.Root, bounds.X, bounds.Width, 0)) yield return cell;

        IEnumerable<IcicleCell> Under(TreeNode parent, double x, double width, int depth)
        {
            var shares = parent.Shares();
            for (int i = 0; i < shares.Count; i++)
            {
                var (child, share) = shares[i];
                double cellWidth = width * share;
                var colour = HierarchicalSeries.ColourOf(child, scale, i, shares.Count);
                yield return new IcicleCell(child, new Rect(x, bounds.Y + depth * rowHeight, cellWidth, rowHeight), colour);
                foreach (var below in Under(child, x, cellWidth, depth + 1)) yield return below;
                x += cellWidth;
            }
        }
    }

    /// <inheritdoc />
    public override void Render(IcicleSeries series)
    {
        double labelSize = series.TextSizeUnder(Context.Theme);
        foreach (var cell in Cells(series))
        {
            Ctx.DrawRectangle(cell.Box, PatchEdge.WhitePixel.StyleOf(cell.Color, cell.Node.Hatching.IsDrawn).WithHatch(cell.Node.Hatching));

            // A label is drawn where it fits its cell, in the ink that reads against the cell's colour; a cell too narrow to hold
            // its name stays unnamed.
            if (series.ShowLabels && !string.IsNullOrEmpty(cell.Node.Label)
                && cell.Box.Height >= CellLabel.Standard.HeightFor(labelSize))
            {
                var font = Context.Theme.DataTextFontOn(cell.Color, labelSize, GroundInk.Readable);
                if (Ctx.ExtentOf(TextLabel.Of(cell.Node.Label), font).Width <= CellLabel.Standard.RoomIn(cell.Box))
                {
                    Ctx.Draw(TextLabel.Of(cell.Node.Label), font, CellLabel.Standard.Name(cell.Box));
                }
            }
        }
    }
}
