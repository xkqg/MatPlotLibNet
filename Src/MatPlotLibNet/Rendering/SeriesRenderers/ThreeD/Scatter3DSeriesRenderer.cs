// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Scatter3DSeries"/> as the marker it names at each projected point, drawn back to front with the scene.</summary>
internal sealed class Scatter3DSeriesRenderer : SceneSeriesRenderer<Scatter3DSeries>
{
    /// <inheritdoc />
    public Scatter3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Scatter3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Scatter3DSeries series)
    {
        if (series.X.Length == 0) return;

        var slot = Slot;
        var fallbackColor = ColorOf(series);

        // The extent and the colours span the values that are there: a missing value (NaN) and an infinity are left out.
        var z = series.Z.Data.DrawableRange();
        var proj = ProjectionFor(series.X.Data.DrawableRange(), series.Y.Data.DrawableRange(), z);

        // A series that names a colour map colours each point by its Z through its colour scale; one that names none is one colour.
        ColormapContext? scale = series.ColorMap is null ? null : series.ColorScaleUnder(Context.Theme);

        // Each point at its depth, with the farthest and the nearest depth of the points drawn; a point that cannot be placed is
        // skipped, as matplotlib masks it.
        var points = new List<PointMark>(series.X.Length);
        double farthest = double.MaxValue, nearest = double.MinValue;
        for (int i = 0; i < series.X.Length; i++)
        {
            var pt = proj.Project(series.X[i], series.Y[i], series.Z[i]);
            if (!pt.IsDrawable()) continue;
            double depth = proj.Depth(series.X[i], series.Y[i], series.Z[i]);
            points.Add(new(depth, i, pt));
            farthest = Math.Min(farthest, depth);
            nearest = Math.Max(nearest, depth);
        }
        if (points.Count == 0) return;

        double depthRange = points.Count > 1 ? nearest - farthest : 1;

        bool emitV3d = Context.Emit3DData;
        double markerRadius = slot.Scatter3DSizeOf(series.GivenMarkerSize).PointsInPixels / 2;

        // The shape is resolved once for the series and every point is drawn through it, as a 2-D scatter does.
        var marker = MarkerShape.Of(series.MarkerStyle, Context.Theme);

        // A theme that names a scatter look keeps every marker one size and, in the default style, fades the farther ones
        // (matplotlib's depthshade); the library's own themes shrink a marker with its distance, to half at the farthest.
        bool oneSize = slot.Scatter3DKeepsOneSize;
        double? leastOpacity = slot.Scatter3DLeastOpacity;
        var shade = leastOpacity is null ? default : DepthShadeOf(proj, series, points);

        DrawBackToFront(points, new Markers(Ctx, series, proj, scale, fallbackColor, slot, marker, shade, leastOpacity,
            new MarkerSizing(markerRadius, farthest, depthRange, oneSize), emitV3d));
    }

    /// <summary>How big a marker is drawn: one size under a theme that names a scatter look, otherwise shrunk with its distance.</summary>
    /// <param name="Radius">The radius of a marker at its full size, in pixels.</param>
    /// <param name="Farthest">The depth of the farthest marker drawn.</param>
    /// <param name="DepthRange">How far the nearest marker stands in front of the farthest.</param>
    /// <param name="OneSize">Whether every marker is drawn at its full size.</param>
    private readonly record struct MarkerSizing(double Radius, double Farthest, double DepthRange, bool OneSize)
    {
        /// <summary>The radius of a marker standing at <paramref name="depth"/>.</summary>
        internal double RadiusAt(double depth) =>
            OneSize ? Radius : Radius * (0.5 + 0.5 * DepthFraction(depth, Farthest, DepthRange));
    }

    /// <summary>The rule that draws a run of the series' markers, resolved once for the series: each marker in its colour, size and
    /// opacity, through the one shape. One call draws the whole run, so the drawing it inlines is set up once a run, not once a marker.
    /// It is compiled fully optimized at once: the scene calls it once a draw with thousands of markers, which tiering would leave in
    /// the form it profiles a method in.</summary>
    private readonly struct Markers(IRenderContext ctx, Scatter3DSeries series, Projection3D proj, ColormapContext? scale, Color fallbackColor,
        SeriesSlot slot, MarkerShape marker, DepthShade shade, double? leastOpacity, MarkerSizing sizing, bool emitV3d) : IMarkRunDrawer<PointMark>
    {
        /// <inheritdoc />
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
        public void DrawRun(MarkRun<PointMark> run)
        {
            // A cross and a plus are strokes in the fill colour, so they take no edge of the theme. A circle is one element a script
            // moves by its centre; any other shape is a group of however many elements it takes, and the script moves the group by
            // where its centre stood when it was drawn (data-c) and where the scene puts it now.
            bool stroked = !marker.TakesAnEdge;
            bool grouped = emitV3d && marker is not CircleMarker;
            Span<char> wire = stackalloc char[Normalized3DPoint.PointLength];
            foreach (var entry in run)
            {
                int idx = entry.Index;
                double xi = series.X[idx], yi = series.Y[idx], zi = series.Z[idx];
                var pt = entry.Pixel;

                // Resolve color: colormap Z-mapping takes priority over flat color
                var color = scale?.ColorOf(zi) ?? fallbackColor;

                if (emitV3d)
                {
                    var n = proj.Normalize(xi, yi, zi);
                    ctx.SetNextElementData("v3d", new string(wire[..n.FormatWire(wire, Normalized3DPoint.MarkDigits)]));
                }

                double opacity = leastOpacity is { } least ? shade.OpacityOf(proj.ProjectedAt(xi, yi, zi).Z, least) : 1;
                var style = slot.ScatterStyleOf(color, opacity);
                if (stroked) style = ShapeStyle.Filled(style.Fill!.Value);

                if (grouped)
                {
                    ctx.SetNextElementData("c", string.Invariant($"{pt.X:0.##},{pt.Y:0.##}"));
                    ctx.Begin3DSubgroup("mpl-3d-marker");
                }

                marker.Draw(ctx, pt, sizing.RadiusAt(entry.Depth) * 2, style);
                if (grouped) ctx.End3DSubgroup();
            }
        }

    }

    /// <summary>Where a marker stands between the farthest (0) and the nearest (1), 0.5 when all stand at one depth: what the
    /// library's own themes scale its size by.</summary>
    private static double DepthFraction(double depth, double farthest, double depthRange) =>
        depthRange > 0 ? (depth - farthest) / depthRange : 0.5;

    /// <summary>The depth shade of the markers drawn, measured over their projected points as matplotlib measures its own.</summary>
    private static DepthShade DepthShadeOf(Projection3D proj, Scatter3DSeries series, List<PointMark> drawn)
    {
        var points = new Projected3DPoint[drawn.Count];
        for (int i = 0; i < points.Length; i++)
        {
            int idx = drawn[i].Index;
            points[i] = proj.ProjectedAt(series.X[idx], series.Y[idx], series.Z[idx]);
        }

        return DepthShade.Of(points);
    }

    /// <summary>A point of the series: its index in the data and its place on the canvas, at its depth.</summary>
    private readonly record struct PointMark(double Depth, int Index, Point Pixel) : IDepthMark;

    /// <summary>How matplotlib's <c>depthshade</c> fades the markers of a 3-D scatter with their distance (<c>_zalpha</c>): a marker's
    /// opacity falls from 1 at the nearest one by its distance over the scale of the data, down to a least opacity.</summary>
    /// <param name="Nearest">The smallest projected depth among the markers.</param>
    /// <param name="Scale">The scale of the data: the root of the summed squares of the ranges of the projected x, y and depth
    /// (<c>_get_data_scale</c>); 0 for fewer than two distinct points, where nothing fades.</param>
    internal readonly record struct DepthShade(double Nearest, double Scale)
    {
        /// <summary>The shade of a set of markers.</summary>
        /// <param name="points">The markers, projected as matplotlib projects them.</param>
        /// <returns>The shade; no scale for no markers.</returns>
        internal static DepthShade Of(ReadOnlySpan<Projected3DPoint> points)
        {
            if (points.IsEmpty) return default;

            double minX = double.MaxValue, maxX = double.MinValue, minY = minX, maxY = maxX, minZ = minX, maxZ = maxX;
            foreach (var p in points)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z);
            }

            double rangeX = maxX - minX, rangeY = maxY - minY, rangeZ = maxZ - minZ;
            return new DepthShade(minZ, new Lighting.Vec3(rangeX, rangeY, rangeZ).Length);
        }

        /// <summary>The opacity of a marker at a depth: <c>clip(1 - (z - nearest) / scale, least, 1)</c>, and 1 where there is no scale to
        /// measure by, as matplotlib draws a marker whose data has none.</summary>
        /// <param name="z">The marker's projected depth.</param>
        /// <param name="least">The least opacity, held between 0 and 1.</param>
        /// <returns>The opacity, from <paramref name="least"/> to 1.</returns>
        internal double OpacityOf(double z, double least) =>
            Scale > 0 ? Math.Clamp(1 - (z - Nearest) / Scale, least.Saturate(), 1) : 1;
    }
}
