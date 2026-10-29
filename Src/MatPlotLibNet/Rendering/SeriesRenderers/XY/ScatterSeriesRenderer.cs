// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ScatterSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ScatterSeriesRenderer : SeriesRenderer<ScatterSeries>
{
    /// <inheritdoc />
    public ScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The samples a scatter keeps (after the viewport cull) and the pixel each stands at.</summary>
    private readonly record struct ScatterPixels(XYData Data, Point[] Centres);

    /// <summary>Works the scatter's pixels out: the one geometry <see cref="Render"/> draws its markers at and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    private ScatterPixels Pixels(ScatterSeries series)
    {
        // Culling re-bases the arrays to index 0, and every per-point array below is indexed by the position in
        // the DATA, so a cull that starts past the first sample would hand each marker another point's size,
        // colour or edge. Any of them set means the points are drawn whole.
        bool indexTied = series.C is not null || series.Colors is not null || series.Sizes is not null
                      || series.EdgeColors is not null || series.LineWidths is not null;
        XYData data = !indexTied && series.MaxDisplayPoints.HasValue
            // The unscaled range: the culler compares against raw sample values, and on a log or symlog axis the
            // transform's range is in scaled space.
            ? ViewportCuller.Cull(series.XData, series.YData, Transform.UnscaledXMin, Transform.UnscaledXMax)
            : new XYData(series.XData, series.YData);
        return new ScatterPixels(data, Transform.TransformBatch(data.X, data.Y));
    }

    /// <summary>The marker's radius in pixels. matplotlib scatter: s is marker AREA in points².  radius = sqrt(s/π) × (dpi/72).
    /// At 100 DPI, (100/72) ≈ 1.389 — converts pt to px at the standard render DPI.</summary>
    private static double RadiusOf(ScatterSeries series, int i) =>
        Math.Sqrt((series.Sizes is not null ? series.Sizes[i] : series.MarkerSize) / Math.PI) * (100.0 / 72.0);

    /// <inheritdoc />
    internal override void AddMarks(ScatterSeries series, MarkObstacles.Builder marks)
    {
        var centres = Pixels(series).Centres;
        for (int i = 0; i < centres.Length; i++)
        {
            // A point the axes cannot place, or a size that is not a number, is not drawn: matplotlib masks x, y and s together.
            if (centres[i].IsDrawable() && RadiusOf(series, i).IsDrawable()) marks.AddPoint(centres[i]);
        }
    }

    /// <inheritdoc />
    public override void Render(ScatterSeries series)
    {
        var defaultColor = ResolveColor(series.Color);
        var (data, pts) = Pixels(series);

        // Pre-compute C normalization bounds once, over the drawable values: a missing value is coloured on its own.
        double cMin = 0, cMax = 1;
        if (series.C is not null)
        {
            var c = series.C.DrawableRange() ?? new Range1D(0, 1);
            cMin = series.VMin ?? c.Lo;
            cMax = series.VMax ?? c.Hi;
        }
        var normalizer = series.NormalizerOrLinear();

        var marker = Slot.ScatterMarkerOf(series.Marker);
        for (int i = 0; i < pts.Length; i++)
        {
            double radius = RadiusOf(series, i);
            // A point the axes cannot place, or a size that is not a number, is not drawn, and has no tooltip:
            // matplotlib's scatter masks x, y and s together (delete_masked_points).
            if (!pts[i].IsDrawable() || !radius.IsDrawable()) continue;
            BeginTooltip($"x={data.X[i].ToString("G5", System.Globalization.CultureInfo.InvariantCulture)}, y={data.Y[i].ToString("G5", System.Globalization.CultureInfo.InvariantCulture)}");
            var c = ResolvePointColor(series, i, defaultColor, normalizer, cMin, cMax);
            var edgeColor = series.EdgeColors is not null && i < series.EdgeColors.Length ? series.EdgeColors[i] : (Color?)null;
            var edgeWidth = series.LineWidths is not null && i < series.LineWidths.Length ? series.LineWidths[i] : 0.0;
            Ctx.DrawMarker(marker, pts[i], 2 * radius, new ShapeStyle(FillOf(c, series.Alpha), edgeColor, edgeWidth));
            EndTooltip();
        }
    }

    /// <summary>The fill a marker is drawn with: the opacity of the series, the way every other filled mark applies it
    /// (<see cref="SeriesRenderer.ApplyAlpha"/>, replacing the colour's own alpha as matplotlib's <c>alpha</c> does). The resting
    /// opacity 1 leaves the colour exactly as it is, so a colour with an alpha of its own keeps it; a fully transparent colour
    /// (the bad colour of a colour map, for a value it cannot colour) stays transparent, as matplotlib ignores
    /// <c>alpha</c> for a bad colour of all zeros.</summary>
    private Color FillOf(Color color, double alpha) =>
        alpha < 1.0 && color.A > 0 ? ApplyAlpha(color, alpha) : color;

    private static Color ResolvePointColor(ScatterSeries series, int i, Color defaultColor,
        INormalizer normalizer, double cMin, double cMax)
    {
        // Priority: Colors[] > C+ColorMap > uniform Color
        if (series.Colors is not null && i < series.Colors.Length)
            return series.Colors[i];
        if (series.C is not null && i < series.C.Length && series.ColorMap is not null)
            return series.ColorMap.ColorOf(series.C[i], normalizer, cMin, cMax);
        return defaultColor;
    }
}
