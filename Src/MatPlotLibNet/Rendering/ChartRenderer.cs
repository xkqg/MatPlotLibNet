// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders a complete <see cref="Figure"/> including subplots, axes, grids, and series onto an <see cref="IRenderContext"/>.</summary>
public sealed class ChartRenderer : IChartRenderer
{
    private const double TitleHeight = 30;

    /// <inheritdoc />
    public void Render(Figure figure, IRenderContext ctx)
    {
        double plotAreaTop = RenderBackground(figure, ctx);

        if (figure.SubPlots.Count == 0) return;

        var plotAreas = ComputeSubPlotLayout(figure, plotAreaTop);

        for (int i = 0; i < figure.SubPlots.Count; i++)
            RenderAxes(figure.SubPlots[i], plotAreas[i], ctx, figure.Theme);
    }

    /// <summary>Renders the figure background and title, returning the top Y coordinate for subplots.</summary>
    internal double RenderBackground(Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;
        var bgColor = figure.BackgroundColor ?? theme.Background;

        ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), bgColor, null, 0);

        double plotAreaTop = figure.Spacing.MarginTop;
        if (figure.Title is not null)
        {
            ctx.DrawText(figure.Title, new Point(figure.Width / 2, figure.Spacing.MarginTop / 2 + 5),
                TitleFont(theme), TextAlignment.Center);
            plotAreaTop += TitleHeight;
        }

        return plotAreaTop;
    }

    /// <summary>Computes subplot layout positions.</summary>
    internal List<Rect> ComputeSubPlotLayout(Figure figure, double plotAreaTop)
    {
        var sp = figure.Spacing;
        double totalWidth = figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = figure.Height - plotAreaTop - sp.MarginBottom;

        // Determine grid dimensions from subplot metadata
        int maxRows = 1, maxCols = 1;
        foreach (var ax in figure.SubPlots)
        {
            if (ax.GridRows > 0) maxRows = Math.Max(maxRows, ax.GridRows);
            if (ax.GridCols > 0) maxCols = Math.Max(maxCols, ax.GridCols);
        }

        // If no grid metadata, lay out in a single row
        if (figure.SubPlots.All(a => a.GridRows == 0))
        {
            maxCols = figure.SubPlots.Count;
            maxRows = 1;
        }

        double cellWidth = (totalWidth - sp.HorizontalGap * (maxCols - 1)) / maxCols;
        double cellHeight = (totalHeight - sp.VerticalGap * (maxRows - 1)) / maxRows;

        var areas = new List<Rect>();
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var ax = figure.SubPlots[i];
            int row, col;

            if (ax.GridIndex > 0)
            {
                // 1-based index -> row/col
                int idx = ax.GridIndex - 1;
                row = idx / maxCols;
                col = idx % maxCols;
            }
            else
            {
                row = i / maxCols;
                col = i % maxCols;
            }

            double x = sp.MarginLeft + col * (cellWidth + sp.HorizontalGap);
            double y = plotAreaTop + row * (cellHeight + sp.VerticalGap);
            areas.Add(new Rect(x, y, cellWidth, cellHeight));
        }

        return areas;
    }

    internal void RenderAxes(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme) =>
        AxesRenderer.Create(axes, plotArea, ctx, theme).Render();

    /// <summary>Creates a bold title font for the figure title.</summary>
    private static Font TitleFont(Theme theme, int sizeOffset = 4) => new()
    {
        Family = theme.DefaultFont.Family,
        Size = theme.DefaultFont.Size + sizeOffset,
        Weight = FontWeight.Bold,
        Color = theme.ForegroundText
    };
}
