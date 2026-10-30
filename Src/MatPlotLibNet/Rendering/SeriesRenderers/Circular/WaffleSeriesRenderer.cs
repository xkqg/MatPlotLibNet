// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="WaffleSeries"/>: a grid of square cells inside the plot area, centred, each cell a box in
/// the grid's own units (a cell is one unit across) mapped to pixels by the one rule that places a box standing on a foot
/// (<see cref="PixelBoxExtensions.BoxOnFoot"/>).</summary>
internal sealed class WaffleSeriesRenderer : SeriesRenderer<WaffleSeries>
{
    /// <inheritdoc />
    public WaffleSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(WaffleSeries series, MarkObstacles.Builder marks)
    {
        foreach (var cell in Cells(series))
        {
            if (cell.Box.IsDrawable()) marks.AddBox(cell.Box);
        }
    }

    /// <summary>One cell of the grid: the box it fills and the category it belongs to.</summary>
    private readonly record struct WaffleCell(Rect Box, int Category);

    /// <summary>The cells in the order they are filled: from the bottom-left corner, row by row from the left, category by
    /// category. The grid is the largest of square cells that fits the plot area, centred in it, so its cells are square at
    /// any size of the figure; the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<WaffleCell> Cells(WaffleSeries series)
    {
        var bounds = Area.PlotBounds;
        double side = Math.Min(bounds.Width / series.Columns, bounds.Height / series.Rows);
        if (!(side > 0)) yield break;

        var grid = new Rect(
            bounds.X + (bounds.Width - side * series.Columns) / 2,
            bounds.Y + (bounds.Height - side * series.Rows) / 2,
            side * series.Columns, side * series.Rows);
        // The grid in its own units: a cell is one unit across, the first row at the foot of the grid.
        var units = new DataTransform(0, series.Columns, 0, series.Rows, grid);
        double clear = series.CellGap / 2;

        int cell = 0;
        var counts = series.CellCounts();
        for (int category = 0; category < counts.Length; category++)
        {
            for (int n = 0; n < counts[category]; n++, cell++)
            {
                int row = cell / series.Columns, column = cell % series.Columns;
                var slot = new CategorySlot(column + clear, column + 0.5, column + 1 - clear);
                yield return new WaffleCell(units.BoxOnFoot(slot, row + clear, row + 1 - clear, rising: true, BarOrientation.Vertical).Rect, category);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(WaffleSeries series)
    {
        foreach (var cell in Cells(series))
        {
            if (cell.Box.IsDrawable())
                Ctx.DrawRectangle(cell.Box, new ShapeStyle(PartColor(series.Colors, cell.Category), null, 0));
        }
    }
}
