// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>Where the annotations of one axes stand once the ones that asked to move have found a place: the place they were put
/// at when nothing is there, otherwise the first of a fixed list that covers the fewest marks (<see cref="MarkObstacles"/>), the
/// same selector a Best legend uses. The list is the anchor, then four steps (one to four text heights) out in eight directions,
/// in the order up, down, right, left, upper right, upper left, lower right, lower left; every place but the anchor lies inside
/// the plot area. Nothing random takes part, so a chart is laid out the same way every time on every platform.</summary>
/// <remarks>The marks a text must keep clear of are the ones it is given (the series' marks and the legend's box) and the
/// annotations placed so far, each text's box once it is placed: a text that does not move is placed first by its caller, so
/// every text that does move keeps clear of it, and a text that moved is an obstacle for the ones after it, in list order.</remarks>
internal sealed class AnnotationLayout
{
    /// <summary>How many steps out from the anchor the layout tries, in text heights.</summary>
    private const int Steps = 4;

    /// <summary>A direction the layout steps in, as a multiple of the text's height: +X is right, +Y is down.</summary>
    private readonly record struct Direction(int X, int Y);

    private static readonly Direction[] Directions =
        [new(0, -1), new(0, 1), new(1, 0), new(-1, 0), new(1, -1), new(-1, -1), new(1, 1), new(-1, 1)];

    private readonly Rect _plotArea;
    private MarkObstacles _obstacles;

    /// <summary>Starts a layout over the marks of one axes.</summary>
    /// <param name="plotArea">The plot area: a text that moves stays inside it.</param>
    /// <param name="obstacles">The marks every text keeps clear of: the series' and the legend's.</param>
    internal AnnotationLayout(Rect plotArea, MarkObstacles obstacles)
    {
        _plotArea = plotArea;
        _obstacles = obstacles;
    }

    /// <summary>Places one text, and records the box it stands in among the marks the texts after it keep clear of.</summary>
    /// <param name="text">The text where it was put: its anchor, its measured size, its alignment and its turn.</param>
    /// <param name="moves">Whether the text asked to move out of the way; a text that did not stands where it was put.</param>
    /// <returns>The point the text is drawn at: where it was put, or the place the layout found for it.</returns>
    internal Point Place(TextBox text, bool moves)
    {
        var at = moves ? Find(text) : text.Anchor;
        _obstacles = _obstacles.Including((text with { Anchor = at }).Bounds());
        return at;
    }

    /// <summary>The first place, in the fixed order, that covers the fewest marks.</summary>
    private Point Find(TextBox text)
    {
        var places = new List<Point> { text.Anchor };
        var boxes = new List<Rect> { text.Bounds() };
        double step = text.Size.Height;
        for (int ring = 1; ring <= Steps; ring++)
        {
            foreach (var direction in Directions)
            {
                var place = new Point(text.Anchor.X + direction.X * ring * step, text.Anchor.Y + direction.Y * ring * step);
                var box = (text with { Anchor = place }).Bounds();
                if (!_plotArea.Contains(box)) continue;
                places.Add(place);
                boxes.Add(box);
            }
        }
        return places[_obstacles.FirstLeastCovered(boxes)];
    }
}
