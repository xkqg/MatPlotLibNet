// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Renders hatch patterns inside a clipped rectangular region using existing
/// <see cref="IRenderContext"/> primitives — no interface changes required.
/// </summary>
internal static class HatchRenderer
{
    /// <summary>
    /// Draws a hatch pattern inside <paramref name="bounds"/> using the given <paramref name="color"/>.
    /// When <paramref name="pattern"/> is <see cref="HatchPattern.None"/> this is a no-op.
    /// </summary>
    public static void DrawHatch(IRenderContext ctx, Rect bounds,
        HatchPattern pattern, Color color, double lineWidth = 1.0, double spacing = 6.0)
    {
        if (pattern == HatchPattern.None) return;

        ctx.PushClip(bounds);

        switch (pattern)
        {
            case HatchPattern.ForwardDiagonal:
                DrawDiagonalLines(ctx, bounds, color, lineWidth, spacing, forward: true);
                break;
            case HatchPattern.BackDiagonal:
                DrawDiagonalLines(ctx, bounds, color, lineWidth, spacing, forward: false);
                break;
            case HatchPattern.Horizontal:
                DrawHorizontalLines(ctx, bounds, color, lineWidth, spacing);
                break;
            case HatchPattern.Vertical:
                DrawVerticalLines(ctx, bounds, color, lineWidth, spacing);
                break;
            case HatchPattern.Cross:
                DrawHorizontalLines(ctx, bounds, color, lineWidth, spacing);
                DrawVerticalLines(ctx, bounds, color, lineWidth, spacing);
                break;
            case HatchPattern.DiagonalCross:
                DrawDiagonalLines(ctx, bounds, color, lineWidth, spacing, forward: true);
                DrawDiagonalLines(ctx, bounds, color, lineWidth, spacing, forward: false);
                break;
            case HatchPattern.Dots:
                DrawDots(ctx, bounds, color, lineWidth, spacing);
                break;
            case HatchPattern.Stars:
                DrawDots(ctx, bounds, color, lineWidth, spacing);
                DrawDiagonalLines(ctx, bounds, color, lineWidth, spacing, forward: true);
                DrawDiagonalLines(ctx, bounds, color, lineWidth, spacing, forward: false);
                break;
        }

        ctx.PopClip();
    }

    // ── Line generators ──────────────────────────────────────────────────────

    private static void DrawHorizontalLines(IRenderContext ctx, Rect b,
        Color color, double lw, double spacing)
    {
        for (double y = b.Y; y <= b.Y + b.Height; y += spacing)
            ctx.DrawLine(new Point(b.X, y), new Point(b.X + b.Width, y), color, lw, LineStyle.Solid);
    }

    private static void DrawVerticalLines(IRenderContext ctx, Rect b,
        Color color, double lw, double spacing)
    {
        for (double x = b.X; x <= b.X + b.Width; x += spacing)
            ctx.DrawLine(new Point(x, b.Y), new Point(x, b.Y + b.Height), color, lw, LineStyle.Solid);
    }

    private static void DrawDiagonalLines(IRenderContext ctx, Rect b,
        Color color, double lw, double spacing, bool forward)
    {
        double size = b.Width + b.Height;
        double startOffset = -size;
        double endOffset   =  size;

        for (double offset = startOffset; offset <= endOffset; offset += spacing)
        {
            Point p1, p2;
            if (forward)
            {
                // / direction: bottom-left to top-right
                p1 = new Point(b.X + offset,              b.Y + b.Height);
                p2 = new Point(b.X + offset + b.Height,   b.Y);
            }
            else
            {
                // \ direction: top-left to bottom-right
                p1 = new Point(b.X + offset,              b.Y);
                p2 = new Point(b.X + offset + b.Height,   b.Y + b.Height);
            }
            ctx.DrawLine(p1, p2, color, lw, LineStyle.Solid);
        }
    }

    private static void DrawDots(IRenderContext ctx, Rect b,
        Color color, double lw, double spacing)
    {
        double r = lw * 0.8;
        for (double x = b.X + spacing / 2; x <= b.X + b.Width; x += spacing)
            for (double y = b.Y + spacing / 2; y <= b.Y + b.Height; y += spacing)
                ctx.DrawCircle(new Point(x, y), r, color, null, 0);
    }
}
