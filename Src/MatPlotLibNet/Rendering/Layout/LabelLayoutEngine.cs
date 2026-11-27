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
/// <para>Algorithm: iterative pair-wise repulsion, in phases. Every label is measured once; then each pass pushes every
/// overlapping pair of label rectangles (<see cref="Rect.Intersects"/>) apart along the axis on which they overlap less, by
/// the whole of that overlap, shared between the two by priority so the label of higher priority moves less, and clamps every
/// label inside the plot bounds. The passes stop at the first that finds no overlap, and after 20 at most, with whatever
/// positions have been reached; dense pies and Sankeys settle well before that. Last, every label that ended up farther
/// from its anchor than the leader threshold gets a leader line.</para>
///
/// <para>The caller hands in the text measurement, so SVG and PNG output agree on label extents with what the renderer draws:
/// <see cref="FontMetricsDefault.Current"/> measures each label as the plain text it is, and the library's own renderers hand in
/// metrics that measure as the surface does, a label with math as the math it is drawn as.</para>
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
    /// <param name="metrics">Text measurement source (typically <see cref="FontMetricsDefault.Current"/>).</param>
    /// <param name="leaderThreshold">When a label's final position is more than this many pixels
    /// from its anchor, the returned placement will include a <see cref="LabelPlacement.LeaderLineStart"/>
    /// for the caller to draw a connector. Default 6 px.</param>
    public static IReadOnlyList<LabelPlacement> Place(
        IReadOnlyList<LabelCandidate> candidates,
        Rect plotBounds,
        IFontMetrics metrics,
        double leaderThreshold = 6.0)
    {
        if (candidates.Count == 0) return Array.Empty<LabelPlacement>();

        // Every label at its anchor, measured once; the passes move the positions.
        int n = candidates.Count;
        var positions = new Point[n];
        var sizes = new Size[n];
        for (int i = 0; i < n; i++)
        {
            positions[i] = candidates[i].AnchorPoint;
            sizes[i] = metrics.ExtentOf(candidates[i].Text, candidates[i].Font).Box;
        }

        for (int iter = 0; iter < MaxIterations; iter++)
        {
            bool anyOverlap = Repel(candidates, positions, sizes);
            ClampInside(candidates, positions, sizes, plotBounds);
            if (!anyOverlap) break;
        }

        // A label that has moved more than the threshold from its anchor keeps the anchor as the start of a leader line, so the
        // caller can draw a connector.
        var result = new LabelPlacement[n];
        for (int i = 0; i < n; i++)
        {
            var candidate = candidates[i];
            var anchor = candidate.AnchorPoint;
            bool moved = anchor.DistanceSquaredTo(positions[i]) > leaderThreshold * leaderThreshold;
            result[i] = new LabelPlacement(
                FinalPoint: positions[i],
                Text: candidate.Text,
                Font: candidate.Font,
                Alignment: candidate.Alignment,
                LeaderLineStart: moved ? anchor : null) { VerticalAlignment = candidate.VerticalAlignment };
        }

        return result;
    }

    /// <summary>One pass over every pair of labels: each pair that overlaps is pushed apart (<see cref="Separate"/>).</summary>
    /// <param name="candidates">The labels.</param>
    /// <param name="positions">Where each label stands now; moved.</param>
    /// <param name="sizes">The size each label was measured at.</param>
    /// <returns>Whether any pair overlapped.</returns>
    private static bool Repel(IReadOnlyList<LabelCandidate> candidates, Point[] positions, Size[] sizes)
    {
        bool anyOverlap = false;
        for (int i = 0; i < positions.Length; i++)
        {
            var ri = RectOf(candidates, positions, sizes, i);
            for (int j = i + 1; j < positions.Length; j++)
            {
                var rj = RectOf(candidates, positions, sizes, j);
                if (!ri.Intersects(rj)) continue;

                anyOverlap = true;
                Separate(candidates, positions, i, j, ri, rj);
                ri = RectOf(candidates, positions, sizes, i);
            }
        }

        return anyOverlap;
    }

    /// <summary>Pushes the overlapping labels <paramref name="i"/> and <paramref name="j"/> apart by the minimum translation vector:
    /// along the axis of the smaller overlap (the cheaper separation), by the whole overlap, which each label takes in proportion to the
    /// other's priority weight, so the label of higher priority stays closer to its anchor.</summary>
    private static void Separate(IReadOnlyList<LabelCandidate> candidates, Point[] positions, int i, int j, Rect ri, Rect rj)
    {
        double overlapX = Math.Min(ri.Right, rj.Right) - Math.Max(ri.X, rj.X);
        double overlapY = Math.Min(ri.Bottom, rj.Bottom) - Math.Max(ri.Y, rj.Y);

        double wi = PriorityWeight(candidates[i].Priority);
        double wj = PriorityWeight(candidates[j].Priority);
        double wSum = wi + wj;
        double shareI = wj / wSum;   // i moves proportionally to j's weight
        double shareJ = wi / wSum;

        Point dirI, dirJ;
        if (overlapX < overlapY)
        {
            double sign = ri.Center.X < rj.Center.X ? -1 : 1;
            dirI = new Point(sign * overlapX * shareI, 0);
            dirJ = new Point(-sign * overlapX * shareJ, 0);
        }
        else
        {
            double sign = ri.Center.Y < rj.Center.Y ? -1 : 1;
            dirI = new Point(0, sign * overlapY * shareI);
            dirJ = new Point(0, -sign * overlapY * shareJ);
        }

        positions[i] = new Point(positions[i].X + dirI.X, positions[i].Y + dirI.Y);
        positions[j] = new Point(positions[j].X + dirJ.X, positions[j].Y + dirJ.Y);
    }

    /// <summary>Moves every label that sticks out of <paramref name="bounds"/> back inside it.</summary>
    private static void ClampInside(IReadOnlyList<LabelCandidate> candidates, Point[] positions, Size[] sizes, Rect bounds)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            var r = RectOf(candidates, positions, sizes, i);
            double dx = 0, dy = 0;
            if (r.X < bounds.X) dx = bounds.X - r.X;
            else if (r.Right > bounds.Right) dx = bounds.Right - r.Right;
            if (r.Y < bounds.Y) dy = bounds.Y - r.Y;
            else if (r.Bottom > bounds.Bottom) dy = bounds.Bottom - r.Bottom;
            if (dx != 0 || dy != 0)
                positions[i] = new Point(positions[i].X + dx, positions[i].Y + dy);
        }
    }

    /// <summary>The bounding rectangle of label <paramref name="i"/> where it now stands: the box of the text at its position, by its
    /// alignment along and across the line (<see cref="TextBox"/>), the letters rising the font size above the baseline as the measured
    /// box says (<see cref="TextExtent.Of"/>).</summary>
    private static Rect RectOf(IReadOnlyList<LabelCandidate> candidates, Point[] positions, Size[] sizes, int i)
    {
        var candidate = candidates[i];
        var placement = new TextPlacement(positions[i], candidate.Alignment) { Vertical = candidate.VerticalAlignment };
        return new TextBox(placement, TextExtent.Of(sizes[i], candidate.Font.Size)).Bounds();
    }

    private static double PriorityWeight(LabelPriority priority) => priority switch
    {
        LabelPriority.High => 3.0,
        LabelPriority.Low  => 0.33,
        _                  => 1.0,
    };
}

// <summary>Input to <see cref="LabelLayoutEngine.Place"/>.</summary>
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
    LabelPriority Priority = LabelPriority.Normal)
{
    /// <summary>How the label stands on its anchor across its line: on its baseline (the default), centred on it, hung from it or
    /// standing on it. It decides the box the label is kept apart by and where it is drawn.</summary>
    public TextVerticalAlignment VerticalAlignment { get; init; }
}

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
    Point? LeaderLineStart)
{
    /// <summary>How the label stands on <see cref="FinalPoint"/> across its line (passed through from the candidate).</summary>
    public TextVerticalAlignment VerticalAlignment { get; init; }
}

/// <summary>Relative resistance to displacement during collision resolution.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum LabelPriority
{
    /// <summary>The label gives way first: it is moved before labels of higher priority.</summary>
    Low    = 0,
    /// <summary>The ordinary priority, which every label has unless it asks for another.</summary>
    Normal = 1,
    /// <summary>The label resists displacement the longest, so the others move around it.</summary>
    High   = 2,
}
