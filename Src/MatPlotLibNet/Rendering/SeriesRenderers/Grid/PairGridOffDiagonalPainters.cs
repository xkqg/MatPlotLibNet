// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Strategy interface for off-diagonal cell painting in
/// <see cref="PairGridSeriesRenderer"/>. Each implementation handles a single
/// <see cref="PairGridOffDiagonalKind"/> value, isolating the rendering logic per
/// kind and keeping the v1.10 addition of <c>Hexbin</c> additive — adding a future
/// kind requires a new <see cref="IPairGridOffDiagonalPainter"/> implementation
/// plus one entry in <see cref="PairGridOffDiagonalPainterRegistry"/>, with no
/// modifications to the renderer's dispatch loop.</summary>
internal interface IPairGridOffDiagonalPainter
{
    /// <summary>Paints one off-diagonal cell. The cell-local axis spans are
    /// pre-computed by the caller (NaN-safe) so all painters share the same
    /// coordinate frame.</summary>
    void Paint(
        IRenderContext ctx,
        PairGridSeries series,
        double[] xData, double[] yData, int n,
        double xMin, double xSpan, double yMin, double ySpan,
        Rect cell,
        Color baseColor,
        bool hueActive,
        Dictionary<int, Color>? hueCache);
}

/// <summary>Singleton registry mapping <see cref="PairGridOffDiagonalKind"/> values
/// to their painter implementations. Adding a new kind = add a new painter class
/// and one entry here; the renderer's dispatch loop is closed for modification.</summary>
internal static class PairGridOffDiagonalPainterRegistry
{
    private static readonly Dictionary<PairGridOffDiagonalKind, IPairGridOffDiagonalPainter> _painters = new()
    {
        [PairGridOffDiagonalKind.Scatter] = new ScatterOffDiagonalPainter(),
        [PairGridOffDiagonalKind.Hexbin]  = new HexbinOffDiagonalPainter(),
        // PairGridOffDiagonalKind.None has no painter — the renderer skips dispatch entirely.
    };

    /// <summary>Resolves the painter for the given kind, or <see langword="null"/> for
    /// <see cref="PairGridOffDiagonalKind.None"/>.</summary>
    internal static IPairGridOffDiagonalPainter? Resolve(PairGridOffDiagonalKind kind) =>
        _painters.TryGetValue(kind, out var painter) ? painter : null;
}

/// <summary>Per-point scatter rendering. Honours <see cref="PairGridSeries.HueGroups"/>
/// when active, falling back to a single base colour otherwise. Non-finite samples
/// are skipped before the cell-pixel projection.</summary>
internal sealed class ScatterOffDiagonalPainter : IPairGridOffDiagonalPainter
{
    public void Paint(
        IRenderContext ctx,
        PairGridSeries series,
        double[] xData, double[] yData, int n,
        double xMin, double xSpan, double yMin, double ySpan,
        Rect cell,
        Color baseColor,
        bool hueActive,
        Dictionary<int, Color>? hueCache)
    {
        double radius = series.MarkerSize;

        if (!hueActive)
        {
            for (int k = 0; k < n; k++)
            {
                if (!double.IsFinite(xData[k]) || !double.IsFinite(yData[k])) continue;
                ctx.DrawCircle(PairGridGeometry.MapPoint(xData[k], yData[k], xMin, xSpan, yMin, ySpan, cell), radius, baseColor, null, 0.0);
            }
            return;
        }

        var hue = series.HueGroups!;
        for (int k = 0; k < n; k++)
        {
            if (!double.IsFinite(xData[k]) || !double.IsFinite(yData[k])) continue;
            int group = hue[k];
            var groupColor = hueCache!.TryGetValue(group, out var c) ? c : PairGridHue.GetColor(group, series.HuePalette);
            ctx.DrawCircle(PairGridGeometry.MapPoint(xData[k], yData[k], xMin, xSpan, yMin, ySpan, cell), radius, groupColor, null, 0.0);
        }
    }
}

/// <summary>Hexagonal density painter. Reuses <see cref="HexGrid"/> for axial
/// binning and vertex math; maps data-space hex centres to cell-pixel space via
/// the same linear scaling as <see cref="ScatterOffDiagonalPainter"/>. Hue is
/// intentionally ignored — density encoding cannot cleanly carry a per-group
/// dimension.</summary>
internal sealed class HexbinOffDiagonalPainter : IPairGridOffDiagonalPainter
{
    public void Paint(
        IRenderContext ctx,
        PairGridSeries series,
        double[] xData, double[] yData, int n,
        double xMin, double xSpan, double yMin, double ySpan,
        Rect cell,
        Color baseColor,
        bool hueActive,
        Dictionary<int, Color>? hueCache)
    {
        // Filter non-finite samples (HexGrid does not).
        var xFiltered = new List<double>(n);
        var yFiltered = new List<double>(n);
        for (int k = 0; k < n; k++)
        {
            if (!double.IsFinite(xData[k]) || !double.IsFinite(yData[k])) continue;
            xFiltered.Add(xData[k]);
            yFiltered.Add(yData[k]);
        }
        if (xFiltered.Count == 0) return;

        double xMax = xMin + xSpan;
        double yMax = yMin + ySpan;
        int gridSize = Math.Max(1, series.HexbinGridSize);

        var bins = HexGrid.ComputeHexBins([.. xFiltered], [.. yFiltered], xMin, xMax, yMin, yMax, gridSize);
        if (bins.Count == 0) return;

        int maxCount = 0;
        foreach (var c in bins.Values) if (c > maxCount) maxCount = c;
        if (maxCount == 0) return;
        double normMin = 0.5;
        double normMax = Math.Max(maxCount, 1);

        var cmap = series.OffDiagonalColorMap ?? ColorMaps.Viridis;
        double hexSize = HexGrid.ComputeHexSize(xMin, xMax, gridSize);

        foreach (var (hex, count) in bins)
        {
            var dataCenter = HexGrid.HexCenter(hex.Q, hex.R, hexSize, xMin, yMin);
            var dataVerts  = HexGrid.HexagonVertices(dataCenter.X, dataCenter.Y, hexSize * 0.95);

            var pixelVerts = new Point[6];
            for (int v = 0; v < 6; v++)
                pixelVerts[v] = PairGridGeometry.MapPoint(dataVerts[v].X, dataVerts[v].Y, xMin, xSpan, yMin, ySpan, cell);

            double normalized = (count - normMin) / (normMax - normMin);
            var fill = cmap.GetColor(Math.Clamp(normalized, 0.0, 1.0));
            ctx.DrawPolygon(pixelVerts, fill, null, 0);
        }
    }
}

/// <summary>Pure cell geometry helpers shared by every off-diagonal painter and
/// the diagonal cell renderer. Extracted so painters don't take a back-reference
/// to <see cref="PairGridSeriesRenderer"/>.</summary>
internal static class PairGridGeometry
{
    /// <summary>Maps a (data-x, data-y) pair into pixel coordinates within the cell.</summary>
    internal static Point MapPoint(double x, double y, double xMin, double xSpan, double yMin, double ySpan, Rect cell)
    {
        double px = cell.X      + (x - xMin) / xSpan * cell.Width;
        double py = cell.Bottom - (y - yMin) / ySpan * cell.Height;
        return new Point(px, py);
    }

    /// <summary>NaN-safe (min, span) computation with degenerate-range guard. An
    /// axis with no finite samples falls back to <c>(0, 1)</c>.</summary>
    internal static void ComputeAxisSpan(double[] data, int n, out double min, out double span)
    {
        double mn = double.MaxValue, mx = double.MinValue;
        bool anyFinite = false;
        for (int k = 0; k < n; k++)
        {
            double v = data[k];
            if (!double.IsFinite(v)) continue;
            if (v < mn) mn = v;
            if (v > mx) mx = v;
            anyFinite = true;
        }
        if (!anyFinite) { min = 0; span = 1; return; }
        if (mn == mx) mx = mn + 1;
        min = mn;
        span = mx - mn;
    }
}

/// <summary>Hue grouping helpers shared by every painter that respects
/// <see cref="PairGridSeries.HueGroups"/>.</summary>
internal static class PairGridHue
{
    /// <summary>Returns whether <c>HueGroups</c> is set with a length matching the
    /// sample axis. Mismatched lengths fall back to single-colour rendering — the
    /// same defensive pattern as <c>ClustermapSeries.ResolveLeafOrder</c>.</summary>
    internal static bool IsValid(PairGridSeries series) =>
        series.HueGroups is { Length: > 0 } hue
        && series.Variables.Length > 0
        && hue.Length == series.Variables[0].Length;

    /// <summary>Builds the per-render hue→colour lookup table, keyed by group ID.
    /// Caller has already verified <see cref="IsValid"/>.</summary>
    internal static Dictionary<int, Color> BuildCache(PairGridSeries series)
    {
        var cache = new Dictionary<int, Color>();
        foreach (int g in series.HueGroups!)
            if (!cache.ContainsKey(g)) cache[g] = GetColor(g, series.HuePalette);
        return cache;
    }

    /// <summary>Resolves the colour for a given hue <paramref name="group"/>. When
    /// <paramref name="userPalette"/> is set, indexes into it modulo length;
    /// otherwise samples <see cref="QualitativeColorMaps.Tab10"/> at the bucket centre.</summary>
    internal static Color GetColor(int group, Color[]? userPalette)
    {
        if (userPalette is { Length: > 0 })
            return userPalette[((group % userPalette.Length) + userPalette.Length) % userPalette.Length];

        const int tab10Buckets = 10;
        int bucket = ((group % tab10Buckets) + tab10Buckets) % tab10Buckets;
        return QualitativeColorMaps.Tab10.GetColor((bucket + 0.5) / tab10Buckets);
    }
}
