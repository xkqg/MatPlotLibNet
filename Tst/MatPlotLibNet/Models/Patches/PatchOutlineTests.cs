// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Patches;

/// <summary>The outline of each straight patch, in data units, against what matplotlib 3.11.2 builds for the same patch: one
/// path segment per vertex of matplotlib's path (a move, the lines, and a close where matplotlib closes the polygon). The
/// expected vertices are matplotlib's own, printed by <c>get_xy()</c> and <c>get_verts()</c>.</summary>
public class PatchOutlineTests
{
    private const double Tolerance = 1e-12;

    /// <summary>The points of an outline in order, the close left out.</summary>
    private static DataPoint[] Vertices(Patch patch) =>
        [.. patch.Outline().Where(segment => segment is not CloseSegment).Select(segment => segment switch
        {
            MoveToSegment move => new DataPoint(move.Point.X, move.Point.Y),
            LineToSegment line => new DataPoint(line.Point.X, line.Point.Y),
            _ => throw new InvalidOperationException($"A straight patch drew a {segment.GetType().Name}."),
        })];

    /// <summary>The kinds of the segments of an outline, in order: what matplotlib's path codes say.</summary>
    private static string Kinds(Patch patch) =>
        string.Join(" ", patch.Outline().Select(segment => segment switch
        {
            MoveToSegment => "M",
            LineToSegment => "L",
            CloseSegment => "Z",
            _ => "?",
        }));

    private static void AssertVertices(DataPoint[] expected, DataPoint[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i].DataX - actual[i].DataX) <= Tolerance && Math.Abs(expected[i].DataY - actual[i].DataY) <= Tolerance,
                $"vertex {i}: expected ({expected[i].DataX}, {expected[i].DataY}), drawn ({actual[i].DataX}, {actual[i].DataY}).");
        }
    }

    /// <summary>Twice the signed area the vertices enclose: positive when they run counter-clockwise.</summary>
    private static double SignedArea(DataPoint[] vertices) =>
        vertices.Select((p, i) => p.DataX * vertices[(i + 1) % vertices.Length].DataY - vertices[(i + 1) % vertices.Length].DataX * p.DataY).Sum();

    // ---- Rectangle ----------------------------------------------------------------------------------------------------

    [Fact]
    public void ARectangle_RunsCounterClockwiseFromItsAnchor_AndCloses()
    {
        var rectangle = new Rectangle(new(1, 2), 3, 4);

        var vertices = Vertices(rectangle);

        Assert.Equal("M L L L Z", Kinds(rectangle));
        // Not turned: the corners are the anchor plus the sides, to the bit.
        Assert.Equal(new DataPoint[] { new(1, 2), new(4, 2), new(4, 6), new(1, 6) }, vertices);
        Assert.True(SignedArea(vertices) > 0, "the corners run clockwise");
    }

    [Fact]
    public void ARectangle_TurnsAboutItsAnchorCorner_CounterClockwise()
    {
        // matplotlib: Rectangle((1, 1), 2, 1, angle=90).get_verts()
        var rectangle = new Rectangle(new(1, 1), 2, 1) { Angle = 90 };

        AssertVertices([new(1, 1), new(1, 3), new(0, 3), new(0, 1)], Vertices(rectangle));
    }

    [Fact]
    public void ARectangleTurnedThroughAnyAngle_KeepsItsAnchorAndItsSides()
    {
        var rectangle = new Rectangle(new(2, -1), 3, 2) { Angle = 30 };

        var v = Vertices(rectangle);

        Assert.Equal(new DataPoint(2, -1), v[0]);
        Assert.Equal(3, Math.Sqrt(Math.Pow(v[1].DataX - v[0].DataX, 2) + Math.Pow(v[1].DataY - v[0].DataY, 2)), Tolerance);
        Assert.Equal(2, Math.Sqrt(Math.Pow(v[3].DataX - v[0].DataX, 2) + Math.Pow(v[3].DataY - v[0].DataY, 2)), Tolerance);
        Assert.Equal(30, Math.Atan2(v[1].DataY - v[0].DataY, v[1].DataX - v[0].DataX) * 180 / Math.PI, 1e-9);
    }

    [Fact]
    public void ANegativeWidthAndHeight_ExtendLeftAndDownFromTheAnchor()
    {
        // matplotlib: Rectangle((1, 1), -2, -1).get_verts()
        var rectangle = new Rectangle(new(1, 1), -2, -1);

        Assert.Equal(new DataPoint[] { new(1, 1), new(-1, 1), new(-1, 0), new(1, 0) }, Vertices(rectangle));
    }

    private static readonly Dictionary<string, Func<Rectangle>> RectanglesWithANumberThatIsNotFinite = new()
    {
        ["anchor x NaN"] = () => new Rectangle(new(double.NaN, 0), 1, 1),
        ["anchor y infinite"] = () => new Rectangle(new(0, double.PositiveInfinity), 1, 1),
        ["width infinite"] = () => new Rectangle(new(0, 0), double.PositiveInfinity, 1),
        ["height NaN"] = () => new Rectangle(new(0, 0), 1, double.NaN),
        ["angle NaN"] = () => new Rectangle(new(0, 0), 1, 1) { Angle = double.NaN },
        ["angle infinite"] = () => new Rectangle(new(0, 0), 1, 1) { Angle = double.NegativeInfinity },
    };

    public static TheoryData<string> RectangleCases { get; } = new(RectanglesWithANumberThatIsNotFinite.Keys);

    [Theory]
    [MemberData(nameof(RectangleCases))]
    public void ARectangleWithANumberThatIsNotFinite_HasNoOutline(string number)
    {
        Assert.Empty(RectanglesWithANumberThatIsNotFinite[number]().Outline());
    }

    // ---- Polygon ------------------------------------------------------------------------------------------------------

    [Fact]
    public void AClosedPolygon_IsItsVerticesAndAClose()
    {
        // matplotlib: Polygon([(0, 0), (1, 0), (1, 1)]).get_path().codes == [1, 2, 2, 79]
        var polygon = new Polygon([new(0, 0), new(1, 0), new(1, 1)]);

        Assert.True(polygon.Closed);
        Assert.Equal("M L L Z", Kinds(polygon));
        Assert.Equal(new DataPoint[] { new(0, 0), new(1, 0), new(1, 1) }, Vertices(polygon));
    }

    [Fact]
    public void AnOpenPolygon_DrawsItsVerticesWithoutClosing()
    {
        var polygon = new Polygon([new(0, 0), new(1, 0), new(1, 1), new(0, 2)]) { Closed = false };

        Assert.Equal("M L L L", Kinds(polygon));
        Assert.Equal(new DataPoint[] { new(0, 0), new(1, 0), new(1, 1), new(0, 2) }, Vertices(polygon));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void APolygonWithFewerThanThreeVertices_HasNoOutline(int count)
    {
        var polygon = new Polygon([.. Enumerable.Range(0, count).Select(i => new DataPoint(i, i * i))]);

        Assert.Empty(polygon.Outline());
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(0, double.NegativeInfinity)]
    public void APolygonWithAVertexThatIsNotFinite_HasNoOutline(double x, double y)
    {
        var polygon = new Polygon([new(0, 0), new(x, y), new(1, 1)]);

        Assert.Empty(polygon.Outline());
    }

    [Fact]
    public void APolygon_KeepsItsOwnCopyOfItsVertices()
    {
        DataPoint[] corners = [new(0, 0), new(1, 0), new(1, 1)];
        var polygon = new Polygon(corners);
        var set = new Polygon([new(5, 5), new(6, 5), new(6, 6)]);
        set.Vertices = corners;

        corners[1] = new(9, 9);

        Assert.Equal(new DataPoint(1, 0), polygon.Vertices[1]);
        Assert.Equal(new DataPoint(1, 0), set.Vertices[1]);
    }

    // ---- Arrow (matplotlib's FancyArrow, in data units) ---------------------------------------------------------------

    [Fact]
    public void TheDefaultArrow_HasMatplotlibsEightVertices_WithItsTipAHeadLengthPastTheEnd()
    {
        // matplotlib: FancyArrow(0, 0, 1, 0).get_xy(): 8 vertices, the tip twice (the path's close), xmax 1.0045.
        var arrow = new Arrow(new(0, 0), 1, 0);

        Assert.Equal(8, arrow.Outline().Count);
        Assert.Equal("M L L L L L L Z", Kinds(arrow));
        AssertVertices(
            [new(1.0045, 0), new(1, -0.0015), new(1, -0.0005), new(0, -0.0005), new(0, 0.0005), new(1, 0.0005), new(1, 0.0015)],
            Vertices(arrow));
        Assert.Equal(1.0045, Vertices(arrow).Max(p => p.DataX), Tolerance);
    }

    [Fact]
    public void AnArrowWhoseLengthIncludesItsHead_EndsWithItsTipOnTheEndPoint()
    {
        // matplotlib: FancyArrow(0, 0, 1, 0, length_includes_head=True).get_xy()
        var arrow = new Arrow(new(0, 0), 1, 0) { LengthIncludesHead = true };

        AssertVertices(
            [new(1, 0), new(0.9955, -0.0015), new(0.9955, -0.0005), new(0, -0.0005), new(0, 0.0005), new(0.9955, 0.0005), new(0.9955, 0.0015)],
            Vertices(arrow));
    }

    [Fact]
    public void ALeftArrow_IsTheHalfBelowItsShaft_Closed()
    {
        // matplotlib: FancyArrow(0, 0, 1, 0, shape='left').get_xy() (its last vertex is the close)
        var arrow = new Arrow(new(0, 0), 1, 0) { Shape = ArrowHeadShape.Left };

        Assert.Equal("M L L L L Z", Kinds(arrow));
        AssertVertices([new(1.0045, 0), new(1, -0.0015), new(1, -0.0005), new(0, -0.0005), new(0, 0)], Vertices(arrow));
    }

    [Fact]
    public void ARightArrow_IsTheMirrorOfTheLeftOne()
    {
        // matplotlib: FancyArrow(0, 0, 1, 0, shape='right').get_xy()
        var arrow = new Arrow(new(0, 0), 1, 0) { Shape = ArrowHeadShape.Right };

        Assert.Equal("M L L L L Z", Kinds(arrow));
        AssertVertices([new(1.0045, 0), new(1, 0.0015), new(1, 0.0005), new(0, 0.0005), new(0, 0)], Vertices(arrow));
    }

    [Fact]
    public void ALeftArrowPointingBack_IsTurnedWithIt()
    {
        // matplotlib: FancyArrow(1, 1, -2, 0, shape='left').get_xy()
        var arrow = new Arrow(new(1, 1), -2, 0) { Shape = ArrowHeadShape.Left };

        AssertVertices([new(-1.0045, 1), new(-1, 1.0015), new(-1, 1.0005), new(1, 1.0005), new(1, 1)], Vertices(arrow));
    }

    [Fact]
    public void AnArrow_IsTurnedToItsDirection_WithItsOverhang()
    {
        // matplotlib: FancyArrow(0, 0, 3, 4, width=0.1, overhang=0.5).get_xy()
        var arrow = new Arrow(new(0, 0), 3, 4) { Width = 0.1, Overhang = 0.5 };

        AssertVertices(
            [new(3.27, 4.36), new(3.12, 3.91), new(3.175, 4.15), new(0.04, -0.03), new(-0.04, 0.03), new(3.095, 4.21), new(2.88, 4.09)],
            Vertices(arrow));
    }

    [Fact]
    public void AHeadWidthOfItsOwn_SetsTheHeadLengthTooUntilThatIsSet()
    {
        // matplotlib: FancyArrow(0, 0, 2, 0, width=0.1, head_width=0.4) (head length 1.5 x 0.4 = 0.6)
        var arrow = new Arrow(new(0, 0), 2, 0) { Width = 0.1, HeadWidth = 0.4 };

        AssertVertices([new(2.6, 0), new(2, -0.2), new(2, -0.05), new(0, -0.05), new(0, 0.05), new(2, 0.05), new(2, 0.2)], Vertices(arrow));
    }

    [Fact]
    public void AHeadLengthOfItsOwn_LeavesTheHeadWidthThreeTimesTheShaft()
    {
        // matplotlib: FancyArrow(0, 0, 2, 0, width=0.1, head_length=0.3) (head width 3 x 0.1)
        var arrow = new Arrow(new(0, 0), 2, 0) { Width = 0.1, HeadLength = 0.3 };

        AssertVertices([new(2.3, 0), new(2, -0.15), new(2, -0.05), new(0, -0.05), new(0, 0.05), new(2, 0.05), new(2, 0.15)], Vertices(arrow));
    }

    [Fact]
    public void AnArrowPointingDown_WithEveryHeadSizeSet_IsMatplotlibs()
    {
        // matplotlib: FancyArrow(0, 0, 0, -1, width=0.2, head_width=0.6, head_length=0.5, overhang=0.5, length_includes_head=True)
        var arrow = new Arrow(new(0, 0), 0, -1)
        {
            Width = 0.2, HeadWidth = 0.6, HeadLength = 0.5, Overhang = 0.5, LengthIncludesHead = true,
        };

        AssertVertices(
            [new(0, -1), new(-0.3, -0.5), new(-0.1, -0.75), new(-0.1, 0), new(0.1, 0), new(0.1, -0.75), new(0.3, -0.5)],
            Vertices(arrow));
    }

    [Fact]
    public void AnArrowOfNoLength_PointsUp_AsMatplotlibsDoes()
    {
        // matplotlib: FancyArrow(1, 2, 0, 0).get_xy(): a zero direction is taken as up.
        var arrow = new Arrow(new(1, 2), 0, 0);

        AssertVertices(
            [new(1, 2.0045), new(1.0015, 2), new(1.0005, 2), new(1.0005, 2), new(0.9995, 2), new(0.9995, 2), new(0.9985, 2)],
            Vertices(arrow));
    }

    private static readonly Dictionary<string, Func<Arrow>> ArrowsWithANumberThatIsNotFinite = new()
    {
        ["start x NaN"] = () => new Arrow(new(double.NaN, 0), 1, 0),
        ["dx NaN"] = () => new Arrow(new(0, 0), double.NaN, 0),
        ["dy infinite"] = () => new Arrow(new(0, 0), 1, double.PositiveInfinity),
        ["width infinite"] = () => new Arrow(new(0, 0), 1, 0) { Width = double.PositiveInfinity },
        ["head width NaN"] = () => new Arrow(new(0, 0), 1, 0) { HeadWidth = double.NaN },
        ["head length NaN"] = () => new Arrow(new(0, 0), 1, 0) { HeadLength = double.NaN },
        ["overhang NaN"] = () => new Arrow(new(0, 0), 1, 0) { Overhang = double.NaN },
    };

    public static TheoryData<string> ArrowCases { get; } = new(ArrowsWithANumberThatIsNotFinite.Keys);

    [Theory]
    [MemberData(nameof(ArrowCases))]
    public void AnArrowWithANumberThatIsNotFinite_HasNoOutline(string number)
    {
        Assert.Empty(ArrowsWithANumberThatIsNotFinite[number]().Outline());
    }
}
