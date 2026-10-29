// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;

namespace MatPlotLibNet.Tests.Rendering.CartesianParts;

/// <summary>
/// An annotation that asked to move out of the way stands at its own place when nothing is there, and otherwise at the first of a
/// fixed list of places, in a fixed order, that the fewest marks cover: the same selector a Best legend uses. Every place but
/// the anchor lies inside the plot area. No random number takes part, so the result is the same on every platform and every run.
/// </summary>
public class AnnotationLayoutTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);
    private static readonly Size Text = new(60, 12);
    private static readonly Point Anchor = new(200, 150);

    private static TextBox Box(Point at, double size = 10) =>
        new(at, Text, size, TextAlignment.Left, TextVerticalAlignment.Baseline, 0);

    /// <summary>The box of the text standing at <paramref name="at"/>, for a mark placed exactly over it.</summary>
    private static Rect Over(Point at) => Box(at).Bounds();

    private static MarkObstacles Marks(params Rect[] boxes)
    {
        var builder = new MarkObstacles.Builder();
        foreach (var box in boxes) builder.AddBox(box);
        return builder.Build();
    }

    /// <summary>A direction the layout tries, as a multiple of the text's height.</summary>
    private readonly record struct Direction(int X, int Y);

    /// <summary>The directions the layout tries, in order, at each step out from the anchor.</summary>
    private static readonly Direction[] Directions =
        [new(0, -1), new(0, 1), new(1, 0), new(-1, 0), new(1, -1), new(-1, -1), new(1, 1), new(-1, 1)];

    private static Point Candidate(int step, int direction) =>
        new(Anchor.X + Directions[direction].X * step * Text.Height, Anchor.Y + Directions[direction].Y * step * Text.Height);

    /// <summary>The places in the order the layout tries them: the anchor, then four steps of the eight directions.</summary>
    private static IEnumerable<Point> Places(Point anchor)
    {
        yield return anchor;
        for (int step = 1; step <= 4; step++)
            foreach (var d in Directions)
                yield return new Point(anchor.X + d.X * step * Text.Height, anchor.Y + d.Y * step * Text.Height);
    }

    [Fact]
    public void ATextWithNothingUnderIt_StaysAtItsAnchor()
    {
        var layout = new AnnotationLayout(Plot, MarkObstacles.None);

        Assert.Equal(Anchor, layout.Place(Box(Anchor), moves: true));
    }

    [Fact]
    public void ATextThatDoesNotMove_StaysWhateverIsUnderIt()
    {
        var layout = new AnnotationLayout(Plot, Marks(Over(Anchor)));

        Assert.Equal(Anchor, layout.Place(Box(Anchor), moves: false));
    }

    [Fact]
    public void ATextOverAMark_MovesOneTextHeightUp_WhenThatPlaceIsFree()
    {
        var layout = new AnnotationLayout(Plot, Marks(Over(Anchor)));

        var placed = layout.Place(Box(Anchor), moves: true);

        Assert.Equal(Candidate(1, 0), placed);
    }

    [Fact]
    public void ATextOverAMarkThatReachesAbove_MovesBelow()
    {
        // The mark covers the anchor's box and the place above it; the place below only touches its lower edge.
        var layout = new AnnotationLayout(Plot, Marks(new Rect(Anchor.X - 5, Anchor.Y - 22, 70, 24)));

        Assert.Equal(Candidate(1, 1), layout.Place(Box(Anchor), moves: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ATextStandsAtTheFirstLeastCoveredPlace_OfTheFixedList(int pattern)
    {
        Rect[][] patterns =
        [
            [new Rect(190, 100, 120, 70)],                                                      // a block over the anchor and its upper right
            [new Rect(150, 120, 100, 20), new Rect(190, 160, 120, 30)],                          // a band above, a band below
            [new Rect(100, 100, 100, 100), new Rect(205, 130, 40, 30)],                         // the left, and a patch to the right
            [new Rect(0, 0, 400, 135), new Rect(0, 165, 400, 135), new Rect(0, 135, 190, 30)],   // everything but a gap to the right
        ];
        var marks = Marks(patterns[pattern]);
        var layout = new AnnotationLayout(Plot, marks);

        var placed = layout.Place(Box(Anchor), moves: true);

        // Reference: the same list, scored by the one rule a legend uses.
        var places = Places(Anchor).Where((p, i) => i == 0 || Plot.Contains(Box(p).Bounds())).ToList();
        var expected = places[marks.FirstLeastCovered([.. places.Select(p => Box(p).Bounds())])];
        Assert.Equal(expected, placed);
    }

    [Fact]
    public void APlaceOutsideThePlotArea_IsNeverChosen_ButTheAnchorAlwaysIs()
    {
        // The text stands at the very top: a place above it would leave the plot area.
        var top = new Point(200, 14);
        var layout = new AnnotationLayout(Plot, Marks(Over(top)));

        var placed = layout.Place(Box(top), moves: true);

        Assert.Equal(Candidate(1, 1).Y - Anchor.Y + top.Y, placed.Y, 9);   // the place below, a text height down
        Assert.True(Plot.Y <= Box(placed).Bounds().Y, "the moved text left the plot area through the top");
    }

    [Fact]
    public void WhenEveryPlaceIsCovered_TheTextStaysAtItsAnchor_ThatTiesWithTheRest()
    {
        var everything = new Rect(-1000, -1000, 3000, 3000);
        var layout = new AnnotationLayout(Plot, Marks(everything));

        Assert.Equal(Anchor, layout.Place(Box(Anchor), moves: true));
    }

    [Fact]
    public void ATextThatWasPlaced_StandsAmongTheMarksForTheTextsAfterIt()
    {
        var layout = new AnnotationLayout(Plot, MarkObstacles.None);

        var first = layout.Place(Box(Anchor), moves: true);
        var second = layout.Place(Box(Anchor), moves: true);

        Assert.Equal(Anchor, first);
        Assert.NotEqual(Anchor, second);
        Assert.False(Box(first).Bounds().Overlaps(Box(second).Bounds()), "the second text was set on top of the first");
    }

    [Fact]
    public void FiftyTexts_AreAllPlaced_WithNoNotANumber_AndTheSameWayTwice()
    {
        List<Point> Run()
        {
            var layout = new AnnotationLayout(Plot, MarkObstacles.None);
            return [.. Enumerable.Range(0, 50).Select(i => layout.Place(Box(new Point(150 + i % 5, 140 + i % 3)), moves: true))];
        }

        var one = Run();
        var two = Run();

        Assert.Equal(50, one.Count);
        Assert.All(one, p => Assert.True(p.IsDrawable()));
        Assert.Equal(one, two);
    }

    [Fact]
    public void ATurnedText_IsPlacedByItsTurnedBox()
    {
        // A text turned a quarter stands upright in a narrow column; a mark beside it that the unturned box would cover does not matter.
        var turned = new TextBox(Anchor, Text, 10, TextAlignment.Left, TextVerticalAlignment.Baseline, 90);
        var beside = Marks(new Rect(Anchor.X + 30, Anchor.Y - 5, 20, 10));   // right of the column, inside the unturned box
        var layout = new AnnotationLayout(Plot, beside);

        Assert.Equal(Anchor, layout.Place(turned, moves: true));
    }
}
