// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>
/// Places a set of text labels so they don't overlap each other. Each caller supplies an
/// anchor point (where the label semantically belongs — centre of a pie wedge, above a bar,
/// next to a Sankey node) and the engine returns a <see cref="LabelPlacement"/> with the
/// final draw position plus, if the label had to move far, an optional leader-line anchor
/// for the caller to connect back to the original spot.
/// </summary>
/// <remarks>
/// <para>Algorithm: iterative pair-wise repulsion. For each iteration, every overlapping
/// pair of label rectangles is detected via <see cref="Rect.Intersects"/> and the
/// lower-priority label is nudged along the normal between centres by half the overlap
/// magnitude. Clamped to the plot bounds each pass. Converges in &lt; 20 iterations for
/// dense pies and Sankeys; falls through on the 20th iteration with whatever positions
/// have been reached.</para>
///
/// <para>Uses <see cref="ChartServices.FontMetrics"/> for text measurement so SVG and PNG
/// output agree on label extents. The measurement source is the same Skia-backed
/// <c>DejaVu Sans</c> glyph metrics that the renderer uses, so there's no drift between
/// what the engine measures and what the renderer draws.</para>
/// </remarks>
public static class LabelLayoutEngine
{
    private const int MaxIterations = 20;

    /// <summary>
    /// Places <paramref name="candidates"/> so their bounding rectangles don't overlap,
    /// clamped inside <paramref name="plotBounds"/>. Returns one <see cref="LabelPlacement"/>
    /// per candidate in the same order.
    /// </summary>
    /// <param name="candidates">Input labels with their desired anchor points.</param>
    /// <param name="plotBounds">Outer bounds labels must stay inside.</param>
    /// <param name="metrics">Text measurement source (typically <see cref="ChartServices.FontMetrics"/>).</param>
    /// <param name="leaderThreshold">When a label's final position is more than this many pixels
    /// from its anchor, the returned placement will include a <see cref="LabelPlacement.LeaderLineStart"/>
    /// for the caller to draw a connector. Default 6 px.</param>
    public static IReadOnlyList<LabelPlacement> Place(
        IReadOnlyList<LabelCandidate> candidates,
        Rect plotBounds,
        IFontMetrics metrics,
        double leaderThreshold = 6.0)
    {
        int n = candidates.Count;
        if (n == 0) return Array.Empty<LabelPlacement>();

        // Working state: one position + size per candidate.
        var positions = new Point[n];
        var sizes = new Size[n];
        for (int i = 0; i < n; i++)
        {
            positions[i] = candidates[i].AnchorPoint;
            sizes[i] = metrics.Measure(candidates[i].Text, candidates[i].Font);
        }

        // Iterative repulsion: each pass walks every overlapping pair and nudges them apart.
        for (int iter = 0; iter < MaxIterations; iter++)
        {
            bool anyOverlap = false;
            for (int i = 0; i < n; i++)
            {
                var ri = RectOf(positions[i], sizes[i], candidates[i].Alignment);
                for (int j = i + 1; j < n; j++)
                {
                    var rj = RectOf(positions[j], sizes[j], candidates[j].Alignment);
                    if (!ri.Intersects(rj)) continue;
                    anyOverlap = true;

                    // Compute the minimum translation vector (MTV) — the shortest shift that
                    // would separate the two rectangles.
                    double overlapX = Math.Min(ri.Right, rj.Right) - Math.Max(ri.X, rj.X);
                    double overlapY = Math.Min(ri.Bottom, rj.Bottom) - Math.Max(ri.Y, rj.Y);

                    // Split the translation: each label moves half along the smaller axis of
                    // overlap (cheaper separation). Priority-weighted: a higher-priority label
                    // contributes less of the displacement, so it stays closer to its anchor.
                    double wi = PriorityWeight(candidates[i].Priority);
                    double wj = PriorityWeight(candidates[j].Priority);
                    double wSum = wi + wj;
                    double shareI = wj / wSum;   // i moves proportionally to j's weight
                    double shareJ = wi / wSum;

                    Point dirI, dirJ;
                    if (overlapX < overlapY)
                    {
                        // Separate along X
                        double sign = ri.Center.X < rj.Center.X ? -1 : 1;
                        dirI = new Point(sign * overlapX * shareI, 0);
                        dirJ = new Point(-sign * overlapX * shareJ, 0);
                    }
                    else
                    {
                        // Separate along Y
                        double sign = ri.Center.Y < rj.Center.Y ? -1 : 1;
                        dirI = new Point(0, sign * overlapY * shareI);
                        dirJ = new Point(0, -sign * overlapY * shareJ);
                    }
                    positions[i] = new Point(positions[i].X + dirI.X, positions[i].Y + dirI.Y);
                    positions[j] = new Point(positions[j].X + dirJ.X, positions[j].Y + dirJ.Y);
                    ri = RectOf(positions[i], sizes[i], candidates[i].Alignment);
                }
            }

            // Clamp each label inside plotBounds on every pass.
            for (int i = 0; i < n; i++)
            {
                var r = RectOf(positions[i], sizes[i], candidates[i].Alignment);
                double dx = 0, dy = 0;
                if (r.X < plotBounds.X) dx = plotBounds.X - r.X;
                else if (r.Right > plotBounds.Right) dx = plotBounds.Right - r.Right;
                if (r.Y < plotBounds.Y) dy = plotBounds.Y - r.Y;
                else if (r.Bottom > plotBounds.Bottom) dy = plotBounds.Bottom - r.Bottom;
                if (dx != 0 || dy != 0)
                    positions[i] = new Point(positions[i].X + dx, positions[i].Y + dy);
            }

            if (!anyOverlap) break;
        }

        // Build the output. If a label has moved more than leaderThreshold from its anchor,
        // record the anchor as LeaderLineStart so the caller can draw a connector.
        var result = new LabelPlacement[n];
        for (int i = 0; i < n; i++)
        {
            var anchor = candidates[i].AnchorPoint;
            double dx = positions[i].X - anchor.X;
            double dy = positions[i].Y - anchor.Y;
            bool moved = dx * dx + dy * dy > leaderThreshold * leaderThreshold;
            result[i] = new LabelPlacement(
                FinalPoint: positions[i],
                Text: candidates[i].Text,
                Font: candidates[i].Font,
                Alignment: candidates[i].Alignment,
                LeaderLineStart: moved ? anchor : null);
        }
        return result;
    }

    /// <summary>Builds the bounding rectangle for a label at a given position + size + alignment.</summary>
    private static Rect RectOf(Point position, Size size, TextAlignment alignment)
    {
        // Treat position.Y as the baseline of the text (SVG convention). Ascent takes ~75 %
        // of the height, descent the remainder — cheap approximation good enough for collision.
        double top = position.Y - size.Height * 0.75;
        double left = alignment switch
        {
            TextAlignment.Center => position.X - size.Width / 2,
            TextAlignment.Right  => position.X - size.Width,
            _                    => position.X,   // Left / default
        };
        return new Rect(left, top, size.Width, size.Height);
    }

    private static double PriorityWeight(LabelPriority priority) => priority switch
    {
        LabelPriority.High => 3.0,
        LabelPriority.Low  => 0.33,
        _                  => 1.0,
    };
}

/// <summary>Input to <see cref="LabelLayoutEngine.Place"/>.</summary>
/// <param name="AnchorPoint">Where the label semantically belongs (wedge centre, bar top, node side).</param>
/// <param name="Text">The label text.</param>
/// <param name="Font">The font used to measure and eventually draw the label.</param>
/// <param name="Alignment">Horizontal text alignment applied when positioning (affects the bounding rect).</param>
/// <param name="Priority">Higher-priority labels resist displacement during collision resolution.</param>
public sealed record LabelCandidate(
    Point AnchorPoint,
    string Text,
    Font Font,
    TextAlignment Alignment = TextAlignment.Center,
    LabelPriority Priority = LabelPriority.Normal);

/// <summary>Result of <see cref="LabelLayoutEngine.Place"/> — one per input candidate.</summary>
/// <param name="FinalPoint">The collision-adjusted draw position.</param>
/// <param name="Text">The label text (passed through from the candidate).</param>
/// <param name="Font">The font (passed through from the candidate).</param>
/// <param name="Alignment">The text alignment (passed through from the candidate).</param>
/// <param name="LeaderLineStart">When non-null, the original anchor point — the caller should
/// draw a leader line from this point to <see cref="FinalPoint"/> so the reader can trace which
/// data point the label describes.</param>
public sealed record LabelPlacement(
    Point FinalPoint,
    string Text,
    Font Font,
    TextAlignment Alignment,
    Point? LeaderLineStart);

/// <summary>Relative resistance to displacement during collision resolution.</summary>
public enum LabelPriority
{
    Low,
    Normal,
    High,
}
