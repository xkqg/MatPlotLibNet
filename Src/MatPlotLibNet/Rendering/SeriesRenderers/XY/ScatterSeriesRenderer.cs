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
                      || series.EdgeColors is not null || series.LineWidths is not null || series.RowKeys is not null;
        XYData data = !indexTied && series.MaxDisplayPoints.HasValue
            ? CulledToView(series.XData, series.YData)
            : new XYData(series.XData, series.YData);
        return new ScatterPixels(data, Transform.TransformBatch(data.X, data.Y));
    }

    /// <summary>The marker's size across, in pixels: its area in points squared (matplotlib's <c>s</c>) is sqrt(s) points across,
    /// and a point is a fixed number of pixels. A size of its own for each point is as named; the one size of the series is the
    /// size it was given, or the theme's when it was given none (<paramref name="uniform"/>, <see cref="SeriesSlot.ScatterSizeOf"/>). Not a number
    /// for an area that is not one, which the point is not drawn for.</summary>
    private static double SizeOf(ScatterSeries series, int i, double uniform) =>
        series.Sizes is not null ? series.Sizes[i].AreaAcrossInPoints.PointsInPixels : uniform;

    /// <summary>The size across, in pixels, of every marker whose series names no size of its own for each.</summary>
    private double UniformSize(ScatterSeries series) => Slot.ScatterSizeOf(series.GivenMarkerSize?.AreaAcrossInPoints).PointsInPixels;

    /// <inheritdoc />
    internal override void AddMarks(ScatterSeries series, MarkObstacles.Builder marks)
    {
        var centres = Pixels(series).Centres;
        double uniform = UniformSize(series);
        for (int i = 0; i < centres.Length; i++)
        {
            // A point the axes cannot place, or a size that is not a number, is not drawn: matplotlib masks x, y and s together.
            if (centres[i].IsDrawable() && SizeOf(series, i, uniform).IsDrawable()) marks.AddPoint(centres[i]);
        }
    }

    /// <inheritdoc />
    public override void Render(ScatterSeries series)
    {
        // What the series is to matplotlib, read once: it says how each of its shapes is joined.
        var artist = series.Artist;
        var slot = Slot;
        var defaultColor = ColorOf(series);
        var (data, pts) = Pixels(series);

        // The values of C colour the markers through the series' colour scale (its map, else the theme's, as matplotlib's scatter(c=...)
        // takes image.cmap), worked out once; a missing value is coloured on its own.
        ColormapContext? scale = series.C is null ? null : series.ColorScaleUnder(Context.Theme);

        // The shape is resolved once for the series and every point is drawn through it.
        var marker = MarkerShape.Of(slot.ScatterMarkerOf(series.GivenMarker), Context.Theme);
        // A filled marker is edged as the theme says where the series names no edge (matplotlib's scatter.edgecolors and
        // patch.linewidth); a cross or a plus is strokes alone.
        bool themedEdge = marker.TakesAnEdge;
        double uniform = UniformSize(series);
        var theme = Context.Theme;
        for (int i = 0; i < pts.Length; i++)
        {
            double size = SizeOf(series, i, uniform);
            // A point the axes cannot place, or a size that is not a number, is not drawn, and has no tooltip:
            // matplotlib's scatter masks x, y and s together (delete_masked_points).
            if (!pts[i].IsDrawable() || !size.IsDrawable()) continue;
            BeginTooltip($"x={data.X[i].InvariantText("G5")}, y={data.Y[i].InvariantText("G5")}");
            var c = ResolvePointColor(series, i, defaultColor, scale);
            var edgeColor = series.EdgeColors.ElementOrNull(i);
            var edgeWidth = series.LineWidths.ElementOrNull(i);
            var fill = FillOf(c, series.Alpha);
            if (themedEdge && slot.ScatterEdgeOf(fill) is { } themed)
            {
                edgeColor ??= FillOf(themed.Color, series.Alpha);
                edgeWidth ??= themed.Width;
            }
            // The mark is one row of the data: say which, so a script can find the same row in another chart.
            if (series.RowKeys is { } keys) Ctx.SetNextElementData("row", keys[i]);
            marker.Draw(Ctx, pts[i], size, theme.JoinedAs(artist, ShapeStyle.Edged(fill, edgeColor, edgeWidth ?? 0.0)));
            EndTooltip();
        }
    }

    /// <summary>The fill a marker is drawn with: the opacity of the series, the way every other filled mark applies it
    /// (<c>WithOpacity</c>, replacing the colour's own alpha as matplotlib's <c>alpha</c> does). The resting
    /// opacity 1 leaves the colour exactly as it is, so a colour with an alpha of its own keeps it; a fully transparent colour
    /// (the bad colour of a colour map, for a value it cannot colour) stays transparent, as matplotlib ignores
    /// <c>alpha</c> for a bad colour of all zeros.</summary>
    private Color FillOf(Color color, double alpha) =>
        color.WithOpacityBelowFull(alpha);

    private static Color ResolvePointColor(ScatterSeries series, int i, Color defaultColor, ColormapContext? scale)
    {
        // Priority: Colors[] > C through the colour scale > uniform Color
        if (series.Colors is not null && i < series.Colors.Length)
            return series.Colors[i];
        if (scale is { } values && i < series.C!.Length)
            return values.ColorOf(series.C[i]);
        return defaultColor;
    }
}
