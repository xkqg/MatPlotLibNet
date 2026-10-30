// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PointAndFigureSeries"/>: every box of every column is a cell of one slot by one box size in
/// data units, put on the plot by the one rule that places a box standing on a foot
/// (<see cref="PixelBoxExtensions.BoxOnFoot"/>); a rising column fills its cells with an X (two crossing lines), a falling one
/// with an O (an ellipse inscribed in the cell), each inset a fraction of the cell from its edges.</summary>
internal sealed class PointAndFigureSeriesRenderer : SeriesRenderer<PointAndFigureSeries>
{
    /// <inheritdoc />
    public PointAndFigureSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PointAndFigureSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <summary>One box of a column: the cell it fills in pixels and whether it holds an X.</summary>
    private readonly record struct Cell(Rect Box, bool Rising);

    /// <summary>The cells of the chart column by column, each column from the box it begins at to the box it ends at: the one
    /// geometry <see cref="Render"/> draws.</summary>
    private IEnumerable<Cell> Cells(PointAndFigureSeries series)
    {
        var columns = series.Columns();
        for (int k = 0; k < columns.Count; k++)
        {
            var column = columns[k];
            var slot = CategorySlot.Of(k);
            var across = new CategorySlot(slot.Left + PointAndFigureSeries.MarkInset, slot.Centre, slot.Right - PointAndFigureSeries.MarkInset);
            for (int box = Math.Min(column.From, column.To); box <= Math.Max(column.From, column.To); box++)
            {
                double foot = (box + PointAndFigureSeries.MarkInset) * series.BoxSize;
                double end = (box + 1 - PointAndFigureSeries.MarkInset) * series.BoxSize;
                yield return new Cell(Transform.BoxOnFoot(across, foot, end, rising: true, BarOrientation.Vertical).Rect, column.Rising);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(PointAndFigureSeries series)
    {
        foreach (var cell in Cells(series))
        {
            if (!cell.Box.IsDrawable())
            {
                continue;
            }

            var stroke = new StrokeStyle(cell.Rising ? series.UpColor : series.DownColor, series.LineWidth, LineStyle.Solid);
            if (cell.Rising)
            {
                var box = cell.Box;
                Ctx.DrawLine(new Point(box.X, box.Y), new Point(box.X + box.Width, box.Y + box.Height), stroke);
                Ctx.DrawLine(new Point(box.X, box.Y + box.Height), new Point(box.X + box.Width, box.Y), stroke);
            }
            else
            {
                Ctx.DrawEllipse(cell.Box, new ShapeStyle(null, stroke.Color, stroke.Thickness));
            }
        }
    }
}
