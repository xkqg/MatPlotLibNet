// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A chord diagram places each group as an arc of a circle, as long as its flows, and joins the arcs by ribbons. The
/// layout is pure geometry in radians (0 at twelve o'clock, clockwise), the rule of d3-chord with two additions that make the
/// picture readable: the ribbons inside an arc run from the farthest group clockwise to the nearest, so the ribbons of a
/// complete graph cross as few times as any layout can; and a matrix that cannot be drawn is refused by name instead of
/// drawing a blank circle. The vectors are worked out from d3's rule and from the matrix of its documentation.</summary>
public class ChordLayoutTests
{
    private const double Tau = 2 * Math.PI;

    /// <summary>C1: the matrix of the d3-chord documentation. Its group sums are 29630, 20230, 40290 and 9850, 100000 in all.</summary>
    private static readonly double[,] D3Matrix =
    {
        { 11975, 5871, 8916, 2868 },
        { 1951, 10048, 2060, 6171 },
        { 8010, 16145, 8090, 8045 },
        { 1013, 990, 940, 6907 },
    };

    private static double[,] Complete(int n)
    {
        var m = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                m[i, j] = i == j ? 0 : 1;
        return m;
    }

    // ── C1: the arcs ────────────────────────────────────────────────────────────

    [Fact]
    public void C1_WithNoPadding_TheArcsAreTheGroupSums_OfTheWholeCircle()
    {
        var layout = ChordLayout.Compute(D3Matrix, padding: 0);

        double[][] expected = [[0, 1.861708], [1.861708, 3.132796], [3.132796, 5.664292], [5.664292, 6.283185]];
        Assert.Equal(4, layout.Arcs.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(i, layout.Arcs[i].Group);
            Assert.Equal(expected[i][0], layout.Arcs[i].Start, 5e-7);
            Assert.Equal(expected[i][1], layout.Arcs[i].End, 5e-7);
        }
    }

    [Fact]
    public void C1_WithPadding_ThePaddingIsTakenFromTheCircleBeforeItIsShared()
    {
        var layout = ChordLayout.Compute(D3Matrix, padding: 0.05);

        double[][] expected = [[0, 1.802448], [1.852448, 3.083076], [3.133076, 5.583992], [5.633992, 6.233185]];
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(expected[i][0], layout.Arcs[i].Start, 5e-7);
            Assert.Equal(expected[i][1], layout.Arcs[i].End, 5e-7);
        }

        // k = (tau - pad * n) / sum: the arcs and the gaps between them make up the circle.
        double covered = layout.Arcs.Sum(a => a.End - a.Start) + 4 * 0.05;
        Assert.Equal(Tau, covered, 12);
    }

    [Fact]
    public void ThePaddingDefaultsToAScoreOfTheCircle()
    {
        Assert.Equal(0.05, ChordLayout.DefaultPadding);
        Assert.Equal(ChordLayout.Compute(D3Matrix, 0.05).Arcs, ChordLayout.Compute(D3Matrix).Arcs);
    }

    [Fact]
    public void ASelfLoop_IsCountedOnce_InItsGroupSum()
    {
        // Row 0 is 11975 on itself and 5871 + 8916 + 2868 elsewhere: 29630 in all, not 29630 + 11975.
        var layout = ChordLayout.Compute(D3Matrix, padding: 0);

        Assert.Equal(29630.0 / 100000 * Tau, layout.Arcs[0].End - layout.Arcs[0].Start, 9);
    }

    // ── the ribbons ─────────────────────────────────────────────────────────────

    [Fact]
    public void TheRibbonsOfAnArc_TileItExactly_FromTheFarthestGroupClockwiseToTheNearest()
    {
        var layout = ChordLayout.Compute(D3Matrix, padding: 0.05);

        // Arc 1 holds, in this order, the ends of its ribbons to groups 0, 3, 2 (the farthest clockwise first: 3 steps, 2, 1) and
        // its own loop last.
        var ends = SubgroupsOf(layout, group: 1);
        Assert.Equal([0, 3, 2, 1], ends.Select(e => e.Other));
        Assert.Equal(layout.Arcs[1].Start, ends[0].Span.Start, 12);
        for (int i = 1; i < ends.Count; i++)
            Assert.Equal(ends[i - 1].Span.End, ends[i].Span.Start, 12);
        Assert.Equal(layout.Arcs[1].End, ends[^1].Span.End, 12);
        // Each end is as wide as the flow it carries.
        double k = (Tau - 0.05 * 4) / 100000;
        Assert.Equal(1951 * k, ends[0].Span.End - ends[0].Span.Start, 12);
        Assert.Equal(6171 * k, ends[1].Span.End - ends[1].Span.Start, 12);
    }

    private readonly record struct Subgroup(int Other, ChordSpan Span);

    /// <summary>The ends of the ribbons that stand on an arc, in the order they stand on it, from the layout's ribbons.</summary>
    private static List<Subgroup> SubgroupsOf(ChordLayoutResult layout, int group)
    {
        var ends = new List<Subgroup>();
        foreach (var ribbon in layout.Ribbons)
        {
            if (ribbon.Source == group) ends.Add(new Subgroup(ribbon.Target, ribbon.SourceSpan));
            if (ribbon.Target == group && ribbon.Source != group) ends.Add(new Subgroup(ribbon.Source, ribbon.TargetSpan));
        }

        return [.. ends.OrderBy(e => e.Span.Start)];
    }

    [Fact]
    public void ARibbonJoinsTheTwoEndsOfAPair_AndIsCalledAfterTheGroupThatSendsMore()
    {
        var layout = ChordLayout.Compute(D3Matrix, padding: 0.05);

        // Between groups 0 and 1: 5871 from 0 to 1 and 1951 from 1 to 0, so the ribbon comes from group 0.
        var ribbon = layout.Ribbons.Single(r => (r.Source, r.Target) == (0, 1));

        Assert.Equal(5871.0, ribbon.Value);
        double k = (Tau - 0.05 * 4) / 100000;
        Assert.Equal(5871 * k, ribbon.SourceSpan.End - ribbon.SourceSpan.Start, 12);
        Assert.Equal(1951 * k, ribbon.TargetSpan.End - ribbon.TargetSpan.Start, 12);
    }

    [Fact]
    public void ASelfLoop_IsOneRibbonThatStandsOnTheSameSpanAtBothEnds()
    {
        var layout = ChordLayout.Compute(D3Matrix, padding: 0.05);

        var loop = layout.Ribbons.Single(r => r.Source == 2 && r.Target == 2);

        Assert.Equal(loop.SourceSpan, loop.TargetSpan);
        Assert.Equal(8090.0, loop.Value);
    }

    [Fact]
    public void C5_AZeroFlowDrawsNoRibbon_AndTheLargestRibbonIsFirst()
    {
        var flows = new double[,] { { 0, 5, 0 }, { 0, 0, 0 }, { 1, 0, 9 } };

        var layout = ChordLayout.Compute(flows, padding: 0);

        // Pairs with a flow either way: (0,1), (0,2), (2,2). (1,2) has none and draws nothing.
        Assert.Equal(3, layout.Ribbons.Count);
        Assert.DoesNotContain(layout.Ribbons, r => (r.Source, r.Target) == (1, 2) || (r.Source, r.Target) == (2, 1));
        Assert.Equal([9.0, 5.0, 1.0], layout.Ribbons.Select(r => r.Value));   // the loop (9), then 0 to 1 (5), then 2 to 0 (1)
    }

    [Fact]
    public void ARibbonWithFlowOnlyOneWay_HasAnEndOfNoWidth()
    {
        var layout = ChordLayout.Compute(new double[,] { { 0, 5 }, { 0, 0 } }, padding: 0);

        var ribbon = Assert.Single(layout.Ribbons);

        Assert.Equal(0.0, ribbon.TargetSpan.End - ribbon.TargetSpan.Start);
    }

    // ── C2: the crossings ───────────────────────────────────────────────────────

    /// <summary>The number of pairs of ribbons whose ends interleave round the circle, with each ribbon taken as the chord
    /// between the middles of its two ends.</summary>
    private static int Crossings(ChordLayoutResult layout)
    {
        var chords = layout.Ribbons
            .Select(r => (A: (r.SourceSpan.Start + r.SourceSpan.End) / 2, B: (r.TargetSpan.Start + r.TargetSpan.End) / 2))
            .ToArray();
        static bool Inside((double A, double B) chord, double x) => x > Math.Min(chord.A, chord.B) && x < Math.Max(chord.A, chord.B);
        int crossings = 0;
        for (int i = 0; i < chords.Length; i++)
            for (int j = i + 1; j < chords.Length; j++)
                if (Inside(chords[i], chords[j].A) != Inside(chords[i], chords[j].B))
                    crossings++;
        return crossings;
    }

    [Theory]
    [InlineData(3, 0)]   // C(3, 4) = 0
    [InlineData(4, 1)]   // C(4, 4) = 1
    [InlineData(5, 5)]   // C(5, 4) = 5
    [InlineData(6, 15)]  // C(6, 4) = 15
    public void C2_TheRibbonsOfACompleteGraph_CrossAsFewTimesAsAnyLayoutCan(int n, int minimum)
    {
        Assert.Equal(minimum, Crossings(ChordLayout.Compute(Complete(n))));
    }

    // ── C3: the labels ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0, -90.0)]
    [InlineData(Math.PI / 2, 0, 0.0)]
    [InlineData(Math.PI, 0, 90.0)]
    [InlineData(3 * Math.PI / 2, 1, 0.0)]
    [InlineData(1.9 * Math.PI, 1, 72.0)]
    public void C3_ALabelStandsAtTheMiddleOfItsArc_ReadFromTheRimOutwards(double midAngle, int alignment, double rotation)
    {
        var arc = new ChordArc(0, midAngle - 0.1, midAngle + 0.1);

        var label = arc.Label();

        Assert.Equal(midAngle, label.Angle, 12);
        Assert.Equal(alignment == 0 ? TextAlignment.Left : TextAlignment.Right, label.Alignment);
        Assert.Equal(rotation, label.Rotation, 9);
    }

    [Fact]
    public void C3_NoLabelIsEverUpsideDown()
    {
        for (double angle = 0; angle < Tau; angle += 0.01)
        {
            var label = new ChordArc(0, angle, angle).Label();

            Assert.InRange(label.Rotation, -90.0 - 1e-9, 90.0 + 1e-9);
        }
    }

    // ── C4: what is refused ─────────────────────────────────────────────────────

    [Fact]
    public void C4_AMatrixThatIsNotSquare_IsRefusedByName()
    {
        var refusal = Assert.Throws<ArgumentException>(() => ChordLayout.Compute(new double[2, 3]));

        Assert.Contains("flows", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("square", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1.0, "negative")]
    [InlineData(double.NaN, "not a number")]
    [InlineData(double.PositiveInfinity, "infinite")]
    public void C4_AFlowThatCannotBeDrawn_IsRefusedByItsPlace(double value, string what)
    {
        var flows = new double[,] { { 1, 2 }, { value, 1 } };

        var refusal = Assert.Throws<ArgumentException>(() => ChordLayout.Compute(flows));

        Assert.Contains("flows[1,0]", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(what, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void C4_AMatrixOfNothing_IsRefusedInsteadOfDrawnAsABlankCircle()
    {
        var refusal = Assert.Throws<ArgumentException>(() => ChordLayout.Compute(new double[3, 3]));

        Assert.Contains("all zero", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMatrixOfNoGroups_LaysOutNothing_AsASeriesWithNoDataIsAnEmptyPlot()
    {
        var layout = ChordLayout.Compute(new double[0, 0]);

        Assert.Empty(layout.Arcs);
        Assert.Empty(layout.Ribbons);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(Tau)]   // the gaps alone would fill the circle
    public void APaddingThatLeavesNoRoomForTheArcs_IsRefusedByName(double padding)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => ChordLayout.Compute(Complete(3), padding));

        Assert.Equal("padding", refusal.ParamName);
    }
}
