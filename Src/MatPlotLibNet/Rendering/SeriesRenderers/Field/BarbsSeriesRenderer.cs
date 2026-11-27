// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="BarbsSeries"/> using meteorological wind barb notation: matplotlib's <c>Barbs</c> glyph under a theme that
/// draws it (<see cref="VectorFieldLook.BarbsLikeMatplotlib"/>), the library's own barb in lines otherwise.</summary>
internal sealed class BarbsSeriesRenderer : SeriesRenderer<BarbsSeries>
{
    /// <inheritdoc />
    public BarbsSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(BarbsSeries series)
    {
        if (series.X.Length == 0) return;

        var color = ColorOf(series);
        double staffLen = series.BarbLengthIn(Context.Theme);
        bool matplotlib = Context.Theme.Looks.VectorField.BarbsLikeMatplotlib;
        // Barbs.__init__: "Explicitly set a line width if we're not given one": kwargs['linewidth'] = 1 (points), edgecolors 'face'.
        var glyphStyle = Context.Theme.JoinedAs(series.Artist, ShapeStyle.Edged(color, color, GlyphLineWidthPoints.PointsInPixels));
        // The library's own barb is lines of its own look: a line's resting width under every theme.
        var lineStroke = Slot.StrokeOf(color, LineWidthRule.Own);

        for (int i = 0; i < series.X.Length; i++)
        {
            double speed = series.Speed.ElementOr(i, 0);
            double dir = series.Direction.ElementOr(i, 0);
            // A missing or infinite wind is left out, as matplotlib deletes a masked point (and an infinite speed has no count of flags).
            if (!double.IsFinite(speed) || !double.IsFinite(dir)) continue;

            // The staff points `dir` degrees clockwise from north: (sx, sy) on the screen (Y down), from the meteorological
            // direction turned to a math angle (east, counter-clockwise). The barbs stand a quarter turn clockwise of it.
            double radians = (-(dir - 90)).ToRadians();
            double sx = Math.Cos(radians), sy = -Math.Sin(radians);

            var origin = Transform.DataToPixel(series.X[i], series.Y[i]);
            if (matplotlib)
            {
                if (origin.IsDrawable()) DrawGlyph(origin, sx, sy, Math.Abs(speed), staffLen, glyphStyle);
                continue;
            }

            var tip = new Point(origin.X + sx * staffLen, origin.Y + sy * staffLen);
            // A barb at a position or in a direction that cannot be placed is skipped with its flags and barbs.
            if (!origin.CanDrawLineTo(tip)) continue;

            // Draw staff
            Ctx.DrawLine(origin, tip, lineStroke);

            // Draw barbs along the staff from tip toward origin
            double remaining = speed;
            double offset = 0;
            double barbSpacing = staffLen / 5.0;

            // Flags (50 kt triangles)
            while (remaining >= 50)
            {
                DrawFlag(origin, sx, sy, staffLen, offset, barbSpacing, color);
                remaining -= 50;
                offset += barbSpacing * 1.5;
            }
            // Full barbs (10 kt lines)
            while (remaining >= 10)
            {
                DrawBarb(origin, sx, sy, staffLen, offset, staffLen * 0.35, lineStroke);
                remaining -= 10;
                offset += barbSpacing;
            }
            // Half barb (5 kt)
            if (remaining >= 5)
                DrawBarb(origin, sx, sy, staffLen, offset, staffLen * 0.2, lineStroke);
        }
    }

    /// <summary>The width matplotlib's barbs are edged with, in points.</summary>
    private const double GlyphLineWidthPoints = 1;

    /// <summary>One barb as matplotlib's <c>Barbs._make_barbs</c> draws it with its default <c>sizes</c>, <c>pivot='tip'</c> and no flip: one
    /// polygon from the base out along the staff, then from its far end inward a flag for each 50 (the second and later half a spacing
    /// nearer the one before), a full barb for each 10 and a half barb for a remaining 5 (set 1.5 spacings in when it stands alone), the
    /// speed first rounded to the nearest 5, half to even (<c>_find_tails</c>, <c>np.around</c>); a speed that rounds to 0 is an empty
    /// circle 0.15 of the staff across the base.</summary>
    /// <param name="origin">The base of the staff, in pixels.</param>
    /// <param name="sx">The X part of the staff's unit direction, on the screen.</param>
    /// <param name="sy">The Y part of the staff's unit direction, on the screen.</param>
    /// <param name="speed">The magnitude, not negative.</param>
    /// <param name="length">The length of the staff, in pixels.</param>
    /// <param name="style">The fill, the edge and its width.</param>
    private void DrawGlyph(Point origin, double sx, double sy, double speed, double length, ShapeStyle style)
    {
        double rounded = HalfIncrement * Math.Round(speed / HalfIncrement, MidpointRounding.ToEven);
        int flags = (int)Math.Floor(rounded / FlagIncrement);
        double rest = rounded - (flags * FlagIncrement);
        int barbs = (int)Math.Floor(rest / FullIncrement);
        bool half = rest - (barbs * FullIncrement) >= HalfIncrement;

        if (flags == 0 && barbs == 0 && !half)
        {
            Ctx.DrawCircle(origin, EmptyRadius * length, ShapeStyle.Outlined(style.Stroke!.Value, style.StrokeThickness));
            return;
        }

        double spacing = Spacing * length, height = Height * length, width = Width * length;
        // A point `along` the staff from the base and `across` it toward the barbs' side (the staff turned a quarter clockwise on the screen).
        Point At(double along, double across) => new(origin.X + (sx * along) - (sy * across), origin.Y + (sy * along) + (sx * across));

        var points = new List<Point>(2 + (3 * (flags + barbs + 1))) { origin };
        double offset = length;
        for (int f = 0; f < flags; f++)
        {
            if (offset != length) offset += spacing / 2;
            points.Add(At(offset, 0));
            points.Add(At(offset - (width / 2), height));
            points.Add(At(offset - width, 0));
            offset -= width + spacing;
        }
        for (int b = 0; b < barbs; b++)
        {
            points.Add(At(offset, 0));
            points.Add(At(offset + (width / 2), height));
            points.Add(At(offset, 0));
            offset -= spacing;
        }
        if (half)
        {
            if (offset == length)
            {
                points.Add(At(offset, 0));
                offset -= 1.5 * spacing;
            }
            points.Add(At(offset, 0));
            points.Add(At(offset + (width / 4), height / 2));
            points.Add(At(offset, 0));
        }
        Ctx.DrawPolygon(points, style);
    }

    // _find_tails' increments (barb_increments' defaults) and _make_barbs' sizes, each a share of the staff.
    private const double HalfIncrement = 5, FullIncrement = 10, FlagIncrement = 50;
    private const double Spacing = 0.125, Height = 0.4, Width = 0.25, EmptyRadius = 0.15;

    private void DrawBarb(Point origin, double sx, double sy, double staffLen, double offset,
        double barbLen, StrokeStyle stroke)
    {
        double bx = origin.X + sx * (staffLen - offset);
        double by = origin.Y + sy * (staffLen - offset);
        // Perpendicular direction
        var barbTip = new Point(bx - sy * barbLen, by + sx * barbLen);
        Ctx.DrawLine(new Point(bx, by), barbTip, stroke);
    }

    private void DrawFlag(Point origin, double sx, double sy, double staffLen, double offset,
        double size, Styling.Color color)
    {
        double bx = origin.X + sx * (staffLen - offset);
        double by = origin.Y + sy * (staffLen - offset);
        double bx2 = origin.X + sx * (staffLen - offset - size);
        double by2 = origin.Y + sy * (staffLen - offset - size);
        var flagTip = new Point(bx - sy * size, by + sx * size);
        Ctx.DrawPolygon([new(bx, by), new(bx2, by2), flagTip], ShapeStyle.Filled(color));
    }
}
