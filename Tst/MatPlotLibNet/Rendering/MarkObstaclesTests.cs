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

    /// <summary>Whether a path touches the box, every segment looked at: the run is the whole path.</summary>
    private static bool TouchesPath(Rect box, Point[] path) => box.Touches(path, new VertexRun(0, path.Length));

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
        Assert.True(TouchesPath(Candidate, Path((0, 120), (300, 120))));
    }

    [Fact]
    public void Touches_IsTrueWhenTheFirstVertexIsInside_EvenOnTheEdge()
    {
        Assert.True(TouchesPath(Candidate, Path((100, 120), (50, 300))));
        Assert.True(TouchesPath(Candidate, Path((125, 120), (50, 300))));
    }

    [Theory]
    [InlineData(0, 90, 300, 90)]      // passes above
    [InlineData(0, 0, 90, 300)]       // passes on the left, slanting
    [InlineData(0, 120, 100, 120)]    // ends exactly on the left edge without entering
    [InlineData(0, 0, 90, 90)]        // ends short of the corner
    public void Touches_IsFalseForAPathThatMissesTheBox(double x0, double y0, double x1, double y1)
    {
        Assert.False(TouchesPath(Candidate, Path((x0, y0), (x1, y1))));
    }

    [Fact]
    public void Touches_IsFalseForAnEmptyPath_AndAPathWithASinglePointOutside()
    {
        Assert.False(TouchesPath(Candidate, []));
        Assert.False(TouchesPath(Candidate, Path((10, 10))));
    }

    [Fact]
    public void Touches_SkipsASegmentWhoseEndTheAxesCouldNotPlace()
    {
        Assert.False(TouchesPath(Candidate, Path((0, 0), (double.NaN, 120), (0, 10))));
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
        Assert.Empty(default(MarkObstacles).Boxes);
        Assert.Empty(default(MarkObstacles).Paths);
        Assert.Empty(default(MarkObstacles).Points);
        Assert.Equal([new Rect(1, 2, 3, 4)], default(MarkObstacles).Including(new Rect(1, 2, 3, 4)).Boxes);
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

    // ---- the pick stays the pick of the full scores, ties and early stops included --------------------------------

    /// <summary>The rule restated the slow way, apart from the library: every candidate scored in full by the four terms, and
    /// the first of the lowest scores taken. The pick must be this one, however the library cuts the work short.</summary>
    private static int ReferencePick(IReadOnlyList<Rect> candidates, List<Point[]> paths, List<Point> points, List<Rect> boxes)
    {
        var scores = candidates.Select(c => ReferenceScore(c, paths, points, boxes)).ToList();
        return scores.IndexOf(scores.Min());
    }

    /// <summary>The score of one candidate restated the slow way: the four terms counted in full.</summary>
    private static int ReferenceScore(Rect c, List<Point[]> paths, List<Point> points, List<Rect> boxes)
    {
        static bool Inside(Rect c, Point p) => c.X < p.X && p.X < c.X + c.Width && c.Y < p.Y && p.Y < c.Y + c.Height;
        static bool Touches(Rect c, Point[] path)
        {
            if (path.Length == 0) return false;
            var f = path[0];
            if (c.X <= f.X && f.X <= c.X + c.Width && c.Y <= f.Y && f.Y <= c.Y + c.Height) return true;
            double cx = c.X + c.Width / 2, cy = c.Y + c.Height / 2;
            for (int i = 0; i + 1 < path.Length; i++)
            {
                double x1 = path[i].X, y1 = path[i].Y, x2 = path[i + 1].X, y2 = path[i + 1].Y;
                if (Math.Abs(x1 + x2 - 2 * cx) < Math.Abs(x1 - x2) + c.Width
                    && Math.Abs(y1 + y2 - 2 * cy) < Math.Abs(y1 - y2) + c.Height
                    && 2 * Math.Abs((x1 - cx) * (y1 - y2) - (y1 - cy) * (x1 - x2)) < c.Width * Math.Abs(y1 - y2) + c.Height * Math.Abs(x1 - x2))
                    return true;
            }
            return false;
        }

        return points.Count(p => Inside(c, p))
            + paths.Sum(path => path.Count(p => Inside(c, p)) + (Touches(c, path) ? 1 : 0))
            + boxes.Count(b => c.X < b.X + b.Width && b.X < c.X + c.Width && c.Y < b.Y + b.Height && b.Y < c.Y + c.Height);
    }

    /// <summary>Seeded marks of every kind (paths with points the axes could not place among them, point centres, boxes) and ten
    /// candidates, some of them the same box twice so that two scores tie, some marks sparse enough that a candidate covers
    /// nothing: the library's pick is the reference pick for every one of them.</summary>
    [Fact]
    public void FirstLeastCovered_IsThePickOfTheFullScores_ForSeededMarksWithTiesAndFreePlaces()
    {
        for (int seed = 1; seed <= 400; seed++)
        {
            var random = new Random(seed);
            double Coordinate() => random.Next(20) switch
            {
                0 => double.NaN,
                1 => double.PositiveInfinity,
                _ => random.NextDouble() * 200,
            };

            var paths = new List<Point[]>();
            var points = new List<Point>();
            var boxes = new List<Rect>();
            int density = random.Next(4);   // 0: hardly anything, so free places are common
            for (int p = random.Next(1 + 2 * density); p > 0; p--)
                paths.Add([.. Enumerable.Range(0, random.Next(1, 4 + 30 * density)).Select(_ => new Point(Coordinate(), Coordinate()))]);
            for (int p = random.Next(1 + 10 * density); p > 0; p--)
                points.Add(new Point(Coordinate(), Coordinate()));
            for (int b = random.Next(1 + density); b > 0; b--)
                boxes.Add(new Rect(random.NextDouble() * 180, random.NextDouble() * 180, 5 + random.NextDouble() * 40, 5 + random.NextDouble() * 40));

            var candidates = new Rect[10];
            for (int i = 0; i < candidates.Length; i++)
                candidates[i] = i > 0 && random.Next(3) == 0
                    ? candidates[random.Next(i)]   // the same box as an earlier one: the earlier must win the tie
                    : new Rect(random.NextDouble() * 150, random.NextDouble() * 150, 20 + random.NextDouble() * 50, 20 + random.NextDouble() * 50);

            var builder = new MarkObstacles.Builder();
            foreach (var path in paths) builder.AddPath(path);
            foreach (var point in points) builder.AddPoint(point);
            foreach (var box in boxes) builder.AddBox(box);

            Assert.True(ReferencePick(candidates, paths, points, boxes) == builder.Build().FirstLeastCovered(candidates), $"seed {seed}");
        }
    }

    /// <summary>The line of a series runs left to right (its X never falls), and a step line repeats an X; candidates stand on
    /// vertices' X and Y exactly, and paths start inside, outside and on the edge of a candidate: the library's pick and every score
    /// are the reference's, however the library narrows the vertices it looks at along X.</summary>
    [Fact]
    public void FirstLeastCovered_IsThePickOfTheFullScores_ForLinesThatRunLeftToRight()
    {
        for (int seed = 1; seed <= 400; seed++)
        {
            var random = new Random(seed);
            var paths = new List<Point[]>();
            for (int p = random.Next(1, 4); p > 0; p--)
            {
                int count = random.Next(1, 300);
                var path = new Point[count];
                double x = random.NextDouble() * 60 - 20;
                for (int i = 0; i < count; i++)
                {
                    x += random.Next(5) switch { 0 => 0, 1 => 5, _ => random.NextDouble() * 3 };   // a repeated X, a whole step, a fraction
                    double y = random.Next(8) == 0 ? double.NaN : Math.Round(random.NextDouble() * 200, random.Next(3));
                    path[i] = new Point(Math.Round(x, random.Next(3)), y);
                }
                paths.Add(path);
            }

            var candidates = new Rect[10];
            for (int i = 0; i < candidates.Length; i++)
            {
                var on = paths[random.Next(paths.Count)];
                var at = on[random.Next(on.Length)];
                candidates[i] = random.Next(3) == 0 && double.IsFinite(at.Y)
                    ? new Rect(at.X, at.Y, 5 + random.Next(40), 5 + random.Next(40))   // a corner on a vertex
                    : new Rect(random.NextDouble() * 400 - 20, random.NextDouble() * 180, 2 + random.NextDouble() * 60, 2 + random.NextDouble() * 60);
            }

            var builder = new MarkObstacles.Builder();
            foreach (var path in paths) builder.AddPath(path);
            var marks = builder.Build();

            Assert.True(ReferencePick(candidates, paths, [], []) == marks.FirstLeastCovered(candidates), $"seed {seed}");
            foreach (var candidate in candidates)
                Assert.True(ReferenceScore(candidate, paths, [], []) == marks.Badness(candidate), $"seed {seed}, candidate {candidate}");
        }
    }

    // ---- what the pick costs ------------------------------------------------------------------------------------

    [Fact]
    public void Badness_WithABound_IsTheScore_WhileTheScoreStaysBelowIt()
    {
        var marks = Marks(m => m
            .AddPath(Path((110, 110), (120, 120), (130, 130), (300, 300)))   // 3 vertices + 1 path
            .AddPoint(new Point(140, 130))                                     // 1 point
            .AddBox(new Rect(120, 120, 10, 10)));                              // 1 box

        Assert.Equal(6, marks.Badness(Candidate, 7));
        Assert.Equal(6, marks.Badness(Candidate));
    }

    /// <summary>A candidate the pick has already beaten is not scored to the end: once its score reaches the bound (the best
    /// score so far) the count stops there, as nothing it could add would let it win.</summary>
    [Fact]
    public void Badness_WithABound_StopsCountingAtTheBound()
    {
        var line = Enumerable.Range(0, 1000).Select(i => new Point(101 + i * 0.04, 120)).ToArray();   // every vertex inside
        var marks = Marks(m => m.AddPath(line).AddPoint(new Point(120, 120)).AddBox(new Rect(120, 120, 10, 10)));

        Assert.Equal(1000 + 1 + 1 + 1, marks.Badness(Candidate));
        Assert.Equal(1, marks.Badness(Candidate, 1));
        Assert.Equal(2, marks.Badness(Candidate, 2));
        Assert.Equal(500, marks.Badness(Candidate, 500));
    }

    [Fact]
    public void CountInside_WithABound_StopsCountingAtTheBound()
    {
        var points = Path((120, 120), (121, 121), (122, 122), (300, 300));

        Assert.Equal(3, Candidate.CountInside(points));
        Assert.Equal(2, Candidate.CountInside(points, 2));
        Assert.Equal(3, Candidate.CountInside(points, 9));
    }

    /// <summary>Picking a place scores every candidate against every mark, so it must not allocate per candidate or per mark:
    /// the marks are walked as the arrays they are held in.</summary>
    [Fact]
    public void PickingAPlace_AllocatesNothing()
    {
        var marks = Marks(m => m
            .AddPath(Path((0, 0), (110, 120), (200, 130)))
            .AddPath(new List<Point> { new(0, 200), new(120, 110), new(300, 0) })
            .AddPoint(new Point(120, 120)).AddPoint(new Point(10, 10))
            .AddBox(new Rect(0, 0, 400, 400)));
        Rect[] candidates = [Candidate, new(90, 90, 40, 40), new(105, 105, 30, 30)];
        marks.FirstLeastCovered(candidates);

        long before = GC.GetAllocatedBytesForCurrentThread();
        marks.FirstLeastCovered(candidates);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    // ---- a builder that starts from marks already collected -----------------------------------------------------

    [Fact]
    public void ASeededBuilder_StartsWithTheMarksItIsGiven_AndAddsAfterThem()
    {
        var seed = Marks(b => b.AddBox(new Rect(1, 2, 3, 4)).AddPath(Path((0, 0), (5, 5))).AddPoint(new Point(7, 8)));

        var marks = new MarkObstacles.Builder(seed).AddBox(new Rect(10, 20, 30, 40)).AddPoint(new Point(9, 9)).Build();

        Assert.Equal([new Rect(1, 2, 3, 4), new Rect(10, 20, 30, 40)], marks.Boxes);
        Assert.Equal([new Point(0, 0), new Point(5, 5)], Assert.Single(marks.Paths));
        Assert.Equal([new Point(7, 8), new Point(9, 9)], marks.Points);
    }

    [Fact]
    public void ASeededBuilder_LeavesTheMarksItWasGivenAsTheyWere()
    {
        var seed = Marks(b => b.AddBox(new Rect(1, 2, 3, 4)));

        new MarkObstacles.Builder(seed).AddBox(new Rect(10, 20, 30, 40)).AddPath(Path((0, 0))).AddPoint(new Point(1, 1));

        Assert.Equal([new Rect(1, 2, 3, 4)], seed.Boxes);
        Assert.Empty(seed.Paths);
        Assert.Empty(seed.Points);
    }

    [Fact]
    public void ABuilderSeededWithNoMarks_IsEmpty()
    {
        var marks = new MarkObstacles.Builder(MarkObstacles.None).Build();

        Assert.Empty(marks.Boxes);
        Assert.Empty(marks.Paths);
        Assert.Empty(marks.Points);
    }
}
