// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The box drawn behind an annotation's text: its style, the padding it stands off the text by, its corner radius and its
/// fill and outline (the FancyBboxPatch of matplotlib's annotations).</summary>
/// <param name="TextBounds">The rectangle of the text the box surrounds.</param>
/// <param name="Style">The shape of the box; <see cref="BoxStyle.None"/> draws nothing.</param>
/// <param name="Padding">How far the box stands off the text on every side.</param>
/// <param name="CornerRadius">The radius of a rounded corner.</param>
/// <param name="FaceColor">The fill, or <see langword="null"/> for none.</param>
/// <param name="EdgeColor">The outline, or <see langword="null"/> for none.</param>
/// <param name="EdgeWidth">The width of the outline.</param>
internal readonly record struct CalloutBox(
    Rect TextBounds, BoxStyle Style, double Padding, double CornerRadius,
    Color? FaceColor, Color? EdgeColor, double EdgeWidth)
{
    /// <summary>The rectangle of the box: the text bounds, padded on every side.</summary>
    internal Rect Box => new(
        TextBounds.X - Padding,
        TextBounds.Y - Padding,
        TextBounds.Width + Padding * 2,
        TextBounds.Height + Padding * 2);

    /// <summary>Draws the box in its style on <paramref name="ctx"/>; a style that is none, or not defined, draws nothing.</summary>
    /// <param name="ctx">The context to draw on.</param>
    public void Draw(IRenderContext ctx) =>
        CalloutShape.Of(Style).Draw(ctx, Box, CornerRadius, ShapeStyle.Edged(FaceColor, EdgeColor, EdgeWidth));
}

/// <summary>The outline of the box behind an annotation's text: one sealed leaf for each <see cref="BoxStyle"/>, chosen in one
/// place (<see cref="Of"/>).</summary>
internal abstract class CalloutShape
{
    private static readonly NoCallout NoOutline = new();
    private static readonly SquareCallout Square = new();
    private static readonly RoundCallout Round = new();
    private static readonly RoundToothCallout RoundTooth = new();
    private static readonly SawtoothCallout Sawtooth = new();

    /// <summary>The outline of <paramref name="style"/>: the one place a box style is mapped to its outline, the same instance
    /// on every call. <see cref="BoxStyle.None"/>, and a style the enum does not declare, draw nothing.</summary>
    /// <param name="style">The style an annotation asks for.</param>
    internal static CalloutShape Of(BoxStyle style) => style switch
    {
        BoxStyle.Square => Square,
        BoxStyle.Round => Round,
        BoxStyle.RoundTooth => RoundTooth,
        BoxStyle.Sawtooth => Sawtooth,
        _ => NoOutline,
    };

    /// <summary>Draws the outline around <paramref name="box"/>.</summary>
    /// <param name="ctx">The context to draw on.</param>
    /// <param name="box">The padded rectangle of the box.</param>
    /// <param name="cornerRadius">The radius of a rounded corner.</param>
    /// <param name="shape">The fill and the outline.</param>
    internal abstract void Draw(IRenderContext ctx, Rect box, double cornerRadius, ShapeStyle shape);
}

/// <summary>No box.</summary>
internal sealed class NoCallout : CalloutShape
{
    /// <inheritdoc />
    internal override void Draw(IRenderContext ctx, Rect box, double cornerRadius, ShapeStyle shape) { }
}

/// <summary>A plain rectangle with sharp corners.</summary>
internal sealed class SquareCallout : CalloutShape
{
    /// <inheritdoc />
    internal override void Draw(IRenderContext ctx, Rect box, double cornerRadius, ShapeStyle shape) => ctx.DrawRectangle(box, shape);
}

/// <summary>A rectangle with rounded corners, each a cubic Bézier curve no wider than half the shorter side of the box: the one outline
/// of a rounded box, whose bottom edge each leaf says (<see cref="TeethBetween"/>), straight or toothed.</summary>
internal abstract class RoundedCallout : CalloutShape
{
    /// <summary>How far along its tangents a corner's control points stand, as a share of the corner's radius: the cubic Bézier
    /// approximation of a quarter circle the rounded boxes have always drawn.</summary>
    private const double CornerControl = 0.5523;

    /// <inheritdoc />
    internal override void Draw(IRenderContext ctx, Rect box, double cornerRadius, ShapeStyle shape) =>
        ctx.DrawPath(Outline(box, cornerRadius), shape);

    /// <summary>The outline: the top edge, the top right corner, the right edge, the bottom right corner, the teeth of the bottom edge
    /// from right to left (<see cref="TeethBetween"/>, <see cref="ToothAt"/>), the bottom left corner, the left edge and the top left
    /// corner, closed. One array of exactly its segments: a legend's rounded frame is drawn every frame.</summary>
    /// <param name="r">The box.</param>
    /// <param name="cr">The radius asked for the corners; no more than half the shorter side is drawn.</param>
    /// <returns>The segments.</returns>
    internal PathSegment[] Outline(Rect r, double cr)
    {
        cr = Math.Min(cr, r.ShorterSide / 2);
        double l = r.X, t = r.Y, ri = r.X + r.Width, b = r.Y + r.Height;
        const double k = CornerControl;

        int teeth = TeethBetween(ri - cr, l + cr);
        var segments = new PathSegment[10 + teeth];
        segments[0] = new MoveToSegment(new Point(l + cr, t));
        segments[1] = new LineToSegment(new Point(ri - cr, t));
        segments[2] = new BezierSegment(new Point(ri - cr + k * cr, t), new Point(ri, t + cr - k * cr), new Point(ri, t + cr));
        segments[3] = new LineToSegment(new Point(ri, b - cr));
        segments[4] = new BezierSegment(new Point(ri, b - cr + k * cr), new Point(ri - cr + k * cr, b), new Point(ri - cr, b));
        for (int i = 0; i < teeth; i++) segments[5 + i] = new LineToSegment(ToothAt(i, ri - cr, b));
        segments[5 + teeth] = new LineToSegment(new Point(l + cr, b));
        segments[6 + teeth] = new BezierSegment(new Point(l + cr - k * cr, b), new Point(l, b - cr + k * cr), new Point(l, b - cr));
        segments[7 + teeth] = new LineToSegment(new Point(l, t + cr));
        segments[8 + teeth] = new BezierSegment(new Point(l, t + cr - k * cr), new Point(l + cr - k * cr, t), new Point(l + cr, t));
        segments[9 + teeth] = new CloseSegment();
        return segments;
    }

    /// <summary>How many teeth the bottom edge holds between the bottom right corner (<paramref name="from"/>) and the bottom left one
    /// (<paramref name="to"/>); none on a straight edge.</summary>
    /// <param name="from">Where the edge starts, at the right.</param>
    /// <param name="to">Where it ends, at the left.</param>
    /// <returns>The number of teeth.</returns>
    private protected virtual int TeethBetween(double from, double to) => 0;

    /// <summary>The end of tooth <paramref name="index"/>, counted from the right.</summary>
    /// <param name="index">The tooth.</param>
    /// <param name="from">Where the edge starts, at the right.</param>
    /// <param name="bottom">The Y of the bottom edge.</param>
    /// <returns>The point the tooth's line runs to.</returns>
    private protected virtual Point ToothAt(int index, double from, double bottom) => new(from, bottom);
}

/// <summary>A rectangle with rounded corners and straight edges.</summary>
internal sealed class RoundCallout : RoundedCallout;

/// <summary>A rounded rectangle with a zigzag bottom edge.</summary>
internal sealed class RoundToothCallout : RoundedCallout
{
    /// <summary>How wide one tooth of the bottom edge is, and how high it rises, in pixels.</summary>
    private const double ToothWidth = 6, ToothHeight = 4;

    /// <inheritdoc />
    /// <remarks>As many whole teeth as fit before the left corner: a step is taken while the edge still reaches past one more tooth.</remarks>
    private protected override int TeethBetween(double from, double to)
    {
        int teeth = 0;
        for (double x = from; x > to + ToothWidth; x -= ToothWidth) teeth++;
        return teeth;
    }

    /// <inheritdoc />
    /// <remarks>Each tooth a step to the left, up and down by turns, the first one up.</remarks>
    private protected override Point ToothAt(int index, double from, double bottom)
    {
        double x = from;
        for (int i = 0; i <= index; i++) x -= ToothWidth;
        return new(x, index % 2 == 0 ? bottom - ToothHeight : bottom);
    }
}

/// <summary>A rectangle with sawtooth edges on all four sides.</summary>
internal sealed class SawtoothCallout : CalloutShape
{
    /// <inheritdoc />
    internal override void Draw(IRenderContext ctx, Rect box, double cornerRadius, ShapeStyle shape) =>
        ctx.DrawPath(BuildSawtooth(box), shape);

    /// <summary>Builds a rectangle with sawtooth edges on all four sides.</summary>
    private static IReadOnlyList<PathSegment> BuildSawtooth(Rect r)
    {
        double l = r.X, t = r.Y, ri = r.X + r.Width, b = r.Y + r.Height;

        var segments = new List<PathSegment> { new MoveToSegment(new Point(l, t)) };

        // Top edge (left → right)
        segments.AddRange(SawteethEdge(l, ri, t, EdgeTerms.Top));
        // Right edge (top → bottom)
        segments.AddRange(SawteethEdge(t, b, ri, EdgeTerms.Right));
        // Bottom edge (right → left)
        segments.AddRange(SawteethEdge(ri, l, b, EdgeTerms.Bottom));
        // Left edge (bottom → top)
        segments.AddRange(SawteethEdge(b, t, l, EdgeTerms.Left));

        segments.Add(new CloseSegment());
        return segments;
    }

    /// <summary>How wide one tooth is along its side, and how far its peak stands out from it, in pixels.</summary>
    private const double ToothWidth = 5, ToothHeight = 3;

    /// <summary>The teeth of one side of the box, from <paramref name="from"/> to <paramref name="to"/> along it, at
    /// <paramref name="across"/>: each tooth's peak stands out of the box, the way out of <paramref name="side"/>.</summary>
    /// <param name="from">Where the side starts along it.</param>
    /// <param name="to">Where it ends along it.</param>
    /// <param name="across">The coordinate of the side across it.</param>
    /// <param name="side">The side of the box: which way it runs and which way is out of the box.</param>
    /// <returns>The segments of the teeth.</returns>
    private static IEnumerable<PathSegment> SawteethEdge(double from, double to, double across, EdgeTerms side)
    {
        double len = Math.Abs(to - from);
        int count = Math.Max(1, (int)(len / ToothWidth));
        double step = (to - from) / count;
        bool alongX = side.AlongX;

        for (int i = 0; i < count; i++)
        {
            double mid = from + step * i + step * 0.5;
            double peak = across + side.Outward * ToothHeight;
            yield return new LineToSegment(Point.Along(alongX, mid, peak));
            yield return new LineToSegment(Point.Along(alongX, from + step * (i + 1), across));
        }
    }
}
