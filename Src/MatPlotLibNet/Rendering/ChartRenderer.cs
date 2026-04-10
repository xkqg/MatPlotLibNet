// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders a complete <see cref="Figure"/> including subplots, axes, grids, and series onto an <see cref="IRenderContext"/>.</summary>
public sealed class ChartRenderer : IChartRenderer
{
    private const double TitleHeight = 30;

    /// <inheritdoc />
    public void Render(Figure figure, IRenderContext ctx)
    {
        // Adjust margins based on actual text extents when TightLayout or ConstrainedLayout is enabled.
        if (figure.Spacing.TightLayout || figure.Spacing.ConstrainedLayout)
            figure.Spacing = new ConstrainedLayoutEngine().Compute(figure, ctx);

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
            var titlePoint = new Point(figure.Width / 2, figure.Spacing.MarginTop / 2 + 5);
            var titleFont  = TitleFont(theme);
            if (MathTextParser.ContainsMath(figure.Title))
                ctx.DrawRichText(MathTextParser.Parse(figure.Title), titlePoint, titleFont, TextAlignment.Center);
            else
                ctx.DrawText(figure.Title, titlePoint, titleFont, TextAlignment.Center);
            plotAreaTop += TitleHeight;
        }

        return plotAreaTop;
    }

    /// <summary>Computes subplot layout positions from grid metadata and spacing.</summary>
    public List<Rect> ComputeSubPlotLayout(Figure figure, double plotAreaTop)
    {
        if (figure.GridSpec is not null)
            return ComputeGridSpecLayout(figure, figure.GridSpec, plotAreaTop);

        return ComputeLegacyLayout(figure, plotAreaTop);
    }

    private List<Rect> ComputeGridSpecLayout(Figure figure, GridSpec gs, double plotAreaTop)
    {
        var sp = figure.Spacing;
        double totalWidth = figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = figure.Height - plotAreaTop - sp.MarginBottom;

        // Compute per-cell widths and heights from ratios (or equal if null)
        double[] colWidths = ComputeRatioSizes(totalWidth, gs.Cols, sp.HorizontalGap, gs.WidthRatios);
        double[] rowHeights = ComputeRatioSizes(totalHeight, gs.Rows, sp.VerticalGap, gs.HeightRatios);

        // Precompute cumulative X positions (left edge of each column)
        double[] colX = new double[gs.Cols];
        colX[0] = sp.MarginLeft;
        for (int c = 1; c < gs.Cols; c++)
            colX[c] = colX[c - 1] + colWidths[c - 1] + sp.HorizontalGap;

        // Precompute cumulative Y positions (top edge of each row)
        double[] rowY = new double[gs.Rows];
        rowY[0] = plotAreaTop;
        for (int r = 1; r < gs.Rows; r++)
            rowY[r] = rowY[r - 1] + rowHeights[r - 1] + sp.VerticalGap;

        var areas = new List<Rect>();
        foreach (var ax in figure.SubPlots)
        {
            var pos = ax.GridPosition ?? GridPosition.Single(0, 0);

            double x = colX[pos.ColStart];
            double y = rowY[pos.RowStart];

            // Width = sum of spanned column widths + gaps between them
            double width = 0;
            for (int c = pos.ColStart; c < pos.ColEnd; c++)
                width += colWidths[c];
            width += sp.HorizontalGap * (pos.ColEnd - pos.ColStart - 1);

            // Height = sum of spanned row heights + gaps between them
            double height = 0;
            for (int r = pos.RowStart; r < pos.RowEnd; r++)
                height += rowHeights[r];
            height += sp.VerticalGap * (pos.RowEnd - pos.RowStart - 1);

            areas.Add(new Rect(x, y, width, height));
        }

        return areas;
    }

    /// <summary>Distributes total available space among cells based on ratios, subtracting gaps.</summary>
    private static double[] ComputeRatioSizes(double totalSpace, int count, double gap, double[]? ratios)
    {
        double available = totalSpace - gap * (count - 1);
        var sizes = new double[count];

        if (ratios is null || ratios.Length != count)
        {
            double cellSize = available / count;
            Array.Fill(sizes, cellSize);
        }
        else
        {
            double ratioSum = 0;
            foreach (var r in ratios) ratioSum += r;
            for (int i = 0; i < count; i++)
                sizes[i] = available * ratios[i] / ratioSum;
        }

        return sizes;
    }

    private List<Rect> ComputeLegacyLayout(Figure figure, double plotAreaTop)
    {
        var sp = figure.Spacing;
        double totalWidth = figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = figure.Height - plotAreaTop - sp.MarginBottom;

        int maxRows = 1, maxCols = 1;
        foreach (var ax in figure.SubPlots)
        {
            if (ax.GridRows > 0) maxRows = Math.Max(maxRows, ax.GridRows);
            if (ax.GridCols > 0) maxCols = Math.Max(maxCols, ax.GridCols);
        }

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

    internal void RenderAxes(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, int depth = 0)
    {
        AxesRenderer.Create(axes, plotArea, ctx, theme).Render();

        // Render inset axes recursively (max depth guard)
        if (depth < 3)
        {
            foreach (var inset in axes.Insets)
            {
                if (inset.InsetBounds is not { } bounds) continue;
                var insetRect = new Rect(
                    plotArea.X + bounds.X * plotArea.Width,
                    plotArea.Y + bounds.Y * plotArea.Height,
                    bounds.Width * plotArea.Width,
                    bounds.Height * plotArea.Height);
                RenderAxes(inset, insetRect, ctx, theme, depth + 1);
            }
        }
    }

    /// <summary>Creates a bold title font for the figure title.</summary>
    private static Font TitleFont(Theme theme, int sizeOffset = 4) => new()
    {
        Family = theme.DefaultFont.Family,
        Size = theme.DefaultFont.Size + sizeOffset,
        Weight = FontWeight.Bold,
        Color = theme.ForegroundText
    };
}
