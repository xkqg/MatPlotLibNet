// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TreeGridSeries"/>: a header row, then one indented row per
/// <see cref="TreeGridRow"/> with its values right-aligned in fixed columns.
/// <para>A row that leads somewhere is wrapped in a hyperlink (the same idiom the stat tile's drill-down uses),
/// so a tree opens and closes through the URL with no script anywhere — and it carries <c>aria-level</c> and
/// <c>aria-expanded</c>, which is the difference between a grid a reader can navigate and a picture of one.</para></summary>
internal sealed class TreeGridSeriesRenderer : SeriesRenderer<TreeGridSeries>
{
    /// <summary>Room left of a label for its chevron.</summary>
    private const double ChevronColumn = 12;

    /// <inheritdoc />
    public TreeGridSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(TreeGridSeries series)
    {
        if (series.Rows.Count == 0)
        {
            return;     // nothing known is nothing drawn — never an empty frame that looks like a fact
        }

        var bounds = Area.PlotBounds;
        int columns = 0;
        foreach (var row in series.Rows)
        {
            columns = Math.Max(columns, row.Cells?.Count ?? 0);
        }
        columns = Math.Max(columns, series.ColumnHeaders?.Length ?? 0);

        double valuesWidth = columns * series.ColumnWidth;
        double nameWidth = Math.Max(60, bounds.Width - valuesWidth);
        var ink = Context.Theme.ForegroundText;
        var font = new Font { Size = series.FontSize, Color = ink };
        double y = bounds.Y + series.RowHeight;

        if (series.ColumnHeaders is { Length: > 0 } headers)
        {
            var headerFont = new Font { Size = series.FontSize, Weight = FontWeight.Bold, Color = ink };
            for (int c = 0; c < headers.Length && c < columns; c++)
            {
                Ctx.DrawText(headers[c], new Point(ColumnRight(bounds, nameWidth, series, c), y), headerFont, TextAlignment.Right);
            }
            Ctx.DrawLine(new Point(bounds.X, y + 4), new Point(bounds.X + bounds.Width, y + 4),
                new StrokeStyle(Context.Theme.DefaultGrid.Color, 1, LineStyle.Solid));
            y += series.RowHeight;
        }

        foreach (var row in series.Rows)
        {
            if (y > bounds.Y + bounds.Height)
            {
                break;  // the region is what it is; a row drawn past its edge is a row on top of the next panel
            }
            RenderRow(series, row, bounds, nameWidth, y, ink);
            y += series.RowHeight;
        }
    }

    private void RenderRow(TreeGridSeries series, TreeGridRow row, Rect bounds, double nameWidth, double y, Color ink)
    {
        bool linked = !string.IsNullOrEmpty(row.Url);
        if (linked)
        {
            Ctx.BeginHyperlink(row.Url!, row.Label, row.Expanded);
        }
        Ctx.SetNextElementData("treegrid-level", (row.Depth + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));

        double x = bounds.X + (row.Depth * series.IndentWidth);
        if (row.Expanded is { } expanded)
        {
            DrawChevron(x + 2, y, series.FontSize, expanded, ink);
        }
        var rowFont = new Font { Size = series.FontSize, Color = row.Accent ?? ink };
        Ctx.DrawTextWithLevel(row.Label, new Point(x + ChevronColumn, y), rowFont, row.Depth + 1);

        for (int c = 0; c < (row.Cells?.Count ?? 0); c++)
        {
            if (string.IsNullOrEmpty(row.Cells![c]))
            {
                continue;
            }
            Ctx.DrawText(row.Cells[c], new Point(ColumnRight(bounds, nameWidth, series, c), y), rowFont, TextAlignment.Right);
        }

        if (linked)
        {
            Ctx.EndHyperlink();
        }
    }

    private static double ColumnRight(Rect bounds, double nameWidth, TreeGridSeries series, int column)
        => bounds.X + nameWidth + ((column + 1) * series.ColumnWidth) - 6;

    /// <summary>The disclosure mark, the stat tile's own: ▸ closed, ▾ open — one wall, one idiom.</summary>
    private void DrawChevron(double x, double baseline, double size, bool expanded, Color ink)
    {
        double top = baseline - size + 2;
        double side = size * 0.55;
        Point[] points = expanded
            ? [new(x, top), new(x + side, top), new(x + (side / 2), top + side)]
            : [new(x, top), new(x + side, top + (side / 2)), new(x, top + side)];

        Ctx.BeginGroup("mpl-treegrid-chevron");
        Ctx.DrawPolygon(points, new ShapeStyle(ink, null, 0));
        Ctx.EndGroup();
    }
}
