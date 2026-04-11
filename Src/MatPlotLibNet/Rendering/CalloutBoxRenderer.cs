// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Draws background boxes (FancyBBoxPatch equivalents) for annotated text callouts.</summary>
internal static class CalloutBoxRenderer
{
    /// <summary>Draws a padded box around <paramref name="textBounds"/> using the specified style.</summary>
    public static void Draw(IRenderContext ctx, Rect textBounds, BoxStyle style,
        double padding, double cornerRadius, Color? faceColor, Color? edgeColor, double edgeWidth)
    {
        if (style == BoxStyle.None) return;

        var box = new Rect(
            textBounds.X - padding,
            textBounds.Y - padding,
            textBounds.Width + padding * 2,
            textBounds.Height + padding * 2);

        switch (style)
        {
            case BoxStyle.Square:
                ctx.DrawRectangle(box, faceColor, edgeColor, edgeWidth);
                break;

            case BoxStyle.Round:
                ctx.DrawPath(BuildRoundedRect(box, cornerRadius), faceColor, edgeColor, edgeWidth);
                break;

            case BoxStyle.RoundTooth:
                ctx.DrawPath(BuildRoundTooth(box, cornerRadius), faceColor, edgeColor, edgeWidth);
                break;

            case BoxStyle.Sawtooth:
                ctx.DrawPath(BuildSawtooth(box), faceColor, edgeColor, edgeWidth);
                break;
        }
    }

    // --- Path builders ---

    /// <summary>Builds a rounded-rectangle path using cubic Bezier corner arcs.</summary>
    private static IReadOnlyList<PathSegment> BuildRoundedRect(Rect r, double cr)
    {
        cr = Math.Min(cr, Math.Min(r.Width / 2, r.Height / 2));
        double l = r.X, t = r.Y, ri = r.X + r.Width, b = r.Y + r.Height;
        const double k = 0.5523; // bezier approximation of quarter-circle

        return
        [
            new MoveToSegment(new Point(l + cr, t)),
            new LineToSegment(new Point(ri - cr, t)),
            new BezierSegment(new Point(ri - cr + k * cr, t), new Point(ri, t + cr - k * cr), new Point(ri, t + cr)),
            new LineToSegment(new Point(ri, b - cr)),
            new BezierSegment(new Point(ri, b - cr + k * cr), new Point(ri - cr + k * cr, b), new Point(ri - cr, b)),
            new LineToSegment(new Point(l + cr, b)),
            new BezierSegment(new Point(l + cr - k * cr, b), new Point(l, b - cr + k * cr), new Point(l, b - cr)),
            new LineToSegment(new Point(l, t + cr)),
            new BezierSegment(new Point(l, t + cr - k * cr), new Point(l + cr - k * cr, t), new Point(l + cr, t)),
            new CloseSegment(),
        ];
    }

    /// <summary>Builds a rounded-rect with a sawtooth (zigzag) bottom edge.</summary>
    private static IReadOnlyList<PathSegment> BuildRoundTooth(Rect r, double cr)
    {
        cr = Math.Min(cr, Math.Min(r.Width / 2, r.Height / 2));
        double l = r.X, t = r.Y, ri = r.X + r.Width, b = r.Y + r.Height;
        const double k = 0.5523;
        const double toothW = 6, toothH = 4;

        var segments = new List<PathSegment>
        {
            new MoveToSegment(new Point(l + cr, t)),
            new LineToSegment(new Point(ri - cr, t)),
            new BezierSegment(new Point(ri - cr + k * cr, t), new Point(ri, t + cr - k * cr), new Point(ri, t + cr)),
            new LineToSegment(new Point(ri, b - cr)),
            new BezierSegment(new Point(ri, b - cr + k * cr), new Point(ri - cr + k * cr, b), new Point(ri - cr, b)),
        };

        // Zigzag bottom edge from right to left
        double x = ri - cr;
        bool up = true;
        while (x > l + cr + toothW)
        {
            x -= toothW;
            segments.Add(new LineToSegment(new Point(x, up ? b - toothH : b)));
            up = !up;
        }
        segments.Add(new LineToSegment(new Point(l + cr, b)));

        segments.Add(new BezierSegment(new Point(l + cr - k * cr, b), new Point(l, b - cr + k * cr), new Point(l, b - cr)));
        segments.Add(new LineToSegment(new Point(l, t + cr)));
        segments.Add(new BezierSegment(new Point(l, t + cr - k * cr), new Point(l + cr - k * cr, t), new Point(l + cr, t)));
        segments.Add(new CloseSegment());
        return segments;
    }

    /// <summary>Builds a rectangle with sawtooth edges on all four sides.</summary>
    private static IReadOnlyList<PathSegment> BuildSawtooth(Rect r)
    {
        double l = r.X, t = r.Y, ri = r.X + r.Width, b = r.Y + r.Height;
        const double toothW = 5, toothH = 3;

        var segments = new List<PathSegment> { new MoveToSegment(new Point(l, t)) };

        // Top edge (left → right)
        segments.AddRange(SawteethEdge(l, ri, t, isHorizontal: true, inward: false, toothW, toothH));
        // Right edge (top → bottom)
        segments.AddRange(SawteethEdge(t, b, ri, isHorizontal: false, inward: true, toothW, toothH));
        // Bottom edge (right → left)
        segments.AddRange(SawteethEdge(ri, l, b, isHorizontal: true, inward: true, toothW, toothH));
        // Left edge (bottom → top)
        segments.AddRange(SawteethEdge(b, t, l, isHorizontal: false, inward: false, toothW, toothH));

        segments.Add(new CloseSegment());
        return segments;
    }

    private static IEnumerable<PathSegment> SawteethEdge(double from, double to, double perpendicular,
        bool isHorizontal, bool inward, double toothW, double toothH)
    {
        double len = Math.Abs(to - from);
        int count = Math.Max(1, (int)(len / toothW));
        double step = (to - from) / count;
        double perpSign = inward ? 1 : -1;

        for (int i = 0; i < count; i++)
        {
            double mid = from + step * i + step * 0.5;
            double peak = perpendicular + perpSign * toothH;
            if (isHorizontal)
            {
                yield return new LineToSegment(new Point(mid, peak));
                yield return new LineToSegment(new Point(from + step * (i + 1), perpendicular));
            }
            else
            {
                yield return new LineToSegment(new Point(peak, mid));
                yield return new LineToSegment(new Point(perpendicular, from + step * (i + 1)));
            }
        }
    }
}
