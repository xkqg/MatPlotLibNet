// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Composite renderer for <see cref="PairGridSeries"/>. Splits the plot
/// area into an N×N grid via <see cref="Rendering.SeriesRenderers.PairGridSeriesRenderer.ComputeCellRects"/> and
/// dispatches each cell:
/// <list type="bullet">
/// <item>Diagonal: histogram or KDE (or none) — branched inline (only 2 active kinds).</item>
/// <item>Off-diagonal: looked up via <see cref="Rendering.SeriesRenderers.PairGridSeriesRenderer"/>
/// (three kinds, <c>Scatter</c>, <c>Hexbin</c> and <c>None</c>, are too many to branch on inline).</item>
/// </list></summary>
/// <remarks>The pair-grid intentionally bypasses the parent-axes coordinate
/// transform — every cell has its own internal data range. Bars and dots are
/// drawn directly into pixel-space sub-panel rects.</remarks>
internal sealed class PairGridSeriesRenderer : SeriesRenderer<PairGridSeries>
{
    private const double HueOverlayAlpha = 0.6;

    /// <inheritdoc />
    public PairGridSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PairGridSeries series)
    {
        int n = series.Variables.Length;
        var cells = ComputeCellRects(Context.Area.PlotBounds, n, series.CellSpacing);
        var color = SeriesColor;

        // Hoist a per-render hue→colour cache so the off-diagonal hue scatter doesn't
        // re-resolve the same group colour once per (cell × sample). For N variables
        // with K distinct groups, the cache is built once for the whole figure
        // instead of N(N-1) × samples lookups.
        var hueCache = series.HasValidHue ? series.HueColors(Context.Theme) : null;

        var offDiagPainter = Resolve(series.OffDiagonalKind);

        double dotRadius = series.DiameterUnder(Context.Theme, Slot) / 2;
        var density = ColorMapping.Linear(series.OffDiagonalColorMap, Context.Theme);

        // The densities are asked for once per draw, not once per diagonal cell: each ask compares the series with the
        // copy of its data the estimate was made from.
        var densities = series.DiagonalKind == PairGridDiagonalKind.Kde ? series.DiagonalDensitiesUnder(Context.Theme.Looks) : null;

        for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
        {
            if (!ShouldRender(i, j, series.Triangular)) continue;
            var cell = cells[i, j];
            if (cell.Width  < MinPanelPx) continue;
            if (cell.Height < MinPanelPx) continue;

            if (i == j)
            {
                RenderDiagonalCell(series, i, cell, color, hueCache, densities);
            }
            else if (offDiagPainter is not null)
            {
                var xData = series.Variables[j];
                var yData = series.Variables[i];
                int sampleCount = Math.Min(xData.Length, yData.Length);
                if (sampleCount == 0) continue;

                var frame = new PairGridCellFrame(AxisSpan.Of(xData, sampleCount), AxisSpan.Of(yData, sampleCount), cell);

                offDiagPainter.Paint(Ctx, new PairGridCell(series, xData, yData, sampleCount, frame, color, hueCache, density, dotRadius));
            }
        }

        if (series.Labels is { } labels) DrawLabels(labels, cells, n);
    }

    /// <summary>Names each variable as seaborn's <c>pairplot</c> does: under the cell of its column in the bottom row, and turned a
    /// quarter beside the cell of its row in the left column, a gap away from the grid, in the theme's axis label font. A variable
    /// past the end of <paramref name="labels"/>, or named by an empty text, goes unnamed.</summary>
    private void DrawLabels(string[] labels, Rect[,] cells, int n)
    {
        var font = Context.Theme.LabelFont();
        for (int v = 0; v < Math.Min(n, labels.Length); v++)
        {
            if (string.IsNullOrEmpty(labels[v])) continue;

            var label = TextLabel.Of(labels[v]);
            var under = cells[n - 1, v];
            Ctx.Draw(label, font, new TextPlacement(new Point(under.Center.X, under.Bottom + HierarchicalLayout.LabelGapPx), TextAlignment.Center)
                { Vertical = TextVerticalAlignment.Top });
            // Turned a quarter, the letters rise away from the grid: the baseline stands the gap and the descent left of it.
            var beside = cells[v, 0];
            Ctx.Draw(label, font, new TextPlacement(new Point(beside.X - HierarchicalLayout.LabelGapPx - FacingInk.Descent * font.Size, beside.Center.Y), TextAlignment.Center)
                { Rotation = 90 });
        }
    }

    /// <summary>Returns whether the cell at (<paramref name="row"/>, <paramref name="col"/>)
    /// should be rendered under the active <see cref="PairGridTriangle"/> selector.</summary>
    private static bool ShouldRender(int row, int col, PairGridTriangle tri) => tri switch
    {
        PairGridTriangle.LowerOnly => row >= col,
        PairGridTriangle.UpperOnly => row <= col,
        _                          => true, // Both
    };

    /// <summary>Paints a univariate distribution into the diagonal cell at index
    /// <paramref name="i"/>. Diagonal stays an inline if/else cascade — only 2
    /// active kinds (Histogram/Kde) — vs the off-diagonal strategy registry where
    /// rule-of-three triggered.</summary>
    private void RenderDiagonalCell(PairGridSeries series, int i, Rect cell, Color color, Dictionary<int, Color>? hueCache, PairGridDensity[][]? densities)
    {
        if (series.DiagonalKind == PairGridDiagonalKind.None) return;

        if (densities is not null)
        {
            foreach (var (group, curve) in densities[i])
                if (curve is { } c) RenderDiagonalKde(c, cell, hueCache is null ? color : hueCache[group]);
            return;
        }

        // Histogram
        if (hueCache is not null) RenderDiagonalHistogramPerHue(series, i, cell, hueCache);
        else            RenderDiagonalHistogramSingle(series.Variables[i], series.DiagonalBins, cell, color, alpha: 1.0);
    }

    /// <summary>Single-colour histogram rendering for one variable. Bin computation
    /// delegates to <see cref="HistogramBinning.Compute"/> — same source-of-truth used
    /// by <see cref="HistogramSeries.ComputeBins"/>.</summary>
    private void RenderDiagonalHistogramSingle(double[] data, int diagonalBins, Rect cell, Color color, double alpha)
    {
        if (data.Length == 0) return;

        var hist = HistogramBinning.Compute(data, diagonalBins);
        var counts = hist.Counts;
        int bins = counts.Length;
        if (bins == 0) return;

        int maxCount = 0;
        for (int b = 0; b < bins; b++) if (counts[b] > maxCount) maxCount = counts[b];
        if (maxCount == 0) return;

        var fill = color.WithOpacityBelowFull(alpha);
        double pixelBinW = cell.Width / bins;
        for (int b = 0; b < bins; b++)
        {
            if (counts[b] == 0) continue;
            double h = (double)counts[b] / maxCount * cell.Height;
            var barRect = new Rect(
                cell.X + b * pixelBinW,
                cell.Bottom - h,
                pixelBinW,
                h);
            Ctx.DrawRectangle(barRect, ShapeStyle.Filled(fill));
        }
    }

    /// <summary>Per-group overlapping histograms with <see cref="HueOverlayAlpha"/> alpha.
    /// <c>PairGridSeries.HueColors(theme)</c> seeds every group ID present in <c>HueGroups</c>,
    /// so the cache lookup is total — no fallback path needed.</summary>
    private void RenderDiagonalHistogramPerHue(PairGridSeries series, int i, Rect cell, Dictionary<int, Color> hueCache)
    {
        foreach (var (group, data) in series.SamplesByHue(i))
            RenderDiagonalHistogramSingle(data, series.DiagonalBins, cell, hueCache[group], HueOverlayAlpha);
    }

    /// <summary>Draws one density curve the series estimated (<see cref="PairGridSeries.DiagonalDensitiesUnder"/>) into the
    /// diagonal cell, its peak at the top of the cell: a line (seaborn's <c>kdeplot</c> draws it with <c>ax.plot</c>), as wide as the theme's
    /// <c>lines.linewidth</c>, a line's resting width under the library's own themes.</summary>
    private void RenderDiagonalKde(XYCurve curve, Rect cell, Color color)
    {
        double xMin = curve.X[0], xMax = curve.X[^1];
        if (xMin == xMax) xMax = xMin + 1;
        double xSpan = xMax - xMin;

        double yMax = 0;
        for (int k = 0; k < curve.Y.Length; k++) if (curve.Y[k] > yMax) yMax = curve.Y[k];
        if (yMax <= 0) return;

        // The curve's own span along X, its peak at the top of the cell.
        var frame = new PairGridCellFrame(new AxisSpan(xMin, xSpan), new AxisSpan(0, yMax), cell);
        var points = new Point[curve.X.Length];
        for (int k = 0; k < curve.X.Length; k++) points[k] = frame.Map(curve.X[k], curve.Y[k]);
        Ctx.DrawLines(points, Slot.StrokeOf(color, LineWidthRule.Line));
    }

    /// <summary>Minimum pixel width/height for a pair-grid cell below which the
    /// cell is suppressed by the renderer to avoid sub-pixel noise. Aliases the
    /// shared <see cref="HierarchicalLayout.MinPanelPx"/> so a single source-of-truth
    /// applies across every composite renderer.</summary>
    private const double MinPanelPx = HierarchicalLayout.MinPanelPx;

    /// <summary>Splits <paramref name="plotBounds"/> into an
    /// <paramref name="n"/>×<paramref name="n"/> grid of cells separated by a
    /// gutter of <paramref name="cellSpacing"/> fractional width / height.
    /// Index ordering: <c>cells[row, col]</c>; row 0 is the top, column 0 the left.</summary>
    /// <param name="plotBounds">The full plot area to subdivide.</param>
    /// <param name="n">The number of cells per side (one per variable). Must be ≥ 1.</param>
    /// <param name="cellSpacing">Gutter as a fraction of total bounds. Caller
    /// is responsible for clamping to a sensible range; the function itself does
    /// not clamp.</param>
    /// <returns>An <c>n</c>×<c>n</c> array of <see cref="Rect"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> &lt; 1.</exception>
    internal static Rect[,] ComputeCellRects(Rect plotBounds, int n, double cellSpacing)
    {
        n.ThrowIfBelow(1, nameof(n));

        if (n == 1)
            return new Rect[1, 1] { { plotBounds } };

        double totalGutterX = cellSpacing * plotBounds.Width;
        double totalGutterY = cellSpacing * plotBounds.Height;
        double cellW   = (plotBounds.Width  - totalGutterX) / n;
        double cellH   = (plotBounds.Height - totalGutterY) / n;
        double gutterW = totalGutterX / (n - 1);
        double gutterH = totalGutterY / (n - 1);

        var cells = new Rect[n, n];
        for (int r = 0; r < n; r++)
        for (int c = 0; c < n; c++)
        {
            cells[r, c] = new Rect(
                plotBounds.X + c * (cellW + gutterW),
                plotBounds.Y + r * (cellH + gutterH),
                cellW,
                cellH);
        }
        return cells;
    }

    private static readonly Dictionary<PairGridOffDiagonalKind, IPairGridOffDiagonalPainter> _painters = new()
    {
        [PairGridOffDiagonalKind.Scatter] = new ScatterOffDiagonalPainter(),
        [PairGridOffDiagonalKind.Hexbin]  = new HexbinOffDiagonalPainter(),
        // PairGridOffDiagonalKind.None has no painter — the renderer skips dispatch entirely.
    };

    /// <summary>Resolves the painter for the given kind, or <see langword="null"/> for
    /// <see cref="PairGridOffDiagonalKind.None"/>.</summary>
    private static IPairGridOffDiagonalPainter? Resolve(PairGridOffDiagonalKind kind) =>
        _painters.TryGetValue(kind, out var painter) ? painter : null;
}
