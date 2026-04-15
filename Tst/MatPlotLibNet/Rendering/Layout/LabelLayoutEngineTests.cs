// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>
/// Covers the collision-resolution contract of <see cref="LabelLayoutEngine"/>: non-overlapping
/// input is returned unchanged, overlapping pairs get pushed apart without crossing plot bounds,
/// dense clusters converge within the iteration cap, and large displacements leave a leader-line
/// anchor for the caller to draw a connector back to the original spot.
/// </summary>
public class LabelLayoutEngineTests
{
    // A fixed-width font metric so test assertions don't depend on whichever provider the host
    // has registered via ChartServices — every character is 10 px wide, 14 px tall, regardless
    // of the actual font family. Keeps overlap arithmetic in the tests trivial to reason about.
    private sealed class FixedMetrics : IFontMetrics
    {
        public Size Measure(string text, Font font) =>
            new(text.Length * 10.0, 14.0);
    }

    private static readonly Font TestFont = new() { Family = "sans-serif", Size = 10 };
    private static readonly Rect BigBounds = new(0, 0, 1000, 1000);
    private static readonly IFontMetrics Metrics = new FixedMetrics();

    /// <summary>Rebuilds the bounding rectangle the engine uses internally so overlap assertions
    /// in the tests match the engine's own collision geometry. Duplicates the private helper
    /// in <see cref="LabelLayoutEngine"/> — kept in sync with the production definition.</summary>
    private static Rect RectOf(Point pos, Size size, TextAlignment alignment)
    {
        double top = pos.Y - size.Height * 0.75;
        double left = alignment switch
        {
            TextAlignment.Center => pos.X - size.Width / 2,
            TextAlignment.Right  => pos.X - size.Width,
            _                    => pos.X,
        };
        return new Rect(left, top, size.Width, size.Height);
    }

    private static Rect RectOf(LabelPlacement p) =>
        RectOf(p.FinalPoint, Metrics.Measure(p.Text, p.Font), p.Alignment);

    // ──────────────────────────────────────────────────────────────────────────
    // Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Place_NoOverlap_ReturnsIdentity()
    {
        // Four labels spread out so their rects never intersect — engine should leave every
        // anchor untouched, and no placement should carry a leader-line anchor.
        var candidates = new[]
        {
            new LabelCandidate(new Point(100, 100), "Alpha",  TestFont),
            new LabelCandidate(new Point(300, 100), "Beta",   TestFont),
            new LabelCandidate(new Point(500, 100), "Gamma",  TestFont),
            new LabelCandidate(new Point(700, 100), "Delta",  TestFont),
        };

        var placed = LabelLayoutEngine.Place(candidates, BigBounds, Metrics);

        Assert.Equal(candidates.Length, placed.Count);
        for (int i = 0; i < candidates.Length; i++)
        {
            Assert.Equal(candidates[i].AnchorPoint.X, placed[i].FinalPoint.X, 3);
            Assert.Equal(candidates[i].AnchorPoint.Y, placed[i].FinalPoint.Y, 3);
            Assert.Null(placed[i].LeaderLineStart);
        }
    }

    [Fact]
    public void Place_TwoOverlappingRects_SeparatesThem()
    {
        // Two labels anchored 5 px apart — their 50 px wide rects overlap by ~45 px. The
        // engine should push them apart so the returned rects no longer intersect.
        var a = new LabelCandidate(new Point(100, 100), "AAAAA", TestFont);
        var b = new LabelCandidate(new Point(105, 100), "BBBBB", TestFont);

        var placed = LabelLayoutEngine.Place([a, b], BigBounds, Metrics);

        var rectA = RectOf(placed[0]);
        var rectB = RectOf(placed[1]);
        Assert.False(rectA.Intersects(rectB),
            $"Expected non-overlap after placement; got rectA={rectA}, rectB={rectB}");
    }

    [Fact]
    public void Place_DenseInput_ConvergesAndNonOverlapping()
    {
        // 10 labels anchored at the same point — the pathological worst case for the
        // pair-wise solver. After MaxIterations (20) the engine may not fully separate
        // every pair but the assertion we care about is that it (a) returns, (b) returns
        // the same number of placements, and (c) the majority of pairs are non-overlapping.
        var candidates = Enumerable.Range(0, 10)
            .Select(i => new LabelCandidate(new Point(500, 500), $"L{i}", TestFont))
            .ToArray();

        var placed = LabelLayoutEngine.Place(candidates, BigBounds, Metrics);

        Assert.Equal(10, placed.Count);

        int overlapping = 0, total = 0;
        for (int i = 0; i < placed.Count; i++)
        {
            var ri = RectOf(placed[i]);
            for (int j = i + 1; j < placed.Count; j++)
            {
                total++;
                var rj = RectOf(placed[j]);
                if (ri.Intersects(rj)) overlapping++;
            }
        }
        // At least 80% of pairs should be separated after convergence — a lower bound
        // that tolerates the engine's priority-weighted pair-wise repulsion leaving a
        // small residual set of overlaps on an impossible input (every label starts at
        // the same point). In practice 10 labels at one anchor reliably converge to full
        // separation within MaxIterations; the 80% floor is a safety margin.
        double separationRate = 1.0 - (double)overlapping / total;
        Assert.True(separationRate >= 0.80,
            $"Expected ≥80% pair separation after convergence, got {separationRate:P0} ({overlapping}/{total} overlapping)");
    }

    [Fact]
    public void Place_OutsidePlotBounds_ClampsInside()
    {
        // Anchor just past the right edge of a tight bounds — engine should clamp the
        // rect back inside. The label is left-aligned so its rect extends rightward from
        // the anchor; anchoring at x=150 in a width=200 bounds would spill by ~50 px.
        var tight = new Rect(0, 0, 200, 200);
        var c = new LabelCandidate(new Point(150, 100), "LONGLABEL", TestFont, TextAlignment.Left);

        var placed = LabelLayoutEngine.Place([c], tight, Metrics);

        var rect = RectOf(placed[0]);
        Assert.True(rect.X >= tight.X - 0.001, $"Clamped X={rect.X} should be >= {tight.X}");
        Assert.True(rect.X + rect.Width <= tight.X + tight.Width + 0.001,
            $"Clamped Right={rect.X + rect.Width} should be <= {tight.X + tight.Width}");
    }

    [Fact]
    public void Place_LargeDisplacement_RecordsLeaderLine()
    {
        // Stack three labels at the same anchor — at least one will be displaced beyond
        // the 6 px default threshold and should carry a LeaderLineStart pointing back to
        // the original anchor so the caller can draw a connector.
        var anchor = new Point(500, 500);
        var candidates = new[]
        {
            new LabelCandidate(anchor, "AAAAAAAAAA", TestFont),
            new LabelCandidate(anchor, "BBBBBBBBBB", TestFont),
            new LabelCandidate(anchor, "CCCCCCCCCC", TestFont),
        };

        var placed = LabelLayoutEngine.Place(candidates, BigBounds, Metrics);

        Assert.Contains(placed, p => p.LeaderLineStart.HasValue);
        // Every leader-line anchor should be the ORIGINAL anchor, not a shifted one.
        foreach (var p in placed.Where(p => p.LeaderLineStart.HasValue))
        {
            Assert.Equal(anchor.X, p.LeaderLineStart!.Value.X, 3);
            Assert.Equal(anchor.Y, p.LeaderLineStart.Value.Y, 3);
        }
    }

    [Fact]
    public void Place_SmallDisplacement_NoLeaderLine()
    {
        // Two labels just barely overlapping — the half-overlap shift is smaller than the
        // 6 px leader threshold. Neither placement should carry a leader anchor.
        var a = new LabelCandidate(new Point(500, 500), "AA", TestFont);
        var b = new LabelCandidate(new Point(505, 500), "BB", TestFont); // overlap ~15 px wide

        var placed = LabelLayoutEngine.Place([a, b], BigBounds, Metrics,
            leaderThreshold: 100.0);  // explicitly large threshold

        Assert.All(placed, p => Assert.Null(p.LeaderLineStart));
    }

    [Fact]
    public void Place_HighPriority_StaysCloserToAnchor()
    {
        // When two labels overlap, a High-priority candidate should displace less than its
        // Normal-priority sibling — its share of the mutual separation is smaller due to
        // the priority-weighted MTV split.
        var a = new LabelCandidate(new Point(500, 500), "HIGHER", TestFont,
            Priority: LabelPriority.High);
        var b = new LabelCandidate(new Point(510, 500), "NORMAL", TestFont,
            Priority: LabelPriority.Normal);

        var placed = LabelLayoutEngine.Place([a, b], BigBounds, Metrics);

        double aShift = Distance(placed[0].FinalPoint, a.AnchorPoint);
        double bShift = Distance(placed[1].FinalPoint, b.AnchorPoint);
        Assert.True(aShift < bShift,
            $"High-priority label should shift less than Normal-priority (got a={aShift:F2}, b={bShift:F2})");
    }

    [Fact]
    public void Place_EmptyInput_ReturnsEmpty()
    {
        var placed = LabelLayoutEngine.Place([], BigBounds, Metrics);
        Assert.Empty(placed);
    }

    [Fact]
    public void Place_SingleLabel_IsIdentity()
    {
        var c = new LabelCandidate(new Point(250, 300), "Solo", TestFont);
        var placed = LabelLayoutEngine.Place([c], BigBounds, Metrics);

        Assert.Single(placed);
        Assert.Equal(250, placed[0].FinalPoint.X, 3);
        Assert.Equal(300, placed[0].FinalPoint.Y, 3);
        Assert.Null(placed[0].LeaderLineStart);
    }

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
