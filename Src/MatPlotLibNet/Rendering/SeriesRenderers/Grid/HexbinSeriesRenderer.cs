// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="HexbinSeries"/> as matplotlib's <c>hexbin</c> draws it: the hexagonal grid of <see cref="HexLattice"/>
/// over the extent of the data, each hexagon filled by its count and outlined in its own colour so neighbours leave no seam
/// (<c>edgecolors='face'</c>, a line of <c>patch.linewidth</c>, 1 pt), clipped to the plot.</summary>
internal sealed class HexbinSeriesRenderer : SeriesRenderer<HexbinSeries>
{
    // matplotlib's patch.linewidth is 1.0 pt, in pixels at the 100 dpi the library's themes are drawn at.
    private const double OutlineWidth = PointLengthExtensions.PixelsPerPoint;

    /// <inheritdoc />
    public HexbinSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(HexbinSeries series)
    {
        // What the series is to matplotlib, read once: it says how each of its shapes is joined.
        var artist = series.Artist;
        if (HexExtent.Of(series.X, series.Y) is not { } extent) return;   // no point could be placed

        var lattice = HexLattice.Over(extent, series.GridSize);
        int[] counts = lattice.Count(series.X, series.Y);
        int minCount = series.MinCount;

        // A hexagon holding fewer than MinCount is left blank, so with no minimum every hexagon of the grid is drawn, the empty ones
        // in the colour of a count of 0, the lowest of the scale, as matplotlib's hexbin draws its grid when it is given no mincnt. The
        // range the counts are coloured over is the series' one rule (ColourRangeOver), which its colour bar covers too.
        var mapping = series.ColorMappingUnder(Context.Theme);
        var scale = mapping.Over(series.ColourRangeOver(counts, mapping));

        // The hexagons at the edge of the extent reach half a step past it, which matplotlib's collection clips to the axes: the series pass
        // cuts them off there (HexbinSeries.MarksReachPastItsRange).
        Span<Point> outline = stackalloc Point[6];
        var theme = Context.Theme;
        for (int cell = 0; cell < counts.Length; cell++)
        {
            if (counts[cell] < minCount || scale.Normalizer.Masks(counts[cell], scale.Min)) continue;

            lattice.OutlineAt(lattice.CentreOf(cell), outline);
            var pixelVerts = new Point[6];
            for (int corner = 0; corner < 6; corner++)
                pixelVerts[corner] = Transform.DataToPixel(outline[corner].X, outline[corner].Y);

            var color = scale.ColorOf(counts[cell]);
            Ctx.DrawPolygon(pixelVerts, theme.JoinedAs(artist, ShapeStyle.Edged(color, color, OutlineWidth)));
        }
    }
}
