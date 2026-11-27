// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>
/// Renders a <see cref="ContourfSeries"/> using the painter's algorithm:
/// fills the entire plot area with the bottom-band color, then paints each
/// ascending band's closed polygon(s) over the previous, and optionally
/// overlays the iso-line boundaries.
/// </summary>
internal sealed class ContourfSeriesRenderer : SeriesRenderer<ContourfSeries>
{
    /// <inheritdoc />
    public ContourfSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ContourfSeries series)
    {
        int rows = series.ZData.GetLength(0);
        int cols = series.ZData.GetLength(1);
        if (rows < 2 || cols < 2) return;

        // The levels the user named bound the bands, or else matplotlib's round levels for the count (ContourfSeries.LevelsUnder).
        if (series.LevelsUnder(Context.Theme) is not { } levels) return;

        var mapping = series.ColorMappingUnder(Context.Theme);

        // A value matplotlib's log norm masks (0 and below) is a value the grid does not have: the cells it is a corner of are left empty.
        var z = series.ZData.MaskedBy(mapping.Normalizer, levels.Length > 0 ? levels[0] : 0);
        var bands = MarchingSquares.ExtractBands(series.XData, series.YData, z, levels);
        if (bands.Length == 0) return;

        var lines = mapping.Over(new(bands[0].LevelLow, bands[^1].LevelHigh));
        if (lines.Max - lines.Min < 1e-12) return;

        // The fill, by the one code a filled contour fills through, at the opacity of the whole layer.
        Context.FillContourBands(series.XData, series.YData, z, bands, mapping, Context.Theme.PatchEdgesOf(series), series.Alpha, series.Hatches);

        // Overlay iso-lines if requested: matplotlib's contourf draws its bands with no edge line, so under the matplotlib themes
        // (PatchLook.LikeCollections) a filled contour that was not asked for lines draws none; a value given is drawn as given.
        if (!Context.Theme.Looks.Patch.ContourLinesOver(series.GivenShowLines)) return;

        // The lines stand on the bands' edges, the same levels.
        var contours = MarchingSquares.Extract(series.XData, series.YData, z, levels);
        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;
            var pixels = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixels.Add(Transform.DataToPixel(pt.X, pt.Y));

            var lineColor = lines.ColorOf(contour.Level);
            Ctx.DrawLines(pixels, StrokeStyle.Solid(lineColor, series.LineWidth));
        }
    }
}
