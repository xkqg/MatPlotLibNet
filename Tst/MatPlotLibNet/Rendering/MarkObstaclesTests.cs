// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// The marks a chart has drawn, as the rectangles, paths and points a legend or a moved annotation must keep clear of,
/// and the one rule that scores a candidate box against them: matplotlib's, four integer terms (line vertices strictly
/// inside, point centres strictly inside, rectangles overlapped, paths touched), the first lowest score winning.
/// </summary>
public class MarkObstaclesTests
{
    // The box a candidate stands in: x 100..150, y 100..140.
    private static readonly Rect Candidate = new(100, 100, 50, 40);

    private static Point[] Path(params (double X, double Y)[] points) =>
        [.. points.Select(p => new Point(p.X, p.Y))];

    private static MarkObstacles Marks(Action<MarkObstacles.Builder> add)
    {
        var builder = new MarkObstacles.Builder();
        add(builder);
        return builder.Build();
    }

    // ---- the geometry ------------------------------------------------------------------------------------------

    [Fact]
    public void CountInside_CountsThePointsStrictlyInsideTheBox_NotThoseOnItsEdge()
    {
        var points = Path((120, 120), (100, 120), (150, 140), (160, 120), (120, 90));

        Assert.Equal(1, Candidate.CountInside(points));
    }

    [Fact]
    public void CountInside_IgnoresAPointTheAxesCouldNotPlace()
    {
        var points = Path((120, 120), (double.NaN, 120), (120, double.PositiveInfinity));

        Assert.Equal(1, Candidate.CountInside(points));
    }

    [Theory]
    [InlineData(120, 120, 130, 130, true)]    // inside
    [InlineData(140, 130, 200, 200, true)]    // overlaps its corner
    [InlineData(150, 100, 200, 140, false)]   // touches along the right edge only
    [InlineData(0, 0, 100, 100, false)]       // touches at the corner only
    [InlineData(200, 200, 210, 210, false)]   // far away
    public void Overlaps_IsTrueWhenTheBoxesShareArea_NotWhenTheyOnlyTouch(
        double x0, double y0, double x1, double y1, bool expected)
    {
        Assert.Equal(expected, Candidate.Overlaps(new Rect(x0, y0, x1 - x0, y1 - y0)));
    }

    [Fact]
    public void Touches_IsTrueWhenASegmentCrossesTheBox_WithNoVertexInsideIt()
    {
        Assert.True(Candidate.Touches(Path((0, 120), (300, 120))));
    }

    [Fact]
    public void Touches_IsTrueWhenTheFirstVertexIsInside_EvenOnTheEdge()
    {
        Assert.True(Candidate.Touches(Path((100, 120), (50, 300))));
        Assert.True(Candidate.Touches(Path((125, 120), (50, 300))));
    }

    [Theory]
    [InlineData(0, 90, 300, 90)]      // passes above
    [InlineData(0, 0, 90, 300)]       // passes on the left, slanting
    [InlineData(0, 120, 100, 120)]    // ends exactly on the left edge without entering
    [InlineData(0, 0, 90, 90)]        // ends short of the corner
    public void Touches_IsFalseForAPathThatMissesTheBox(double x0, double y0, double x1, double y1)
    {
        Assert.False(Candidate.Touches(Path((x0, y0), (x1, y1))));
    }

    [Fact]
    public void Touches_IsFalseForAnEmptyPath_AndAPathWithASinglePointOutside()
    {
        Assert.False(Candidate.Touches([]));
        Assert.False(Candidate.Touches(Path((10, 10))));
    }

    [Fact]
    public void Touches_SkipsASegmentWhoseEndTheAxesCouldNotPlace()
    {
        Assert.False(Candidate.Touches(Path((0, 0), (double.NaN, 120), (0, 10))));
    }

    // ---- the score ---------------------------------------------------------------------------------------------

    [Fact]
    public void Badness_OfNothing_IsZero()
    {
        Assert.Equal(0, MarkObstacles.None.Badness(Candidate));
    }

    [Fact]
    public void Badness_CountsEachVertexInsideThePathAndThePathItselfOnce()
    {
        var marks = Marks(m => m.AddPath(Path((110, 110), (120, 120), (130, 130), (300, 300))));

        Assert.Equal(3 + 1, marks.Badness(Candidate));
    }

    [Fact]
    public void Badness_OfAPathThatCrossesWithNoVertexInside_IsOne()
    {
        var marks = Marks(m => m.AddPath(Path((0, 120), (300, 120))));

        Assert.Equal(1, marks.Badness(Candidate));
    }

    [Fact]
    public void Badness_CountsEachPointCentreStrictlyInside()
    {
        var marks = Marks(m => m.AddPoint(new Point(120, 120)).AddPoint(new Point(100, 120)).AddPoint(new Point(130, 130)));

        Assert.Equal(2, marks.Badness(Candidate));
    }

    [Fact]
    public void Badness_CountsEachBoxThatOverlapsOnce_AndNotOneThatOnlyTouches()
    {
        var marks = Marks(m => m
            .AddBox(new Rect(120, 120, 100, 100))       // overlaps
            .AddBox(new Rect(0, 0, 500, 500))           // covers it: still one
            .AddBox(new Rect(150, 100, 50, 40)));       // touches only

        Assert.Equal(2, marks.Badness(Candidate));
    }

    [Fact]
    public void Badness_AddsTheFourTerms()
    {
        var marks = Marks(m => m
            .AddPath(Path((110, 110), (300, 300)))      // 1 vertex + 1 path
            .AddPoint(new Point(140, 130))              // 1 point
            .AddBox(new Rect(120, 120, 10, 10)));       // 1 box

        Assert.Equal(1 + 1 + 1 + 1, marks.Badness(Candidate));
    }

    [Fact]
    public void Including_AddsABox_AndLeavesTheMarksItWasMadeFrom()
    {
        var marks = Marks(m => m.AddPoint(new Point(120, 120)));

        var more = marks.Including(new Rect(110, 110, 20, 20));

        Assert.Equal(1, marks.Badness(Candidate));
        Assert.Equal(2, more.Badness(Candidate));
    }

    [Fact]
    public void ADefaultValue_IsNoMarks()
    {
        Assert.Equal(0, default(MarkObstacles).Badness(Candidate));
        Assert.Equal(0, default(MarkObstacles).FirstLeastCovered([Candidate]));
    }

    // ---- the pick ----------------------------------------------------------------------------------------------

    private static readonly Rect[] Candidates =
        [new(0, 0, 10, 10), new(20, 0, 10, 10), new(40, 0, 10, 10), new(60, 0, 10, 10)];

    private static Point Centre(int candidate) => Candidates[candidate].Center;

    [Fact]
    public void FirstLeastCovered_WithNoMarks_IsTheFirstCandidate()
    {
        Assert.Equal(0, MarkObstacles.None.FirstLeastCovered(Candidates));
    }

    [Fact]
    public void FirstLeastCovered_IsTheFirstCandidateNothingCovers()
    {
        var marks = Marks(m => m.AddPoint(Centre(0)).AddPoint(Centre(1)));

        Assert.Equal(2, marks.FirstLeastCovered(Candidates));
    }

    [Fact]
    public void FirstLeastCovered_StopsAtTheFirstZero_EvenWhenALaterOneIsAlsoZero()
    {
        var marks = Marks(m => m.AddPoint(Centre(0)).AddPoint(Centre(1)));

        Assert.NotEqual(3, marks.FirstLeastCovered(Candidates));
    }

    [Fact]
    public void FirstLeastCovered_WhenEveryCandidateIsCovered_IsTheLeastCovered_AndTheFirstOfTheLeast()
    {
        var marks = Marks(m => m
            .AddPoint(Centre(0)).AddPoint(Centre(0)).AddPoint(Centre(0))
            .AddPoint(Centre(1)).AddPoint(Centre(1))
            .AddPoint(Centre(2))
            .AddPoint(Centre(3)));

        Assert.Equal(2, marks.FirstLeastCovered(Candidates));
    }

    [Fact]
    public void FirstLeastCovered_WhenEveryCandidateIsCoveredAlike_IsTheFirst()
    {
        var marks = Marks(m => m.AddPoint(Centre(0)).AddPoint(Centre(1)).AddPoint(Centre(2)).AddPoint(Centre(3)));

        Assert.Equal(0, marks.FirstLeastCovered(Candidates));
    }

    [Fact]
    public void FirstLeastCovered_OfNoCandidates_IsTheFirstSlotAnyway()
    {
        Assert.Equal(0, MarkObstacles.None.FirstLeastCovered([]));
    }
}
