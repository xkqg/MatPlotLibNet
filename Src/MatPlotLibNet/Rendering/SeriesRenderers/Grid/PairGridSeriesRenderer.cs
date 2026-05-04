// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Composite renderer for <see cref="PairGridSeries"/>. Splits the plot
/// area into an N×N grid via <see cref="PairGridLayout.ComputeCellRects"/> and
/// dispatches each cell:
/// <list type="bullet">
/// <item>Diagonal: histogram or KDE (or none) — branched inline (only 2 active kinds).</item>
/// <item>Off-diagonal: looked up via <see cref="PairGridOffDiagonalPainterRegistry"/>
/// (rule-of-three triggered when v1.10 added <c>Hexbin</c> alongside <c>Scatter</c>
/// and <c>None</c>).</item>
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
        var cells = PairGridLayout.ComputeCellRects(Context.Area.PlotBounds, n, series.CellSpacing);
        var color = SeriesColor;
        bool hueActive = PairGridHue.IsValid(series);

        // Hoist a per-render hue→colour cache so the off-diagonal hue scatter doesn't
        // re-resolve the same group colour once per (cell × sample). For N variables
        // with K distinct groups, the cache is built once for the whole figure
        // instead of N(N-1) × samples lookups.
        var hueCache = hueActive ? PairGridHue.BuildCache(series) : null;

        var offDiagPainter = PairGridOffDiagonalPainterRegistry.Resolve(series.OffDiagonalKind);

        for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
        {
            if (!ShouldRender(i, j, series.Triangular)) continue;
            var cell = cells[i, j];
            if (cell.Width  < PairGridLayout.MinPanelPx) continue;
            if (cell.Height < PairGridLayout.MinPanelPx) continue;

            if (i == j)
            {
                RenderDiagonalCell(series, i, cell, color, hueActive, hueCache);
            }
            else if (offDiagPainter is not null)
            {
                var xData = series.Variables[j];
                var yData = series.Variables[i];
                int sampleCount = Math.Min(xData.Length, yData.Length);
                if (sampleCount == 0) continue;

                PairGridGeometry.ComputeAxisSpan(xData, sampleCount, out double xMin, out double xSpan);
                PairGridGeometry.ComputeAxisSpan(yData, sampleCount, out double yMin, out double ySpan);

                offDiagPainter.Paint(
                    Ctx, series, xData, yData, sampleCount,
                    xMin, xSpan, yMin, ySpan, cell, color, hueActive, hueCache);
            }
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
    private void RenderDiagonalCell(PairGridSeries series, int i, Rect cell, Color color, bool hueActive, Dictionary<int, Color>? hueCache)
    {
        if (series.DiagonalKind == PairGridDiagonalKind.None) return;

        if (series.DiagonalKind == PairGridDiagonalKind.Kde)
        {
            if (hueActive) RenderDiagonalKdePerHue(series, i, cell, hueCache!);
            else            RenderDiagonalKdeSingle(series.Variables[i], cell, color);
            return;
        }

        // Histogram
        if (hueActive) RenderDiagonalHistogramPerHue(series, i, cell, hueCache!);
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

        var fill = alpha < 1.0 ? ApplyAlpha(color, alpha) : color;
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
            Ctx.DrawRectangle(barRect, fill, null, 0.0);
        }
    }

    /// <summary>Per-group overlapping histograms with <see cref="HueOverlayAlpha"/> alpha.
    /// <see cref="PairGridHue.BuildCache"/> seeds every group ID present in <c>HueGroups</c>,
    /// so the cache lookup is total — no fallback path needed.</summary>
    private void RenderDiagonalHistogramPerHue(PairGridSeries series, int i, Rect cell, Dictionary<int, Color> hueCache)
    {
        var groups = SplitByHue(series.Variables[i], series.HueGroups!);
        foreach (var (group, data) in groups)
            RenderDiagonalHistogramSingle(data, series.DiagonalBins, cell, hueCache[group], HueOverlayAlpha);
    }

    /// <summary>Single-colour KDE rendering for one variable.</summary>
    private void RenderDiagonalKdeSingle(double[] data, Rect cell, Color color)
    {
        if (data.Length == 0) return;

        // Filter non-finite samples before sorting — NaN sorts unpredictably and
        // would corrupt the bandwidth + density estimate.
        var sorted = data.Where(double.IsFinite).ToArray();
        if (sorted.Length == 0) return;
        Array.Sort(sorted);

        double bw = GaussianKde.SilvermanBandwidth(sorted);
        var curve = GaussianKde.Evaluate(sorted, bw, numPoints: 100);

        if (curve.X.Length < 2) return;

        double xMin = curve.X[0], xMax = curve.X[^1];
        if (xMin == xMax) xMax = xMin + 1;
        double xSpan = xMax - xMin;

        double yMax = 0;
        for (int k = 0; k < curve.Y.Length; k++) if (curve.Y[k] > yMax) yMax = curve.Y[k];
        if (yMax <= 0) return;

        var points = new Point[curve.X.Length];
        for (int k = 0; k < curve.X.Length; k++)
        {
            double px = cell.X      + (curve.X[k] - xMin) / xSpan * cell.Width;
            double py = cell.Bottom - curve.Y[k] / yMax * cell.Height;
            points[k] = new Point(px, py);
        }
        Ctx.DrawLines(points, color, thickness: 1.5, LineStyle.Solid);
    }

    /// <summary>Per-group KDE curves on the diagonal. See note on
    /// <see cref="RenderDiagonalHistogramPerHue"/> for why the cache lookup is total.</summary>
    private void RenderDiagonalKdePerHue(PairGridSeries series, int i, Rect cell, Dictionary<int, Color> hueCache)
    {
        var groups = SplitByHue(series.Variables[i], series.HueGroups!);
        foreach (var (group, data) in groups)
            RenderDiagonalKdeSingle(data, cell, hueCache[group]);
    }

    /// <summary>One hue group's samples extracted from a parent variable. Carries
    /// the group ID so caller can resolve a colour, and the per-group sub-array.</summary>
    private readonly record struct HueSlice(int Group, double[] Data);

    /// <summary>Splits a single variable's samples by their group ID into
    /// per-group sub-arrays. Caller has already verified <c>hue.Length == data.Length</c>.</summary>
    private static List<HueSlice> SplitByHue(double[] data, int[] hue)
    {
        var byGroup = new Dictionary<int, List<double>>();
        for (int k = 0; k < data.Length; k++)
        {
            int g = hue[k];
            if (!byGroup.TryGetValue(g, out var list))
                byGroup[g] = list = [];
            list.Add(data[k]);
        }
        var result = new List<HueSlice>(byGroup.Count);
        foreach (var (g, list) in byGroup) result.Add(new HueSlice(g, [.. list]));
        result.Sort((a, b) => a.Group.CompareTo(b.Group));
        return result;
    }
}
