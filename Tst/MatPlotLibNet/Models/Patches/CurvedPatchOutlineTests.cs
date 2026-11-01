// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Models.Patches;

/// <summary>The outline of each curved patch, in data units, against what matplotlib 3.11.2 builds for the same patch: cubic Bézier
/// curves, every control point in data units, never an arc in pixels. A circle and an ellipse are matplotlib's unit circle
/// (<c>Path.unit_circle</c>: eight cubic curves, 26 vertices) placed on the shape; a wedge and an arc are matplotlib's arc of the
/// unit circle (<c>Path.arc</c>: 2 to the power of the number of quarter turns it sweeps, rounded up, cubic curves). The expected
/// vertices are matplotlib's own, printed by <c>get_patch_transform().transform(get_path().vertices)</c>; the curves use sine and
/// cosine, so they are compared to a tolerance, never byte for byte.</summary>
public class CurvedPatchOutlineTests
{
    private const double Tolerance = 1e-12;

    /// <summary>The vertices of an outline in matplotlib's order: a move's or a line's point, a curve's two control points and its
    /// end; a close is left out.</summary>
    private static DataPoint[] Vertices(IEnumerable<PathSegment> outline) =>
        [.. outline.SelectMany(segment => segment switch
        {
            MoveToSegment move => new[] { move.Point },
            LineToSegment line => [line.Point],
            BezierSegment curve => [curve.Control1, curve.Control2, curve.End],
            CloseSegment => [],
            _ => throw new InvalidOperationException($"A curved patch drew a {segment.GetType().Name}."),
        }).Select(point => new DataPoint(point.X, point.Y))];

    private static DataPoint[] Vertices(Patch patch) => Vertices(patch.Outline());

    /// <summary>The kinds of the segments of an outline, in order: M a move, L a line, C a cubic curve, Z a close.</summary>
    private static string Kinds(IEnumerable<PathSegment> outline) =>
        string.Join(" ", outline.Select(segment => segment switch
        {
            MoveToSegment => "M",
            LineToSegment => "L",
            BezierSegment => "C",
            CloseSegment => "Z",
            _ => "?",
        }));

    private static string Kinds(Patch patch) => Kinds(patch.Outline());

    /// <summary>The kinds of an outline of <paramref name="curves"/> cubic curves after a move.</summary>
    private static string Curves(int curves) => string.Join(" ", Enumerable.Repeat("C", curves).Prepend("M"));

    /// <summary>The number of vertices matplotlib's path has for an outline: one per move, line and close, three per curve.</summary>
    private static int MatplotlibVertexCount(IEnumerable<PathSegment> outline) =>
        outline.Sum(segment => segment is BezierSegment ? 3 : 1);

    private static void AssertVertices(DataPoint[] expected, DataPoint[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(Math.Abs(expected[i].DataX - actual[i].DataX) <= Tolerance && Math.Abs(expected[i].DataY - actual[i].DataY) <= Tolerance,
                $"vertex {i}: expected ({expected[i].DataX}, {expected[i].DataY}), drawn ({actual[i].DataX}, {actual[i].DataY}).");
        }
    }

    // ---- Circle and Ellipse: matplotlib's unit circle ------------------------------------------------------------------

    [Fact]
    public void ACircle_IsMatplotlibsTwentySixVertices_EightCubicCurvesAndAClose()
    {
        // matplotlib: Circle((2, 3), 1.5): 26 vertices, codes [1, 4 x 24, 79], from the bottom counter-clockwise.
        var circle = new Circle(new(2, 3), 1.5);

        Assert.Equal(Curves(8) + " Z", Kinds(circle));
        Assert.Equal(26, MatplotlibVertexCount(circle.Outline()));
        AssertVertices(
            [
                new(2.0, 1.5),
                new(2.39780465, 1.5), new(2.7793698061772805, 1.6580494626176374), new(3.0606601717798214, 1.9393398282201786),
                new(3.3419505373823624, 2.2206301938227195), new(3.5, 2.60219535), new(3.5, 3.0),
                new(3.5, 3.39780465), new(3.3419505373823624, 3.7793698061772805), new(3.0606601717798214, 4.060660171779821),
                new(2.7793698061772805, 4.341950537382362), new(2.39780465, 4.5), new(2.0, 4.5),
                new(1.60219535, 4.5), new(1.2206301938227198, 4.341950537382362), new(0.9393398282201786, 4.060660171779821),
                new(0.6580494626176374, 3.7793698061772805), new(0.5, 3.39780465), new(0.5, 3.0),
                new(0.5, 2.60219535), new(0.6580494626176374, 2.2206301938227195), new(0.9393398282201786, 1.9393398282201786),
                new(1.2206301938227198, 1.6580494626176374), new(1.60219535, 1.5), new(2.0, 1.5),
            ],
            Vertices(circle));
    }

    [Fact]
    public void AnEllipse_IsTheUnitCircle_ScaledToItsDiameters_TurnedCounterClockwise_AndMoved()
    {
        // matplotlib: Ellipse((1, 2), 4, 2, angle=30)
        var ellipse = new Ellipse(new(1, 2), 4, 2) { Angle = 30 };

        Assert.Equal(Curves(8) + " Z", Kinds(ellipse));
        AssertVertices(
            [
                new(1.5, 1.1339745962155612),
                new(1.9593452435247698, 1.3991776962155613), new(2.347255580583559, 1.7448043667879833), new(2.578298261984863, 2.0947343454907528),
                new(2.8093409433861667, 2.444664324193522), new(2.864652357568877, 2.770327378237615), new(2.7320508075688776, 3.0),
                new(2.5994492575688772, 3.2296726217623846), new(2.2897610726013133, 3.344603058982961), new(1.8711914807983154, 3.319479216882342),
                new(1.4526218889953175, 3.2943553747817234), new(0.9593452435247698, 3.1312285037844387), new(0.5, 2.866025403784439),
                new(0.0406547564752302, 2.6008223037844385), new(-0.3472555805835591, 2.2551956332120167), new(-0.5782982619848629, 1.9052656545092472),
                new(-0.8093409433861667, 1.5553356758064776), new(-0.8646523575688774, 1.229672621762385), new(-0.7320508075688774, 1.0),
                new(-0.5994492575688775, 0.7703273782376152), new(-0.28976107260131334, 0.6553969410170393), new(0.12880851920168457, 0.6805207831176578),
                new(0.5473781110046824, 0.7056446252182766), new(1.0406547564752302, 0.8687714962155613), new(1.5, 1.1339745962155612),
            ],
            Vertices(ellipse));
    }

    /// <summary>The width and the height are whole diameters, as matplotlib's are: an ellipse 4 wide and 2 high reaches 2 either side
    /// of its centre and 1 above and below it.</summary>
    [Fact]
    public void AnEllipsesWidthAndHeight_AreItsFullDiameters()
    {
        var v = Vertices(new Ellipse(new(0, 0), 4, 2));

        AssertVertices([new(0, -1), new(2, 0), new(0, 1), new(-2, 0)], [v[0], v[6], v[12], v[18]]);
    }

    [Fact]
    public void ACircle_IsAnEllipseWhoseWidthAndHeightAreTwiceItsRadius()
    {
        AssertVertices(Vertices(new Ellipse(new(-1, 4), 3, 3)), Vertices(new Circle(new(-1, 4), 1.5)));
    }

    // ---- Path.arc: the arcs of a wedge and of an arc ---------------------------------------------------------------------

    /// <summary>matplotlib's <c>Path.arc</c>: the sweep is unwrapped to less than a whole turn, a whole number of turns (to 1e-12) is
    /// a whole circle, an arc of no sweep is one curve of no length, and the number of curves is 2 to the power of the quarter
    /// turns swept, rounded up. Measured: <c>len(Path.arc(theta1, theta2).vertices)</c> = 3 × curves + 1.</summary>
    [Theory]
    [InlineData(0, 90, 2)]
    [InlineData(90, 0, 8)]
    [InlineData(350, 10, 2)]
    [InlineData(0, 360, 16)]
    [InlineData(0, 720, 16)]
    [InlineData(30, 30, 1)]
    [InlineData(-45, 45, 2)]
    [InlineData(10, 100.5, 4)]
    [InlineData(0, 90.0000001, 4)]
    [InlineData(300, 60, 4)]
    public void AnArcOfTheUnitCircle_HasAsManyCurvesAsMatplotlibsPathArc(double theta1, double theta2, int curves)
    {
        var arc = new Arc(new(0, 0), 2, 2) { Theta1 = theta1, Theta2 = theta2 };

        Assert.Equal(Curves(curves), Kinds(arc));
        Assert.Equal(3 * curves + 1, MatplotlibVertexCount(arc.Outline()));
    }

    [Fact]
    public void AnArcOfTheUnitCircle_IsMatplotlibsPathArc_VertexForVertex()
    {
        // matplotlib: Path.arc(10, 100.5).vertices
        AssertVertices(
            [
                new(0.984807753012208, 0.17364817766693033),
                new(0.9618769859444901, 0.30369502004796733), new(0.9134120799050273, 0.42792098987449106), new(0.8422172337162865, 0.5391383229110002),
                new(0.7710223875275458, 0.6503556559475092), new(0.6784977258600454, 0.7463771414602265), new(0.569996762596303, 0.8216469379421637),
                new(0.4614957993325607, 0.8969167344241009), new(0.33915232481798707, 0.9499545788416133), new(0.21004518135738254, 0.977691680331046),
                new(0.08093803789677798, 1.0054287818204788), new(-0.05239374104920397, 1.007319659884816), new(-0.18223552549214753, 0.9832549075639546),
            ],
            Vertices(new Arc(new(0, 0), 2, 2) { Theta1 = 10, Theta2 = 100.5 }));
    }

    /// <summary>An arc runs counter-clockwise from its first angle to its second, so a first angle past the second sweeps through
    /// 0: from 90 to 0 is three quarters of a turn, not a quarter backwards (matplotlib: <c>Path.arc(90, 0)</c>, 25 vertices).</summary>
    [Fact]
    public void AnArcFromALargerToASmallerAngle_RunsCounterClockwiseThroughZero()
    {
        var arc = new Arc(new(0, 0), 2, 2) { Theta1 = 90, Theta2 = 0 };

        var v = Vertices(arc);

        Assert.Equal(25, v.Length);
        AssertVertices(
            [new(6.123233995736766e-17, 1.0), new(-0.19775764286696548, 1.0), new(-0.3911407623751409, 0.9413378720315526), new(-0.555570233019602, 0.8314696123025453)],
            v[..4]);
        AssertVertices([new(1, 0)], [v[^1]]);
    }

    // ---- Wedge ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void AWedge_IsItsArc_ALineToItsCentre_AndAClose()
    {
        // matplotlib: Wedge((1, 1), 2, 300, 60): codes [1, 4 x 12, 2, 79]; 300 to 60 sweeps through 0.
        var wedge = new Wedge(new(1, 1), 2, 300, 60);

        Assert.Equal(Curves(4) + " L Z", Kinds(wedge));
        AssertVertices(
            [
                new(2.0, -0.7320508075688772),
                new(2.304015784657757, -0.5565272124588261), new(2.5565272124588265, -0.30401578465775625), new(2.7320508075688776, 6.661338147750939e-16),
                new(2.9075744026789287, 0.3040157846577576), new(3.0, 0.6489528097798971), new(3.0, 0.9999999999999996),
                new(3.0, 1.3510471902201018), new(2.9075744026789283, 1.6959842153422433), new(2.732050807568877, 2.0),
                new(2.556527212458826, 2.3040157846577567), new(2.3040157846577562, 2.5565272124588265), new(1.9999999999999996, 2.7320508075688776),
                new(1.0, 1.0),
            ],
            Vertices(wedge));
    }

    /// <summary>A wedge that sweeps a whole turn is the whole disc, drawn from 0 whatever its first angle, with no edge to the
    /// centre: matplotlib moves to the centre instead of drawing a line to it (51 vertices, codes [1, 4 x 48, 1, 79]).</summary>
    [Theory]
    [InlineData(0, 360)]
    [InlineData(45, 405)]
    [InlineData(-90, 270)]
    public void AWedgeOfAWholeTurn_IsTheWholeDisc_WithNoEdgeToTheCentre(double theta1, double theta2)
    {
        var wedge = new Wedge(new(0, 0), 1, theta1, theta2);

        Assert.Equal(Curves(16) + " M Z", Kinds(wedge));
        Assert.Equal(51, MatplotlibVertexCount(wedge.Outline()));
        var v = Vertices(wedge);
        AssertVertices([new(1, 0), new(1, 0), new(0, 0)], [v[0], v[^2], v[^1]]);
    }

    /// <summary>A wedge whose two angles are the same sweeps nothing and encloses nothing: matplotlib's path is a curve of no length
    /// on the circle, a line to the centre and a close, so an edge draws the radius at that angle.</summary>
    [Fact]
    public void AWedgeOfNoSweep_EnclosesNothing_ItsEdgeIsTheRadius()
    {
        // matplotlib: Wedge((0, 0), 1, 90, 90): 6 vertices, codes [1, 4, 4, 4, 2, 79]
        var wedge = new Wedge(new(0, 0), 1, 90, 90);

        Assert.Equal("M C L Z", Kinds(wedge));
        var top = new DataPoint(6.123233995736766e-17, 1.0);
        AssertVertices([top, top, top, top, new(0, 0)], Vertices(wedge));
    }

    [Fact]
    public void AWedgeWithAWidth_IsAnAnnulusSector_ItsInnerRingRunBackwards()
    {
        // matplotlib: Wedge((0, 0), 1, 0, 90, width=0.25): codes [1, 4 x 6, 2, 4 x 6, 79]; inner radius 1 - 0.25.
        var wedge = new Wedge(new(0, 0), 1, 0, 90) { Width = 0.25 };

        Assert.Equal(Curves(2) + " L C C Z", Kinds(wedge));
        AssertVertices(
            [
                new(1.0, 0.0),
                new(1.0, 0.26511477349130247), new(0.8945712353149831, 0.5196423270581121), new(0.7071067811865476, 0.7071067811865476),
                new(0.5196423270581121, 0.8945712353149831), new(0.2651147734913025, 1.0), new(6.123233995736766e-17, 1.0),
                new(4.592425496802574e-17, 0.75),
                new(0.1988360801184769, 0.75), new(0.38973174529358406, 0.6709284264862373), new(0.5303300858899107, 0.5303300858899107),
                new(0.6709284264862373, 0.38973174529358406), new(0.75, 0.19883608011847687), new(0.75, 0.0),
            ],
            Vertices(wedge));
    }

    /// <summary>A whole ring with a width is two closed rings, the inner one run backwards so a fill leaves the hole open, and no
    /// edge between them: matplotlib moves to the inner ring (99 vertices, codes [1, 4 x 48, 1, 4 x 48, 79]).</summary>
    [Fact]
    public void AWholeAnnulus_IsTwoRings_WithNoEdgeBetweenThem()
    {
        var wedge = new Wedge(new(0, 0), 2, 0, 360) { Width = 0.5 };

        Assert.Equal(Curves(16) + " " + Curves(16) + " Z", Kinds(wedge));
        Assert.Equal(99, MatplotlibVertexCount(wedge.Outline()));
        var v = Vertices(wedge);
        AssertVertices([new(2, 0), new(1.5, -3.6739403974420594e-16), new(1.5, 0)], [v[0], v[49], v[^1]]);
    }

    // ---- Arc -----------------------------------------------------------------------------------------------------------

    /// <summary>An arc is open: matplotlib's <c>Arc</c> path is <c>Path.arc</c> alone, with no close. On an ellipse that is not a
    /// circle its angles name the direction from the centre, so they are stretched to the angles of the unit circle that land
    /// there before the arc is built (matplotlib's <c>_theta_stretch</c>), then the ellipse is turned through its own angle.</summary>
    [Fact]
    public void AnArc_IsMatplotlibsOpenArc_OnItsTurnedEllipse()
    {
        // matplotlib: Arc((1, 2), 4, 2, angle=30, theta1=20, theta2=200): codes [1, 4 x 12]
        var arc = new Arc(new(1, 2), 4, 2) { Angle = 30, Theta1 = 20, Theta2 = 200 };

        Assert.Equal(Curves(4), Kinds(arc));
        AssertVertices(
            [
                new(2.1060647029881987, 3.318156583427833),
                new(1.728648809395505, 3.347753678484438), new(1.2496937869697415, 3.237197208765839), new(0.7754726335063546, 3.011018011015685),
                new(0.30125148004296765, 2.7848388132655306), new(-0.13036025660798511, 2.4611015835955588), new(-0.42359434980742616, 2.11163879955402),
                new(-0.7168284430068674, 1.7621760155124808), new(-0.8482645922322865, 1.414898904386712), new(-0.7887390703217247, 1.1468630934006616),
                new(-0.7292135484111633, 0.8788272824146113), new(-0.4834805965808926, 0.7114405116287721), new(-0.1060647029881987, 0.6818434165721672),
            ],
            Vertices(arc));
    }

    /// <summary>On an ellipse that is not turned, the arc starts and ends on the rays from the centre at its two angles.</summary>
    [Theory]
    [InlineData(300, 60)]
    [InlineData(20, 135)]
    public void AnArcOnAnEllipse_StartsAndEndsOnTheRaysAtItsAngles(double theta1, double theta2)
    {
        var v = Vertices(new Arc(new(0, 0), 4, 2) { Theta1 = theta1, Theta2 = theta2 });

        double Direction(DataPoint p) => (Math.Atan2(p.DataY, p.DataX) * 180 / Math.PI + 360) % 360;
        Assert.Equal(theta1 % 360, Direction(v[0]), 1e-9);
        Assert.Equal(theta2 % 360, Direction(v[^1]), 1e-9);
    }

    /// <summary>An arc whose two angles are the same sweeps nothing, on an ellipse too: matplotlib stretches both angles alike and
    /// draws one curve of no length at the point in that direction (<c>Arc((0, 0), 4, 2, theta1=30, theta2=30)</c>: 4 vertices at
    /// (1.3093073414159548, 0.7559289460184542), codes [1, 4, 4, 4]).</summary>
    [Fact]
    public void AnArcOfNoSweepOnAnEllipse_IsOneCurveOfNoLength_InTheDirectionOfItsAngle()
    {
        var arc = new Arc(new(0, 0), 4, 2) { Theta1 = 30, Theta2 = 30 };

        Assert.Equal("M C", Kinds(arc));
        var point = new DataPoint(1.3093073414159548, 0.7559289460184542);
        AssertVertices([point, point, point, point], Vertices(arc));
    }

    /// <summary>An arc at rest is the whole ellipse, open (matplotlib's theta 0 to 360: 49 vertices, no close); a whole turn between
    /// two angles that name the same direction is not stretched, so it starts where its first angle points.</summary>
    [Fact]
    public void AnArcOfAWholeTurn_IsTheWholeEllipse_Open()
    {
        var atRest = new Arc(new(0, 0), 2, 2);
        var wholeFromBelow = new Arc(new(0, 0), 4, 2) { Theta1 = -90, Theta2 = 270 };

        Assert.Equal(Curves(16), Kinds(atRest));
        Assert.Equal(Curves(16), Kinds(wholeFromBelow));
        AssertVertices([new(1, 0)], [Vertices(atRest)[0]]);
        AssertVertices([new(0, -1), new(0, -1)], [Vertices(wholeFromBelow)[0], Vertices(wholeFromBelow)[^1]]);
    }

    // ---- what cannot be drawn ------------------------------------------------------------------------------------------

    private static readonly Dictionary<string, Func<Patch>> CurvedPatchesThatCannotBeDrawn = new()
    {
        ["circle of radius 0"] = () => new Circle(new(0, 0), 0),
        ["circle of negative radius"] = () => new Circle(new(0, 0), -1),
        ["circle centre NaN"] = () => new Circle(new(double.NaN, 0), 1),
        ["circle radius infinite"] = () => new Circle(new(0, 0), double.PositiveInfinity),
        ["ellipse of width 0"] = () => new Ellipse(new(0, 0), 0, 1),
        ["ellipse of negative height"] = () => new Ellipse(new(0, 0), 1, -1),
        ["ellipse angle NaN"] = () => new Ellipse(new(0, 0), 2, 1) { Angle = double.NaN },
        ["ellipse centre infinite"] = () => new Ellipse(new(0, double.NegativeInfinity), 2, 1),
        ["wedge of radius 0"] = () => new Wedge(new(0, 0), 0, 0, 90),
        ["wedge of negative radius"] = () => new Wedge(new(0, 0), -2, 0, 90),
        ["wedge first angle NaN"] = () => new Wedge(new(0, 0), 1, double.NaN, 90),
        ["wedge second angle infinite"] = () => new Wedge(new(0, 0), 1, 0, double.PositiveInfinity),
        ["wedge first angle infinite"] = () => new Wedge(new(0, 0), 1, double.NegativeInfinity, 90),
        ["wedge width NaN"] = () => new Wedge(new(0, 0), 1, 0, 90) { Width = double.NaN },
        ["arc of height 0"] = () => new Arc(new(0, 0), 2, 0),
        ["arc of negative width"] = () => new Arc(new(0, 0), -2, 1),
        ["arc angle infinite"] = () => new Arc(new(0, 0), 2, 1) { Angle = double.PositiveInfinity },
        ["arc first angle NaN"] = () => new Arc(new(0, 0), 2, 1) { Theta1 = double.NaN },
        ["arc centre NaN"] = () => new Arc(new(0, double.NaN), 2, 2),
    };

    public static TheoryData<string> CasesThatCannotBeDrawn { get; } = new(CurvedPatchesThatCannotBeDrawn.Keys);

    /// <summary>A shape with no size, or with a number that is not finite, has no outline and draws nothing: the rule every patch
    /// follows, so the drawing skips it and the range takes nothing from it.</summary>
    [Theory]
    [MemberData(nameof(CasesThatCannotBeDrawn))]
    public void ACurvedPatchWithNoSizeOrANumberThatIsNotFinite_HasNoOutline(string patch)
    {
        Assert.Empty(CurvedPatchesThatCannotBeDrawn[patch]().Outline());
    }

    /// <summary>A curved outline is moves, cubic curves, lines and closes in data units, never an arc in pixels, which the drawing
    /// could not map through the axes' transform.</summary>
    [Fact]
    public void NoCurvedPatch_UsesAnArcInPixels()
    {
        Patch[] patches =
        [
            new Circle(new(1, 1), 1), new Ellipse(new(1, 1), 3, 1) { Angle = 10 }, new Wedge(new(1, 1), 2, 10, 300) { Width = 1 },
            new Arc(new(1, 1), 3, 2) { Theta1 = 45, Theta2 = 10 },
        ];

        Assert.All(patches, patch => Assert.DoesNotContain(patch.Outline(), segment => segment is ArcSegment));
    }
}
